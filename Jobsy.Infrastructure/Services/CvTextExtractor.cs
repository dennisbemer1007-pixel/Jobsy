using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;

namespace Jobsy.Infrastructure.Services;

public sealed class CvTextExtractor : ICvTextExtractor
{
    private static readonly Regex PdfLiteral = new(@"\((?:\\.|[^\\)]){2,}\)", RegexOptions.Compiled);
    private static readonly Regex PdfHex = new(@"<[0-9A-Fa-f]{8,}>", RegexOptions.Compiled);

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
            var raw = Encoding.Latin1.GetString(content);
            var sb = new StringBuilder();
            foreach (Match match in PdfLiteral.Matches(raw))
            {
                var inner = match.Value.Trim('(', ')');
                inner = inner
                    .Replace("\\n", " ")
                    .Replace("\\r", " ")
                    .Replace("\\t", " ")
                    .Replace("\\(", "(")
                    .Replace("\\)", ")")
                    .Replace("\\\\", "\\");
                if (inner.Any(char.IsLetter))
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(' ');
                    }

                    sb.Append(inner);
                }

                if (sb.Length > 16_000)
                {
                    break;
                }
            }

            if (sb.Length < 40)
            {
                foreach (Match match in PdfHex.Matches(raw))
                {
                    var hex = match.Value.Trim('<', '>');
                    if (hex.Length % 2 != 0)
                    {
                        continue;
                    }

                    var bytes = new byte[hex.Length / 2];
                    for (var i = 0; i < bytes.Length; i++)
                    {
                        bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
                    }

                    var decoded = Encoding.UTF8.GetString(bytes);
                    if (decoded.Any(char.IsLetter))
                    {
                        if (sb.Length > 0)
                        {
                            sb.Append(' ');
                        }

                        sb.Append(decoded);
                    }

                    if (sb.Length > 16_000)
                    {
                        break;
                    }
                }
            }

            return Truncate(sb.ToString());
        }
        catch
        {
            return string.Empty;
        }
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
