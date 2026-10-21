using Jobsy.Web.Localization;

namespace Jobsy.Tests;

public class DiscoveryCopyDashTests
{
    [Theory]
    [InlineData("Discovery.Intro.Why")]
    [InlineData("Discovery.Intro.Lobsy")]
    [InlineData("Discovery.Intro.LightLead")]
    [InlineData("Discovery.Step5.AboutMeHint")]
    [InlineData("Passport.Data.DislikesSub")]
    public void Dutch_discovery_copy_avoids_em_dash(string key)
    {
        var text = UiStrings.Get(key, "nl");
        Assert.DoesNotContain(" — ", text, StringComparison.Ordinal);
    }
}
