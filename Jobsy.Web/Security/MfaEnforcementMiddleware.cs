using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Core.Ops;
using Jobsy.Core.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Jobsy.Web.Security;

/// <summary>
/// Blocks privileged local-password sessions that lack <c>MfaVerified</c> from
/// reaching protected UI. Stale sessions are signed out and sent to re-login
/// instead of a dead-end MFA prompt without a challenge cookie.
/// </summary>
public sealed class MfaEnforcementMiddleware
{
    private readonly RequestDelegate _next;

    public MfaEnforcementMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsAllowlisted(context.Request.Path))
        {
            await _next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var authMethod = context.User.FindFirstValue("auth_method") ?? string.Empty;
        if (authMethod.StartsWith("external", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var runtime = context.RequestServices.GetService(typeof(ITestAccountsRuntime)) as ITestAccountsRuntime;
        var isTestViewer = TestDataRules.IsTestViewer(context.User);
        if (isTestViewer && runtime?.IsActive == true)
        {
            await _next(context);
            return;
        }

        var roleClaim = context.User.FindFirstValue(ClaimTypes.Role)
                        ?? context.User.FindFirstValue("role");
        if (!Enum.TryParse<UserRole>(roleClaim, ignoreCase: true, out var role)
            || !MfaPolicy.IsRequiredFor(role, isTestViewer, runtime?.IsActive == true))
        {
            await _next(context);
            return;
        }

        if (context.User.HasClaim(JobsyClaimTypes.MfaVerified, "1"))
        {
            await _next(context);
            return;
        }

        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (context.RequestServices is not null
            && context.RequestServices.GetService(typeof(IAuthenticationService)) is not null)
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            DeviceSessionCookie.Clear(context);
        }

        var destination = Uri.EscapeDataString(
            context.Request.Path + context.Request.QueryString);
        context.Response.Redirect("/login?error=mfa-required&returnUrl=" + destination);
    }

    private static bool IsAllowlisted(PathString path)
        => path.StartsWithSegments("/account")
           || path.StartsWithSegments("/login")
           || path.StartsWithSegments("/logout")
           || path.StartsWithSegments("/_blazor")
           || path.StartsWithSegments("/_framework")
           || path.StartsWithSegments("/health")
           || path.StartsWithSegments("/healthz")
           || path.StartsWithSegments("/css")
           || path.StartsWithSegments("/js")
           || path.StartsWithSegments("/lib")
           || path.StartsWithSegments("/icons")
           || path.StartsWithSegments("/images")
           || path.StartsWithSegments("/media")
           || path.StartsWithSegments("/favicon")
           || path.StartsWithSegments("/manifest")
           || path.StartsWithSegments("/service-worker")
           || path.StartsWithSegments("/leerling");
}

public static class MfaEnforcementMiddlewareExtensions
{
    public static IApplicationBuilder UseMfaEnforcement(this IApplicationBuilder app)
        => app.UseMiddleware<MfaEnforcementMiddleware>();
}
