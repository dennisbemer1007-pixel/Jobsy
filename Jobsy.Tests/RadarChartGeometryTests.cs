using Jobsy.Web.Components.Shared.Charts;

namespace Jobsy.Tests;

public class RadarChartGeometryTests
{
    [Fact]
    public void Six_equal_hundred_percent_values_form_hexagon_at_radius()
    {
        var values = new int?[] { 100, 100, 100, 100, 100, 100 };
        const double radius = 50;
        var points = RadarChartGeometry.PolygonPoints(values, 0, 0, radius);

        Assert.Equal(6, points.Count);
        foreach (var (x, y) in points)
        {
            var dist = Math.Sqrt(x * x + y * y);
            Assert.InRange(dist, radius - 0.05, radius + 0.05);
        }
    }

    [Fact]
    public void Zero_values_sit_at_center()
    {
        var values = new int?[] { 0, 0, 0, 0, 0 };
        var points = RadarChartGeometry.PolygonPoints(values, 10, 20, 50);

        Assert.Equal(5, points.Count);
        foreach (var (x, y) in points)
        {
            Assert.Equal(10, x, 1);
            Assert.Equal(20, y, 1);
        }
    }

    [Fact]
    public void ToSvgPoints_uses_invariant_one_decimal_format()
    {
        var points = new List<(double X, double Y)> { (1.25, 2.75), (-3.0, 0.04) };
        Assert.Equal("1.3,2.8 -3.0,0.0", RadarChartGeometry.ToSvgPoints(points));
    }

    [Fact]
    public void AxisEndpoints_returns_requested_axis_counts()
    {
        Assert.Equal(5, RadarChartGeometry.AxisEndpoints(5, 0, 0, 40).Count);
        Assert.Equal(6, RadarChartGeometry.AxisEndpoints(6, 0, 0, 40).Count);

        var pentagon = RadarChartGeometry.AxisEndpoints(5, 0, 0, 40);
        foreach (var (x, y) in pentagon)
        {
            var dist = Math.Sqrt(x * x + y * y);
            Assert.InRange(dist, 39.95, 40.05);
        }
    }
}
