using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using UglyToad.PdfPig;

namespace Jobsy.Infrastructure.Services;

public sealed class CvTextExtractor : ICvTextExtractor
{
    private static readonly Regex ContentStreamRegex = new(
        @"stream\s*(\r?\n)(.*?)(\r?\n)endstream",
        RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex StreamTextLiteralRegex = new(
        @"(?<!\$)\((?:\\.|[^\\()]){1,2000}\)",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public string Extract(byte[] content, string contentType, string fileName)
    {
        if (content.Length == 0)
        {
            return string.Empty;
        }

        var type = (contentType ?? string.Empty).Trim().ToLowerInvariant();
        var ext = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
        if (type == CandidateCvFileRules.PdfContentType || ext == ".pdf")
        {
            return ExtractPdf(content);
        }

        if (type == CandidateCvFileRules.DocxContentType || ext == ".docx")
        {
            return ExtractDocx(content);
        }

        return string.Empty;
    }

    private static string ExtractPdf(byte[] content)
    {
        try
        {
            var visible = TryExtractVisiblePageText(content);
            if (visible.Length < 40)
            {
                visible = ExtractFromContentStreamsOnly(content);
            }

            visible = CvPdfVisibleTextRepair.Repair(visible);
            return Truncate(CvPdfTextSanitizer.CleanExtractedBodyText(visible));
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>Visible page text only (no document Info dict or XMP metadata streams).</summary>
    private static string TryExtractVisiblePageText(byte[] content)
    {
        using var document = PdfDocument.Open(content);
        var sb = new StringBuilder();
        foreach (var page in document.GetPages())
        {
            var pageText = page.Text;
            if (string.IsNullOrWhiteSpace(pageText))
            {
                continue;
            }

            if (sb.Length > 0)
            {
                sb.Append(' ');
            }

            sb.Append(pageText);
            if (sb.Length > 16_000)
            {
                break;
            }
        }

        return sb.ToString();
    }

    private static string ExtractFromContentStreamsOnly(byte[] content)
    {
        var raw = Encoding.Latin1.GetString(content);
        raw = CvPdfTextSanitizer.StripMetadataObjectsFromRawPdf(raw);
        var sb = new StringBuilder();
        foreach (Match stream in ContentStreamRegex.Matches(raw))
        {
            var body = stream.Groups[2].Value;
            foreach (Match match in StreamTextLiteralRegex.Matches(body))
            {
                AppendDecodedLiteral(sb, match.Value);
                if (sb.Length > 16_000)
                {
                    return sb.ToString();
                }
            }
        }

        return sb.ToString();
    }

    private static void AppendDecodedLiteral(StringBuilder sb, string wrapped)
    {
        var inner = wrapped.Trim('(', ')');
        inner = inner
            .Replace("\\n", " ", StringComparison.Ordinal)
            .Replace("\\r", " ", StringComparison.Ordinal)
            .Replace("\\t", " ", StringComparison.Ordinal)
            .Replace("\\(", "(", StringComparison.Ordinal)
            .Replace("\\)", ")", StringComparison.Ordinal)
            .Replace("\\\\", "\\", StringComparison.Ordinal);
        if (!inner.Any(char.IsLetter))
        {
            return;
        }

        if (CvPdfMetadataGuard.IsPdfMetadataNoise(inner))
        {
            return;
        }

        if (sb.Length > 0)
        {
            sb.Append(' ');
        }

        sb.Append(inner);
    }

    private static string ExtractDocx(byte[] content)
    {
        try
        {
            using var zip = new System.IO.Compression.ZipArchive(new MemoryStream(content), System.IO.Compression.ZipArchiveMode.Read, leaveOpen: false);
            var entry = zip.GetEntry("word/document.xml");
            if (entry is null)
            {
                return string.Empty;
            }

            using var stream = entry.Open();
            var xml = XDocument.Load(stream);
            var texts = xml.Descendants().Where(e => e.Name.LocalName == "t").Select(e => e.Value);
            return Truncate(string.Join(" ", texts));
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string Truncate(string text)
    {
        var normalized = Regex.Replace(text, @"\s+", " ").Trim();
        return normalized.Length > 12_000 ? normalized[..12_000] : normalized;
    }
}