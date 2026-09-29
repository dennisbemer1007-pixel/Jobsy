using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Features;

namespace Jobsy.Web.Navigation;

/// <summary>
/// Ordered candidate nav slots. Discovery is reserved (empty until file 08).
/// </summary>
public enum CandidateNavSlot
{
    Discovery,
    Passport,
    Search,
    Applications,
    Career,
    Saved
}

public static class RoleNavCatalog
{
    public static readonly NavItem[] Anonymous = [];

    public static readonly NavItem[] Admin =
    [
        new("Nav.Home", "/home", NavIcons.Home),
        new("Nav.JobMap", "/", NavIcons.Map),
        new("Nav.Vacancies", "/admin/vacancies", NavIcons.Vacancies, ["/admin/moderation"]),
        new("Admin.AtsVacancies", "/admin/ats-vacancies", NavIcons.List),
        new("Nav.Finance", "/admin/finance", NavIcons.Finance, ["/admin/tokens", "/admin/token-finance", "/admin/sales-managers", "/admin/ambassadeurs", "/admin/sales"]),
        new("Nav.Companies", "/admin/companies", NavIcons.Companies),
        new("Nav.Settings", "/admin/settings", NavIcons.Settings,
            ["/admin/integrations", "/admin/users", "/admin/personal-data-access-log", "/admin/logging", "/admin/feedback", "/admin/wages", "/admin/masterdata", "/admin/exclusivity", "/admin/notifications", "/admin/company", "/admin/about", "/admin/marketing-flyer", "/admin/api-keys", "/admin/cnames", "/admin/vacancy-categories", "/admin/training", "/admin/mail-test"])
    ];

    /// <summary>
    /// Legacy candidate array (passport OFF + employers ON). Kept for callers that still
    /// reference <see cref="Candidate"/>; prefer <see cref="CandidateItems"/>.
    /// </summary>
    public static readonly NavItem[] Candidate =
    [
        new("Nav.Search", "/", NavIcons.Search),
        new("Nav.Saved", "/candidate/liked", NavIcons.Liked, ["/candidate/shared"]),
        new("Nav.Applications", "/candidate/applications", NavIcons.Applications),
        new("Nav.CareerPath", "/carriere", NavIcons.Career),
        new("Nav.Profile", "/candidate/profile", NavIcons.Profile, ["/profiel", "/home"])
    ];

    public static readonly NavItem SearchItem =
        new("Nav.Search", "/", NavIcons.Search);

    public static readonly NavItem SavedItem =
        new("Nav.Saved", "/candidate/liked", NavIcons.Liked, ["/candidate/shared"]);

    public static readonly NavItem ApplicationsItem =
        new("Nav.Applications", "/candidate/applications", NavIcons.Applications);

    public static readonly NavItem CareerItem =
        new("Nav.CareerPath", "/carriere", NavIcons.Career);

    public static readonly NavItem ProfileItem =
        new("Nav.Profile", "/candidate/profile", NavIcons.Profile, ["/profiel", "/home"]);

    public static readonly NavItem PassportItem =
        new("Nav.Passport", "/candidate/paspoort", NavIcons.Profile,
            ["/candidate/profile", "/profiel", "/home"]);

    /// <summary>Sollicitaties with Bewaard URLs as active aliases (passport-ON order).</summary>
    public static readonly NavItem ApplicationsWithSavedAliases =
        new("Nav.Applications", "/candidate/applications", NavIcons.Applications,
            ["/candidate/liked", "/candidate/shared"]);

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
        new("Nav.Talent", "/employer/talent", NavIcons.Users, ["/employer/talent-contacts", "/employer/kandidaatinzichten"]),
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
            DesktopOnly: true)
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
        new("Nav.CandidateInsights", "/employer/kandidaatinzichten", NavIcons.Users)
    ];

    public static readonly NavItem[] Branch =
    [
        new("Nav.Home", "/home", NavIcons.Home),
        new("Nav.JobMap", "/", NavIcons.Map),
        new("Nav.Vacancies", "/branch/vacancies", NavIcons.Vacancies, ["/employer/vacancies", "/branch/vacancies/new"]),
        new("Nav.Applications", "/branch/applicants", NavIcons.Applications),
        new("Nav.Talent", "/employer/talent", NavIcons.Users, ["/employer/talent-contacts", "/employer/kandidaatinzichten"]),
        new("Nav.MyTokens", "/branch/tokens", NavIcons.Tokens),
        new("Nav.CompanyDetails", "/employer/company", NavIcons.Companies),
        new("Nav.Takeovers", "/employer/takeovers", NavIcons.Branches)
    ];

    public static readonly NavItem Takeovers = new("Nav.Takeovers", "/employer/takeovers", NavIcons.Branches);

    public static readonly NavItem[] Intermediary =
    [
        new("Nav.Home", "/home", NavIcons.Home),
        new("Nav.JobMap", "/", NavIcons.Map),
        new("Nav.Vacancies", "/employer/vacancies", NavIcons.Vacancies, ["/branch/vacancies/new", "/branch/applicants"]),
        new("Nav.Talent", "/employer/talent", NavIcons.Users, ["/employer/talent-contacts"]),
        new("Nav.Clients", "/intermediary", NavIcons.Companies),
        new("Nav.Team", "/intermediary/team", NavIcons.Users),
        new("Nav.Tokens", "/employer/tokens", NavIcons.Tokens)
    ];

    public static readonly NavItem BalanceAndTracking =
        new("Nav.BalanceAndTracking", "/employer/tokens", NavIcons.Tokens, ["/branch/tokens"]);

    public static readonly NavItem[] SalesManager =
    [
        new("Nav.Home", "/home", NavIcons.Home, ["/salesmanager"]),
        new("Nav.SalesToolkit", "/salesmanager/toolkit", NavIcons.Shared),
        new("Nav.SalesReferrals", "/salesmanager/referrals", NavIcons.Users),
        new("Nav.Onboarding", "/salesmanager/onboarding", NavIcons.Users),
        new("Nav.Invoices", "/salesmanager/invoices", NavIcons.Tokens)
    ];

    public static readonly NavItem[] Ambassadeur =
    [
        new("Nav.Home", "/home", NavIcons.Home, ["/ambassadeur"]),
        new("Nav.AmbassadeurToolkit", "/ambassadeur/toolkit", NavIcons.Shared),
        new("Nav.AmbassadeurFinance", "/ambassadeur/finance", NavIcons.Tokens),
        new("Nav.Onboarding", "/ambassadeur/onboarding", NavIcons.Users)
    ];

    /// <summary>
    /// Pure-function candidate nav. Passport OFF keeps today's order; passport ON uses §N slots
    /// (Discovery reserved empty until file 08). Employers OFF hides Zoeken / Bewaard / Sollicitaties.
    /// </summary>
    public static IReadOnlyList<NavItem> CandidateItems(FeatureFlagSnapshot flags)
    {
        // Discovery slot reserved (empty until file 08) — nothing rendered.
        _ = CandidateNavSlot.Discovery;

        if (flags.CandidatePassportEnabled)
        {
            if (!flags.EmployersEnabled)
            {
                // [Ontdekkingsreis] · Mijn Paspoort · Carrière
                return [PassportItem, CareerItem];
            }

            // [Ontdekkingsreis] · Mijn Paspoort · Zoeken · Sollicitaties · Carrière
            return [PassportItem, SearchItem, ApplicationsWithSavedAliases, CareerItem];
        }

        if (!flags.EmployersEnabled)
        {
            // Career · Profile
            return [CareerItem, ProfileItem];
        }

        // Employers ON + passport OFF: exactly today's order
        return Candidate;
    }

    /// <summary>
    /// True when Bewaard is its own bottom-nav item (legacy order).
    /// False when passport is ON (Saved moves into Sollicitaties tabs) or employers OFF.
    /// </summary>
    public static bool ShowsSavedInNav(FeatureFlagSnapshot flags)
        => flags.EmployersEnabled && !flags.CandidatePassportEnabled;

    public static IReadOnlyList<NavItem> ForUser(ClaimsPrincipal? user)
        => ForUser(user, FeatureFlagSnapshot.Defaults);

    public static IReadOnlyList<NavItem> ForUser(ClaimsPrincipal? user, FeatureFlagSnapshot flags)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            return Anonymous;
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.Admin))
        {
            return Admin;
        }

        // Employer-side / acquisition catalogs empty when employers OFF (Admin already returned).
        if (!flags.EmployersEnabled)
        {
            if (RoleClaimMatching.HasRole(user, JobsyRoles.Candidate))
            {
                return CandidateItems(flags);
            }

            if (RoleClaimMatching.HasRole(user, JobsyRoles.SalesManager)
                || RoleClaimMatching.HasRole(user, JobsyRoles.Ambassadeur)
                || RoleClaimMatching.HasAnyRole(user, JobsyRoles.EmployerRoles))
            {
                return Anonymous;
            }
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.SalesManager))
        {
            return SalesManager;
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.Ambassadeur))
        {
            return Ambassadeur;
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.Candidate))
        {
            return CandidateItems(flags);
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.EnterpriseManager))
        {
            return WithSalesReferralNav(WithOptionalCandidateApplications(Enterprise, user), user);
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.RegionalManager))
        {
            return WithOptionalCandidateApplications(Regional, user);
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.BranchManager))
        {
            return WithSalesReferralNav(WithOptionalCandidateApplications(Branch, user), user);
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.Intermediary))
        {
            return WithOptionalCandidateApplications(Intermediary, user);
        }

        return Anonymous;
    }

    /// <summary>
    /// Role-specific “Hoe werkt Lobsy” page, shown in the account menu rather than bottom-nav.
    /// </summary>
    public static string? HowLobsyHrefFor(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true
            || RoleClaimMatching.HasRole(user, JobsyRoles.Admin))
        {
            return null;
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.Candidate))
        {
            return "/candidate/hoe-werkt-lobsy";
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.SalesManager)
            || RoleClaimMatching.HasRole(user, JobsyRoles.Ambassadeur)
            || RoleClaimMatching.HasAnyRole(user, JobsyRoles.EmployerRoles))
        {
            return "/hoe-werkt-lobsy";
        }

        return null;
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
