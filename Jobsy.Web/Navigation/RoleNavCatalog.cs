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
        new("Nav.Finance", "/admin/finance", NavIcons.Finance, ["/admin/tokens", "/admin/token-finance", "/admin/sales-managers", "/admin/ambassadeurs", "/admin/sales"]),
        new("Nav.Companies", "/admin/companies", NavIcons.Companies),
        new("Nav.Settings", "/admin/settings", NavIcons.Settings,
            ["/admin/integrations", "/admin/users", "/admin/logging", "/admin/wages", "/admin/masterdata", "/admin/exclusivity", "/admin/notifications", "/admin/company", "/admin/about", "/admin/marketing-flyer", "/admin/api-keys", "/admin/cnames", "/admin/vacancy-categories", "/admin/mail-test"])
    ];

    public static readonly NavItem[] Candidate =
    [
        new("Nav.Search", "/", NavIcons.Search),
        new("Nav.Saved", "/candidate/liked", NavIcons.Liked, ["/candidate/shared"]),
        new("Nav.MyApplications", "/candidate/applications", NavIcons.Applications),
        new("Nav.Profile", "/candidate/profile", NavIcons.Profile, ["/home"]),
        new("Nav.HowLobsyWorks", "/candidate/hoe-werkt-lobsy", NavIcons.Info)
    ];

    public static readonly NavItem MyApplicationsReadOnly =
        new("Nav.MyApplications", "/candidate/applications", NavIcons.Applications);

    /// <summary>
    /// Bedrijfsmanager: mobile/PWA keeps daily ops (home, vacancies, tokens, users).
    /// Heavy org administration lives under the desktop-only Organization hub.
    /// </summary>
    public static readonly NavItem[] Enterprise =
    [
        new("Nav.Home", "/home", NavIcons.Home),
        new("Nav.JobMap", "/", NavIcons.Map),
        new("Nav.Vacancies", "/employer/vacancies", NavIcons.Vacancies, ["/branch/vacancies/new"]),
        new("Nav.Applications", "/branch/applicants", NavIcons.Applications),
        new("Nav.Tokens", "/employer/tokens", NavIcons.Tokens, ["/regional/tokens", "/admin/tokens", "/branch/tokens"]),
        new("Nav.Users", "/employer/users", NavIcons.Users),
        new("Nav.Organization", "/employer/organization", NavIcons.Settings,
            [
                "/employer/salary-tables",
                "/employer/branches",
                "/employer/takeovers",
                "/employer/regions",
                "/employer/company",
                "/employer/csv-import"
            ],
            DesktopOnly: true),
        new("Nav.HowLobsyWorks", "/hoe-werkt-lobsy", NavIcons.Info)
    ];

    public static readonly NavItem CsvImport =
        new("Nav.CsvImport", "/employer/csv-import", NavIcons.Batch);

    public static readonly NavItem Organization =
        new("Nav.Organization", "/employer/organization", NavIcons.Settings,
            [
                "/employer/salary-tables",
                "/employer/branches",
                "/employer/takeovers",
                "/employer/regions",
                "/employer/company",
                "/employer/csv-import"
            ],
            DesktopOnly: true);

    public static readonly NavItem[] Regional =
    [
        new("Nav.Home", "/home", NavIcons.Home),
        new("Nav.JobMap", "/", NavIcons.Map),
        new("Nav.Vacancies", "/employer/vacancies", NavIcons.Vacancies, ["/regional", "/branch/applicants"]),
        new("Nav.MyBranches", "/regional/branches", NavIcons.Branches, ["/employer/takeovers"]),
        new("Nav.HowLobsyWorks", "/hoe-werkt-lobsy", NavIcons.Info)
    ];

    public static readonly NavItem[] Branch =
    [
        new("Nav.Home", "/home", NavIcons.Home),
        new("Nav.JobMap", "/", NavIcons.Map),
        new("Nav.Vacancies", "/branch/vacancies", NavIcons.Vacancies, ["/employer/vacancies", "/branch/vacancies/new"]),
        new("Nav.Applications", "/branch/applicants", NavIcons.Applications),
        new("Nav.MyTokens", "/branch/tokens", NavIcons.Tokens),
        new("Nav.CompanyDetails", "/employer/company", NavIcons.Companies),
        new("Nav.Takeovers", "/employer/takeovers", NavIcons.Branches),
        new("Nav.HowLobsyWorks", "/hoe-werkt-lobsy", NavIcons.Info)
    ];

    public static readonly NavItem Takeovers = new("Nav.Takeovers", "/employer/takeovers", NavIcons.Branches);

    public static readonly NavItem[] Intermediary =
    [
        new("Nav.Home", "/home", NavIcons.Home),
        new("Nav.JobMap", "/", NavIcons.Map),
        new("Nav.Vacancies", "/employer/vacancies", NavIcons.Vacancies, ["/branch/vacancies/new", "/branch/applicants"]),
        new("Nav.Clients", "/intermediary", NavIcons.Companies),
        new("Nav.Team", "/intermediary/team", NavIcons.Users),
        new("Nav.Tokens", "/employer/tokens", NavIcons.Tokens),
        new("Nav.HowLobsyWorks", "/hoe-werkt-lobsy", NavIcons.Info)
    ];

    public static readonly NavItem BalanceAndTracking =
        new("Nav.BalanceAndTracking", "/employer/tokens", NavIcons.Tokens, ["/branch/tokens"]);

    public static readonly NavItem[] SalesManager =
    [
        new("Nav.Home", "/home", NavIcons.Home, ["/salesmanager"]),
        new("Nav.SalesToolkit", "/salesmanager/toolkit", NavIcons.Shared),
        new("Nav.SalesReferrals", "/salesmanager/referrals", NavIcons.Users),
        new("Nav.Onboarding", "/salesmanager/onboarding", NavIcons.Users),
        new("Nav.Invoices", "/salesmanager/invoices", NavIcons.Tokens),
        new("Nav.HowLobsyWorks", "/hoe-werkt-lobsy", NavIcons.Info)
    ];

    public static readonly NavItem[] Ambassadeur =
    [
        new("Nav.Home", "/home", NavIcons.Home, ["/ambassadeur"]),
        new("Nav.AmbassadeurToolkit", "/ambassadeur/toolkit", NavIcons.Shared),
        new("Nav.AmbassadeurFinance", "/ambassadeur/finance", NavIcons.Tokens),
        new("Nav.Onboarding", "/ambassadeur/onboarding", NavIcons.Users),
        new("Nav.HowLobsyWorks", "/hoe-werkt-lobsy", NavIcons.Info)
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
            return WithHowLobsyLast(SalesManager);
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.Ambassadeur))
        {
            return WithHowLobsyLast(Ambassadeur);
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.Candidate))
        {
            return WithHowLobsyLast(Candidate);
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.EnterpriseManager))
        {
            return WithHowLobsyLast(WithSalesReferralNav(WithOptionalCandidateApplications(Enterprise, user), user));
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.RegionalManager))
        {
            return WithHowLobsyLast(WithOptionalCandidateApplications(Regional, user));
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.BranchManager))
        {
            return WithHowLobsyLast(WithSalesReferralNav(WithOptionalCandidateApplications(Branch, user), user));
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.Intermediary))
        {
            return WithHowLobsyLast(WithOptionalCandidateApplications(Intermediary, user));
        }

        return Anonymous;
    }

    /// <summary>
    /// Keeps "Hoe werkt Lobsy" as the rightmost bottom-nav item after optional nav appends.
    /// </summary>
    internal static IReadOnlyList<NavItem> WithHowLobsyLast(IReadOnlyList<NavItem> items)
    {
        NavItem? how = null;
        List<NavItem>? rest = null;
        foreach (var item in items)
        {
            if (string.Equals(item.TitleKey, "Nav.HowLobsyWorks", StringComparison.Ordinal))
            {
                how = item;
                continue;
            }

            rest ??= new List<NavItem>(items.Count);
            rest.Add(item);
        }

        if (how is null)
        {
            return items;
        }

        rest ??= [];
        rest.Add(how);
        return rest;
    }

    private static IReadOnlyList<NavItem> WithSalesReferralNav(
        IReadOnlyList<NavItem> baseItems,
        ClaimsPrincipal user)
    {
        if (!user.HasClaim(JobsyClaimTypes.HasSalesReferral, "1"))
        {
            return baseItems;
        }

        // Replace Tokens / Mijn tokens with "Mijn Saldo & Tracking" for referred entrepreneurs.
        return baseItems
            .Select(item => item.Href is "/employer/tokens" or "/branch/tokens"
                ? BalanceAndTracking with { Href = item.Href, ExtraActivePaths = item.ExtraActivePaths }
                : item)
            .ToList();
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
