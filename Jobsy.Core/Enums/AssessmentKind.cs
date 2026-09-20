namespace Jobsy.Core.Enums;

/// <summary>
/// Separated assessment engines: competence (who you are / Big Five) vs career (what you want / RIASEC).
/// </summary>
public enum AssessmentKind
{
    Competence = 0,
    Career = 1
}

public static class AssessmentKindLabels
{
    public const string Competence = "competence";
    public const string Career = "career";

    public static bool TryParse(string? value, out AssessmentKind kind)
    {
        if (string.Equals(value, Competence, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "competentie", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "competency", StringComparison.OrdinalIgnoreCase))
        {
            kind = AssessmentKind.Competence;
            return true;
        }

        if (string.Equals(value, Career, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "beroep", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "beroepen", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "riasec", StringComparison.OrdinalIgnoreCase))
        {
            kind = AssessmentKind.Career;
            return true;
        }

        kind = default;
        return false;
    }

    public static AssessmentKind ParseOrDefault(string? value, AssessmentKind fallback = AssessmentKind.Competence)
        => TryParse(value, out var kind) ? kind : fallback;

    public static string ToSlug(AssessmentKind kind) => kind switch
    {
        AssessmentKind.Career => Career,
        _ => Competence
    };

    public static string ToDutch(AssessmentKind kind) => kind switch
    {
        AssessmentKind.Career => "Beroepentest",
        _ => "Competentietest"
    };
}
