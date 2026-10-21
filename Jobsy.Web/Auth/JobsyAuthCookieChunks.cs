using System.Globalization;

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
        for (var i = 1; i <= 12; i++)
        {
            DeleteOne(http, cookieName + "C" + i.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static void DeleteOne(HttpContext http, string name)
    {
        http.Response.Cookies.Delete(
            name,
            new CookieOptions
            {
                Path = "/",
                Secure = true,
                SameSite = SameSiteMode.Lax,
                HttpOnly = true
            });
        http.Response.Cookies.Delete(
            name,
            new CookieOptions
            {
                Path = "/",
                SameSite = SameSiteMode.Lax,
                HttpOnly = true
            });
    }
}
