using System.Security.Claims;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Ops;

/// <summary>
/// Vacancy and partner view/click stats are for real visitors.
/// Test accounts and admins are excluded so acceptance sweeps do not inflate them.
/// </summary>
public static class PublicStatsExclusion
{
    public static bool ShouldSkip(User? user, ClaimsPrincipal? principal)
    {
        if (user is { IsTestAccount: true })
        {
            return true;
        }

        if (user?.Role == UserRole.Admin)
        {
            return true;
        }

        return TestDataRules.CanSeeTestData(principal);
    }
}
