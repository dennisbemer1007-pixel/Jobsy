using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Placeholder routing for bulk flows (discover/PushBom). Uses Haversine × cruise speed / circuity
/// so minutes stay aligned with <c>TravelReach.Estimate</c> and the map rings.
/// </summary>
public class MockRoutingService : IRoutingService
{
    public Task<RouteResult> GetRouteAsync(
        double fromLatitude,
        double fromLongitude,
        double toLatitude,
        double toLongitude,
        TransportMode transportMode,
        CancellationToken cancellationToken = default)
    {
        var mode = NormalizeMode(transportMode);
        var distanceMeters = GeoDistance.HaversineKm(
            new GeoPoint(fromLatitude, fromLongitude),
            new GeoPoint(toLatitude, toLongitude)) * 1000.0;
        var durationSeconds = distanceMeters / (TravelReach.CrowFliesKmPerHour(mode) * 1000.0 / 3600.0);

        return Task.FromResult(new RouteResult(distanceMeters, durationSeconds, mode));
    }

    private static TransportMode NormalizeMode(TransportMode mode)
    {
        if (mode.HasFlag(TransportMode.Bike)) return TransportMode.Bike;
        if (mode.HasFlag(TransportMode.Car)) return TransportMode.Car;
        if (mode.HasFlag(TransportMode.PublicTransport)) return TransportMode.PublicTransport;
        if (mode.HasFlag(TransportMode.Walking)) return TransportMode.Walking;
        return TransportMode.Bike;
    }
}
