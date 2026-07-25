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

public record RouteResult(
    double DistanceMeters,
    double DurationSeconds,
    TransportMode TransportMode);
