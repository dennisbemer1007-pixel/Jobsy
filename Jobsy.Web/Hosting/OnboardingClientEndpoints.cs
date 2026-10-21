using Jobsy.Web.Navigation;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;

namespace Jobsy.Web.Hosting;

/// <summary>Browser-direct helpers for onboarding steps 1–2 (no Blazor circuit).</summary>
public static class OnboardingClientEndpoints
{
    public const string DismissWelcomePath = "/api/onboarding-client/dismiss-welcome";

    public static IEndpointRouteBuilder MapOnboardingClientEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/onboarding-client/address-suggest", SuggestAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Candidate" });

        app.MapPost(DismissWelcomePath, DismissWelcomeAsync)
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Candidate" });

        return app;
    }

    private static async Task<IResult> DismissWelcomeAsync(
        HttpContext http,
        IAntiforgery antiforgery,
        JobsyApiClient api)
    {
        if (!await antiforgery.IsRequestValidAsync(http))
        {
            return Results.BadRequest();
        }

        try
        {
            await api.SaveMyOnboardingProgressAsync(1, source: "welcome", ct: http.RequestAborted);
        }
        catch
        {
            return Results.Redirect(OnboardingRoutes.ClassicStartPath + "?welcomeError=1");
        }

        return Results.Redirect(OnboardingRoutes.ClassicStartPath);
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
