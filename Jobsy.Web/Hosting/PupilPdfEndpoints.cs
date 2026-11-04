using System.Net.Http.Headers;
using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.MboHbo;
using Jobsy.Web.Auth;
using Jobsy.Web.Scholen;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Authentication;

namespace Jobsy.Web.Hosting;

/// <summary>
/// <c>GET /leerling/pdf</c> on the web host. The PDF is rendered by the API at the same
/// path; a plain link on the pupil pages would otherwise 404 on the web origin.
/// Schools-off and no-store are applied by <c>SchoolsFeatureMiddleware</c> and
/// <c>LeerlingNoStoreMiddleware</c> because the path starts with <c>/leerling</c>.
/// </summary>
public static class PupilPdfEndpoints
{
    public const string Path = "/leerling/pdf";
    public const string HttpClientName = "JobsyPupilStoryPdf";

    public static IEndpointRouteBuilder MapPupilPdfEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(Path, DownloadAsync).RequireAuthorization(JobsyPolicies.PupilSession);
        return app;
    }

    private static async Task<IResult> DownloadAsync(
        HttpContext http,
        IHttpClientFactory httpClientFactory,
        JobsyApiClient api)
    {
        http.Response.Headers.CacheControl = "no-store";
        if (PupilStoryPdfLink.NeedsCanonicalRedirect(http.Request))
        {
            return Results.Redirect(PupilStoryPdfLink.SanitizeHref(
                PupilStoryPdfLink.HrefFromQuery(http.Request.Query)));
        }

        var (bytes, fileName, status) = await FetchStoryPdfAsync(http, httpClientFactory);
        if (http.Response.HasStarted)
        {
            return Results.Empty;
        }

        if (!IsPdfPayload(bytes))
        {
            if (status == StatusCodes.Status200OK)
            {
                status = StatusCodes.Status500InternalServerError;
            }

            bytes = null;
        }

        if (bytes is { Length: > 0 })
        {
            return Results.File(bytes, "application/pdf", fileName);
        }

        // A bare 401/409 is re-executed as /status/{code} and shown as 404.
        // Pupils always stay on the pupil site.
        if (status is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden)
        {
            await http.SignOutAsync(PupilAuthDefaults.Scheme);
            PupilApiSessionCookie.Clear(http);
            return Results.Redirect("/leerling?error=expired");
        }

        var progress = await api.GetPupilProgressAsync(http.RequestAborted);
        if (progress?.QuestionSet == PupilQuestionSet.MboHbo)
        {
            var dest = status == 409
                ? $"{PupilMboHboEntryPaths.Paspoort}?pdf=locked"
                : $"{PupilMboHboEntryPaths.Paspoort}?pdf=error";
            return Results.Redirect(dest);
        }

        if (progress?.QuestionSet == PupilQuestionSet.Vo)
        {
            return Results.Redirect("/leerling/paspoort?pdf=error");
        }

        return Results.Redirect(PupilGroep78EntryPaths.Start);
    }

    internal static async Task<(byte[]? Bytes, string FileName, int StatusCode)> FetchStoryPdfAsync(
        HttpContext http,
        IHttpClientFactory httpClientFactory)
    {
        var ticket = ResolveApiTicket(http);
        if (string.IsNullOrWhiteSpace(ticket))
        {
            return (null, "lobsy-verhaal.pdf", StatusCodes.Status401Unauthorized);
        }

        var path = JobsyApiClient.BuildPupilStoryPdfPath(http.Request.QueryString.Value);
        var client = httpClientFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.TryAddWithoutValidation("Cookie", $"{PupilAuthDefaults.CookieName}={ticket}");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/pdf"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.8));

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            http.RequestAborted);
        if (!response.IsSuccessStatusCode)
        {
            return (null, "lobsy-verhaal.pdf", (int)response.StatusCode);
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(http.RequestAborted);
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                       ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                       ?? "lobsy-verhaal.pdf";
        return (bytes, fileName, (int)response.StatusCode);
    }

    private static bool IsPdfPayload(byte[]? bytes)
        => bytes is { Length: > 4 }
           && bytes[0] == (byte)'%'
           && bytes[1] == (byte)'P'
           && bytes[2] == (byte)'D'
           && bytes[3] == (byte)'F';

    private static string? ResolveApiTicket(HttpContext http)
    {
        var codeId = http.User.FindFirst(PupilClaimTypes.PupilCodeId)?.Value;
        var stored = http.RequestServices.GetService<PupilApiTicketStore>()?.Get(codeId);
        if (!string.IsNullOrWhiteSpace(stored))
        {
            return stored;
        }

        return PupilApiSessionCookie.Read(http);
    }
}
