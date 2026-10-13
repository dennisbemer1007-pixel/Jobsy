using Jobsy.Core.Email;
using Jobsy.Core.Options;
using Jobsy.Infrastructure.Services;

namespace Jobsy.Tests;

public class FromResolutionTests
{
    [Fact]
    public void Db_wins_then_config_then_default_and_adds_display_name()
    {
        Assert.Equal(
            "Custom <db@mail.lobsy.nl>",
            MailAddressResolution.ResolveFromAddress("Custom <db@mail.lobsy.nl>", new MailOptions { FromAddress = "cfg@mail.lobsy.nl" }));

        Assert.Equal(
            "Lobsy <cfg@mail.lobsy.nl>",
            MailAddressResolution.ResolveFromAddress(null, new MailOptions { FromAddress = "cfg@mail.lobsy.nl" }));

        Assert.Contains("hallo@mail.lobsy.nl", MailAddressResolution.ResolveFromAddress(null, new MailOptions()), StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("Lobsy <", MailAddressResolution.EnsureLobsyDisplayName("bare@mail.lobsy.nl"), StringComparison.Ordinal);
    }

    [Fact]
    public void Production_warning_when_domain_is_not_mail_lobsy_nl()
    {
        Assert.False(MailAddressResolution.IsFromDomainMismatch("Lobsy <hallo@mail.lobsy.nl>"));
        Assert.True(MailAddressResolution.IsFromDomainMismatch("Lobsy <noreply@lobsy.nl>"));
        Assert.Equal("support@lobsy.nl", SmtpEmailService.EffectiveReplyTo(new MailOptions()));
    }
}
