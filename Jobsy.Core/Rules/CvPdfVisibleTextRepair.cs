using System.Text.RegularExpressions;

namespace Jobsy.Core.Rules;

/// <summary>
/// PdfPig sometimes emits glued tokens (no space between headings and body words). Repair before grounding harvest.
/// </summary>
public static partial class CvPdfVisibleTextRepair
{
    private static readonly string[] SectionHeadings =
    [
        "werkervaring",
        "opleiding",
        "rijbewijs",
        "certificaten",
    ];

    public static string Repair(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var repaired = text;
        foreach (var heading in SectionHeadings)
        {
            repaired = Regex.Replace(
                repaired,
                $"(?<=[\\p{{L}}\\d])(?={heading})",
                " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            repaired = Regex.Replace(
                repaired,
                $"(?<={heading})(?=[\\p{{L}}\\d])",
                " ",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        repaired = LetterCaseBoundaryRegex().Replace(repaired, " ");
        return CollapseSpaces(repaired);
    }

    private static string CollapseSpaces(string value)
        => Regex.Replace(value, @"\s+", " ").Trim();

    [GeneratedRegex(@"(?<=[\p{Ll}])(?=[\p{Lu}])", RegexOptions.CultureInvariant)]
    private static partial Regex LetterCaseBoundaryRegex();
}