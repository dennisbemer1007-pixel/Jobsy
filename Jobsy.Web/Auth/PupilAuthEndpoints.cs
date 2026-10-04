using System.Globalization;
using System.Net.Http.Json;
using System.Security.Claims;
using Jobsy.Core;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.Scholen;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;
using Jobsy.Web.Services;

namespace Jobsy.Web.Auth;

/// <summary>SSR pupil login + stop endpoints (cookie lives on the Web host).</summary>
public static class PupilAuthEndpoints
{
    public static void MapPupilAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/leerling/login", async (
            HttpContext http,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IAntiforgery antiforgery) =>
        {
            http.Response.Headers.CacheControl = "no-store";

            if (!await antiforgery.IsRequestValidAsync(http))
            {
                return Results.Redirect("/leerling?error=retry");
            }

            // Staff/candidate already signed in → no mixing.
            var staff = await http.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (staff.Succeeded && staff.Principal?.Identity?.IsAuthenticated == true)
            {
                return Results.Redirect("/leerling?error=staff");
            }

            var form = await http.Request.ReadFormAsync();
            if (!Guid.TryParse(form["schoolId"], out var schoolId)
                || !Guid.TryParse(form["classId"], out var classId))
            {
                return Results.Redirect("/leerling?error=invalid");
            }

            var code = form["code"].ToString();
            var apiBase = JobsyPublicUrl.NormalizeBaseUrl(configuration["ApiBaseUrl"], "http://localhost:5200/");
            var client = httpClientFactory.CreateClient("JobsyAuthProvision");
            client.BaseAddress = new Uri(apiBase);
            client.Timeout = TimeSpan.FromSeconds(15);

            using var response = await client.PostAsJsonAsync(
                "api/pupil/login",
                new PupilLoginRequest(schoolId, classId, code));
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                var err = "invalid";
                if ((int)response.StatusCode == 429)
                {
                    err = "cooldown";
                }
                else if ((int)response.StatusCode == 409)
                {
                    err = "window";
                }

                var qs = QuestionSetQuerySuffix(body);
                return Results.Redirect(
                    $"/leerling?error={err}&schoolId={schoolId:D}&classId={classId:D}{qs}");
            }

            var login = JsonSerializer.Deserialize<PupilLoginResponse>(body, JobsyApiClient.ApiJson);
            if (login is null)
            {
                return Results.Redirect("/leerling?error=invalid");
            }

            var principal = CreatePrincipal(login);
            await http.SignInAsync(
                PupilAuthDefaults.Scheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = false,
                    AllowRefresh = true,
                    ExpiresUtc = null
                });

            return Results.Redirect(login.RedirectPath);
        }).AllowAnonymous().RequireRateLimiting("pupil-login");

        app.MapPost("/leerling/stop", async (HttpContext http, IAntiforgery antiforgery) =>
        {
            http.Response.Headers.CacheControl = "no-store";
            if (!await antiforgery.IsRequestValidAsync(http))
            {
                // Still sign out — pause must always work on shared Chromebooks.
            }

            await http.SignOutAsync(PupilAuthDefaults.Scheme);
            var form = await http.Request.ReadFormAsync();
            var part = form["part"].ToString();
            if (string.Equals(part, "1", StringComparison.Ordinal))
            {
                return Results.Redirect("/leerling/stop?done=deel1");
            }

            return Results.Redirect("/leerling/stop?done=1");
        }).AllowAnonymous();
    }

    public static ClaimsPrincipal CreatePrincipal(PupilLoginResponse login)
    {
        var iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            .ToString(CultureInfo.InvariantCulture);
        var claims = new List<Claim>
        {
            new(PupilClaimTypes.PupilCodeId, login.PupilCodeId.ToString("D")),
            new(PupilClaimTypes.ClassId, login.ClassId.ToString("D")),
            new(PupilClaimTypes.SchoolId, login.SchoolId.ToString("D")),
            new(PupilClaimTypes.SessionVersion, login.SessionVersion.ToString(CultureInfo.InvariantCulture)),
            new(PupilClaimTypes.IssuedAt, iat),
            new(PupilClaimTypes.ClassLabel, login.ClassLabel),
            new(PupilClaimTypes.CodeDisplay, login.CodeDisplay),
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, PupilAuthDefaults.Scheme));
    }

    private static string QuestionSetQuerySuffix(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("questionSet", out var qs)
                && qs.ValueKind is JsonValueKind.String or JsonValueKind.Number)
            {
                var raw = qs.ValueKind == JsonValueKind.Number
                    ? qs.GetInt32().ToString(CultureInfo.InvariantCulture)
                    : qs.GetString();
                if (!string.IsNullOrWhiteSpace(raw)
                    && (string.Equals(raw, "Vo", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(raw, "2", StringComparison.Ordinal)))
                {
                    return "&questionSet=Vo";
                }

                if (!string.IsNullOrWhiteSpace(raw)
                    && (string.Equals(raw, "Groep78", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(raw, "1", StringComparison.Ordinal)))
                {
                    return "&questionSet=Groep78";
                }
            }
        }
        catch (JsonException)
        {
            // Fall through — login page defaults to G78 copy.
        }

        return string.Empty;
    }
}
