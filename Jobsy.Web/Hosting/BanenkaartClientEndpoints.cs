using Jobsy.Core.Hosting;
using Jobsy.Web.Banenkaart;
using Jobsy.Web.KandidaatBanen;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

namespace Jobsy.Web.Hosting;

public static class BanenkaartClientEndpoints
{
    public static IEndpointRouteBuilder MapBanenkaartClientEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(BanenkaartClientCatalog.AllPath, BanenkaartClientCatalog.GetAllAsync).AllowAnonymous();
        app.MapGet(BanenkaartClientPins.Path, BanenkaartClientPins.GetAsync).AllowAnonymous();
        app.MapGet("/api/banenkaart-client/address-suggest", AddressSuggestAsync).AllowAnonymous();
        app.MapPost(BanenkaartSsrQuery.ApplyPath, ApplyFiltersAsync).AllowAnonymous();
        app.MapPost(BanenkaartSsrQuery.ResetPath, ResetFiltersAsync).AllowAnonymous();
        app.MapPost(BanenkaartSsrQuery.SavePath, SaveVacancyAsync)
            .RequireAuthorization()
            .DisableAntiforgery();

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

        var fromQuery = BanenkaartSsrQuery.Parse(http.Request.Query);
        var merged = BanenkaartFilterCookie.Merge(http, fromQuery);
        var state = BanenkaartSsrQuery.FromForm(form, merged);
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
        JobsyApiClient api,
        CultureState culture,
        ILoggerFactory loggerFactory,
        DeploymentEnvironmentLabel deployment,
        CancellationToken cancellationToken)
    {
        if (!http.Request.HasFormContentType)
        {
            return Results.BadRequest();
        }

        var form = await http.Request.ReadFormAsync(cancellationToken);
        var logger = loggerFactory.CreateLogger(typeof(BanenkaartClientEndpoints));
        return await BanenkaartClientSaveResult.ExecuteAsync(
            http,
            form,
            api,
            culture,
            logger,
            deployment,
            cancellationToken);
    }

    private static async Task<IResult> AddressSuggestAsync(
        string? q,
        IGeocodingClient geocoder,
        CancellationToken cancellationToken)
    {
        var query = (q ?? string.Empty).Trim();
        if (query.Length < 3)
        {
            return Results.Json(Array.Empty<AddressSuggestDto>());
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(4));
        try
        {
            var results = await geocoder.SuggestAsync(query, timeout.Token);
            var payload = results
                .Select(s => new AddressSuggestDto(s.Label, s.Latitude, s.Longitude))
                .ToList();
            return Results.Json(payload);
        }
        catch (OperationCanceledException)
        {
            return Results.Json(Array.Empty<AddressSuggestDto>());
        }
        catch
        {
            return Results.Json(Array.Empty<AddressSuggestDto>());
        }
    }

    private sealed record AddressSuggestDto(string Label, double Latitude, double Longitude);

    private static RedirectHttpResult SeeOther(string url)
        => TypedResults.Redirect(url, permanent: false, preserveMethod: false);
}
