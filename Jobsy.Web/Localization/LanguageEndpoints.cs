using Jobsy.Core.Localization;
using Jobsy.Core.Privacy;
using Jobsy.Web.Auth;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace Jobsy.Web.Localization;

public static class LanguageEndpoints
{
    public const string CookieName = CultureState.CookieName;
    public const int CookieMaxAgeSeconds = 60 * 60 * 24 * 365;

    public static void MapLanguageEndpoints(this WebApplication app)
    {
        app.MapGet("/taal/{lang}", (HttpContext http, string lang, [FromQuery] string? returnUrl) =>
            {
                var primary = (lang ?? "").Trim().Replace('_', '-').Split('-', 2)[0].ToLowerInvariant();
                if (!CultureRequest.TrySupportedCode(primary, out var normalized))
                {
                    // Spec: invalid lang → 400 / "/". Prefer a safe home redirect for browsers.
                    return Results.Redirect("/");
                }
                var secure = http.Request.IsHttps;
                http.Response.Cookies.Append(
                    CookieName,
                    normalized,
                    new CookieOptions
                    {
                        Path = "/",
                        MaxAge = TimeSpan.FromSeconds(CookieMaxAgeSeconds),
                        SameSite = SameSiteMode.Lax,
                        Secure = secure,
                        IsEssential = true,
                        HttpOnly = false
                    });

                http.Response.Headers.CacheControl = "no-store";
                http.Response.Headers["X-Robots-Tag"] = "noindex, nofollow";

                var target = AuthRedirects.IsLocalReturnUrl(returnUrl) ? returnUrl! : "/";
                return Results.Redirect(target);
            })
            .AllowAnonymous()
            .WithDisplayName("SetCulture");
    }
}

public static class CookieConsentEndpoints
{
    public static void MapCookieConsentEndpoints(this WebApplication app)
    {
        app.MapPost("/account/cookie-consent/analytics-token", (
                HttpContext http,
                ICookieConsentTokenService tokens) =>
            {
                if (!IsSameOrigin(http))
                {
                    return Results.StatusCode(StatusCodes.Status403Forbidden);
                }

                var token = tokens.MintAnalyticsToken();
                return Results.Json(new { token });
            })
            .AllowAnonymous()
            .DisableAntiforgery()
            .WithDisplayName("CookieConsentAnalyticsToken");
    }

    internal static bool IsSameOrigin(HttpContext http)
    {
        var secFetchSite = http.Request.Headers["Sec-Fetch-Site"].ToString();
        if (!string.IsNullOrEmpty(secFetchSite)
            && !secFetchSite.Equals("same-origin", StringComparison.OrdinalIgnoreCase)
            && !secFetchSite.Equals("none", StringComparison.OrdinalIgnoreCase)
            && !secFetchSite.Equals("same-site", StringComparison.OrdinalIgnoreCase))
        {
            // Cross-site navigations / embeds are rejected.
            if (secFetchSite.Equals("cross-site", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        var origin = http.Request.Headers.Origin.ToString();
        if (string.IsNullOrWhiteSpace(origin))
        {
            // Non-browser or same-origin navigation without Origin — allow when Sec-Fetch-Site is ok.
            return string.IsNullOrEmpty(secFetchSite)
                   || secFetchSite.Equals("same-origin", StringComparison.OrdinalIgnoreCase)
                   || secFetchSite.Equals("none", StringComparison.OrdinalIgnoreCase)
                   || secFetchSite.Equals("same-site", StringComparison.OrdinalIgnoreCase);
        }

        if (!Uri.TryCreate(origin, UriKind.Absolute, out var originUri))
        {
            return false;
        }

        var request = http.Request;
        var expectedHost = request.Host.Host;
        if (!string.Equals(originUri.Host, expectedHost, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var expectedScheme = request.Scheme;
        return string.Equals(originUri.Scheme, expectedScheme, StringComparison.OrdinalIgnoreCase);
    }
}
