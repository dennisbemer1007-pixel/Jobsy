namespace Jobsy.Tests;

public sealed class LineIconsTests
{
    [Theory]
    [InlineData("eye")]
    [InlineData("save")]
    [InlineData("warning")]
    [InlineData("compass")]
    public void Resolve_returns_svg(string name)
    {
        var svg = Jobsy.Web.Components.Ui.LineIcons.Resolve(name);
        Assert.False(string.IsNullOrWhiteSpace(svg));
        Assert.Contains("<svg", svg, StringComparison.Ordinal);
        Assert.Contains(Jobsy.Web.Components.Ui.LineIcons.StrokeWidth, svg, StringComparison.Ordinal);
    }
}
