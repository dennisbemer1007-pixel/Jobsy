using Jobsy.Core.Enums;

namespace Jobsy.Core.Interfaces;

public interface IRoutingService
{
    Task<RouteResult> GetRouteAsync(
        double fromLatitude,
        double fromLongitude,
        double toLatitude,
        double toLongitude,
        TransportMode transportMode,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Road-network / timetable routing for a single origin→destination (vacancy detail).
/// Returns null when the upstream router has no path. Not for bulk discover matching.
/// </summary>
public interface IExactRoutingService
{
    Task<RouteResult?> TryGetRouteAsync(
        double fromLatitude,
        double fromLongitude,
        double toLatitude,
        double toLongitude,
        TransportMode transportMode,
        CancellationToken cancellationToken = default);
}

public record RouteResult(
    double DistanceMeters,
    double DurationSeconds,
    TransportMode TransportMode);
