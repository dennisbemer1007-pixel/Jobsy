using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Jobsy.Web.Security;

/// <summary>
/// Enforces admin-configured inactivity timeout for interactive cookie sessions.
/// Tracks last activity in a Data Protection–sealed HttpOnly cookie and signs out when idle.
/// </summary>
public sealed class SessionInactivityMiddleware
{
    public const string LastActivityCookieName = "Jobsy.LastActivity";
    public const string SessionExpiredPath = "/login?error=session-expired";

    private readonly RequestDelegate _next;

    public SessionInactivityMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ISessionTimeoutProvider timeoutProvider)
    {
        if (ShouldSkip(context))
        {
            await _next(context);
            return;
        }

        var user = context.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            SessionActivityCookie.Clear(context);
            await _next(context);
            return;
        }

        var timeoutMinutes = await timeoutProvider.GetInactivityTimeoutMinutesAsync(context.RequestAborted);
        var now = DateTimeOffset.UtcNow;
        var lastActivity = SessionActivityCookie.TryRead(context);

        // Missing / invalid / forged activity cookie on an authenticated request is treated as expired
        // so clients cannot reset or extend the idle clock by mutating Jobsy.LastActivity.
        if (lastActivity is null
            || now - lastActivity.Value > TimeSpan.FromMinutes(timeoutMinutes))
        {
            await ExpireSessionAsync(context);
            return;
        }

        SessionActivityCookie.Stamp(context, now);
        await _next(context);
    }

    private static bool ShouldSkip(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/css", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/js", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/images", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/_blazor", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, "/account/logout", StringComparison.OrdinalIgnoreCase)
            || string.Equals(path, "/account/session-security", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/account/external", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/signin-", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/login", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static async Task ExpireSessionAsync(HttpContext context)
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        SessionActivityCookie.Clear(context);

        if (IsApiOrJsonRequest(context))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers["X-Jobsy-Session"] = "expired";
            return;
        }

        var returnUrl = context.Request.Path + context.Request.QueryString;
        var target = SessionExpiredPath;
        if (!string.IsNullOrWhiteSpace(returnUrl)
            && returnUrl != "/"
            && !returnUrl.StartsWith("/login", StringComparison.OrdinalIgnoreCase)
            && !returnUrl.StartsWith("/account/session-activity", StringComparison.OrdinalIgnoreCase))
        {
            target += "&returnUrl=" + Uri.EscapeDataString(returnUrl);
        }

        context.Response.Redirect(target);
    }

    private static bool IsApiOrJsonRequest(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api")
            || context.Request.Path.StartsWithSegments("/account/session-activity"))
        {
            return true;
        }

        var accept = context.Request.Headers.Accept.ToString();
        return accept.Contains("application/json", StringComparison.OrdinalIgnoreCase);
    }
}

public static class SessionInactivityMiddlewareExtensions
{
    public static IApplicationBuilder UseSessionInactivity(this IApplicationBuilder app)
        => app.UseMiddleware<SessionInactivityMiddleware>();
}
