using System.Text.Json;
using Jobsy.Core.Features;
using Jobsy.Web.Localization;
using Jobsy.Web.Onboarding;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Web.Hosting;

/// <summary>Browser-direct onboarding BFF (no Blazor circuit for save/advance).</summary>
public static class OnboardingClientEndpoints
{
    public static IEndpointRouteBuilder MapOnboardingClientEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/onboarding-client/address-suggest", SuggestAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Candidate" });

        app.MapPost("/api/onboarding-client/begin", BeginAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Candidate" });

        app.MapPut("/api/onboarding-client/step1", SaveStep1Async)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Candidate" });

        app.MapPut("/api/onboarding-client/steps/{step:int}", SaveStepAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Candidate" });

        app.MapPost("/api/onboarding-client/kompas/complete", CompleteKompasAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Candidate" });

        return app;
    }

    private static async Task<IResult> BeginAsync(
        HttpContext http,
        IAntiforgery antiforgery,
        JobsyApiClient api,
        CultureState culture,
        CancellationToken cancellationToken)
    {
        if (!await antiforgery.IsRequestValidAsync(http))
        {
            return AntiforgeryError();
        }

        var (status, payload) = await OnboardingClientStepBffSaver.BeginWelcomeAsync(api, culture, cancellationToken);
        return Results.Json(payload, statusCode: status);
    }

    private static async Task<IResult> SaveStepAsync(
        HttpContext http,
        int step,
        JsonElement body,
        IAntiforgery antiforgery,
        JobsyApiClient api,
        IGeocodingClient geocoder,
        IFeatureFlags featureFlags,
        CultureState culture,
        CancellationToken cancellationToken)
    {
        if (!await antiforgery.IsRequestValidAsync(http))
        {
            return AntiforgeryError();
        }

        var (status, payload) = await OnboardingClientStepBffSaver.SaveStepAsync(
            step,
            body,
            api,
            geocoder,
            featureFlags,
            culture,
            cancellationToken);
        return Results.Json(payload, statusCode: status);
    }

    private static async Task<IResult> CompleteKompasAsync(
        HttpContext http,
        IAntiforgery antiforgery,
        JobsyApiClient api,
        IFeatureFlags featureFlags,
        CultureState culture,
        CancellationToken cancellationToken)
    {
        if (!await antiforgery.IsRequestValidAsync(http))
        {
            return AntiforgeryError();
        }

        var (status, payload) = await OnboardingClientStepBffSaver.CompleteKompasAsync(
            api,
            featureFlags,
            culture,
            cancellationToken);
        return Results.Json(payload, statusCode: status);
    }

    private static IResult AntiforgeryError()
        => Results.Json(
            new { message = "Ongeldige sessie. Vernieuw de pagina en probeer opnieuw." },
            statusCode: StatusCodes.Status400BadRequest);

    private static async Task<IResult> SaveStep1Async(
        HttpContext http,
        OnboardingClientStep1Request body,
        IAntiforgery antiforgery,
        JobsyApiClient api,
        IGeocodingClient geocoder,
        CultureState culture,
        CancellationToken cancellationToken)
    {
        if (!await antiforgery.IsRequestValidAsync(http))
        {
            return Results.Json(
                new { message = "Ongeldige sessie. Vernieuw de pagina en probeer opnieuw." },
                statusCode: StatusCodes.Status400BadRequest);
        }

        var (status, payload) = await OnboardingClientStep1Saver.SaveAsync(
            body,
            api,
            geocoder,
            culture,
            cancellationToken);
        return Results.Json(payload, statusCode: status);
    }

    private static async Task<IResult> SuggestAsync(
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
}
