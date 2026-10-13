using Jobsy.Core.Authorization;

namespace Jobsy.Tests;

/// <summary>
/// Maps <see cref="JobsyPolicies"/> names to role claim values.
/// Kept in sync with <c>AuthorizationExtensions</c> via
/// <see cref="AuthorizationMatrixReflectionTests.Policy_role_map_matches_authorization_options"/>.
/// </summary>
public static class AuthorizationPolicyRoleMap
{
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> PolicyRoles { get; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
        {
            [JobsyPolicies.RequireAdmin] = [JobsyRoles.Admin],
            [JobsyPolicies.RequireEmployer] = JobsyRoles.EmployerRoles,
            [JobsyPolicies.RequireAdminOrEmployer] = [JobsyRoles.Admin, .. JobsyRoles.EmployerRoles],
            [JobsyPolicies.RequireCandidate] = [JobsyRoles.Candidate],
            [JobsyPolicies.RequireSalesManager] = [JobsyRoles.SalesManager],
            [JobsyPolicies.RequireAdminOrSalesManager] = [JobsyRoles.Admin, JobsyRoles.SalesManager],
            [JobsyPolicies.RequireAmbassadeur] = [JobsyRoles.Ambassadeur],
            [JobsyPolicies.RequireAdminOrAmbassadeur] = [JobsyRoles.Admin, JobsyRoles.Ambassadeur],
            [JobsyPolicies.RequireSchoolAdmin] = [JobsyRoles.SchoolAdmin],
            [JobsyPolicies.RequireTeacher] = [JobsyRoles.Teacher],
            [JobsyPolicies.RequireSchoolStaff] = [JobsyRoles.SchoolAdmin, JobsyRoles.Teacher],
            [JobsyPolicies.RequireDashboardAccess] =
            [
                JobsyRoles.Admin,
                JobsyRoles.BranchManager,
                JobsyRoles.RegionalManager,
                JobsyRoles.EnterpriseManager,
                JobsyRoles.Intermediary,
                JobsyRoles.SalesManager,
                JobsyRoles.Ambassadeur
            ],
            // API-key scheme: no Jobsy role claims.
            [JobsyPolicies.RequireApiKey] = Array.Empty<string>()
        };
}
