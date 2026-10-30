using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Web.Hosting;

public static class KbExtClientEndpoints
{
    public static IEndpointRouteBuilder MapKbExtClientEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/kb-ext-client/import", ImportAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = JobsyRoles.Candidate })
            .DisableAntiforgery();

        return app;
    }

    private sealed record ImportBody(string? Url);

    private static async Task<IResult> ImportAsync(
        ImportBody body,
        JobsyApiClient api,
        CancellationToken cancellationToken)
    {
        var url = body.Url?.Trim();
        if (string.IsNullOrWhiteSpace(url))
        {
            return Results.Json(new { error = "missing_url" });
        }

        try
        {
            var dto = await api.ImportExternalVacancyAsync(url, cancellationToken);
            if (dto is null)
            {
                return Results.Json(new { error = "feature_disabled" });
            }

            return Results.Json(new { redirect = KbRoutes.ExternalVacancyDetail(dto.Id) });
        }
        catch (Exception ex)
        {
            return Results.Json(new { error = ex.Message });
        }
    }
}
