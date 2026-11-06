using System.Security.Claims;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Ops;

/// <summary>Central helpers for the test ↔ real boundary (claims + visibility).</summary>
public static class TestDataRules
{
    public const string BoundaryErrorCode = "test_account_boundary";

    public static bool IsTestViewer(ClaimsPrincipal? principal)
        => principal?.HasClaim(TestAccountClaimTypes.TestAccount, TestAccountClaimTypes.TestAccountValue) == true;

    public static bool CanSeeTestData(ClaimsPrincipal? principal)
    {
        if (IsTestViewer(principal))
        {
            return true;
        }

        var role = principal?.FindFirst(ClaimTypes.Role)?.Value
                   ?? principal?.FindFirst("role")?.Value;
        return string.Equals(role, nameof(UserRole.Admin), StringComparison.OrdinalIgnoreCase)
               || principal?.IsInRole(nameof(UserRole.Admin)) == true;
    }

    public static bool IsBoundaryViolation(bool actorIsTest, bool targetIsTest)
        => actorIsTest != targetIsTest;
}
