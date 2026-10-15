using Jobsy.Web.Components.Brand;
using Jobsy.Web.Media;

namespace Jobsy.Tests;

public class MascotMarkupSmokeTests
{
    [Fact]
    public void Traced_rustig_markup_uses_valid_svg_quotes()
    {
        var inner = LobsyMascotTraced.InnerMarkup(MascotPose.Rustig);
        Assert.StartsWith("<g transform='translate(", inner, StringComparison.Ordinal);
        Assert.Contains("fill='#D93A12'", inner, StringComparison.Ordinal);
    }
}
