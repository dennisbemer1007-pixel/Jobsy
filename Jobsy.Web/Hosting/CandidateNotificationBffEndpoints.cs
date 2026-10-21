using System.Net.Http.Headers;
using System.Net.Http.Json;
using Jobsy.Web.Auth;
using Jobsy.Web.Security;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Web.Hosting;

/// <summary>BFF for the header notification bell (fetch/mark read without a Blazor circuit).</summary>
public static class CandidateNotificationBffEndpoints
{
    public const string HttpClientName = "JobsyCandidateNotificationsBff";

    public static IEndpointRouteBuilder MapCandidateNotificationBffEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/bff/candidate/notifications")
            .RequireAuthorization()
            .DisableAntiforgery();

        group.MapGet("", async (
            HttpContext http,
            int? take,
            IHttpClientFactory httpClientFactory,
            JobsyAccessTokenIssuer accessTokens) =>
        {
            var jwt = accessTokens.TryCreate(http.User, TrustedClientIp.Resolve(http));
            if (string.IsNullOrWhiteSpace(jwt))
            {
                return Results.Unauthorized();
            }

            var limit = Math.Clamp(take ?? 20, 1, 50);
            var client = httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/notifications?take={limit}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            using var response = await client.SendAsync(request, http.RequestAborted);
            if (!response.IsSuccessStatusCode)
            {
                return Results.StatusCode((int)response.StatusCode);
            }

            var json = await response.Content.ReadAsStringAsync(http.RequestAborted);
            return Results.Content(json, "application/json");
        }).RequireRateLimiting("auth");

        group.MapGet("/unread-count", async (
            HttpContext http,
            IHttpClientFactory httpClientFactory,
            JobsyAccessTokenIssuer accessTokens) =>
        {
            var jwt = accessTokens.TryCreate(http.User, TrustedClientIp.Resolve(http));
            if (string.IsNullOrWhiteSpace(jwt))
            {
                return Results.Unauthorized();
            }

            var client = httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/notifications/unread-count");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            using var response = await client.SendAsync(request, http.RequestAborted);
            if (!response.IsSuccessStatusCode)
            {
                return Results.StatusCode((int)response.StatusCode);
            }

            var json = await response.Content.ReadAsStringAsync(http.RequestAborted);
            return Results.Content(json, "application/json");
        }).RequireRateLimiting("auth");

        group.MapPost("/{id:guid}/read", async (
            HttpContext http,
            Guid id,
            IHttpClientFactory httpClientFactory,
            JobsyAccessTokenIssuer accessTokens) =>
        {
            var jwt = accessTokens.TryCreate(http.User, TrustedClientIp.Resolve(http));
            if (string.IsNullOrWhiteSpace(jwt))
            {
                return Results.Unauthorized();
            }

            var client = httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Post, $"api/notifications/{id:D}/read");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            using var response = await client.SendAsync(request, http.RequestAborted);
            if (!response.IsSuccessStatusCode)
            {
                return Results.StatusCode((int)response.StatusCode);
            }

            var json = await response.Content.ReadAsStringAsync(http.RequestAborted);
            return Results.Content(json, "application/json");
        }).RequireRateLimiting("auth");

        group.MapPost("/read-all", async (
            HttpContext http,
            IHttpClientFactory httpClientFactory,
            JobsyAccessTokenIssuer accessTokens) =>
        {
            var jwt = accessTokens.TryCreate(http.User, TrustedClientIp.Resolve(http));
            if (string.IsNullOrWhiteSpace(jwt))
            {
                return Results.Unauthorized();
            }

            var client = httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/notifications/read-all");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            using var response = await client.SendAsync(request, http.RequestAborted);
            return Results.StatusCode((int)response.StatusCode);
        }).RequireRateLimiting("auth");

        return app;
    }
}
