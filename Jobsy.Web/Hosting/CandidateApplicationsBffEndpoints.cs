using System.Net.Http.Headers;
using Jobsy.Web.Auth;
using Jobsy.Web.Security;
using Microsoft.AspNetCore.Antiforgery;

namespace Jobsy.Web.Hosting;

/// <summary>BFF for candidate applications page (withdraw without a Blazor circuit).</summary>
public static class CandidateApplicationsBffEndpoints
{
    public const string WithdrawPath = "/candidate/applications/withdraw";
    public const string HttpClientName = "JobsyCandidateApplicationsBff";

    public static IEndpointRouteBuilder MapCandidateApplicationsBffEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost(WithdrawPath, async (
            HttpContext http,
            IAntiforgery antiforgery,
            IHttpClientFactory httpClientFactory,
            JobsyAccessTokenIssuer accessTokens) =>
        {
            if (http.User.Identity?.IsAuthenticated != true)
            {
                return Results.Redirect("/login?returnUrl=" + Uri.EscapeDataString("/candidate/applications"));
            }

            var form = await http.Request.ReadFormAsync(http.RequestAborted);
            if (!await antiforgery.IsRequestValidAsync(http))
            {
                return Results.Redirect("/candidate/applications?error=1");
            }

            if (!Guid.TryParse(form["application_id"].ToString(), out var applicationId) || applicationId == Guid.Empty)
            {
                return Results.Redirect("/candidate/applications?error=1");
            }

            var jwt = accessTokens.TryCreate(http.User, TrustedClientIp.Resolve(http));
            if (string.IsNullOrWhiteSpace(jwt))
            {
                return Results.Redirect("/candidate/applications?error=1");
            }

            var client = httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Post, $"api/applications/{applicationId:D}/withdraw");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            using var response = await client.SendAsync(request, http.RequestAborted);
            if (!response.IsSuccessStatusCode)
            {
                return Results.Redirect("/candidate/applications?error=1");
            }

            return Results.Redirect("/candidate/applications?withdrawn=1");
        }).RequireAuthorization().WithOrder(-1000);

        app.MapGet("/candidate/applications/{applicationId:guid}/lobsy-cv.pdf", async (
            HttpContext http,
            Guid applicationId,
            IHttpClientFactory httpClientFactory,
            JobsyAccessTokenIssuer accessTokens) =>
        {
            var jwt = accessTokens.TryCreate(http.User, TrustedClientIp.Resolve(http));
            if (string.IsNullOrWhiteSpace(jwt))
            {
                return Results.Unauthorized();
            }

            var client = httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/applications/{applicationId:D}/lobsy-cv.pdf");
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
            var bytes = await response.Content.ReadAsByteArrayAsync(http.RequestAborted);
            return Results.File(bytes, "application/pdf", fileName);
        })
        .RequireAuthorization()
        .RequireRateLimiting("public-pdf");

        return app;
    }
}
