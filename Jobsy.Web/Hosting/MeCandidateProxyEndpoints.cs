using System.Net.Http.Headers;
using System.Net.Http.Json;
using Jobsy.Web.Auth;
using Jobsy.Web.Security;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Web.Hosting;

/// <summary>
/// Browser calls <c>/api/me/*</c> on the Web host; forward to Jobsy.Api with the session JWT.
/// </summary>
public static class MeCandidateProxyEndpoints
{
    public static IEndpointRouteBuilder MapMeCandidateProxyEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/me/paspoort.pdf", ProxyPaspoortPdfAsync)
            .RequireAuthorization()
            .RequireRateLimiting("public-pdf");

        app.MapGet("/api/me/role-fit", ProxyRoleFitGetAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Candidate" });

        app.MapPost("/api/me/role-fit", ProxyRoleFitPostAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Candidate" });

        return app;
    }

    private static async Task<IResult> ProxyPaspoortPdfAsync(
        HttpContext http,
        IHttpClientFactory httpClientFactory,
        JobsyAccessTokenIssuer accessTokens)
    {
        var jwt = accessTokens.TryCreate(http.User, TrustedClientIp.Resolve(http));
        if (string.IsNullOrWhiteSpace(jwt))
        {
            return Results.Unauthorized();
        }

        var client = httpClientFactory.CreateClient(CandidatePassportPdfEndpoints.HttpClientName);
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/me/paspoort.pdf");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, http.RequestAborted);
        if (!response.IsSuccessStatusCode)
        {
            return Results.StatusCode((int)response.StatusCode);
        }

        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                       ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                       ?? "Lobsy-paspoort.pdf";
        http.Response.Headers.CacheControl = "no-store";
        var bytes = await response.Content.ReadAsByteArrayAsync(http.RequestAborted);
        return Results.File(bytes, "application/pdf", fileName);
    }

    private static async Task<IResult> ProxyRoleFitGetAsync(
        HttpContext http,
        JobsyApiClient api)
    {
        var state = await api.GetMyRoleFitAsync(http.RequestAborted);
        return state is null ? Results.NotFound() : Results.Json(state);
    }

    private static async Task<IResult> ProxyRoleFitPostAsync(
        HttpContext http,
        JobsyApiClient api)
    {
        RoleFitPostBody? body;
        try
        {
            body = await http.Request.ReadFromJsonAsync<RoleFitPostBody>(http.RequestAborted);
        }
        catch
        {
            return Results.BadRequest();
        }

        if (body is null || string.IsNullOrWhiteSpace(body.JobTitle))
        {
            return Results.BadRequest();
        }

        try
        {
            var state = await api.EvaluateRoleFitAsync(body.JobTitle.Trim(), body.VacancyId, http.RequestAborted);
            return Results.Json(state);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private sealed record RoleFitPostBody(string JobTitle, Guid? VacancyId);
}
