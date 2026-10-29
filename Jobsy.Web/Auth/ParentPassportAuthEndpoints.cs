using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Core;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Web.Auth;

public static class ParentPassportAuthEndpoints
{
    public static void MapParentPassportAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/ouder/kijk", async (
            HttpContext http,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IAntiforgery antiforgery) =>
        {
            http.Response.Headers.CacheControl = "no-store";

            if (!await antiforgery.IsRequestValidAsync(http))
            {
                return Results.Redirect("/ouder?error=retry");
            }

            var staff = await http.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (staff.Succeeded && staff.Principal?.Identity?.IsAuthenticated == true)
            {
                return Results.Redirect("/ouder?error=staff");
            }

            var form = await http.Request.ReadFormAsync();
            var code = form["code"].ToString();
            var apiBase = JobsyPublicUrl.NormalizeBaseUrl(configuration["ApiBaseUrl"], "http://localhost:5200/");
            var client = httpClientFactory.CreateClient("JobsyAuthProvision");
            client.BaseAddress = new Uri(apiBase);
            client.Timeout = TimeSpan.FromSeconds(15);

            using var response = await client.PostAsJsonAsync(
                "api/parent/passport/session",
                new ParentPassportCodeRequest(code));
            if (!response.IsSuccessStatusCode)
            {
                var err = (int)response.StatusCode == 429 ? "cooldown" : "invalid";
                return Results.Redirect($"/ouder?error={err}");
            }

            var apiTicket = ReadApiTicket(response);
            if (string.IsNullOrEmpty(apiTicket))
            {
                return Results.Redirect("/ouder?error=invalid");
            }

            var body = await response.Content.ReadAsStringAsync();
            ParentPassportSessionDto? session;
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (!doc.RootElement.TryGetProperty("pupilCodeId", out var idEl)
                    || !Guid.TryParse(idEl.GetString(), out var codeId)
                    || !doc.RootElement.TryGetProperty("accessExpiresUnix", out var expEl)
                    || !expEl.TryGetInt64(out var expiresUnix))
                {
                    return Results.Redirect("/ouder?error=invalid");
                }

                session = new ParentPassportSessionDto(codeId, expiresUnix);
            }
            catch (JsonException)
            {
                return Results.Redirect("/ouder?error=invalid");
            }

            var parentAuth = await http.AuthenticateAsync(ParentPassportAuthDefaults.Scheme);
            if (parentAuth.Succeeded)
            {
                await http.SignOutAsync(ParentPassportAuthDefaults.Scheme);
            }

            ParentApiSessionCookie.Set(http, apiTicket);
            var principal = ParentPassportPrincipal.Create(session);
            await http.SignInAsync(
                ParentPassportAuthDefaults.Scheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = false,
                    AllowRefresh = false,
                    ExpiresUtc = DateTimeOffset.FromUnixTimeSeconds(session.AccessExpiresUnix)
                });
            return Results.Redirect("/ouder");
        }).AllowAnonymous().RequireRateLimiting("parent-passport");

        app.MapGet("/ouder/uitloggen", async (HttpContext http) =>
        {
            http.Response.Headers.CacheControl = "no-store";
            await http.SignOutAsync(ParentPassportAuthDefaults.Scheme);
            ParentApiSessionCookie.Clear(http);
            return Results.Redirect("/ouder");
        }).AllowAnonymous();
    }

    private static string? ReadApiTicket(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            return null;
        }

        foreach (var header in values)
        {
            if (ParentApiSessionCookie.TryReadTicket(header, out var value))
            {
                return value;
            }
        }

        return null;
    }
}
