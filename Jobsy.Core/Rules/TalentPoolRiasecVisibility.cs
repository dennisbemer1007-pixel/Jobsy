namespace Jobsy.Core.Rules;

/// <summary>
/// Employer talent-pool RIASEC labels and the tag filter share one switch:
/// <c>TalentPool:ShowRiasecTagFilter</c> (default off). Competency match tags stay visible.
/// </summary>
public static class TalentPoolRiasecVisibility
{
    public const string ConfigKey = "TalentPool:ShowRiasecTagFilter";

    /// <summary>Everyday Dutch names for the six Holland types. Employers must not see these when the switch is off.</summary>
    public static readonly string[] DutchTypeLabels =
    [
        "Aanpakken met je handen",
        "Uitzoeken hoe het zit",
        "Iets moois of nieuws maken",
        "Mensen helpen",
        "Aanjagen en verkopen",
        "Netjes organiseren"
    ];

    public static bool IsRiasecCode(string? tag)
        => !string.IsNullOrWhiteSpace(tag)
           && CareerTestCatalog.HollandLetter.ContainsKey(tag.Trim());

    /// <summary>
    /// True for a career-test token: English type name, Dutch type label, or a Holland code (R, S, SEC).
    /// Competency labels such as Samenwerken are not career-test output.
    /// </summary>
    public static bool IsCareerTestOutput(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return false;
        }

        var value = tag.Trim();
        if (IsRiasecCode(value))
        {
            return true;
        }

        if (DutchTypeLabels.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        if (value.Length is >= 1 and <= 3
            && value.All(c => "RIASEC".Contains(char.ToUpperInvariant(c))))
        {
            return true;
        }

        return false;
    }

    public static bool ContainsCareerTestName(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        foreach (var name in CareerTestCatalog.RiasecCodes)
        {
            if (text.Contains(name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        foreach (var name in DutchTypeLabels)
        {
            if (text.Contains(name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
