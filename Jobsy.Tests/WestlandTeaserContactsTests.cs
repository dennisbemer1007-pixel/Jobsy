using Jobsy.Web.Teaser;

namespace Jobsy.Tests;

public class WestlandTeaserContactsTests
{
    [Fact]
    public void BuildWhatsAppUrl_UsesConfiguredDigits()
    {
        var url = WestlandTeaserContacts.BuildWhatsAppUrl("+31 6 1234 5678");
        Assert.StartsWith("https://wa.me/31612345678?text=", url);
        Assert.Contains(Uri.EscapeDataString(WestlandTeaserContacts.DefaultWhatsAppMessage), url);
    }

    [Fact]
    public void BuildWhatsAppUrl_FallsBackWhenEmpty()
    {
        var url = WestlandTeaserContacts.BuildWhatsAppUrl("  ");
        Assert.StartsWith($"https://wa.me/{WestlandTeaserContacts.DefaultWhatsAppE164}?text=", url);
    }
}
