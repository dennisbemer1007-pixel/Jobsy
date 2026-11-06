using Jobsy.Core.Email;
using Jobsy.Core.Email.Localization;
using Jobsy.Core.Email.Model;
using Jobsy.Core.Security;

namespace Jobsy.Tests;

public class AccountEmailCopyTests
{
    private static readonly string[] Keys =
    [
        "RegistrationActivation",
        "RegistrationCredentials",
        "TakeoverEmailVerification",
        "TakeoverRequest",
        "TakeoverSubmitted",
        "TakeoverApproved",
        "TakeoverRejected",
        "SalesManagerInvite",
        "AmbassadeurInvite",
        "AccountLockout",
        "SupportAccessRequested",
        "MailTest",
        "MfaResetByAdmin"
    ];

    [Fact]
    public void Account_mails_cta_and_mascot_rules()
    {
        var ctx = EmailSampleContext.ForPreview("https://lobsy.nl");
        foreach (var key in Keys)
        {
            var mail = TransactionalEmails.Compose(key, ctx);
            var def = EmailTemplateRegistry.GetRequired(key);
            var cta = Count(mail.Html, "data-lobsy-cta");
            Assert.Equal(def.Kind == EmailKind.Security ? 0 : 1, cta);
            if (key is "RegistrationCredentials" or "TakeoverApproved")
            {
                Assert.Contains("width=\"64\"", mail.Html, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void No_entra_sbi_or_lobsy_subject_suffix_or_nieuw_wachtwoord()
    {
        foreach (var lang in EmailStrings.Languages)
        {
            foreach (var (key, value) in EmailStrings.All[lang])
            {
                Assert.DoesNotContain("Entra", value, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("SBI", value, StringComparison.Ordinal);
                Assert.DoesNotContain("nieuw wachtwoord", value, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("new password", value, StringComparison.OrdinalIgnoreCase);
                if (key.EndsWith(".Subject", StringComparison.Ordinal) || key.EndsWith(".SubjectWithCode", StringComparison.Ordinal))
                {
                    Assert.DoesNotContain("— Lobsy", value, StringComparison.Ordinal);
                    // Suffix " - Lobsy" is banned on rewritten keys; allowlist check uses em dash and end marker.
                    Assert.False(value.TrimEnd().EndsWith(" - Lobsy", StringComparison.Ordinal), key);
                }
            }
        }
    }

    [Theory]
    [InlineData(1, 15)]
    [InlineData(2, 30)]
    [InlineData(3, 60)]
    [InlineData(4, 120)]
    [InlineData(5, 240)]
    [InlineData(9, 240)]
    public void AccountLockout_duration_matches_login_lockout_rules(int lockoutsInWindow, int expectedMinutes)
    {
        var duration = LoginLockoutRules.LockoutDuration(lockoutsInWindow);
        Assert.Equal(TimeSpan.FromMinutes(expectedMinutes), duration);
        var mail = TransactionalEmails.AccountLockout(
            "https://lobsy.nl", LoginLockoutRules.FailedAttemptsBeforeLockout, DateTime.UtcNow.Add(duration), duration);
        Assert.DoesNotContain("nieuw wachtwoord", mail.Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("mailto:", mail.Html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TakeoverRequest_may_name_requester_for_manager_decision()
    {
        var mail = TransactionalEmails.TakeoverRequest(
            "https://lobsy.nl", "Bakkerij", "123456789012", "Sanne van Dijk", "sanne@example.nl",
            DateTime.UtcNow);
        Assert.Contains("Sanne van Dijk", mail.Text, StringComparison.Ordinal);
        Assert.Contains("sanne@example.nl", mail.Text, StringComparison.Ordinal);
    }


    [Theory]
    [InlineData(15)]
    [InlineData(240)]
    public void AccountLockout_nl_body_contains_duration_label(int minutes)
    {
        var duration = TimeSpan.FromMinutes(minutes);
        var label = EmailFormat.Duration(duration, EmailCulture.Nl);
        var mail = TransactionalEmails.AccountLockout(
            "https://lobsy.nl",
            LoginLockoutRules.FailedAttemptsBeforeLockout,
            DateTime.UtcNow.Add(duration),
            duration,
            EmailCulture.Nl);
        Assert.Contains(label, mail.Text, StringComparison.Ordinal);
        Assert.Contains("Wachtwoord vergeten?", mail.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("nieuw wachtwoord", mail.Text, StringComparison.OrdinalIgnoreCase);
    }

    private static int Count(string haystack, string needle)
    {
        var n = 0;
        var i = 0;
        while ((i = haystack.IndexOf(needle, i, StringComparison.Ordinal)) >= 0)
        {
            n++;
            i += needle.Length;
        }

        return n;
    }
}
