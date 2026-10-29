using Jobsy.Core.Authorization;
using Jobsy.Web.Auth;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Authentication;

namespace Jobsy.Web.Hosting;

/// <summary>
/// Parent PDF downloads on the web host (proxied to the API).
/// </summary>
public static class ParentPassportPdfEndpoints
{
    public const string PassportPath = "/ouder/pdf";
    public const string AnswersPath = "/ouder/antwoorden/pdf";

    public static IEndpointRouteBuilder MapParentPassportPdfEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(PassportPath, DownloadPassport).RequireAuthorization(JobsyPolicies.ParentPassportSession);
        app.MapGet(AnswersPath, DownloadAnswers).RequireAuthorization(JobsyPolicies.ParentPassportSession);
        return app;
    }

    private static Task<IResult> DownloadPassport(HttpContext http, JobsyApiClient api)
        => DownloadAsync(http, (q, ct) => api.GetParentPassportPdfAsync(q, ct));

    private static Task<IResult> DownloadAnswers(HttpContext http, JobsyApiClient api)
        => DownloadAsync(http, (q, ct) => api.GetParentAnswersPdfAsync(q, ct));

    private static async Task<IResult> DownloadAsync(
        HttpContext http,
        Func<string?, CancellationToken, Task<(byte[]? Bytes, string? FileName, int StatusCode)>> fetch)
    {
        http.Response.Headers.CacheControl = "no-store";
        var query = http.Request.QueryString.Value;
        var (bytes, fileName, status) = await fetch(query, http.RequestAborted);
        if (http.Response.HasStarted)
        {
            return Results.Empty;
        }

        if (bytes is { Length: > 0 })
        {
            return Results.File(bytes, "application/pdf", fileName);
        }

        if (status is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden)
        {
            await http.SignOutAsync(ParentPassportAuthDefaults.Scheme);
            ParentApiSessionCookie.Clear(http);
            return Results.Redirect("/ouder?error=expired");
        }

        return Results.Redirect("/ouder?error=invalid");
    }
}
