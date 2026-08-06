using Jobsy.Web.Teaser;

namespace Jobsy.Tests;

public class WestlandTeaserContactsTests
{
    [Fact]
    public void TryBuildWhatsAppUrl_UsesConfiguredDigits()
    {
        var url = WestlandTeaserContacts.TryBuildWhatsAppUrl("+31 6 1234 5678");
        Assert.NotNull(url);
        Assert.StartsWith("https://wa.me/31612345678?text=", url);
        Assert.Contains(Uri.EscapeDataString(WestlandTeaserContacts.DefaultWhatsAppMessage), url);
    }

    [Fact]
    public void TryBuildWhatsAppUrl_ReturnsNullWhenEmpty()
    {
        Assert.Null(WestlandTeaserContacts.TryBuildWhatsAppUrl("  "));
        Assert.Null(WestlandTeaserContacts.TryBuildWhatsAppUrl(null));
    }

    [Fact]
    public void TryBuildWhatsAppUrl_RejectsPlaceholderNumber()
    {
        Assert.Null(WestlandTeaserContacts.TryBuildWhatsAppUrl("31600000000"));
    }
}
