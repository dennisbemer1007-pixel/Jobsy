using Jobsy.Core.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Web.Auth;

/// <summary>
/// Opaque API parent passport ticket from <c>POST api/parent/passport/session</c>.
/// Forwarded to <c>api/parent/*</c> and <c>GET /ouder/pdf</c> on the API.
/// </summary>
public static class ParentApiSessionCookie
{
    public const string Name = "Lobsy.Ouder.Api";

    public static bool TryReadTicket(string setCookieHeader, out string value)
    {
        value = "";
        if (string.IsNullOrWhiteSpace(setCookieHeader))
        {
            return false;
        }

        var pair = setCookieHeader.Split(';', 2)[0].Trim();
        var eq = pair.IndexOf('=');
        if (eq <= 0)
        {
            return false;
        }

        var name = pair[..eq].Trim();
        if (!string.Equals(name, ParentPassportAuthDefaults.CookieName, StringComparison.Ordinal))
        {
            return false;
        }

        value = pair[(eq + 1)..].Trim();
        if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
        {
            value = value[1..^1];
        }

        return value.Length > 0;
    }

    public static string? Read(HttpContext http)
    {
        if (http.Request.Cookies.TryGetValue(Name, out var value)
            && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return null;
    }

    public static void Set(HttpContext http, string apiTicket)
        => http.Response.Cookies.Append(Name, apiTicket, Options(http));

    public static void Clear(HttpContext http)
        => http.Response.Cookies.Delete(Name, Options(http));

    private static CookieOptions Options(HttpContext http)
    {
        var env = http.RequestServices.GetService<IWebHostEnvironment>();
        var secureAlways = env is not null && !env.IsDevelopment();
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = secureAlways || http.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            IsEssential = true
        };
    }
}
