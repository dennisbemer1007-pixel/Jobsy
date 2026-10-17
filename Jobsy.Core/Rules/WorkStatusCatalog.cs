namespace Jobsy.Core.Rules;

/// <summary>
/// Optional multi-select work status for onboarding / passport. Stored in PreferencesJson; not used in match scoring.
/// </summary>
public static class WorkStatusCatalog
{
    public const string Scholier = "scholier";
    public const string Student = "student";
    public const string Stagiair = "stagiair";
    public const string LoondienstFulltime = "loondienst-fulltime";
    public const string LoondienstParttime = "loondienst-parttime";
    public const string Flex = "flex";
    public const string Zzp = "zzp";
    public const string Zoekend = "zoekend";
    public const string Herintreder = "herintreder";
    public const string GepensioneerdWerkt = "gepensioneerd-werkt";
    public const string Ondernemer = "ondernemer";

    public const int MaxSelections = 11;

    public static readonly string[] AllCodes =
    [
        Scholier, Student, Stagiair, LoondienstFulltime, LoondienstParttime,
        Flex, Zzp, Zoekend, Herintreder, GepensioneerdWerkt, Ondernemer
    ];

    private static readonly Dictionary<string, string> CanonicalMap =
        new(StringComparer.OrdinalIgnoreCase);

    static WorkStatusCatalog()
    {
        foreach (var code in AllCodes)
        {
            CanonicalMap[code] = code;
        }
    }

    public static bool IsKnown(string? code)
        => !string.IsNullOrWhiteSpace(code) && CanonicalMap.ContainsKey(code.Trim());

    public static string? Canonical(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        return CanonicalMap.TryGetValue(code.Trim(), out var canonical) ? canonical : null;
    }

    public static string LabelKey(string code) => $"WorkStatus.{Canonical(code) ?? code}";

    /// <summary>Soft context only: candidate may prefer internships, side jobs or entry roles.</summary>
    public static bool PrefersInternshipsOrSideJobs(IReadOnlyCollection<string>? codes)
    {
        if (codes is null || codes.Count == 0)
        {
            return false;
        }

        foreach (var raw in codes)
        {
            var code = Canonical(raw);
            if (code is Scholier or Student or Stagiair)
            {
                return true;
            }
        }

        return false;
    }
}
