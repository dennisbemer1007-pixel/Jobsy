using Jobsy.Web.Career;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Web.Hosting;

public static class CareerClientEndpoints
{
    public static IEndpointRouteBuilder MapCareerClientEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/career-client/save-dream", SaveDreamAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Candidate" });

        return app;
    }

    private static async Task<IResult> SaveDreamAsync(
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
            var tab = CareerSsrTabs.Normalize(form["returnTab"].ToString());
            return CareerClientDreamSaver.RedirectWithError(tab, culture["Common.Error.TryAgain"]);
        }

        return await CareerClientDreamSaver.SaveAsync(form, api, culture, cancellationToken);
    }
}
