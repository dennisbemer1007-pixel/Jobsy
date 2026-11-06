using Jobsy.Web.Security;
using Microsoft.AspNetCore.DataProtection;

namespace Jobsy.Web.Auth;

/// <summary>HttpOnly cookie wrapping user id + raw MFA trust token (30 days).</summary>
public static class MfaTrustCookie
{
    public const string Name = "Jobsy.MfaTrust";
    public const string ProtectorPurpose = "Jobsy.MfaTrust.v1";

    public static void Set(
        HttpContext http,
        IDataProtectionProvider dataProtection,
        Guid userId,
        string rawToken)
    {
        var protector = dataProtection.CreateProtector(ProtectorPurpose);
        var payload = $"{userId:D}|{rawToken}";
        http.Response.Cookies.Append(
            Name,
            protector.Protect(payload),
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = JobsyCookie.ShouldMarkSecure(http),
                MaxAge = TimeSpan.FromDays(30),
                Path = "/"
            });
    }

    public static string? TryReadRawToken(
        HttpContext http,
        IDataProtectionProvider dataProtection,
        out Guid userId)
    {
        userId = Guid.Empty;
        if (!http.Request.Cookies.TryGetValue(Name, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        try
        {
            var protector = dataProtection.CreateProtector(ProtectorPurpose);
            var payload = protector.Unprotect(raw);
            var pipe = payload.IndexOf('|');
            if (pipe <= 0 || pipe == payload.Length - 1)
            {
                return null;
            }

            if (!Guid.TryParse(payload[..pipe], out userId))
            {
                return null;
            }

            return payload[(pipe + 1)..];
        }
        catch
        {
            return null;
        }
    }

    public static void Clear(HttpContext http)
    {
        http.Response.Cookies.Delete(Name, new CookieOptions { Path = "/", Secure = true });
        http.Response.Cookies.Delete(Name, new CookieOptions { Path = "/" });
    }
}
