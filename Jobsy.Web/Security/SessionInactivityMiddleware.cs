using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Jobsy.Web.Security;

/// <summary>
/// Enforces admin-configured inactivity timeout for interactive cookie sessions.
/// Tracks last activity in an HttpOnly cookie and signs out when idle.
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
            ClearLastActivity(context);
            await _next(context);
            return;
        }

        var timeoutMinutes = await timeoutProvider.GetInactivityTimeoutMinutesAsync(context.RequestAborted);
        var now = DateTimeOffset.UtcNow;
        var lastActivity = ReadLastActivity(context);

        // Missing activity cookie on an authenticated request is treated as expired so clients
        // cannot reset the idle clock by deleting Jobsy.LastActivity.
        if (lastActivity is null
            || now - lastActivity.Value > TimeSpan.FromMinutes(timeoutMinutes))
        {
            await ExpireSessionAsync(context);
            return;
        }

        WriteLastActivity(context, now);
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

    private static DateTimeOffset? ReadLastActivity(HttpContext context)
    {
        if (!context.Request.Cookies.TryGetValue(LastActivityCookieName, out var raw)
            || string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unix))
        {
            return DateTimeOffset.FromUnixTimeSeconds(unix);
        }

        if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
        {
            return parsed.ToUniversalTime();
        }

        return null;
    }

    private static void WriteLastActivity(HttpContext context, DateTimeOffset utcNow)
    {
        context.Response.Cookies.Append(
            LastActivityCookieName,
            utcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
            BuildCookieOptions(context, utcNow.AddHours(12)));
    }

    private static void ClearLastActivity(HttpContext context)
    {
        if (context.Request.Cookies.ContainsKey(LastActivityCookieName))
        {
            context.Response.Cookies.Delete(LastActivityCookieName, BuildCookieOptions(context, DateTimeOffset.UtcNow));
        }
    }

    private static async Task ExpireSessionAsync(HttpContext context)
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        ClearLastActivity(context);

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
            && !returnUrl.StartsWith("/login", StringComparison.OrdinalIgnoreCase))
        {
            target += "&returnUrl=" + Uri.EscapeDataString(returnUrl);
        }

        context.Response.Redirect(target);
    }

    private static bool IsApiOrJsonRequest(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            return true;
        }

        var accept = context.Request.Headers.Accept.ToString();
        return accept.Contains("application/json", StringComparison.OrdinalIgnoreCase);
    }

    private static CookieOptions BuildCookieOptions(HttpContext context, DateTimeOffset expires)
    {
        var secure = context.Request.IsHttps
                     || string.Equals(
                         context.Request.Headers["X-Forwarded-Proto"],
                         "https",
                         StringComparison.OrdinalIgnoreCase);
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Expires = expires,
            Path = "/"
        };
    }
}

public static class SessionInactivityMiddlewareExtensions
{
    public static IApplicationBuilder UseSessionInactivity(this IApplicationBuilder app)
        => app.UseMiddleware<SessionInactivityMiddleware>();
}
