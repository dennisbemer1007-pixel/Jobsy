using Jobsy.Core.Authorization;
using Jobsy.Core.Scholen.PupilPassport;
using Jobsy.Web.Auth;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Authentication;

namespace Jobsy.Web.Hosting;

/// <summary>
/// <c>GET /ouder/pdf</c> on the web host. The PDF is rendered by the API at the same path.
/// </summary>
public static class ParentPassportPdfEndpoints
{
    public const string Path = "/ouder/pdf";

    public static IEndpointRouteBuilder MapParentPassportPdfEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(Path, async (HttpContext http, JobsyApiClient api) =>
        {
            http.Response.Headers.CacheControl = "no-store";
            var query = http.Request.QueryString.Value;
            var (bytes, fileName, status) = await api.GetParentPassportPdfAsync(query, http.RequestAborted);
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
        }).RequireAuthorization(JobsyPolicies.ParentPassportSession);

        return app;
    }
}
