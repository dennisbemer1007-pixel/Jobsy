using System.Security.Claims;
using Jobsy.Core.Authorization;

namespace Jobsy.Web.Security;

/// <summary>Prevents privileged UI routes from rendering before the interactive MFA check.</summary>
public sealed class MfaEnforcementMiddleware
{
    private readonly RequestDelegate _next;

    public MfaEnforcementMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/admin")
            && context.User.Identity?.IsAuthenticated == true
            && context.User.IsInRole("Admin")
            && !context.User.HasClaim(JobsyClaimTypes.MfaVerified, "1"))
        {
            var destination = Uri.EscapeDataString(context.Request.Path + context.Request.QueryString);
            context.Response.Redirect("/account/mfa?returnUrl=" + destination);
            return;
        }

        await _next(context);
    }
}

public static class MfaEnforcementMiddlewareExtensions
{
    public static IApplicationBuilder UseMfaEnforcement(this IApplicationBuilder app)
        => app.UseMiddleware<MfaEnforcementMiddleware>();
}
