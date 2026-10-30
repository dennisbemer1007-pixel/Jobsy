using Jobsy.Web.Localization;
using Jobsy.Web.Saved;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Web.Hosting;

public static class SavedClientEndpoints
{
    public static IEndpointRouteBuilder MapSavedClientEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/saved-client/unsave", UnsaveAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Candidate" })
            .DisableAntiforgery();

        return app;
    }

    private static async Task<IResult> UnsaveAsync(
        HttpContext http,
        JobsyApiClient api,
        CultureState culture,
        CancellationToken cancellationToken)
    {
        if (!http.Request.HasFormContentType)
        {
            return Results.BadRequest();
        }

        var form = await http.Request.ReadFormAsync(cancellationToken);
        return await SavedClientUnsave.PostAsync(http, form, api, culture, cancellationToken);
    }
}
