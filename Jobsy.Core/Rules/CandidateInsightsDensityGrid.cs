using Jobsy.Core.ValueObjects;

namespace Jobsy.Core.Rules;

/// <summary>
/// Fixed global square grid (~1.5 km) for density cells. Anchored on a global origin
/// (not the vestiging) so cells do not shift between requests.
/// </summary>
public static class CandidateInsightsDensityGrid
{
    public const double CellSizeKm = 1.5;

    // Equirectangular meters using a fixed earth-radius approximation (global origin).
    private const double MetersPerDegreeLat = 110_540.0;
    private const double MetersPerDegreeLngAtEquator = 111_320.0;
    private const double CellMeters = CellSizeKm * 1000.0;

    public static (int CellX, int CellY) ToCell(GeoPoint point)
    {
        var x = point.Longitude * MetersPerDegreeLngAtEquator;
        var y = point.Latitude * MetersPerDegreeLat;
        var cellX = (int)Math.Floor(x / CellMeters);
        var cellY = (int)Math.Floor(y / CellMeters);
        return (cellX, cellY);
    }

    public static string CellId(int cellX, int cellY) => $"{cellX}:{cellY}";

    public static (double CenterLat, double CenterLng) CellCenter(int cellX, int cellY)
    {
        var centerX = (cellX + 0.5) * CellMeters;
        var centerY = (cellY + 0.5) * CellMeters;
        return (centerY / MetersPerDegreeLat, centerX / MetersPerDegreeLngAtEquator);
    }
}
