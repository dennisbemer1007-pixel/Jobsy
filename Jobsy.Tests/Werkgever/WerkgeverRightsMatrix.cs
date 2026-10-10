using Jobsy.Core.Authorization;
using Jobsy.Web.Navigation;

namespace Jobsy.Tests.Werkgever;

/// <summary>
/// Page route × role allow/deny and mutating endpoint expectations (§R).
/// Completeness is guarded by <see cref="WerkgeverRightsMatrixCompletenessTests"/>.
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
        new("/werkgever/uren", "BranchManager,RegionalManager,EnterpriseManager,Intermediary,Admin", true, true, true),
        new("/werkgever/talentpool", "BranchManager,RegionalManager,EnterpriseManager,Intermediary", true, true, true),
        new("/werkgever/kandidaatinzichten", "BranchManager,RegionalManager,EnterpriseManager", true, true, true),
        new("/werkgever/organisatie/vestigingen", "RegionalManager,EnterpriseManager,Admin", true, true, false),
        new("/werkgever/organisatie/team", "EnterpriseManager,Admin", true, false, false),
        new("/werkgever/organisatie/profiel", "BranchManager,EnterpriseManager,Admin,Intermediary", true, false, true),
        new("/werkgever/organisatie/salaristabellen", "BranchManager,EnterpriseManager,Admin", true, false, true),
        new("/werkgever/organisatie/salaristabellen/{TableId:guid}", "BranchManager,EnterpriseManager,Admin", true, false, true),
        new("/werkgever/tokens", "BranchManager,RegionalManager,EnterpriseManager,Intermediary", true, true, true),
        new("/werkgever/tokens/verbruik", "RegionalManager,EnterpriseManager,Intermediary", true, true, false),
        new("/werkgever/tokens/mutaties", "BranchManager,RegionalManager,EnterpriseManager,Intermediary", true, true, true),
        new("/werkgever/tokens/facturen", "EnterpriseManager,Intermediary,Admin", true, false, false),
        new("/werkgever/koppelingen", "EnterpriseManager,Admin", true, false, false),
        new("/werkgever/overnames", "BranchManager,EnterpriseManager,Admin", true, false, true),
        new("/werkgever/wervingsmateriaal", "BranchManager,RegionalManager,EnterpriseManager,Admin", true, true, true),
        new("/werkgever/partner", "EnterpriseManager,Intermediary", true, false, false),
        new("/werkgever/partner/uitbetalen", "EnterpriseManager,Intermediary", true, false, false),
    ];

    public static readonly IReadOnlyList<ApiRow> Apis =
    [
        new("api/werkgever/dashboard", true, true, true),
        new("api/werkgever/te-doen", true, true, true),
        new("api/werkgever/tokens/summary", true, true, true),
        new("api/vacancies/manage", true, true, true),
        // Vacancies mutate
        new("api/vacancies", true, false, true),
        new("api/vacancies/{id}", true, false, true),
        new("api/vacancies/publish", true, false, true),
        new("api/vacancies/{id}/approve-publish", true, false, false),
        new("api/vacancies/{id}/highlight", true, false, true),
        new("api/vacancies/{id}/pushbom", true, false, true),
        new("api/vacancies/{id}/extend", true, false, true),
        new("api/vacancies/{id}/inactive", true, false, true),
        new("api/vacancies/{id}/ready", true, false, true),
        new("api/vacancies/{id}/contact-preference", true, false, true),
        new("api/vacancies/{id}/email-verification", true, false, true),
        // Applications mutate (employer)
        new("api/applications/{id}/react", true, false, true),
        new("api/applications/{id}/viewed", true, false, true),
        new("api/applications/{id}/contact", true, false, true),
        new("api/applications/vacancies/{vacancyId}/fulfill/{applicationId}", true, false, true),
        // Company users / org
        new("api/company-users/invite", true, false, false),
        new("api/company-users/{id}", true, false, false),
        new("api/regions", true, false, false),
        new("api/regions/{id}", true, false, false),
        new("api/companies/from-kvk", true, false, false),
        new("api/companies/{companyId}/token-management", true, false, false),
        new("api/companies/{companyId}/csv-batch-import", true, false, false),
        new("api/companies/{companyId}/email-verification", true, false, true),
        new("api/companies/{companyId}/contact-preference", true, false, true),
        new("api/companies/{companyId}/billing-preference", true, false, false),
        new("api/companies/{id}/billing-history", true, false, false),
        // Tokens
        new("api/tokens/checkout", true, false, false),
        new("api/tokens/top-up-quote", true, false, false),
        new("api/tokens/allocate", true, false, false),
        new("api/werkgever/token-requests", true, false, true),
        new("api/werkgever/token-requests/{id}/approve", true, false, false),
        new("api/werkgever/token-requests/{id}/reject", true, false, false),
        new("api/werkgever/token-requests/{id}/withdraw", true, false, true),
        // Insights
        new("api/employer/candidate-insights/unlock", true, false, true),
        new("api/employer/candidate-insights/unlock-request", false, false, true),
        new("api/employer/candidate-insights/unlock-request/{id}/reject", true, false, false),
        new("api/employer/candidate-insights/export.csv", true, true, true),
        // Talent / takeovers / salary / flyers / culture / csv / api keys
        new("api/employer/talent/unlock", true, false, true),
        new("api/employer/talent/{requestId}/withdraw", true, false, true),
        new("api/registration/takeovers/{id}/approve", true, false, true),
        new("api/registration/takeovers/{id}/reject", true, false, true),
        new("api/salary-tables", true, false, true),
        new("api/company/culture", true, false, true),
        new("api/vacancies/csv-import", true, false, false),
        new("api/vacancies/csv-import/row", true, false, false),
        new("api/companies/{companyId}/api-keys", true, false, false),
        new("api/companies/{companyId}/api-keys/{apiKeyId}/deactivate", true, false, false),
        new("api/companies/{companyId}/api-keys/email-credentials", true, false, false),
    ];

    public static bool RoleAllowed(PageRow row, EmployerRole role) => role switch
    {
        EmployerRole.Bedrijfsmanager => row.Bm,
        EmployerRole.Regiomanager => row.Rm,
        EmployerRole.Vestigingsmanager => row.Vm,
        _ => false
    };

    /// <summary>Markdown fragment compared by roles-matrix freshness tests.</summary>
    public static string GenerateMarkdown()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("## Werkgever redesign (BM / RM / VM)");
        sb.AppendLine();
        sb.AppendLine("Generated from `Jobsy.Tests/Werkgever/WerkgeverRightsMatrix.cs`. Do not hand-edit — regenerate via the matrix completeness / freshness tests.");
        sb.AppendLine();
        sb.AppendLine("Legend: ● full · ◐ read-only · ◯ own scope · — hidden/403.");
        sb.AppendLine();
        sb.AppendLine("| Page | BM | RM | VM | Authorize roles |");
        sb.AppendLine("|---|---|---|---|---|");
        foreach (var p in Pages)
        {
            var bm = p.Bm ? "●" : "—";
            var rm = p.Rm
                ? (p.AuthorizeRoles.Contains("RegionalManager", StringComparison.Ordinal) ? "◐" : "●")
                : "—";
            var vm = p.Vm ? "◯" : "—";
            if (p.Bm && !p.Rm && p.Vm)
            {
                vm = "◯";
            }

            sb.Append("| `").Append(p.Route).Append("` | ")
                .Append(bm).Append(" | ").Append(rm).Append(" | ").Append(vm).Append(" | `")
                .Append(p.AuthorizeRoles).AppendLine("` |");
        }

        sb.AppendLine();
        sb.AppendLine("| Mutating / employer API | BM | RM | VM |");
        sb.AppendLine("|---|---|---|---|");
        foreach (var a in Apis)
        {
            sb.Append("| `").Append(a.Path).Append("` | ")
                .Append(a.Bm ? "yes" : "no").Append(" | ")
                .Append(a.Rm ? "yes" : "no").Append(" | ")
                .Append(a.Vm ? "yes" : "no").AppendLine(" |");
        }

        sb.AppendLine();
        return sb.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
    }
}
