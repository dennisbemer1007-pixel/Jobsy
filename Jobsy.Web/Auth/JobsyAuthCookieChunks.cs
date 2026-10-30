using System.Globalization;
using Jobsy.Web.Security;

namespace Jobsy.Web.Auth;

/// <summary>
/// Clears chunked <c>Jobsy.Auth</c> cookies before re-issuing a ticket.
/// Orphan chunks after repeated <see cref="Microsoft.AspNetCore.Authentication.SignInAsync"/>
/// can inflate the browser Cookie header until the proxy returns 502.
/// </summary>
public static class JobsyAuthCookieChunks
{
    public const string DefaultCookieName = "Jobsy.Auth";

    public static void DeleteAll(HttpContext http, string cookieName = DefaultCookieName)
    {
        DeleteOne(http, cookieName);
        for (var i = 1; i <= 20; i++)
        {
            DeleteOne(http, cookieName + "C" + i.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static void DeleteOne(HttpContext http, string name)
    {
        var secure = JobsyCookie.ShouldMarkSecure(http);
        var opts = new CookieOptions
        {
            Path = "/",
            SameSite = SameSiteMode.Lax,
            HttpOnly = true,
            Secure = secure,
            Expires = DateTimeOffset.UnixEpoch
        };
        http.Response.Cookies.Delete(name, opts);
        http.Response.Cookies.Append(name, string.Empty, opts);
        if (secure)
        {
            var devOpts = new CookieOptions
            {
                Path = "/",
                SameSite = SameSiteMode.Lax,
                HttpOnly = true,
                Secure = false,
                Expires = DateTimeOffset.UnixEpoch
            };
            http.Response.Cookies.Delete(name, devOpts);
            http.Response.Cookies.Append(name, string.Empty, devOpts);
        }
    }
}
