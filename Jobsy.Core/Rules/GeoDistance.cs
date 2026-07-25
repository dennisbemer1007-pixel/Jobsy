using Jobsy.Core.ValueObjects;

namespace Jobsy.Core.Rules;

public static class GeoDistance
{
    private const double EarthRadiusKm = 6371.0;

    public static double HaversineKm(GeoPoint a, GeoPoint b)
    {
        var dLat = DegreesToRadians(b.Latitude - a.Latitude);
        var dLon = DegreesToRadians(b.Longitude - a.Longitude);
        var lat1 = DegreesToRadians(a.Latitude);
        var lat2 = DegreesToRadians(b.Latitude);

        var h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        return 2 * EarthRadiusKm * Math.Asin(Math.Min(1, Math.Sqrt(h)));
    }

    public static bool IsWithinKm(GeoPoint origin, GeoPoint point, double km)
        => HaversineKm(origin, point) <= km;

    private static double DegreesToRadians(double degrees) => degrees * (Math.PI / 180.0);
}
