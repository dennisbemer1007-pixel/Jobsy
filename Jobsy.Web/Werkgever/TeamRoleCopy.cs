namespace Jobsy.Web.Werkgever;

/// <summary>Shared role-card copy for invite drawer and "Wat mag elke rol?" panel.</summary>
public static class TeamRoleCopy
{
    public const string BedrijfsmanagerKey = "EnterpriseManager";
    public const string RegiomanagerKey = "RegionalManager";
    public const string VestigingsmanagerKey = "BranchManager";

    public static readonly IReadOnlyList<(string Role, string TitleKey, string BodyKey)> Cards =
    [
        (BedrijfsmanagerKey, "WgTeam.Role.Bm.Title", "WgTeam.Role.Bm.Body"),
        (RegiomanagerKey, "WgTeam.Role.Rm.Title", "WgTeam.Role.Rm.Body"),
        (VestigingsmanagerKey, "WgTeam.Role.Vm.Title", "WgTeam.Role.Vm.Body"),
    ];

    public static string ScopeLabel(
        string role,
        string? companyName,
        string? regionName,
        Func<string, string> t)
    {
        if (string.Equals(role, BedrijfsmanagerKey, StringComparison.Ordinal))
        {
            return t("WgTeam.Scope.AllBranches");
        }

        if (string.Equals(role, RegiomanagerKey, StringComparison.Ordinal))
        {
            return string.IsNullOrWhiteSpace(regionName)
                ? t("WgTeam.Scope.RegionUnknown")
                : string.Format(t("WgTeam.Scope.Region"), regionName);
        }

        return string.IsNullOrWhiteSpace(companyName) ? "—" : companyName;
    }

    public static string RoleLabelKey(string role) => role switch
    {
        BedrijfsmanagerKey => "WgShell.Role.BM",
        RegiomanagerKey => "WgShell.Role.RM",
        VestigingsmanagerKey => "WgShell.Role.VM",
        "Intermediary" => "WgShell.Role.IM",
        _ => "WgShell.Role.BM"
    };

    public static string RolePillClass(string role) => role switch
    {
        BedrijfsmanagerKey => "status-pill--info",
        RegiomanagerKey => "status-pill--line",
        VestigingsmanagerKey => "status-pill--neutral",
        _ => "status-pill--neutral"
    };

    /// <summary>Derive FullName for invite API when the drawer only collects e-mail.</summary>
    public static string FullNameFromEmail(string email)
    {
        var local = (email ?? "").Split('@')[0].Trim();
        if (string.IsNullOrWhiteSpace(local))
        {
            return "Uitgenodigd";
        }

        var parts = local.Replace('.', ' ').Replace('_', ' ').Replace('-', ' ')
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', parts.Select(p =>
            p.Length == 0 ? p : char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant()));
    }
}
