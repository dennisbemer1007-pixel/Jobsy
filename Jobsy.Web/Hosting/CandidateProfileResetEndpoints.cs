using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Core.Contracts;
using Jobsy.Core.Privacy;
using Jobsy.Web.Auth;
using Jobsy.Web.Security;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Web.Hosting;

/// <summary>
/// BFF for candidate profile reset: antiforgery + rate limit, then forward to API with JWT.
/// </summary>
public static class CandidateProfileResetEndpoints
{
    public const string HttpClientName = "JobsyCandidateProfileReset";
    public const string ResetPath = "/candidate/profile/reset";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static IEndpointRouteBuilder MapCandidateProfileResetEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost(ResetPath, async (
            HttpContext http,
            ResetCandidateProfileRequest body,
            IAntiforgery antiforgery,
            IHttpClientFactory httpClientFactory,
            JobsyAccessTokenIssuer accessTokens,
            ILoggerFactory loggerFactory) =>
        {
            if (!await antiforgery.IsRequestValidAsync(http))
            {
                return PrivacyErrorJson(
                    "Ongeldige sessie. Vernieuw de pagina en probeer opnieuw.",
                    StatusCodes.Status400BadRequest);
            }

            var clientIp = TrustedClientIp.Resolve(http);
            var jwt = accessTokens.TryCreate(http.User, clientIp);
            if (string.IsNullOrWhiteSpace(jwt))
            {
                var code = PrivacyOperationErrors.CreateSupportCode();
                loggerFactory
                    .CreateLogger("Jobsy.Web.CandidateProfileReset")
                    .LogError(
                        "Profile reset could not mint API token for authenticated user ({SupportCode})",
                        code);
                return PrivacyErrorJson(PrivacyOperationErrors.GenericMessageNl, StatusCodes.Status502BadGateway, code);
            }

            try
            {
                var client = httpClientFactory.CreateClient(HttpClientName);
                using var request = new HttpRequestMessage(HttpMethod.Post, "api/privacy/reset-candidate-profile");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
                if (http.Request.Headers.TryGetValue("X-Jobsy-Language", out var lang))
                {
                    request.Headers.TryAddWithoutValidation("X-Jobsy-Language", lang.ToString());
                }

                request.Content = JsonContent.Create(body);
                using var response = await client.SendAsync(request, http.RequestAborted);
                var json = await response.Content.ReadAsStringAsync(http.RequestAborted);
                if (!response.IsSuccessStatusCode)
                {
                    if (TryReadPrivacyError(json, out var message, out var supportCode))
                    {
                        return PrivacyErrorJson(
                            message ?? PrivacyOperationErrors.GenericMessageNl,
                            (int)response.StatusCode,
                            supportCode);
                    }

                    var fallbackCode = PrivacyOperationErrors.CreateSupportCode();
                    loggerFactory
                        .CreateLogger("Jobsy.Web.CandidateProfileReset")
                        .LogError(
                            "Profile reset API returned {StatusCode} without privacy payload ({SupportCode}) body={Body}",
                            (int)response.StatusCode,
                            fallbackCode,
                            json);
                    return PrivacyErrorJson(
                        PrivacyOperationErrors.GenericMessageNl,
                        (int)response.StatusCode,
                        fallbackCode);
                }

                var result = JsonSerializer.Deserialize<ResetCandidateProfileResponse>(json, JsonOptions);
                return result is null
                    ? PrivacyErrorJson(PrivacyOperationErrors.GenericMessageNl, StatusCodes.Status502BadGateway)
                    : Results.Ok(result);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                var supportCode = PrivacyOperationErrors.CreateSupportCode();
                loggerFactory
                    .CreateLogger("Jobsy.Web.CandidateProfileReset")
                    .LogError(ex, "Profile reset forward to API failed ({SupportCode})", supportCode);
                return PrivacyErrorJson(
                    PrivacyOperationErrors.GenericMessageNl,
                    StatusCodes.Status502BadGateway,
                    supportCode);
            }
        })
        .RequireAuthorization()
        .RequireRateLimiting("auth");

        return app;
    }

    private static IResult PrivacyErrorJson(string message, int statusCode, string? supportCode = null)
    {
        supportCode ??= PrivacyOperationErrors.CreateSupportCode();
        return Results.Json(
            new
            {
                message,
                supportCode,
                referenceCode = supportCode
            },
            statusCode: statusCode);
    }

    private static bool TryReadPrivacyError(string json, out string? message, out string? supportCode)
    {
        message = null;
        supportCode = null;
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("message", out var msg))
            {
                message = msg.GetString();
            }

            if (doc.RootElement.TryGetProperty("supportCode", out var sc))
            {
                supportCode = sc.GetString();
            }
            else if (doc.RootElement.TryGetProperty("referenceCode", out var rc))
            {
                supportCode = rc.GetString();
            }

            return !string.IsNullOrWhiteSpace(message) || !string.IsNullOrWhiteSpace(supportCode);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
