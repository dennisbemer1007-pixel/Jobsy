namespace Jobsy.Core.Contracts;

/// <summary>
/// Precomputed banenkaart camera for first paint. Center is the centroid of active
/// public pins; zoom follows the same span heuristic as <c>jobMap.js</c>.
/// </summary>
public sealed record VacancyMapView(
    double CenterLat,
    double CenterLng,
    double Zoom,
    int PinCount)
{
    public bool HasPins => PinCount > 0;
}
