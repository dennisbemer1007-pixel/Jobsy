using Jobsy.Web.Security;
using Microsoft.AspNetCore.DataProtection;

namespace Jobsy.Web.Auth;

/// <summary>
/// Short-lived data-protected cookie so /login can prefill the e-mail after invalid/locked/too-many
/// without putting it in the URL. HttpOnly, Lax, 5 minutes.
/// </summary>
public static class LoginHintCookie
{
    public const string Name = "Jobsy.LoginHint";
    public const string ProtectorPurpose = "Jobsy.LoginHint.v1";

    public static void Set(HttpContext http, IDataProtectionProvider dataProtection, string emailNormalized)
    {
        if (string.IsNullOrWhiteSpace(emailNormalized))
        {
            return;
        }

        var protector = dataProtection.CreateProtector(ProtectorPurpose);
        http.Response.Cookies.Append(
            Name,
            protector.Protect(emailNormalized.Trim()),
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = JobsyCookie.ShouldMarkSecure(http),
                MaxAge = TimeSpan.FromMinutes(5),
                Path = "/"
            });
    }

    public static string? TryReadAndClear(HttpContext http, IDataProtectionProvider dataProtection)
    {
        if (!http.Request.Cookies.TryGetValue(Name, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        Clear(http);
        try
        {
            var protector = dataProtection.CreateProtector(ProtectorPurpose);
            var value = protector.Unprotect(raw);
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
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
