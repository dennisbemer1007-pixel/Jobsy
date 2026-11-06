using System.Text.Json;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services.CandidateExternalVacancies;

public sealed class ExternalVacancyTravelEstimator : IExternalVacancyTravelEstimator
{
    private readonly JobsyDbContext _db;
    private readonly IGeocodingService _geocoding;
    private readonly IExactRoutingService _routing;

    public ExternalVacancyTravelEstimator(
        JobsyDbContext db,
        IGeocodingService geocoding,
        IExactRoutingService routing)
    {
        _db = db;
        _geocoding = geocoding;
        _routing = routing;
    }

    public async Task<int?> TryEstimateTravelMinutesAsync(
        Guid candidateUserId,
        string vacancyPlaceText,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(vacancyPlaceText))
        {
            return null;
        }

        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == candidateUserId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        double? fromLat = user.HomeLocation?.Latitude;
        double? fromLng = user.HomeLocation?.Longitude;
        var transport = TransportMode.Bike;

        if (fromLat is null || fromLng is null)
        {
            var home = TryReadHomeAddress(user.PreferencesJson);
            if (string.IsNullOrWhiteSpace(home))
            {
                return null;
            }

            var geoHome = await _geocoding.GeocodeAsync(home, cancellationToken);
            if (geoHome is null)
            {
                return null;
            }

            fromLat = geoHome.Latitude;
            fromLng = geoHome.Longitude;
            transport = TryReadPreferredTransport(user.PreferencesJson);
        }

        var geoPlace = await _geocoding.GeocodeAsync(vacancyPlaceText.Trim(), cancellationToken);
        if (geoPlace is null)
        {
            return null;
        }

        var route = await _routing.TryGetRouteAsync(
            fromLat.Value,
            fromLng.Value,
            geoPlace.Latitude,
            geoPlace.Longitude,
            transport,
            cancellationToken);
        if (route is null)
        {
            return null;
        }

        return Math.Max(1, (int)Math.Round(route.DurationSeconds / 60.0, MidpointRounding.AwayFromZero));
    }

    private static string? TryReadHomeAddress(string? preferencesJson)
    {
        if (string.IsNullOrWhiteSpace(preferencesJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(preferencesJson);
            if (doc.RootElement.TryGetProperty("homeAddress", out var home)
                && home.ValueKind == JsonValueKind.String)
            {
                return home.GetString();
            }
        }
        catch
        {
            // ignore malformed prefs
        }

        return null;
    }

    private static TransportMode TryReadPreferredTransport(string? preferencesJson)
    {
        if (string.IsNullOrWhiteSpace(preferencesJson))
        {
            return TransportMode.Bike;
        }

        try
        {
            using var doc = JsonDocument.Parse(preferencesJson);
            if (doc.RootElement.TryGetProperty("preferredTransport", out var t)
                && t.ValueKind == JsonValueKind.String)
            {
                return TransportLabels.Parse(t.GetString());
            }
        }
        catch
        {
            // ignore
        }

        return TransportMode.Bike;
    }
}
