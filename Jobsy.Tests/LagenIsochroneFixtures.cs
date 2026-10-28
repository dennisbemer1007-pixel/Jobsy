using System.Globalization;
using System.Text.Json;

namespace Jobsy.Tests;

/// <summary>
/// Irregular bike isochrone polygons around Naaldwijk (mockup lagen/src/app.js ring shapes, not circles).
/// </summary>
public static class LagenIsochroneFixtures
{
    internal static string FeatureCollectionJson(double originLat, double originLng, string? minutesQuery = null)
    {
        var requested = ParseRequestedMinutes(minutesQuery);
        var rings = requested.Count > 0
            ? requested
            : [30, 20, 10];

        var features = rings.Select(minutes =>
        {
            var radiusKm = RadiusKmForMinutes(minutes);
            var seed = minutes * 0.041;
            return new
            {
                type = "Feature",
                properties = new { minutes },
                geometry = new
                {
                    type = "Polygon",
                    coordinates = new[] { IrregularRing(originLng, originLat, radiusKm, seed) }
                }
            };
        }).ToList();

        return JsonSerializer.Serialize(new { type = "FeatureCollection", features });
    }

    private static List<int> ParseRequestedMinutes(string? minutesQuery)
    {
        if (string.IsNullOrWhiteSpace(minutesQuery))
        {
            return [];
        }

        var list = new List<int>();
        foreach (var part in minutesQuery.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out var m) && m > 0)
            {
                list.Add(m);
            }
        }

        return list.Distinct().OrderByDescending(x => x).ToList();
    }

    private static double RadiusKmForMinutes(int minutes)
        => 5.4 * (minutes / 30.0);

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
