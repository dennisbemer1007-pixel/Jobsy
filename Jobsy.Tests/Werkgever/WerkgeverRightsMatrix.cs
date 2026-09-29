using Jobsy.Core.Authorization;
using Jobsy.Web.Navigation;

namespace Jobsy.Tests.Werkgever;

/// <summary>
/// Page route × role allow/deny and mutating endpoint expectations (§R).
/// </summary>
public static class WerkgeverRightsMatrix
{
    public sealed record PageRow(string Route, string AuthorizeRoles, bool Bm, bool Rm, bool Vm);

    public sealed record ApiRow(string Path, bool Bm, bool Rm, bool Vm, int AnonymousStatus = 401, int CandidateStatus = 403);

    public static readonly IReadOnlyList<PageRow> Pages =
    [
        new("/werkgever", "BranchManager,RegionalManager,EnterpriseManager,Intermediary", true, true, true),
        new("/werkgever/te-doen", "BranchManager,RegionalManager,EnterpriseManager", true, true, true),
        new("/werkgever/vacatures", "BranchManager,RegionalManager,EnterpriseManager,Intermediary,Admin", true, true, true),
        new("/werkgever/vacatures/nieuw", "BranchManager,EnterpriseManager,Intermediary", true, false, true),
        new("/werkgever/sollicitaties", "BranchManager,RegionalManager,EnterpriseManager,Intermediary,Admin", true, true, true),
        new("/werkgever/sollicitaties/{ApplicationId:guid}", "BranchManager,RegionalManager,EnterpriseManager,Intermediary,Admin", true, true, true),
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

    public static readonly IReadOnlyList<ApiRow> Apis =
    [
        new("api/werkgever/dashboard", true, true, true),
        new("api/werkgever/te-doen", true, true, true),
        new("api/vacancies/manage", true, true, true),
        // Mutating vacancy lifecycle: RM forbidden; VM scoped; BM allowed (matrix exercised in VacanciesManageApiTests).
        new("api/vacancies/{id}/approve-publish", true, false, false),
        new("api/vacancies/{id}/highlight", true, false, true),
        new("api/vacancies/{id}/pushbom", true, false, true),
        new("api/vacancies/{id}/extend", true, false, true),
        new("api/vacancies/{id}/inactive", true, false, true),
        // Sollicitaties mutating (04): RM forbidden; VM scoped; BM allowed.
        new("api/applications/{id}/react", true, false, true),
        new("api/applications/{id}/contact", true, false, true),
        new("api/applications/vacancies/{vacancyId}/fulfill/{applicationId}", true, false, true),
        // Organisatie / team (05)
        new("api/company-users/invite", true, false, false),
        new("api/company-users/{id}", true, false, false),
        new("api/regions", true, true, false),
        new("api/companies/from-kvk", true, false, false),
    ];

    public static bool RoleAllowed(PageRow row, EmployerRole role) => role switch
    {
        EmployerRole.Bedrijfsmanager => row.Bm,
        EmployerRole.Regiomanager => row.Rm,
        EmployerRole.Vestigingsmanager => row.Vm,
        _ => false
    };
}
