using System.Security.Claims;
using Jobsy.Core.Authorization;

namespace Jobsy.Core.Authorization;

/// <summary>
/// Role claim matching for Entra/OIDC (ClaimTypes.Role, "roles", namespaced /roles).
/// </summary>
public static class RoleClaimMatching
{
    public static bool HasRole(ClaimsPrincipal user, string role) =>
        user.IsInRole(role)
        || user.Claims.Any(c =>
            (c.Type == ClaimTypes.Role
             || c.Type == "roles"
             || c.Type == "role"
             || c.Type.EndsWith("/roles", StringComparison.OrdinalIgnoreCase))
            && string.Equals(c.Value, role, StringComparison.OrdinalIgnoreCase));

    public static bool HasAnyRole(ClaimsPrincipal user, params string[] roles) =>
        roles.Any(r => HasRole(user, r));
}
