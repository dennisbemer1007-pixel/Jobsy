using System.Text.Json;
using Jobsy.Web.Security;
using Microsoft.AspNetCore.DataProtection;

namespace Jobsy.Web.Auth;

/// <summary>Short-lived HttpOnly cookie binding the e-mail-code flow to this browser.</summary>
public static class EmailCodeCookie
{
    public const string Name = "Jobsy.EmailCode";
    public const string ProtectorPurpose = "Jobsy.EmailCode.v1";

    public sealed record Payload(Guid ChallengeId, string EmailNormalized, string? ReturnUrl, string? Van);

    public static void Set(HttpContext http, IDataProtectionProvider dataProtection, Payload payload)
    {
        var protector = dataProtection.CreateProtector(ProtectorPurpose);
        var json = JsonSerializer.Serialize(payload);
        http.Response.Cookies.Append(
            Name,
            protector.Protect(json),
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = JobsyCookie.ShouldMarkSecure(http),
                MaxAge = TimeSpan.FromMinutes(10),
                Path = "/"
            });
    }

    public static Payload? TryRead(HttpContext http, IDataProtectionProvider dataProtection)
    {
        if (!http.Request.Cookies.TryGetValue(Name, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        try
        {
            var protector = dataProtection.CreateProtector(ProtectorPurpose);
            var json = protector.Unprotect(raw);
            return JsonSerializer.Deserialize<Payload>(json);
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

    public static string MaskEmail(string emailNormalized)
    {
        if (string.IsNullOrWhiteSpace(emailNormalized))
        {
            return "•••";
        }

        var at = emailNormalized.IndexOf('@');
        if (at <= 0)
        {
            return "•••";
        }

        var local = emailNormalized[..at];
        var domain = emailNormalized[(at + 1)..];
        var visible = local.Length == 0 ? "•" : local[0].ToString();
        return $"{visible}•••@{domain}";
    }
}
