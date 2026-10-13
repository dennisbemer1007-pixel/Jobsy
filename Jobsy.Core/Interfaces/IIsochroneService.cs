using Jobsy.Core.Enums;

namespace Jobsy.Core.Interfaces;

/// <summary>
/// Travel-time polygons (isochrones) for the banenkaart rings.
/// Returns GeoJSON FeatureCollection JSON with one Polygon/MultiPolygon per minute value.
/// </summary>
public interface IIsochroneService
{
    /// <param name="latitude">Origin latitude.</param>
    /// <param name="longitude">Origin longitude.</param>
    /// <param name="mode">Transport mode (bike/car/walk; OV typically falls back to circles).</param>
    /// <param name="minutes">Contour minutes (e.g. 10,20,30), largest first in the response.</param>
    /// <returns>GeoJSON FeatureCollection string, or null when the provider failed.</returns>
    Task<string?> TryGetIsochronesAsync(
        double latitude,
        double longitude,
        TransportMode mode,
        IReadOnlyList<int> minutes,
        CancellationToken cancellationToken = default);
}
