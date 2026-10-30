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
                return Results.BadRequest(new { message = "Ongeldige sessie. Vernieuw de pagina en probeer opnieuw." });
            }

            var clientIp = TrustedClientIp.Resolve(http);
            var jwt = accessTokens.TryCreate(http.User, clientIp);
            if (string.IsNullOrWhiteSpace(jwt))
            {
                return Results.Unauthorized();
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
                    try
                    {
                        using var doc = JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty("message", out var msg))
                        {
                            return Results.Json(new { message = msg.GetString() }, statusCode: (int)response.StatusCode);
                        }
                    }
                    catch (JsonException)
                    {
                        // fall through
                    }

                    var refCode = PrivacyOperationErrors.ShortReference(http.TraceIdentifier);
                    loggerFactory
                        .CreateLogger("Jobsy.Web.CandidateProfileReset")
                        .LogError(
                            "Profile reset API returned {StatusCode} without message (ref {ReferenceCode})",
                            (int)response.StatusCode,
                            refCode);
                    return Results.Json(
                        new
                        {
                            message = PrivacyOperationErrors.GenericMessageNl,
                            referenceCode = refCode
                        },
                        statusCode: (int)response.StatusCode);
                }

                var result = JsonSerializer.Deserialize<ResetCandidateProfileResponse>(json, JsonOptions);
                return result is null
                    ? Results.StatusCode(502)
                    : Results.Ok(result);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                var refCode = PrivacyOperationErrors.ShortReference(http.TraceIdentifier);
                loggerFactory
                    .CreateLogger("Jobsy.Web.CandidateProfileReset")
                    .LogError(ex, "Profile reset forward to API failed (ref {ReferenceCode})", refCode);
                return Results.Json(
                    new
                    {
                        message = PrivacyOperationErrors.GenericMessageNl,
                        referenceCode = refCode
                    },
                    statusCode: StatusCodes.Status502BadGateway);
            }
        })
        .RequireAuthorization()
        .RequireRateLimiting("auth");

        return app;
    }
}
