using Jobsy.Web.Localization;
using Jobsy.Web.Saved;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Web.Hosting;

public static class SavedClientEndpoints
{
    public static IEndpointRouteBuilder MapSavedClientEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/saved-client/unsave", UnsaveAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Candidate" });

        return app;
    }

    private static async Task<IResult> UnsaveAsync(
        HttpContext http,
        IAntiforgery antiforgery,
        JobsyApiClient api,
        CultureState culture,
        CancellationToken cancellationToken)
    {
        if (!http.Request.HasFormContentType)
        {
            return Results.BadRequest();
        }

        var form = await http.Request.ReadFormAsync(cancellationToken);
        if (!await antiforgery.IsRequestValidAsync(http))
        {
            var filter = SavedSsrQuery.NormalizeFilter(form["filter"].ToString());
            var sort = SavedSsrQuery.NormalizeSort(form["sort"].ToString());
            return TypedResults.Redirect(
                SavedSsrQuery.BuildUrl(filter, sort, error: culture["Common.Error.TryAgain"]),
                permanent: false,
                preserveMethod: false);
        }

        return await SavedClientUnsave.PostAsync(form, api, culture, cancellationToken);
    }
}
