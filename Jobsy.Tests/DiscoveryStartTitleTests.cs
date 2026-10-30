using Jobsy.Web.Localization;

namespace Jobsy.Tests;

public sealed class DiscoveryStartTitleTests
{
    [Fact]
    public void Discovery_start_title_has_no_name_fallback()
    {
        Assert.Equal("Hoi, fijn dat je er bent", UiStrings.Get("Discovery.Start.TitleNoName", "nl"));
        Assert.DoesNotContain("{0}", UiStrings.Get("Discovery.Start.TitleNoName", "nl"), StringComparison.Ordinal);
    }
}
