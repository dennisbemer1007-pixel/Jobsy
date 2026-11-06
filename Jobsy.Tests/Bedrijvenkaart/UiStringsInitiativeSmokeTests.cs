using Jobsy.Web.Localization;

namespace Jobsy.Tests.Bedrijvenkaart;

public class UiStringsInitiativeSmokeTests
{
    [Fact]
    public void Common_Change_is_translated_nl_and_en()
    {
        Assert.Equal("Wijzigen", UiStrings.Get("Common.Change", "nl"));
        Assert.Equal("Change", UiStrings.Get("Common.Change", "en"));
    }
}
