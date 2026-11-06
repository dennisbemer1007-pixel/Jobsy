namespace Jobsy.Core.Rules;

/// <summary>
/// Shareable work-preference and contract codes for the passport (step 02).
/// Private dislikes stay in <c>DiscoveryCatalogs</c> and are never mapped here.
/// </summary>
public static class WorkPreferenceCatalogs
{
    public const int MaxContractPreferences = 3;
    public const string NoContractPreference = "geen-voorkeur";

    public static readonly string[] IndoorCodes = ["prefer", "ok", "rather-not"];
    public static readonly string[] OutdoorCodes = ["prefer", "ok", "ok-not-frost", "rather-not"];
    public static readonly string[] PhysicalWorkCodes = ["light", "standing", "lifting-15", "lifting-25"];
    public static readonly string[] PaceCodes = ["norm-ok", "calm", "piek"];
    public static readonly string[] VacancyPaceCodes = ["calm", "norm-ok", "piek"];
    public static readonly string[] IndoorOutdoorCodes = ["prefer-indoor", "mix", "prefer-outdoor"];
    public static readonly string[] CustomerContactCodes = ["much", "some", "none"];
    public static readonly string[] TeamModeCodes = ["team", "mixed", "solo"];
    public static readonly string[] StructureCodes = ["fixed", "mixed", "free"];
    public static readonly string[] ShiftsPreferenceCodes = ["day", "evening", "night", "flex"];
    public static readonly string[] ContractCodes = ["uitzend", "vast", "tijdelijk", "seizoen", "oproep", NoContractPreference];

    public static string? CanonicalIndoor(string? code) => Find(IndoorCodes, code);
    public static string? CanonicalOutdoor(string? code) => Find(OutdoorCodes, code);
    public static string? CanonicalPhysicalWork(string? code) => Find(PhysicalWorkCodes, code);
    public static string? CanonicalPace(string? code) => Find(PaceCodes, code);
    public static string? CanonicalVacancyPace(string? code) => Find(VacancyPaceCodes, code);
    public static string? CanonicalIndoorOutdoor(string? code) => Find(IndoorOutdoorCodes, code);
    public static string? CanonicalCustomerContact(string? code) => Find(CustomerContactCodes, code);
    public static string? CanonicalTeamMode(string? code) => Find(TeamModeCodes, code);
    public static string? CanonicalStructure(string? code) => Find(StructureCodes, code);
    public static string? CanonicalShiftsPreference(string? code) => Find(ShiftsPreferenceCodes, code);
    public static string? CanonicalContract(string? code) => Find(ContractCodes, code);

    public static bool IsKnownIndoor(string? code) => CanonicalIndoor(code) is not null;
    public static bool IsKnownOutdoor(string? code) => CanonicalOutdoor(code) is not null;
    public static bool IsKnownPhysicalWork(string? code) => CanonicalPhysicalWork(code) is not null;
    public static bool IsKnownPace(string? code) => CanonicalPace(code) is not null;
    public static bool IsKnownContract(string? code) => CanonicalContract(code) is not null;

    public static string IndoorLabelKey(string code) => $"WorkPref.Indoor.{CanonicalIndoor(code) ?? code}";
    public static string OutdoorLabelKey(string code) => $"WorkPref.Outdoor.{CanonicalOutdoor(code) ?? code}";
    public static string PhysicalLabelKey(string code) => $"WorkPref.Physical.{CanonicalPhysicalWork(code) ?? code}";
    public static string PaceLabelKey(string code) => $"WorkPref.Pace.{CanonicalPace(code) ?? code}";
    public static string TeamModeLabelKey(string code) => $"WorkPref.TeamMode.{CanonicalTeamMode(code) ?? code}";
    public static string StructureLabelKey(string code) => $"WorkPref.Structure.{CanonicalStructure(code) ?? code}";
    public static string CustomerContactLabelKey(string code) => $"WorkPref.Customer.{CanonicalCustomerContact(code) ?? code}";
    public static string ShiftsLabelKey(string code) => $"WorkPref.Shifts.{CanonicalShiftsPreference(code) ?? code}";
    public static string VacancyPaceLabelKey(string code) => $"WorkPref.VacancyPace.{CanonicalVacancyPace(code) ?? code}";
    public static string IndoorOutdoorLabelKey(string code) => $"WorkPref.Place.{CanonicalIndoorOutdoor(code) ?? code}";
    public static string ContractLabelKey(string code) => $"Contract.{CanonicalContract(code) ?? code}";

    /// <summary>
    /// Drops unknown codes. <c>geen-voorkeur</c> excludes every other code. At most 3.
    /// </summary>
    public static IReadOnlyList<string> NormalizeContracts(IEnumerable<string>? codes)
    {
        var canonical = new List<string>();
        foreach (var raw in codes ?? [])
        {
            var code = CanonicalContract(raw);
            if (code is null || canonical.Contains(code, StringComparer.Ordinal))
            {
                continue;
            }

            canonical.Add(code);
        }

        if (canonical.Contains(NoContractPreference, StringComparer.Ordinal))
        {
            return [NoContractPreference];
        }

        return canonical.Take(MaxContractPreferences).ToArray();
    }

    private static string? Find(IReadOnlyList<string> catalog, string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var trimmed = code.Trim();
        return catalog.FirstOrDefault(c => string.Equals(c, trimmed, StringComparison.OrdinalIgnoreCase));
    }
}
