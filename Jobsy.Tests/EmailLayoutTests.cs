using Jobsy.Core.Email;
using Jobsy.Core.Email.Model;

namespace Jobsy.Tests;

/// <summary>
/// Former EmailLayoutTests — layout/buttons/facts are covered by EmailRendererTests.
/// Deep-link expectations live in EmailLinksTests.
/// </summary>
public class EmailLayoutTests
{
    [Fact]
    public void Renderer_replaces_legacy_wrap_contract()
    {
        var mail = TransactionalEmails.MailTest("https://lobsy.nl");
        Assert.Contains("data-lobsy-layout=\"2\"", mail.Html);
        Assert.Contains("lobsy-mark-72.png", mail.Html);
        Assert.Contains("Lobsy", mail.Html);
        Assert.Contains(EmailTheme.Light.Brand, mail.Html);
        Assert.DoesNotContain("cid:", mail.Html);
    }

    [Fact]
    public void Format_helpers_moved_to_EmailFormat()
    {
        Assert.Contains("€", EmailFormat.FormatEuro(12.5m));
        Assert.EndsWith(" km", EmailFormat.FormatKm(1.2));
    }

    [Fact]
    public void EmailText_format_supports_bold_args()
    {
        var text = EmailText.Format("Hallo {0}", EmailArg.Bold("Wereld"));
        Assert.Equal("Hallo Wereld", text.Flatten());
        Assert.Contains(text.Segments, s => s is BoldSegment);
    }
}
