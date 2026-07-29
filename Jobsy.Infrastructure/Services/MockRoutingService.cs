using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;

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
        var distanceMeters = HaversineMeters(fromLatitude, fromLongitude, toLatitude, toLongitude);
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

    private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000;
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2))
                * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}
