using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Jobsy.Core;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.WerkgeverApk;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Web.Auth;

public static class WerkgeverApkEmployeeAuthEndpoints
{
    public static void MapWerkgeverApkEmployeeAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/loopbaan-apk/login", async (
            HttpContext http,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IAntiforgery antiforgery) =>
        {
            http.Response.Headers.CacheControl = "no-store";

            if (!await antiforgery.IsRequestValidAsync(http))
            {
                return Results.Redirect("/loopbaan-apk?error=retry");
            }

            var staff = await http.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (staff.Succeeded && staff.Principal?.Identity?.IsAuthenticated == true)
            {
                return Results.Redirect("/loopbaan-apk?error=staff");
            }

            var form = await http.Request.ReadFormAsync();
            if (!Guid.TryParse(form["companyId"], out var companyId)
                || !Guid.TryParse(form["teamId"], out var teamId))
            {
                return Results.Redirect("/loopbaan-apk?error=invalid");
            }

            var code = form["code"].ToString();
            var apiBase = JobsyPublicUrl.NormalizeBaseUrl(configuration["ApiBaseUrl"], "http://localhost:5200/");
            var client = httpClientFactory.CreateClient("JobsyAuthProvision");
            client.BaseAddress = new Uri(apiBase);

            using var response = await client.PostAsJsonAsync(
                "api/werkgever-apk/employee/login",
                new WerkgeverApkEmployeeLoginRequest(companyId, teamId, code));
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                return Results.Redirect("/loopbaan-apk?error=invalid");
            }

            var login = JsonSerializer.Deserialize<WerkgeverApkEmployeeLoginResponse>(body, JobsyApiClient.ApiJson);
            var apiTicket = ReadApiTicket(response);
            if (login is null || string.IsNullOrEmpty(apiTicket))
            {
                return Results.Redirect("/loopbaan-apk?error=invalid");
            }

            var principal = CreatePrincipal(login);
            await http.SignInAsync(
                WerkgeverApkAuthDefaults.Scheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = false,
                    AllowRefresh = true,
                    ExpiresUtc = null
                });

            WerkgeverApkApiSessionCookie.Set(http, apiTicket);
            return Results.Redirect(login.RedirectPath);
        }).AllowAnonymous().RequireRateLimiting("pupil-login");
    }

    private static ClaimsPrincipal CreatePrincipal(WerkgeverApkEmployeeLoginResponse login)
    {
        var iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
        var claims = new List<Claim>
        {
            new(WerkgeverApkClaimTypes.EmployeeCodeId, login.EmployeeCodeId.ToString("D")),
            new(WerkgeverApkClaimTypes.TeamId, login.TeamId.ToString("D")),
            new(WerkgeverApkClaimTypes.CompanyId, login.CompanyId.ToString("D")),
            new(WerkgeverApkClaimTypes.SessionVersion, login.SessionVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new(WerkgeverApkClaimTypes.IssuedAt, iat),
            new(WerkgeverApkClaimTypes.CodeDisplay, login.CodeDisplay),
            new(WerkgeverApkClaimTypes.TeamName, login.TeamName),
            new(WerkgeverApkClaimTypes.CompanyName, login.CompanyName),
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, WerkgeverApkAuthDefaults.Scheme));
    }

    private static string? ReadApiTicket(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            return null;
        }

        foreach (var header in values)
        {
            if (!header.StartsWith(WerkgeverApkAuthDefaults.CookieName + "=", StringComparison.Ordinal))
            {
                continue;
            }

            var part = header.Split(';')[0];
            return part[(WerkgeverApkAuthDefaults.CookieName.Length + 1)..];
        }

        return null;
    }
}