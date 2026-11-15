using Jobsy.Core.Authorization;
using Microsoft.AspNetCore.DataProtection;

namespace Jobsy.Web.Auth;

public static class WerkgeverApkApiSessionCookie
{
    public const string Name = "Lobsy.LoopbaanApk.Api";

    public static void Set(HttpContext http, string protectedTicket)
        => http.Response.Cookies.Append(Name, protectedTicket, new CookieOptions
        {
            HttpOnly = true,
            Secure = http.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            IsEssential = true
        });

    public static string? Read(HttpContext http)
        => http.Request.Cookies.TryGetValue(Name, out var value) ? value : null;

    public static void Clear(HttpContext http)
        => http.Response.Cookies.Delete(Name, new CookieOptions { Path = "/" });

    public static bool TryReadTicket(string setCookieHeader, out string ticket)
    {
        ticket = "";
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
        if (!string.Equals(name, WerkgeverApkAuthDefaults.CookieName, StringComparison.Ordinal))
        {
            return false;
        }

        ticket = pair[(eq + 1)..].Trim();
        if (ticket.Length >= 2 && ticket[0] == '"' && ticket[^1] == '"')
        {
            ticket = ticket[1..^1];
        }

        return ticket.Length > 0;
    }
}
