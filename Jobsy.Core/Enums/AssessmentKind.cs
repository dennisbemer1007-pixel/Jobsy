namespace Jobsy.Core.Enums;

/// <summary>
/// Separated assessment engines: competence (Big Five/IPIP), career (RIASEC),
/// culture &amp; personality (culture dims + IPIP-style work facets).
/// </summary>
public enum AssessmentKind
{
    Competence = 0,
    Career = 1,
    Culture = 2
}

public static class AssessmentKindLabels
{
    public const string Competence = "competence";
    public const string Career = "career";
    public const string Culture = "culture";

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

        if (string.Equals(value, Culture, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "cultuur", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "cultuurscan", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "personality", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "gedrag", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "disc", StringComparison.OrdinalIgnoreCase))
        {
            // Legacy "disc" / "gedrag" slugs redirect to the culture & personality scan.
            kind = AssessmentKind.Culture;
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
        AssessmentKind.Culture => Culture,
        _ => Competence
    };

    public static string ToDutch(AssessmentKind kind) => kind switch
    {
        AssessmentKind.Career => "Beroepentest",
        AssessmentKind.Culture => "Cultuurscan",
        _ => "Competentietest"
    };
}
