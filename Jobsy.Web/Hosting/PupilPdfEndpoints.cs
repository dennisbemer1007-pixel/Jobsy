using Jobsy.Core.Authorization;
using Jobsy.Web.Services;

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
            if (bytes is null || bytes.Length == 0)
            {
                return Results.StatusCode(status is >= 400 and < 600 ? status : StatusCodes.Status502BadGateway);
            }

            return Results.File(bytes, "application/pdf", fileName);
        }).RequireAuthorization(JobsyPolicies.PupilSession);

        return app;
    }
}
