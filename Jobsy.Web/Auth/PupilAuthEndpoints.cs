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
            var apiTicket = ReadApiTicket(response);
            if (login is null || string.IsNullOrEmpty(apiTicket))
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

            // The API ticket is protected with Jobsy.Api keys. Keep it opaque and
            // forward it on later api/pupil calls. The Web cookie above is only
            // for this host's authorization.
            PupilApiSessionCookie.Set(http, apiTicket);
            return Results.Redirect(login.RedirectPath);
        }).AllowAnonymous().RequireRateLimiting("pupil-login");

        // POST must not share /leerling/stop with the Blazor page: both match and
        // the pause button returns 500. The confirmation page stays GET /leerling/stop.
        app.MapPost("/leerling/pauze", async (HttpContext http, IAntiforgery antiforgery) =>
        {
            http.Response.Headers.CacheControl = "no-store";
            if (!await antiforgery.IsRequestValidAsync(http))
            {
                // Still sign out — pause must always work on shared Chromebooks.
            }

            var pupil = await http.AuthenticateAsync(PupilAuthDefaults.Scheme);
            var codeId = pupil.Principal?.FindFirst(PupilClaimTypes.PupilCodeId)?.Value;
            http.RequestServices.GetService<PupilApiTicketStore>()?.Remove(codeId);
            await http.SignOutAsync(PupilAuthDefaults.Scheme);
            PupilApiSessionCookie.Clear(http);
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

    private static string? ReadApiTicket(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            return null;
        }

        foreach (var header in values)
        {
            if (PupilApiSessionCookie.TryReadTicket(header, out var value))
            {
                return value;
            }
        }

        return null;
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
