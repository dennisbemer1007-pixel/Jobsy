using Jobsy.Web.Navigation;

namespace Jobsy.Tests;

public sealed class NotificationNavigationTests
{
    [Theory]
    [InlineData("/candidate/applications/00000000-0000-0000-0000-000000000001/after-match/panel-ssr", true)]
    [InlineData("/werkgever/sollicitaties/00000000-0000-0000-0000-000000000001/after-match/panel", true)]
    [InlineData("/candidate/applications", true)]
    [InlineData("/candidate/paspoort", true)]
    [InlineData("/werkgever/meldingen", false)]
    [InlineData("/notifications", false)]
    public void RequiresForceLoad_for_ssr_and_after_match_targets(string path, bool expected)
    {
        Assert.Equal(expected, NotificationNavigation.RequiresForceLoad(path));
    }
}
