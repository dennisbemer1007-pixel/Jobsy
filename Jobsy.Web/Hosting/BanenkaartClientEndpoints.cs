using Jobsy.Web.Banenkaart;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Jobsy.Web.Hosting;

public static class BanenkaartClientEndpoints
{
    public static IEndpointRouteBuilder MapBanenkaartClientEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(BanenkaartClientCatalog.AllPath, BanenkaartClientCatalog.GetAllAsync).AllowAnonymous();
        app.MapGet(BanenkaartClientPins.Path, BanenkaartClientPins.GetAsync).AllowAnonymous();
        app.MapPost(BanenkaartSsrQuery.ApplyPath, ApplyFiltersAsync).AllowAnonymous();
        app.MapPost(BanenkaartSsrQuery.ResetPath, ResetFiltersAsync).AllowAnonymous();
        app.MapPost(BanenkaartSsrQuery.SavePath, SaveVacancyAsync)
            .RequireAuthorization();

        return app;
    }

    private static async Task<IResult> ApplyFiltersAsync(
        HttpContext http,
        IAntiforgery antiforgery,
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
            return SeeOther(BanenkaartSsrQuery.BuildUrl(
                BanenkaartSsrQuery.Parse(http.Request.Query),
                error: culture["Common.Error.TryAgain"]));
        }

        var state = BanenkaartSsrQuery.FromForm(form);
        BanenkaartFilterCookie.Persist(http.Response, state);
        return SeeOther(BanenkaartSsrQuery.BuildUrl(state));
    }

    private static async Task<IResult> ResetFiltersAsync(
        HttpContext http,
        IAntiforgery antiforgery,
        CultureState culture,
        CancellationToken cancellationToken)
    {
        if (!http.Request.HasFormContentType)
        {
            return Results.BadRequest();
        }

        await http.Request.ReadFormAsync(cancellationToken);
        if (!await antiforgery.IsRequestValidAsync(http))
        {
            return SeeOther(BanenkaartSsrQuery.BuildUrl(
                BanenkaartSsrQuery.Parse(http.Request.Query),
                error: culture["Common.Error.TryAgain"]));
        }

        var fresh = new BanenkaartSsrQuery.State(
            null,
            [],
            KbMapStart.DefaultMaxTravelMinutes,
            KbMapStart.DefaultTransport,
            15,
            0,
            40,
            null,
            null,
            null,
            0,
            [],
            [],
            "distance",
            false,
            null,
            null,
            null,
            null,
            false,
            1);
        BanenkaartFilterCookie.Clear(http.Response);
        return SeeOther(BanenkaartSsrQuery.BuildUrl(fresh));
    }

    private static async Task<IResult> SaveVacancyAsync(
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
            var fallback = BanenkaartSsrQuery.Parse(http.Request.Query);
            return SeeOther(BanenkaartSsrQuery.BuildUrl(fallback, error: culture["Common.Error.TryAgain"]));
        }

        if (!Guid.TryParse(form["vacancyId"].ToString(), out var vacancyId))
        {
            var fallback = BanenkaartSsrQuery.Parse(http.Request.Query);
            return SeeOther(BanenkaartSsrQuery.BuildUrl(fallback, error: culture["Common.Error.TryAgain"]));
        }

        var liked = string.Equals(form["liked"].ToString(), "1", StringComparison.Ordinal)
                    || string.Equals(form["liked"].ToString(), "true", StringComparison.OrdinalIgnoreCase);

        try
        {
            await api.SetLikedAsync(vacancyId, liked, cancellationToken);
            var returnQuery = form["returnQuery"].ToString();
            if (!string.IsNullOrWhiteSpace(returnQuery) && returnQuery.StartsWith('?'))
            {
                return SeeOther("/banenkaart" + returnQuery + "&saved=1");
            }

            var state = BanenkaartSsrQuery.Parse(http.Request.Query);
            return SeeOther(BanenkaartSsrQuery.BuildUrl(state, saved: true));
        }
        catch
        {
            var state = BanenkaartSsrQuery.Parse(http.Request.Query);
            return SeeOther(BanenkaartSsrQuery.BuildUrl(state, error: culture["Common.Error.TryAgain"]));
        }
    }

    private static RedirectHttpResult SeeOther(string url)
        => TypedResults.Redirect(url, permanent: false, preserveMethod: false);
}
