using Jobsy.Core.Authorization;
using Jobsy.Web.Auth;
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
            var (bytes, fileName, status) = await api.GetPupilStoryPdfAsync(http.RequestAborted);
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

            return Results.Redirect("/leerling/start");
        }).RequireAuthorization(JobsyPolicies.PupilSession);

        return app;
    }
}
