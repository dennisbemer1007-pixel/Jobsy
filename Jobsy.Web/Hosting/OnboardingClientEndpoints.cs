using Jobsy.Web.Localization;
using Jobsy.Web.Onboarding;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Web.Hosting;

/// <summary>Browser-direct helpers for onboarding steps 1–2 (no Blazor circuit).</summary>
public static class OnboardingClientEndpoints
{
    public static IEndpointRouteBuilder MapOnboardingClientEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/onboarding-client/address-suggest", SuggestAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Candidate" });

        app.MapPut("/api/onboarding-client/step1", SaveStep1Async)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Candidate" });

        return app;
    }

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
