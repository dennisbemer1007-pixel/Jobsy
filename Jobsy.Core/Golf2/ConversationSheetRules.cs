using System.Text.RegularExpressions;

namespace Jobsy.Core.Golf2;

/// <summary>Privacy rules for the Westland gespreksblad (no scores, tests or birth dates).</summary>
public static class ConversationSheetRules
{
    public const string CustomTextLabel = "Eigen toevoeging";

    public static readonly string[] ForbiddenFieldKeys =
    [
        "scores",
        "score",
        "testResults",
        "testResult",
        "birthDate",
        "birthdate",
        "geboortedatum",
        "dateOfBirth"
    ];

    private static readonly Regex ScoreLike = new(
        @"\b\d{1,3}\s*%|\bscore\b|\bpercent\b|\bresultaat\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex TestResultLike = new(
        @"\b(testresultaat|test\s*result|holland\s*code|riasec|competentie\s*score|waarden\s*test|culture\s*test)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex BirthDateLike = new(
        @"\b(geboortedatum|geboorte\s*datum|date\s*of\s*birth|birth\s*date)\b|\b\d{1,2}[-/]\d{1,2}[-/]\d{2,4}\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static IReadOnlyList<string> ValidateFieldKeys(IEnumerable<string> keys)
    {
        var errors = new List<string>();
        foreach (var key in keys)
        {
            if (IsForbiddenFieldKey(key))
            {
                errors.Add($"Veld '{key}' is niet toegestaan op het gespreksblad.");
            }
        }

        return errors;
    }

    public static bool IsForbiddenFieldKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        return ForbiddenFieldKeys.Any(f =>
            string.Equals(f, key.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public static IReadOnlyList<string> ValidateFreeText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var errors = new List<string>();
        if (ScoreLike.IsMatch(text))
        {
            errors.Add("Scores horen niet op het gespreksblad.");
        }

        if (TestResultLike.IsMatch(text))
        {
            errors.Add("Testresultaten horen niet op het gespreksblad.");
        }

        if (BirthDateLike.IsMatch(text))
        {
            errors.Add("Geboortedatum hoort niet op het gespreksblad.");
        }

        return errors;
    }
}
