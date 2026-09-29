using System.Security.Claims;
using Jobsy.Core.Authorization;

namespace Jobsy.Web.Security;

/// <summary>Web-side MFA/session claim helpers (same checks as API PersonalDataAccessLogExtensions).</summary>
public static class AdminSessionClaims
{
    public static bool IsMfaVerifiedInSession(ClaimsPrincipal user)
        => string.Equals(
            user.FindFirstValue(JobsyClaimTypes.MfaVerified),
            "1",
            StringComparison.Ordinal);

    public static bool IsExternalAuthMethod(ClaimsPrincipal user)
    {
        var authMethod = user.FindFirstValue("auth_method");
        return !string.IsNullOrWhiteSpace(authMethod)
               && authMethod.StartsWith("external", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsMfaSatisfiedInSession(ClaimsPrincipal user)
        => IsMfaVerifiedInSession(user) || IsExternalAuthMethod(user);
}
