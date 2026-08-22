using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.DataProtection;

namespace Jobsy.Web.Security;

/// <summary>
/// Issues and validates an HMAC-protected (Data Protection) last-activity cookie
/// bound to the authenticated subject so clients cannot forge idle timestamps.
/// </summary>
public static class SessionActivityCookie
{
    public const string ProtectorPurpose = "Jobsy.Web.SessionActivity.v1";
    private static readonly TimeSpan FutureSkew = TimeSpan.FromMinutes(2);

    public static void Stamp(HttpContext http, DateTimeOffset utcNow)
    {
        var subject = ResolveSubject(http.User);
        if (string.IsNullOrEmpty(subject))
        {
            return;
        }

        var protector = http.RequestServices
            .GetRequiredService<IDataProtectionProvider>()
            .CreateProtector(ProtectorPurpose);
        var payload = subject + "|" + utcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        var protectedValue = protector.Protect(payload);

        http.Response.Cookies.Append(
            SessionInactivityMiddleware.LastActivityCookieName,
            protectedValue,
            BuildCookieOptions(http, utcNow.AddHours(12)));
    }

    public static DateTimeOffset? TryRead(HttpContext http)
    {
        if (!http.Request.Cookies.TryGetValue(SessionInactivityMiddleware.LastActivityCookieName, out var raw)
            || string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var subject = ResolveSubject(http.User);
        if (string.IsNullOrEmpty(subject))
        {
            return null;
        }

        try
        {
            var protector = http.RequestServices
                .GetRequiredService<IDataProtectionProvider>()
                .CreateProtector(ProtectorPurpose);
            var payload = protector.Unprotect(raw);
            var parts = payload.Split('|', 2);
            if (parts.Length != 2)
            {
                return null;
            }

            if (!string.Equals(parts[0], subject, StringComparison.Ordinal))
            {
                return null;
            }

            if (!long.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var unix))
            {
                return null;
            }

            var stamped = DateTimeOffset.FromUnixTimeSeconds(unix);
            var now = DateTimeOffset.UtcNow;
            // Reject forged future timestamps beyond a small clock-skew allowance.
            if (stamped > now.Add(FutureSkew))
            {
                return null;
            }

            return stamped;
        }
        catch
        {
            // Tampered / legacy plaintext cookie / key-ring mismatch.
            return null;
        }
    }

    public static void Clear(HttpContext http)
    {
        http.Response.Cookies.Delete(
            SessionInactivityMiddleware.LastActivityCookieName,
            BuildCookieOptions(http, DateTimeOffset.UtcNow));
    }

    public static string? ResolveSubject(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        return user.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? user.FindFirstValue(ClaimTypes.Email)
               ?? user.FindFirstValue("email")
               ?? user.Identity?.Name;
    }

    private static CookieOptions BuildCookieOptions(HttpContext context, DateTimeOffset expires)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = JobsyCookie.ShouldMarkSecure(context),
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Expires = expires,
            Path = "/"
        };
    }
}
