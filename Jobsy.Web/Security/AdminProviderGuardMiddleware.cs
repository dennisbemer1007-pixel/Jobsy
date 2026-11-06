using System.Security.Claims;
using Jobsy.Core.Enums;
using Jobsy.Core.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Jobsy.Web.Security;

/// <summary>
/// Signs out Admin principals that authenticated via a disallowed external provider
/// (Google or personal Microsoft).
/// </summary>
public sealed class AdminProviderGuardMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public AdminProviderGuardMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var roleClaim = context.User.FindFirstValue(ClaimTypes.Role)
                            ?? context.User.FindFirstValue("role");
            if (Enum.TryParse<UserRole>(roleClaim, ignoreCase: true, out var role)
                && role == UserRole.Admin)
            {
                var authMethod = context.User.FindFirstValue("auth_method") ?? string.Empty;
                string? provider = null;
                if (authMethod.StartsWith("external:", StringComparison.OrdinalIgnoreCase))
                {
                    provider = authMethod["external:".Length..];
                }

                var tid = context.User.FindFirstValue("idp_tid");
                var allowed = AdminLoginProviderPolicy.ParseAllowedTenants(
                    _configuration["JobsyAuth:AdminAllowedEntraTenants"]);
                if (!AdminLoginProviderPolicy.IsAllowed(role, provider, tid, allowed))
                {
                    if (context.Request.Path.StartsWithSegments("/api"))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return;
                    }

                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    DeviceSessionCookie.Clear(context);
                    context.Response.Redirect("/login?error=admin-provider");
                    return;
                }
            }
        }

        await _next(context);
    }
}

public static class AdminProviderGuardMiddlewareExtensions
{
    public static IApplicationBuilder UseAdminProviderGuard(this IApplicationBuilder app)
        => app.UseMiddleware<AdminProviderGuardMiddleware>();
}
