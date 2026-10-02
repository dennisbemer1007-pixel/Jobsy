using Jobsy.Web.Components.Candidate.Journey;

namespace Jobsy.Tests;

public class CareerClimbGeometryTests
{
    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(4, false)]
    [InlineData(5, false)]
    [InlineData(6, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, true)]
    [InlineData(5, true)]
    [InlineData(6, true)]
    public void Stones_never_crowd_and_stay_inside_the_zone(int stepCount, bool mobile)
    {
        var points = CareerClimbGeometry.Points(stepCount, mobile);
        var min = CareerClimbGeometry.MinDistance(mobile);
        var w = CareerClimbGeometry.Width(mobile);
        var h = CareerClimbGeometry.Height(mobile);

        Assert.Equal(stepCount + 2, points.Count);

        foreach (var p in points)
        {
            Assert.InRange(p.X, 0, w);
            Assert.InRange(p.Y, 0, h);
        }

        for (var i = 0; i < points.Count; i++)
        {
            for (var j = i + 1; j < points.Count; j++)
            {
                var d = CareerClimbGeometry.Distance(points[i], points[j]);
                Assert.True(
                    d >= min,
                    $"stones {i} and {j} are {d:0.#} px apart (min {min}) for {stepCount} steps, mobile={mobile}");
            }
        }
    }

    [Fact]
    public void Lobster_climbs_towards_the_light()
    {
        var points = CareerClimbGeometry.Points(3, mobile: false);
        Assert.True(points[^1].Y < points[0].Y, "the dream stone must sit higher than the start stone");
    }

    [Fact]
    public void Mobile_band_rises_towards_the_end_side()
    {
        var points = CareerClimbGeometry.Points(3, mobile: true);
        Assert.True(points[^1].X > points[0].X);
        Assert.True(points[^1].Y < points[0].Y);
    }

    [Fact]
    public void Three_steps_sit_exactly_on_the_five_anchors()
    {
        var desktop = CareerClimbGeometry.Points(3, mobile: false);
        Assert.Equal(5, desktop.Count);
        Assert.Equal(70, desktop[0].X);
        Assert.Equal(690, desktop[0].Y);
    }
}
