using System.Net.Http.Headers;
using Jobsy.Web.Auth;
using Jobsy.Web.Security;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Web.Hosting;

/// <summary>
/// Browser-direct PDF downloads (no base64 through Blazor). Mobile Safari/PWA friendly.
/// </summary>
public static class CandidatePassportPdfEndpoints
{
    public const string LobsyCvPath = "/candidate/download/lobsy-cv.pdf";
    public const string HttpClientName = "JobsyCandidatePassportPdf";

    public static IEndpointRouteBuilder MapCandidatePassportPdfEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(LobsyCvPath, async (
            HttpContext http,
            IHttpClientFactory httpClientFactory,
            JobsyAccessTokenIssuer accessTokens) =>
        {
            var clientIp = TrustedClientIp.Resolve(http);
            var jwt = accessTokens.TryCreate(http.User, clientIp);
            if (string.IsNullOrWhiteSpace(jwt))
            {
                return Results.Unauthorized();
            }

            var client = httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/me/lobsy-cv.pdf");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, http.RequestAborted);
            if (!response.IsSuccessStatusCode)
            {
                return Results.StatusCode((int)response.StatusCode);
            }

            var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                           ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                           ?? $"Lobsy-CV-{DateTime.UtcNow:yyyyMMdd}.pdf";
            http.Response.Headers.CacheControl = "no-store";
            var stream = await response.Content.ReadAsStreamAsync(http.RequestAborted);
            return Results.File(stream, "application/pdf", fileName);
        })
        .RequireAuthorization()
        .RequireRateLimiting("export");

        return app;
    }
}
