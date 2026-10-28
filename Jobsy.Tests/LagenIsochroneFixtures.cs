using System.Globalization;
using System.Text.Json;

namespace Jobsy.Tests;

/// <summary>
/// Irregular bike isochrone polygons around Naaldwijk (mockup lagen/src/app.js ring shapes, not circles).
/// </summary>
public static class LagenIsochroneFixtures
{
    internal static string FeatureCollectionJson(double originLat, double originLng)
    {
        var rings = new (int minutes, double radiusKm, double seed)[]
        {
            (30, 5.4, 2.1),
            (20, 3.6, 1.3),
            (10, 1.8, 0.4)
        };

        var features = rings.Select(r => new
        {
            type = "Feature",
            properties = new { minutes = r.minutes },
            geometry = new
            {
                type = "Polygon",
                coordinates = new[] { IrregularRing(originLng, originLat, r.radiusKm, r.seed) }
            }
        }).ToList();

        return JsonSerializer.Serialize(new { type = "FeatureCollection", features });
    }

    /// <summary>Mockup: K(dx,dy) = [lng + dx/68.55, lat + dy/111.2].</summary>
    private static double[][] IrregularRing(double originLng, double originLat, double radiusKm, double seed)
    {
        const int steps = 120;
        var ring = new double[steps + 1][];
        for (var i = 0; i <= steps; i++)
        {
            var a = i / (double)steps * 2 * Math.PI;
            var wobble = 1
                + 0.11 * Math.Sin(3 * a + seed)
                + 0.06 * Math.Sin(7 * a + seed * 2)
                + 0.035 * Math.Sin(13 * a + seed * 3);
            var r = radiusKm * wobble;
            var dx = r * Math.Cos(a);
            var dy = r * Math.Sin(a);
            var lng = originLng + dx / 68.55;
            var lat = originLat + dy / 111.2;
            ring[i] = [lng, lat];
        }

        return ring;
    }
}
