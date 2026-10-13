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

    /// <summary>
    /// Admin uses <see cref="AdminNav"/> + <c>AdminLayout</c> sidebar; no bottom nav (D1).
    /// Scholen admin pages live under <see cref="AdminNav"/> (Organisaties group).
    /// </summary>
    public static readonly NavItem[] Admin = [];

    /// <summary>
    /// Legacy candidate array (passport OFF + employers ON). Kept for callers that still
    /// reference <see cref="Candidate"/>; prefer <see cref="CandidateItems"/>.
    /// </summary>
    public static readonly NavItem[] Candidate =
    [
        new("Nav.Search", "/banenkaart", NavIcons.Search, ["/"]),
        new("Nav.Saved", "/candidate/liked", NavIcons.Liked, ["/candidate/shared"]),
        new("Nav.Applications", "/candidate/applications", NavIcons.Applications),
        new("Nav.CareerPath", "/carriere", NavIcons.Career),
        new("Nav.Profile", "/candidate/profile", NavIcons.Profile, ["/profiel", "/home"])
    ];

    /// <summary>
    /// Zoeken / banenkaart. Match stays a map button (not a nav item) but marks Zoeken active.
    /// </summary>
    public static readonly NavItem SearchItem =
        new("Nav.Search", "/banenkaart", NavIcons.Search, ["/", "/candidate/match"]);

    public static readonly NavItem SavedItem =
        new("Nav.Saved", "/candidate/liked", NavIcons.Liked, ["/candidate/shared"]);

    public static readonly NavItem ApplicationsItem =
        new("Nav.Applications", "/candidate/applications", NavIcons.Applications);

    public static readonly NavItem CareerItem =
        new("Nav.CareerPath", "/carriere", NavIcons.Career);

    public static readonly NavItem HoursItem =
        new("Nav.Hours", "/candidate/uren", NavIcons.Clock, ShortTitleKey: "Nav.Hours.Short");

    public static readonly NavItem ProfileItem =
        new("Nav.Profile", "/candidate/profile", NavIcons.Profile, ["/profiel", "/home"]);

    public static readonly NavItem PassportItem =
        new("Nav.Passport", "/candidate/paspoort", NavIcons.Profile,
            ["/candidate/profile", "/profiel", "/home"],
            ShortTitleKey: "Nav.Passport.Short");

    /// <summary>De ontdekkingsreis — filled in file 08 (§N Discovery slot).</summary>
    public static readonly NavItem DiscoveryItem =
        new("Nav.Discovery", "/candidate/ontdekkingsreis", NavIcons.Compass,
            ["/candidate/start"],
            ShortTitleKey: "Nav.Discovery.Short");

    /// <summary>Sollicitaties with Bewaard URLs as active aliases (passport-ON order).</summary>
    public static readonly NavItem ApplicationsWithSavedAliases =
        new("Nav.Applications", "/candidate/applications", NavIcons.Applications,
            ["/candidate/liked", "/candidate/shared"]);
    public static readonly NavItem MyApplicationsReadOnly =
        new("Nav.MyApplications", "/candidate/applications", NavIcons.Applications);

    /// <summary>Employer roles use <see cref="WerkgeverNav"/> + <c>WerkgeverLayout</c> (D1).</summary>
    public static readonly NavItem[] Enterprise = [];

    public static readonly NavItem[] Regional = [];

    public static readonly NavItem[] Branch = [];

    public static readonly NavItem[] Intermediary = [];

    /// <summary>Legacy filter target for CSV import (href only; not shown in RoleNavCatalog employer lists).</summary>
    public static readonly NavItem CsvImport =
        new("Nav.CsvImport", "/werkgever/koppelingen", NavIcons.Batch);

    /// <summary>Legacy filter target for takeovers (href only).</summary>
    public static readonly NavItem Takeovers =
        new("Nav.Takeovers", "/werkgever/overnames", NavIcons.Branches);

    /// <summary>Nav comes from <see cref="SalesNav"/> inside SalesLayout.</summary>
    public static readonly NavItem[] SalesManager = [];

    public static readonly NavItem[] Ambassadeur =
    [
        new("Nav.Home", "/home", NavIcons.Home, ["/ambassadeur"]),
        new("Nav.AmbassadeurToolkit", "/ambassadeur/toolkit", NavIcons.Shared),
        new("Nav.AmbassadeurFinance", "/ambassadeur/finance", NavIcons.Tokens),
        new("Nav.Onboarding", "/ambassadeur/onboarding", NavIcons.Users)
    ];

    public static readonly NavItem[] SchoolAdmin = [];

    public static readonly NavItem[] Teacher = [];

    /// <summary>
    /// Pure-function candidate nav. Passport OFF keeps today's order (D1); passport ON (default)
    /// uses Discovery · Passport · Zoeken · Sollicitaties · Carrière. Employers OFF hides
    /// Zoeken / Bewaard / Sollicitaties. Match is never a nav item.
    /// </summary>
    public static IReadOnlyList<NavItem> CandidateItems(FeatureFlagSnapshot flags)
    {
        _ = CandidateNavSlot.Discovery;

        if (flags.CandidatePassportEnabled)
        {
            if (!flags.EmployersEnabled)
            {
                // De ontdekkingsreis · Mijn Paspoort · Carrière
                return [DiscoveryItem, PassportItem, CareerItem];
            }

            // De ontdekkingsreis · Mijn Paspoort · Zoeken · Sollicitaties · Carrière
            return [DiscoveryItem, PassportItem, SearchItem, ApplicationsWithSavedAliases, CareerItem];
        }

        if (!flags.EmployersEnabled)
        {
            // Career · Profile
            return [CareerItem, ProfileItem];
        }

        // Employers ON + passport OFF: exactly today's legacy order
        return Candidate;
    }

    /// <summary>
    /// True when Bewaard is its own bottom-nav item (legacy passport-OFF order).
    /// False when passport is ON (Bewaard is a Sollicitaties tab) or employers OFF.
    /// </summary>
    public static bool ShowsSavedInNav(FeatureFlagSnapshot flags)
        => flags.EmployersEnabled && !flags.CandidatePassportEnabled;

    /// <summary>Inserts Uren before Carrière when the candidate has an active Maqqie contract.</summary>
    public static IReadOnlyList<NavItem> WithMaqqieHoursNav(IReadOnlyList<NavItem> items)
    {
        if (items.Any(i => string.Equals(i.Href, HoursItem.Href, StringComparison.OrdinalIgnoreCase)))
        {
            return items;
        }

        var list = items.ToList();
        var careerIdx = list.FindIndex(i => string.Equals(i.Href, CareerItem.Href, StringComparison.OrdinalIgnoreCase));
        if (careerIdx >= 0)
        {
            list.Insert(careerIdx, HoursItem);
        }
        else
        {
            list.Add(HoursItem);
        }

        return list;
    }

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

        if (RoleClaimMatching.HasRole(user, JobsyRoles.SchoolAdmin)
            || RoleClaimMatching.HasRole(user, JobsyRoles.Teacher))
        {
            // School/teacher chrome comes from ScholenNav + SchoolLayout (not bottom-nav catalog).
            return RoleClaimMatching.HasRole(user, JobsyRoles.Teacher) && !RoleClaimMatching.HasRole(user, JobsyRoles.SchoolAdmin)
                ? Teacher
                : SchoolAdmin;
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

        // Employer roles: empty — WerkgeverLayout owns navigation (D1).
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

    public static bool IsActive(NavItem item, string relativePath, IReadOnlyList<NavItem>? siblings = null)
    {
        var path = NormalizePath(relativePath);
        var itemHref = NormalizePath(item.Href);

        if (string.Equals(path, itemHref, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

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

        return item.ExtraActivePaths.Any(p =>
        {
            var candidate = NormalizePath(p);
            // Exact "/" is allowed as ExtraActivePath (map dual-route during landing 04).
            if (candidate is "/")
            {
                return string.Equals(path, "/", StringComparison.OrdinalIgnoreCase);
            }

            return MatchesPathOrPrefix(path, candidate);
        });
    }

    public static string TokensHrefFor(ClaimsPrincipal user)
    {
        if (RoleClaimMatching.HasAnyRole(user, JobsyRoles.EmployerRoles))
        {
            return "/werkgever/tokens";
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

        if (candidate is "/" or "")
        {
            return false;
        }

        return path.StartsWith(candidate + "/", StringComparison.OrdinalIgnoreCase);
    }
}
