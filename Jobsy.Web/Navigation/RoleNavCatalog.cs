using System.Security.Claims;
using Jobsy.Core.Authorization;

namespace Jobsy.Web.Navigation;

public static class RoleNavCatalog
{
    public static readonly NavItem[] Anonymous =
    [
        new("Nav.Search", "/", NavIcons.Search),
        new("Nav.Saved", "/candidate/liked", NavIcons.Liked),
        new("Nav.Profile", "/login", NavIcons.Profile)
    ];

    public static readonly NavItem[] Admin =
    [
        new("Nav.Home", "/home", NavIcons.Home),
        new("Nav.JobMap", "/", NavIcons.Map),
        new("Nav.Vacancies", "/admin/vacancies", NavIcons.Vacancies, ["/admin/moderation"]),
        new("Nav.Finance", "/admin/finance", NavIcons.Finance, ["/admin/tokens"]),
        new("Nav.Companies", "/admin/companies", NavIcons.Companies),
        new("Nav.Settings", "/admin/settings", NavIcons.Settings, ["/admin/integrations", "/admin/users", "/admin/logging", "/admin/wages"])
    ];

    public static readonly NavItem[] Candidate =
    [
        new("Nav.Search", "/", NavIcons.Search),
        new("Nav.Saved", "/candidate/liked", NavIcons.Liked, ["/candidate/shared", "/candidate/applications"]),
        new("Nav.Profile", "/candidate/profile", NavIcons.Profile, ["/home"])
    ];

    public static readonly NavItem[] Enterprise =
    [
        new("Nav.Home", "/home", NavIcons.Home),
        new("Nav.JobMap", "/", NavIcons.Map),
        new("Nav.Vacancies", "/employer/vacancies", NavIcons.Vacancies, ["/branch", "/branch/vacancies/new"]),
        new("Nav.SalaryTables", "/employer/salary-tables", NavIcons.Wages),
        new("Nav.Tokens", "/employer/tokens", NavIcons.Tokens, ["/regional/tokens", "/admin/tokens"]),
        new("Nav.Branches", "/employer/branches", NavIcons.Branches, ["/employer/takeovers"]),
        new("Nav.Regions", "/employer/regions", NavIcons.Regions),
        new("Nav.Users", "/employer/users", NavIcons.Users)
    ];

    public static readonly NavItem[] Regional =
    [
        new("Nav.Home", "/home", NavIcons.Home),
        new("Nav.JobMap", "/", NavIcons.Map),
        new("Nav.Vacancies", "/employer/vacancies", NavIcons.Vacancies, ["/regional", "/branch"]),
        new("Nav.MyBranches", "/regional/branches", NavIcons.Branches, ["/employer/takeovers"])
    ];

    public static readonly NavItem[] Branch =
    [
        new("Nav.Home", "/home", NavIcons.Home),
        new("Nav.JobMap", "/", NavIcons.Map),
        new("Nav.Vacancies", "/employer/vacancies", NavIcons.Vacancies, ["/branch", "/branch/vacancies/new"]),
        new("Nav.MyTokens", "/branch/tokens", NavIcons.Tokens),
        new("Nav.Takeovers", "/employer/takeovers", NavIcons.Branches)
    ];

    public static readonly NavItem[] Intermediary =
    [
        new("Nav.Home", "/home", NavIcons.Home),
        new("Nav.JobMap", "/", NavIcons.Map),
        new("Nav.Clients", "/intermediary", NavIcons.Companies),
        new("Nav.BatchTool", "/intermediary/batch", NavIcons.Batch),
        new("Nav.Tokens", "/employer/tokens", NavIcons.Tokens)
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

        if (RoleClaimMatching.HasRole(user, JobsyRoles.Candidate))
        {
            return Candidate;
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.EnterpriseManager))
        {
            return Enterprise;
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.RegionalManager))
        {
            return Regional;
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.BranchManager))
        {
            return Branch;
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.Intermediary))
        {
            return Intermediary;
        }

        return Anonymous;
    }

    public static bool IsActive(NavItem item, string relativePath)
    {
        var path = NormalizePath(relativePath);

        if (string.Equals(path, NormalizePath(item.Href), StringComparison.OrdinalIgnoreCase))
        {
            return true;
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
