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

    public static IEndpointRouteBuilder MapPupilPdfEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(Path, async (HttpContext http, JobsyApiClient api) =>
        {
            http.Response.Headers.CacheControl = "no-store";
            if (PupilStoryPdfLink.NeedsCanonicalRedirect(http.Request))
            {
                return Results.Redirect(PupilStoryPdfLink.HrefFromQuery(http.Request.Query));
            }

            var query = JobsyApiClient.BuildPupilStoryPdfPath(http.Request.QueryString.Value);
            var apiQuery = query.StartsWith("leerling/pdf?", StringComparison.Ordinal)
                ? "?" + query["leerling/pdf?".Length..]
                : string.Empty;
            var (bytes, fileName, status) = await api.GetPupilStoryPdfAsync(
                apiQuery,
                http.RequestAborted);
            if (http.Response.HasStarted)
            {
                return Results.Empty;
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
        }).RequireAuthorization(JobsyPolicies.PupilSession);

        return app;
    }
}
