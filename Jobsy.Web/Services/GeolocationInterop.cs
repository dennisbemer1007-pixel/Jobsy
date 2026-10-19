using Jobsy.Web.Localization;
using Jobsy.Web.Models;
using Microsoft.JSInterop;

namespace Jobsy.Web.Services;

/// <summary>Shared browser geolocation via <c>jobsyGeo</c> (banenkaart + onboarding).</summary>
public static class GeolocationInterop
{
    public static async Task EnsureReadyAsync(IJSRuntime js, CancellationToken cancellationToken = default)
    {
        await js.InvokeVoidAsync("jobsyGeo.whenReady", 8_000);
        cancellationToken.ThrowIfCancellationRequested();
    }

    public static async Task<GeolocationPositionDto> RequestLocationAsync(IJSRuntime js)
    {
        await EnsureReadyAsync(js);
        return await js.InvokeAsync<GeolocationPositionDto>("jobsyGeo.requestLocation");
    }

    public static string MapGeolocationError(CultureState culture, Exception ex)
    {
        var message = ex.Message?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(message))
        {
            return culture["Onboarding.LocationDenied"];
        }

        var lower = message.ToLowerInvariant();
        if (lower.Contains("denied", StringComparison.Ordinal)
            || lower.Contains("geweigerd", StringComparison.Ordinal)
            || lower.Contains("permission", StringComparison.Ordinal)
            || lower.Contains("toestemming", StringComparison.Ordinal))
        {
            return culture["Onboarding.LocationDenied"];
        }

        if (lower.Contains("timeout", StringComparison.Ordinal)
            || lower.Contains("te lang", StringComparison.Ordinal))
        {
            return culture["Onboarding.LocationTimeout"];
        }

        if (lower.Contains("not available", StringComparison.Ordinal)
            || lower.Contains("niet beschikbaar", StringComparison.Ordinal)
            || lower.Contains("unavailable", StringComparison.Ordinal)
            || lower.Contains("kon niet worden bepaald", StringComparison.Ordinal))
        {
            return culture["Onboarding.LocationUnavailable"];
        }

        if (lower.Contains("https", StringComparison.Ordinal)
            || lower.Contains("beveiligde verbinding", StringComparison.Ordinal)
            || lower.Contains("secure", StringComparison.Ordinal))
        {
            return culture["Onboarding.LocationRequiresHttps"];
        }

        if (lower.Contains("jobsygeo", StringComparison.Ordinal)
            || lower.Contains("requestlocation", StringComparison.Ordinal)
            || lower.Contains("undefined", StringComparison.Ordinal))
        {
            return culture["Onboarding.LocationUnavailable"];
        }

        return message;
    }
}
