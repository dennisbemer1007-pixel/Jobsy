using System.Security.Claims;
using Jobsy.Core.Authorization;

namespace Jobsy.Web.Navigation;

public static class RoleNavCatalog
{
    public static readonly NavItem[] Anonymous = [];

    public static readonly NavItem[] Admin =
    [
        new("Nav.Home", "/home", NavIcons.Home),
        new("Nav.JobMap", "/", NavIcons.Map),
        new("Nav.Vacancies", "/admin/vacancies", NavIcons.Vacancies, ["/admin/moderation"]),
        new("Nav.Finance", "/admin/finance", NavIcons.Finance, ["/admin/tokens", "/admin/sales-managers"]),
        new("Nav.Companies", "/admin/companies", NavIcons.Companies),
        new("Nav.Settings", "/admin/settings", NavIcons.Settings,
            ["/admin/integrations", "/admin/users", "/admin/logging", "/admin/wages", "/admin/masterdata", "/admin/notifications", "/admin/company", "/admin/about", "/admin/api-keys"])
    ];

    public static readonly NavItem[] Candidate =
    [
        new("Nav.Search", "/", NavIcons.Search),
        new("Nav.HowLobsyWorks", "/candidate/hoe-werkt-lobsy", NavIcons.Info),
        new("Nav.Saved", "/candidate/liked", NavIcons.Liked, ["/candidate/shared"]),
        new("Nav.MyApplications", "/candidate/applications", NavIcons.Applications),
        new("Nav.Profile", "/candidate/profile", NavIcons.Profile, ["/home"])
    ];

    public static readonly NavItem MyApplicationsReadOnly =
        new("Nav.MyApplications", "/candidate/applications", NavIcons.Applications);

    public static readonly NavItem[] Enterprise =
    [
        new("Nav.Home", "/home", NavIcons.Home),
        new("Nav.JobMap", "/", NavIcons.Map),
        new("Nav.Vacancies", "/employer/vacancies", NavIcons.Vacancies, ["/branch/vacancies/new", "/branch/applicants"]),
        new("Nav.SalaryTables", "/employer/salary-tables", NavIcons.Wages),
        new("Nav.Tokens", "/employer/tokens", NavIcons.Tokens, ["/regional/tokens", "/admin/tokens", "/branch/tokens"]),
        new("Nav.Branches", "/employer/branches", NavIcons.Branches, ["/employer/takeovers"]),
        new("Nav.Regions", "/employer/regions", NavIcons.Regions),
        new("Nav.Users", "/employer/users", NavIcons.Users),
        new("Nav.CsvImport", "/employer/csv-import", NavIcons.Batch),
        new("Nav.CompanyDetails", "/employer/company", NavIcons.Companies)
    ];

    public static readonly NavItem CsvImport =
        new("Nav.CsvImport", "/employer/csv-import", NavIcons.Batch);

    public static readonly NavItem[] Regional =
    [
        new("Nav.Home", "/home", NavIcons.Home),
        new("Nav.JobMap", "/", NavIcons.Map),
        new("Nav.Vacancies", "/employer/vacancies", NavIcons.Vacancies, ["/regional", "/branch/applicants"]),
        new("Nav.MyBranches", "/regional/branches", NavIcons.Branches, ["/employer/takeovers"])
    ];

    public static readonly NavItem[] Branch =
    [
        new("Nav.Home", "/home", NavIcons.Home),
        new("Nav.JobMap", "/", NavIcons.Map),
        new("Nav.Vacancies", "/employer/vacancies", NavIcons.Vacancies, ["/branch/vacancies/new", "/branch/applicants"]),
        new("Nav.MyTokens", "/branch/tokens", NavIcons.Tokens),
        new("Nav.CompanyDetails", "/employer/company", NavIcons.Companies),
        new("Nav.Takeovers", "/employer/takeovers", NavIcons.Branches)
    ];

    public static readonly NavItem Takeovers = new("Nav.Takeovers", "/employer/takeovers", NavIcons.Branches);

    public static readonly NavItem[] Intermediary =
    [
        new("Nav.Home", "/home", NavIcons.Home),
        new("Nav.JobMap", "/", NavIcons.Map),
        new("Nav.Clients", "/intermediary", NavIcons.Companies),
        new("Nav.BatchTool", "/intermediary/batch", NavIcons.Batch),
        new("Nav.Tokens", "/employer/tokens", NavIcons.Tokens)
    ];

    public static readonly NavItem[] SalesManager =
    [
        new("Nav.Home", "/home", NavIcons.Home, ["/salesmanager"]),
        new("Nav.Onboarding", "/salesmanager/onboarding", NavIcons.Users),
        new("Nav.Invoices", "/salesmanager/invoices", NavIcons.Tokens)
    ];

    public static IReadOnlyList<NavItem> ForUser(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            return Anonymous;
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.Admin))
        {
            return Admin;
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.SalesManager))
        {
            return SalesManager;
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.Candidate))
        {
            return Candidate;
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.EnterpriseManager))
        {
            return WithOptionalCandidateApplications(Enterprise, user);
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.RegionalManager))
        {
            return WithOptionalCandidateApplications(Regional, user);
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.BranchManager))
        {
            return WithOptionalCandidateApplications(Branch, user);
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.Intermediary))
        {
            return WithOptionalCandidateApplications(Intermediary, user);
        }

        return Anonymous;
    }

    private static IReadOnlyList<NavItem> WithOptionalCandidateApplications(
        NavItem[] baseItems,
        ClaimsPrincipal user)
    {
        if (!user.HasClaim(JobsyClaimTypes.HasCandidateApplications, "1"))
        {
            return baseItems;
        }

        if (baseItems.Any(i => i.Href == MyApplicationsReadOnly.Href))
        {
            return baseItems;
        }

        return [.. baseItems, MyApplicationsReadOnly];
    }

    public static bool IsActive(NavItem item, string relativePath, IReadOnlyList<NavItem>? siblings = null)
    {
        var path = NormalizePath(relativePath);
        var itemHref = NormalizePath(item.Href);

        if (string.Equals(path, itemHref, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Another nav item owns this path exactly (e.g. /branch/tokens vs Vacatures ExtraActivePaths /branch).
        if (siblings is not null
            && siblings.Any(other =>
                !ReferenceEquals(other, item)
                && string.Equals(path, NormalizePath(other.Href), StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (item.ExtraActivePaths is null)
        {
            return false;
        }

        return item.ExtraActivePaths.Any(p => MatchesPathOrPrefix(path, NormalizePath(p)));
    }

    public static string TokensHrefFor(ClaimsPrincipal user)
    {
        if (RoleClaimMatching.HasRole(user, JobsyRoles.BranchManager))
        {
            return "/branch/tokens";
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.RegionalManager)
            || RoleClaimMatching.HasRole(user, JobsyRoles.EnterpriseManager)
            || RoleClaimMatching.HasRole(user, JobsyRoles.Intermediary))
        {
            return "/employer/tokens";
        }

        return "/home";
    }

    public static string NormalizePath(string relativePath)
    {
        var path = relativePath.Split('?', 2)[0].Split('#', 2)[0].Trim('/');
        return string.IsNullOrEmpty(path) ? "/" : "/" + path;
    }

    private static bool MatchesPathOrPrefix(string path, string candidate)
    {
        if (string.Equals(path, candidate, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Avoid treating "/" as a prefix of every path.
        if (candidate is "/" or "")
        {
            return false;
        }

        return path.StartsWith(candidate + "/", StringComparison.OrdinalIgnoreCase);
    }
}
