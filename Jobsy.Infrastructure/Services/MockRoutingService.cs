using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Placeholder routing service. Replace with OSRM HTTP client when the Docker container is available.
/// Uses a rough Haversine estimate with mode-specific speeds for local demo/dev.
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
        var speed = TravelReach.SpeedKmPerHour(mode);
        var durationSeconds = distanceMeters / (speed * 1000.0 / 3600.0);

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
