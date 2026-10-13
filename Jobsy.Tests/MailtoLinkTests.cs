using Jobsy.Web.Services;

namespace Jobsy.Tests;

public class MailtoLinkTests
{
    [Fact]
    public void Body_encodes_newlines_once_as_percent_0A()
    {
        var href = MailtoLink.Build("Onderwerp", "Hoi,\n\nRegel twee\n");
        Assert.Contains("body=", href, StringComparison.Ordinal);
        var body = href.Split("body=", 2)[1];
        Assert.Contains("%0A", body, StringComparison.Ordinal);
        Assert.DoesNotContain("%250A", body, StringComparison.Ordinal);
        Assert.StartsWith("mailto:?subject=", href, StringComparison.Ordinal);
    }
}
