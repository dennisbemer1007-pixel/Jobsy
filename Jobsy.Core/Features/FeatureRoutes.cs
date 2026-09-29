using System.Security.Claims;
using Jobsy.Core.Authorization;

namespace Jobsy.Core.Features;

/// <summary>Pure home-path resolution based on auth identity and feature flags.</summary>
public static class FeatureRoutes
{
    public const string OntdekPath = "/ontdek";
    public const string CandidateProfilePath = "/candidate/profile";
    public const string AdminHomePath = "/home";
    public const string EmployersOffAccessDeniedPath = "/access-denied?reason=employers-off";

    public static string HomeFor(ClaimsPrincipal? user, FeatureFlagSnapshot flags)
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
                return "/";
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
            // File 02 switches to /candidate/paspoort when CandidatePassportEnabled.
            return CandidateProfilePath;
        }

        // Employer-side / acquisition roles only
        if (IsEmployerSideOnly(user))
        {
            return EmployersOffAccessDeniedPath;
        }

        return OntdekPath;
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
