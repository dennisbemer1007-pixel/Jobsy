using Jobsy.Core.Authorization;
using Jobsy.Web.Navigation;

namespace Jobsy.Tests.Werkgever;

/// <summary>
/// Page route × role allow/deny and mutating endpoint expectations (§R).
/// </summary>
public static class WerkgeverRightsMatrix
{
    public sealed record PageRow(string Route, string AuthorizeRoles, bool Bm, bool Rm, bool Vm);

    public static readonly IReadOnlyList<PageRow> Pages =
    [
        new("/werkgever", "BranchManager,RegionalManager,EnterpriseManager,Intermediary", true, true, true),
        new("/werkgever/vacatures", "BranchManager,RegionalManager,EnterpriseManager,Intermediary,Admin", true, true, true),
        new("/werkgever/vacatures/nieuw", "BranchManager,EnterpriseManager,Intermediary", true, false, true),
        new("/werkgever/sollicitaties", "BranchManager,RegionalManager,EnterpriseManager,Intermediary,Admin", true, true, true),
        new("/werkgever/talentpool", "BranchManager,RegionalManager,EnterpriseManager,Intermediary", true, true, true),
        new("/werkgever/kandidaatinzichten", "BranchManager,RegionalManager,EnterpriseManager", true, true, true),
        new("/werkgever/organisatie/vestigingen", "RegionalManager,EnterpriseManager,Admin", true, true, false),
        new("/werkgever/organisatie/team", "EnterpriseManager,Admin", true, false, false),
        new("/werkgever/organisatie/profiel", "BranchManager,EnterpriseManager,Admin,Intermediary", true, false, true),
        new("/werkgever/organisatie/salaristabellen", "BranchManager,EnterpriseManager,Admin", true, false, true),
        new("/werkgever/tokens", "BranchManager,RegionalManager,EnterpriseManager,Intermediary", true, true, true),
        new("/werkgever/koppelingen", "EnterpriseManager,Admin", true, false, false),
        new("/werkgever/overnames", "BranchManager,EnterpriseManager,Admin", true, false, true),
        new("/werkgever/partner", "EnterpriseManager,Intermediary", true, false, false),
    ];

    public static bool RoleAllowed(PageRow row, EmployerRole role) => role switch
    {
        EmployerRole.Bedrijfsmanager => row.Bm,
        EmployerRole.Regiomanager => row.Rm,
        EmployerRole.Vestigingsmanager => row.Vm,
        _ => false
    };
}
