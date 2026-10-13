using System.Globalization;

namespace Jobsy.Web.Components.Shared.Charts;

/// <summary>Static polar geometry for dependency-free SVG radar charts.</summary>
public static class RadarChartGeometry
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public static IReadOnlyList<(double X, double Y)> PolygonPoints(
        IReadOnlyList<int?> values01to100,
        double cx,
        double cy,
        double radius,
        double startAngleDeg = -90)
    {
        if (values01to100.Count == 0)
            return [];

        var n = values01to100.Count;
        var points = new (double X, double Y)[n];
        for (var i = 0; i < n; i++)
        {
            var value = Math.Clamp(values01to100[i] ?? 0, 0, 100);
            var dist = radius * value / 100.0;
            var angleRad = DegToRad(startAngleDeg + i * 360.0 / n);
            points[i] = (cx + dist * Math.Cos(angleRad), cy + dist * Math.Sin(angleRad));
        }

        return points;
    }

    public static IReadOnlyList<(double X, double Y)> AxisEndpoints(
        int axisCount,
        double cx,
        double cy,
        double radius,
        double startAngleDeg = -90)
    {
        if (axisCount <= 0)
            return [];

        var points = new (double X, double Y)[axisCount];
        for (var i = 0; i < axisCount; i++)
        {
            var angleRad = DegToRad(startAngleDeg + i * 360.0 / axisCount);
            points[i] = (cx + radius * Math.Cos(angleRad), cy + radius * Math.Sin(angleRad));
        }

        return points;
    }

    public static string ToSvgPoints(IReadOnlyList<(double X, double Y)> points)
    {
        if (points.Count == 0)
            return string.Empty;

        return string.Join(
            " ",
            points.Select(p =>
                $"{p.X.ToString("0.0", Invariant)},{p.Y.ToString("0.0", Invariant)}"));
    }

    public static string FormatCoord(double value) => value.ToString("0.0", Invariant);

    public static double DegToRad(double deg) => deg * Math.PI / 180.0;
}
