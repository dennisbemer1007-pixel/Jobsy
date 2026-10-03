using System.Security.Claims;
using Jobsy.Core.Authorization;

namespace Jobsy.Core.Features;

/// <summary>Pure home-path resolution based on auth identity and feature flags.</summary>
public static class FeatureRoutes
{
    public const string OntdekPath = "/ontdek";
    public const string CandidateProfilePath = "/candidate/profile";
    public const string CandidatePassportPath = "/candidate/paspoort";
    /// <summary>De ontdekkingsreis entry (same path as <c>OnboardingRoutes.DiscoveryPath</c>).</summary>
    public const string CandidateDiscoveryPath = "/candidate/ontdekkingsreis";
    public const string AdminHomePath = "/home";
    public const string EmployersOffAccessDeniedPath = "/access-denied?reason=employers-off";
    public const string SchoolsOffAccessDeniedPath = "/access-denied?reason=schools-off";
    public const string SchoolPortalPath = "/school";
    public const string TeacherPortalPath = "/leraar";
    /// <summary>Friendly page for anonymous visitors on employer routes while employers are OFF.</summary>
    public const string EmployersComingSoonPath = "/werkgevers/binnenkort";

    /// <summary>
    /// Role home. For candidates with the paspoort flag ON,
    /// <paramref name="passportReady"/> selects ontdekkingsreis vs Mijn Paspoort
    /// (see <see cref="CandidateLanding.IsPassportReady"/>). Flag OFF ignores readiness.
    /// </summary>
    public static string HomeFor(
        ClaimsPrincipal? user,
        FeatureFlagSnapshot flags,
        bool passportReady = false)
    {
        if (flags.EmployersEnabled)
        {
            if (user?.Identity?.IsAuthenticated != true)
            {
                return "/";
            }

            if (RoleClaimMatching.HasRole(user, JobsyRoles.Admin))
            {
                return AdminHomePath;
            }

            if (RoleClaimMatching.HasRole(user, JobsyRoles.Candidate))
            {
                return CandidateHome(flags, passportReady);
            }

            if (TrySchoolStaffHome(user, flags, out var schoolHome))
            {
                return schoolHome;
            }

            return AdminHomePath;
        }

        // Employers OFF. Anonymous home is the landing page (canonical "/"), not /ontdek.
        if (user?.Identity?.IsAuthenticated != true)
        {
            return "/";
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.Admin))
        {
            return AdminHomePath;
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.Candidate))
        {
            return CandidateHome(flags, passportReady);
        }

        if (TrySchoolStaffHome(user, flags, out var schoolHomeOff))
        {
            return schoolHomeOff;
        }

        // Employer-side / acquisition roles only
        if (IsEmployerSideOnly(user))
        {
            return EmployersOffAccessDeniedPath;
        }

        return OntdekPath;
    }

    /// <summary>
    /// Candidate branch of <see cref="HomeFor"/>. Paspoort ON: not ready → discovery,
    /// ready → passport. Paspoort OFF: employers ON → <c>/</c>, OFF → classic profile.
    /// </summary>
    public static string CandidateHome(FeatureFlagSnapshot flags, bool passportReady)
    {
        if (flags.CandidatePassportEnabled)
        {
            return passportReady ? CandidatePassportPath : CandidateDiscoveryPath;
        }

        return flags.EmployersEnabled ? "/" : CandidateProfilePath;
    }

    /// <summary>
    /// Portal path for a school admin or teacher who is not also an admin or candidate.
    /// Null for every other principal. Does not look at the schools flag.
    /// </summary>
    public static string? SchoolStaffPortal(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.Admin)
            || RoleClaimMatching.HasRole(user, JobsyRoles.Candidate))
        {
            return null;
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.SchoolAdmin))
        {
            return SchoolPortalPath;
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.Teacher))
        {
            return TeacherPortalPath;
        }

        return null;
    }

    /// <summary>
    /// School staff home. When the portal is off, this is the friendly access-denied page
    /// so role landing does not send them back into /school or /leraar.
    /// </summary>
    public static bool TrySchoolStaffHome(ClaimsPrincipal? user, FeatureFlagSnapshot flags, out string path)
    {
        var portal = SchoolStaffPortal(user);
        if (portal is null)
        {
            path = "";
            return false;
        }

        path = flags.SchoolsEnabled ? portal : SchoolsOffAccessDeniedPath;
        return true;
    }

    /// <summary>
    /// True when the user has only employer/acquisition roles (no Candidate, no Admin).
    /// </summary>
    public static bool IsEmployerSideOnly(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.Admin)
            || RoleClaimMatching.HasRole(user, JobsyRoles.Candidate))
        {
            return false;
        }

        return RoleClaimMatching.HasAnyRole(user, JobsyRoles.EmployerRoles)
               || RoleClaimMatching.HasRole(user, JobsyRoles.SalesManager)
               || RoleClaimMatching.HasRole(user, JobsyRoles.Ambassadeur);
    }

    /// <summary>
    /// Where to send someone when an Employers-gated page is OFF.
    /// Employer-side users go to the access-denied page. Signed-in candidates and admins
    /// go to their own home. Anonymous visitors on candidate vacancy pages go home ("/").
    /// Other anonymous visitors go to the binnenkort page, unless the page set an explicit fallback.
    /// </summary>
    public static string EmployersOffRedirect(
        ClaimsPrincipal? user,
        FeatureFlagSnapshot flags,
        bool passportReady,
        bool candidateVacancySurface,
        string? explicitFallback)
    {
        if (IsEmployerSideOnly(user))
        {
            return EmployersOffAccessDeniedPath;
        }

        if (user?.Identity?.IsAuthenticated == true)
        {
            return HomeFor(user, flags, passportReady);
        }

        if (candidateVacancySurface)
        {
            return HomeFor(null, flags, passportReady);
        }

        if (!string.IsNullOrWhiteSpace(explicitFallback))
        {
            return explicitFallback;
        }

        return EmployersComingSoonPath;
    }

    /// <summary>
    /// Candidate vacancy surfaces keep the candidate home redirect and are not sent to the werkgevers page.
    /// </summary>
    public static bool IsCandidateVacancySurface(Type pageType)
    {
        var name = pageType.FullName ?? pageType.Name;
        if (name.Contains(".Pages.Candidate.", StringComparison.Ordinal))
        {
            return true;
        }

        return pageType.Name == "VacancyDetail";
    }
}
