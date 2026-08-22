using Jobsy.Core.ValueObjects;

namespace Jobsy.Core.Rules;

/// <summary>Google-encoded polyline (OSRM / MOTIS <c>legGeometry.points</c>).</summary>
public static class EncodedPolyline
{
    public static IReadOnlyList<GeoPoint> Decode(string? encoded, int precision = 5)
    {
        if (string.IsNullOrWhiteSpace(encoded))
        {
            return [];
        }

        precision = Math.Clamp(precision, 4, 8);
        var factor = Math.Pow(10, precision);
        var points = new List<GeoPoint>();
        var index = 0;
        var lat = 0;
        var lng = 0;
        var length = encoded.Length;

        while (index < length)
        {
            lat += ReadDelta(encoded, ref index, length);
            if (index >= length)
            {
                break;
            }

            lng += ReadDelta(encoded, ref index, length);
            points.Add(new GeoPoint(lat / factor, lng / factor));
        }

        return points;
    }

    public static double LengthMeters(string? encoded, int precision = 5)
    {
        var points = Decode(encoded, precision);
        if (points.Count < 2)
        {
            return 0;
        }

        var meters = 0.0;
        for (var i = 1; i < points.Count; i++)
        {
            meters += GeoDistance.HaversineKm(points[i - 1], points[i]) * 1000.0;
        }

        return meters;
    }

    private static int ReadDelta(string encoded, ref int index, int length)
    {
        var result = 0;
        var shift = 0;
        int b;
        do
        {
            if (index >= length)
            {
                break;
            }

            b = encoded[index++] - 63;
            result |= (b & 0x1f) << shift;
            shift += 5;
        } while (b >= 0x20);

        return (result & 1) != 0 ? ~(result >> 1) : result >> 1;
    }
}
