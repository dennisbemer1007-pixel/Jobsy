using System.Security.Claims;
using Jobsy.Core.Authorization;

namespace Jobsy.Web.Navigation;

public static class RoleNavCatalog
{
    public static readonly NavItem[] Anonymous = [];

/// <summary>
    /// Admin uses <see cref="AdminNav"/> + <c>AdminLayout</c> sidebar; no bottom nav (D1).
    /// Scholen admin pages live under <see cref="AdminNav"/> (Organisaties group).
    /// </summary>
    public static readonly NavItem[] Admin = [];

    public static readonly NavItem[] Candidate =
    [
        new("Nav.Search", "/", NavIcons.Search),
        new("Nav.Saved", "/candidate/liked", NavIcons.Liked, ["/candidate/shared"]),
        new("Nav.Applications", "/candidate/applications", NavIcons.Applications),
        new("Nav.CareerPath", "/carriere", NavIcons.Career),
        new("Nav.Profile", "/candidate/profile", NavIcons.Profile, ["/profiel", "/home"])
    ];

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

    public static readonly NavItem[] SchoolAdmin = [];

    public static readonly NavItem[] Teacher = [];

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

        if (RoleClaimMatching.HasRole(user, JobsyRoles.SchoolAdmin)
            || RoleClaimMatching.HasRole(user, JobsyRoles.Teacher))
        {
            // School/teacher chrome comes from ScholenNav + SchoolLayout (not bottom-nav catalog).
            return RoleClaimMatching.HasRole(user, JobsyRoles.Teacher) && !RoleClaimMatching.HasRole(user, JobsyRoles.SchoolAdmin)
                ? Teacher
                : SchoolAdmin;
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
            return Candidate;
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

        return item.ExtraActivePaths.Any(p => MatchesPathOrPrefix(path, NormalizePath(p)));
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
