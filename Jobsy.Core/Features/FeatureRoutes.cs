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

            return AdminHomePath;
        }

        // Employers OFF
        if (user?.Identity?.IsAuthenticated != true)
        {
            return OntdekPath;
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.Admin))
        {
            return AdminHomePath;
        }

        if (RoleClaimMatching.HasRole(user, JobsyRoles.Candidate))
        {
            return CandidateHome(flags, passportReady);
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
}
