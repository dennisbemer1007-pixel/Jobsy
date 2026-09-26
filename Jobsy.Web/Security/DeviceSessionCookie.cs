using System.Security.Claims;
using Jobsy.Core.Rules;

namespace Jobsy.Web.Security;

public static class DeviceSessionCookie
{
    public static void Set(HttpContext http, string refreshToken, DateTime expiresAtUtc)
    {
        var env = http.RequestServices.GetRequiredService<IHostEnvironment>();
        http.Response.Cookies.Append(
            DeviceSessionRules.CookieName,
            refreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = !env.IsDevelopment(),
                SameSite = SameSiteMode.Lax,
                Path = "/",
                IsEssential = true,
                MaxAge = DeviceSessionRules.Lifetime,
                Expires = new DateTimeOffset(expiresAtUtc, TimeSpan.Zero)
            });
    }

    public static string? Read(HttpContext http)
        => http.Request.Cookies.TryGetValue(DeviceSessionRules.CookieName, out var value)
            ? value
            : null;

    public static void Clear(HttpContext http)
    {
        http.Response.Cookies.Delete(
            DeviceSessionRules.CookieName,
            new CookieOptions
            {
                Path = "/",
                Secure = true,
                SameSite = SameSiteMode.Lax,
                HttpOnly = true
            });
        // Also clear without Secure for Development cookies.
        http.Response.Cookies.Delete(
            DeviceSessionRules.CookieName,
            new CookieOptions
            {
                Path = "/",
                SameSite = SameSiteMode.Lax,
                HttpOnly = true
            });
    }

    public static bool HasDeviceSessionClaim(ClaimsPrincipal? user)
        => user?.HasClaim(c =>
               c.Type == Jobsy.Core.Authorization.JobsyClaimTypes.HasDeviceSession
               && c.Value == "1") == true
           || user?.HasClaim(c => c.Type == Jobsy.Core.Authorization.JobsyClaimTypes.DeviceSessionId) == true;
}
