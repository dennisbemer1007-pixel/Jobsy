namespace Jobsy.Core.Rules;

/// <summary>Broad richting suggestions for education (HAVO/VWO/VMBO/MBO/HBO/WO). Free text is always allowed.</summary>
public static class EducationDirectionCatalog
{
    private static readonly string[] AllRaw =
    [
        .. OnboardingWizardCatalog.EducationDirectionHavoVwo,
        .. OnboardingWizardCatalog.EducationDirectionVmboMbo,
        "Administratie",
        "Agro & natuur",
        "Bouw",
        "Creatief & media",
        "Engineering",
        "Financiën",
        "Gezondheidszorg",
        "Handel & retail",
        "Horeca",
        "Human resources",
        "ICT & software",
        "Juridisch",
        "Logistiek & transport",
        "Marketing & communicatie",
        "Onderwijs",
        "Productie & industrie",
        "Sales",
        "Veiligheid & beveiliging",
        "Zorg & welzijn"
    ];

    public static readonly string[] All = AllRaw
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public static bool SupportsDirection(string? educationLevel)
    {
        var level = (educationLevel ?? "").Trim();
        if (level.Length == 0)
        {
            return false;
        }

        if (string.Equals(level, EducationLevelLabels.None, StringComparison.OrdinalIgnoreCase)
            || string.Equals(level, "Basisschool", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    public static bool ProfileSupportsDirection(IEnumerable<string>? selectedLevels)
    {
        if (selectedLevels is null)
        {
            return false;
        }

        return selectedLevels.Any(level => SupportsDirection(level));
    }

    public static IReadOnlyList<string> Filter(string? query, int maxResults = 8)
    {
        var q = (query ?? "").Trim();
        if (q.Length == 0)
        {
            return All.Take(maxResults).ToList();
        }

        return All
            .Where(item => item.Contains(q, StringComparison.OrdinalIgnoreCase))
            .Take(maxResults)
            .ToList();
    }
}
