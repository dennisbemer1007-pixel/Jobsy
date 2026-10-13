using System.Globalization;
using System.Text.Json;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Circle-polygon isochrones for tests/dev — same radii as <see cref="TravelReach"/>.
/// </summary>
public sealed class MockIsochroneService : IIsochroneService
{
    public Task<string?> TryGetIsochronesAsync(
        double latitude,
        double longitude,
        TransportMode mode,
        IReadOnlyList<int> minutes,
        CancellationToken cancellationToken = default)
    {
        if (!IsFinite(latitude) || !IsFinite(longitude) || minutes is null || minutes.Count == 0)
        {
            return Task.FromResult<string?>(null);
        }

        var ordered = minutes
            .Where(m => m > 0)
            .Distinct()
            .OrderByDescending(m => m)
            .ToList();
        if (ordered.Count == 0)
        {
            return Task.FromResult<string?>(null);
        }

        var features = new List<object>(ordered.Count);
        foreach (var mins in ordered)
        {
            var radiusM = TravelReach.RingRadiusMeters(mode, mins);
            if (radiusM < 40)
            {
                continue;
            }

            features.Add(new
            {
                type = "Feature",
                properties = new { minutes = mins },
                geometry = new
                {
                    type = "Polygon",
                    coordinates = new[] { CircleRing(latitude, longitude, radiusM) }
                }
            });
        }

        var fc = new { type = "FeatureCollection", features };
        return Task.FromResult<string?>(JsonSerializer.Serialize(fc));
    }

    private static bool IsFinite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);

    private static double[][] CircleRing(double lat, double lng, double radiusM)
    {
        const int steps = 64;
        var coords = new double[steps + 1][];
        for (var i = 0; i <= steps; i++)
        {
            var bearing = i * (360.0 / steps);
            coords[i] = Destination(lat, lng, radiusM, bearing);
        }

        return coords;
    }

    private static double[] Destination(double lat, double lng, double distanceM, double bearingDeg)
    {
        const double r = 6371000.0;
        var δ = distanceM / r;
        var θ = bearingDeg * Math.PI / 180.0;
        var φ1 = lat * Math.PI / 180.0;
        var λ1 = lng * Math.PI / 180.0;
        var sinφ1 = Math.Sin(φ1);
        var cosφ1 = Math.Cos(φ1);
        var sinδ = Math.Sin(δ);
        var cosδ = Math.Cos(δ);
        var sinφ2 = sinφ1 * cosδ + cosφ1 * sinδ * Math.Cos(θ);
        var φ2 = Math.Asin(sinφ2);
        var y = Math.Sin(θ) * sinδ * cosφ1;
        var x = cosδ - sinφ1 * sinφ2;
        var λ2 = λ1 + Math.Atan2(y, x);
        return
        [
            λ2 * 180.0 / Math.PI,
            φ2 * 180.0 / Math.PI
        ];
    }
}
