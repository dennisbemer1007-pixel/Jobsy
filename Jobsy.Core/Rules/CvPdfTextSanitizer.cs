using System.Text.RegularExpressions;

namespace Jobsy.Core.Rules;

/// <summary>Removes PDF document-info noise from plain text before CV extraction/grounding.</summary>
public static partial class CvPdfTextSanitizer
{
    public static string StripInfoDictionaryFromRawPdf(string rawPdfLatin1)
    {
        if (string.IsNullOrEmpty(rawPdfLatin1))
        {
            return string.Empty;
        }

        return InfoDictionaryLiteralRegex().Replace(rawPdfLatin1, " ");
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
}
