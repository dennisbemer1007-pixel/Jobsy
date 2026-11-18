using System.Text.RegularExpressions;

namespace Jobsy.Core.Rules;

/// <summary>Removes PDF document-info noise from plain text before CV extraction/grounding.</summary>
public static partial class CvPdfTextSanitizer
{
    public static string StripInfoDictionaryFromRawPdf(string rawPdfLatin1)
        => StripMetadataObjectsFromRawPdf(rawPdfLatin1);

    public static string StripMetadataObjectsFromRawPdf(string rawPdfLatin1)
    {
        if (string.IsNullOrEmpty(rawPdfLatin1))
        {
            return string.Empty;
        }

        var text = InfoDictionaryLiteralRegex().Replace(rawPdfLatin1, " ");
        text = InfoDictionaryNestedRegex().Replace(text, " ");
        text = MetadataStreamRegex().Replace(text, " ");
        return text;
    }

    public static string CleanExtractedBodyText(string extracted)
    {
        if (string.IsNullOrWhiteSpace(extracted))
        {
            return string.Empty;
        }

        var text = extracted;
        foreach (var phrase in MetadataPhrases)
        {
            text = text.Replace(phrase, " ", StringComparison.OrdinalIgnoreCase);
        }

        return CollapseSpaces(text);
    }

    private static readonly string[] MetadataPhrases =
    [
        "ReportLab PDF Library",
        "ReportLab",
        "Adobe PDF Library",
        "Microsoft Word",
        "Skia/PDF",
        "PDFsharp",
    ];

    private static string CollapseSpaces(string value)
        => Regex.Replace(value, @"\s+", " ").Trim();

    [GeneratedRegex(
        @"/(?:Producer|Creator|Title|Author|Subject|Keywords|CreationDate|ModDate)\s*\((?:\\.|[^\\)])*\)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex InfoDictionaryLiteralRegex();

    [GeneratedRegex(
        @"/(?:Producer|Creator|Title|Author|Subject|Keywords|CreationDate|ModDate)\s*\(((?:\\.|[^\\)])|\([^\\)]*\))*\)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex InfoDictionaryNestedRegex();

    [GeneratedRegex(
        @"/Type\s*/Metadata\b.*?(?:stream\s*[\r\n]+.*?[\r\n]+endstream|<<[^>]*>>)",
        RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MetadataStreamRegex();
}
