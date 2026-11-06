using Jobsy.Web.Features;
using Jobsy.Web.Localization;

namespace Jobsy.Tests;

public class LandingTextTests
{
    [Fact]
    public void For_returns_Zw_sibling_when_present()
    {
        var on = LandingText.For("PublicFooter.BrandLine", LandingVariant.On, "nl");
        var zw = LandingText.For("PublicFooter.BrandLine", LandingVariant.Zw, "nl");
        Assert.Contains("werk", on, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("richting", zw, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual(on, zw);
    }

    [Fact]
    public void For_falls_back_to_base_key_when_Zw_missing()
    {
        var on = LandingText.For("PublicNav.Login", LandingVariant.On, "nl");
        var zw = LandingText.For("PublicNav.Login", LandingVariant.Zw, "nl");
        Assert.Equal(on, zw);
        Assert.Equal("Inloggen", zw);
    }
}
