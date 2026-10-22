using Jobsy.Web.Localization;
using Jobsy.Web.Navigation;
using Jobsy.Web.Passport;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Jobsy.Web.Hosting;

public static class PassportClientEndpoints
{
    public static IEndpointRouteBuilder MapPassportClientEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/passport-client/save-data", SaveDataAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Candidate" });

        return app;
    }

    private static async Task<IResult> SaveDataAsync(
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
            var tab = PassportTabs.Normalize(form["returnTab"].ToString());
            return PassportClientDataSaver.RedirectWithError(tab, culture["Common.Error.TryAgain"]);
        }

        return await PassportClientDataSaver.SaveAsync(form, api, culture, cancellationToken);
    }
}
