using Jobsy.Core.Email;
using Jobsy.Core.Email.Localization;
using Jobsy.Core.Email.Model;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class CandidateEmailCopyTests
{
    private static readonly string[] CandidateKeys =
    [
        "ApplicationConfirmation",
        "ApplicationVerificationCode",
        "EmployerReactionAccepted",
        "EmployerReactionRejected",
        "EmployerContacting",
        "ApplicationHired",
        "ApplicationFilledElsewhere",
        "PushBom",
        "AccountUnsubscribeVerification",
        "ParentalConsent"
    ];

    [Fact]
    public void Candidate_mails_have_expected_cta_and_mascot_rules()
    {
        var ctx = EmailSampleContext.ForPreview("https://lobsy.nl");
        foreach (var key in CandidateKeys)
        {
            var mail = TransactionalEmails.Compose(key, ctx);
            var ctaCount = CountOccurrences(mail.Html, "data-lobsy-cta");
            var def = EmailTemplateRegistry.GetRequired(key);
            if (def.Kind == EmailKind.Security)
            {
                Assert.Equal(0, ctaCount);
            }
            else
            {
                Assert.Equal(1, ctaCount);
            }

            var hasMascot = mail.Html.Contains("mascot", StringComparison.OrdinalIgnoreCase)
                            || mail.Html.Contains("/images/email/", StringComparison.Ordinal)
                               && mail.Html.Contains("width=\"64\"", StringComparison.Ordinal);
            // Good-news keys show mascot image at 64px besides the logo.
            var expectMascot = key is "EmployerReactionAccepted" or "EmployerContacting" or "ApplicationHired";
            if (expectMascot)
            {
                Assert.Contains("width=\"64\"", mail.Html, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Greeting_uses_first_name_only()
    {
        var mail = TransactionalEmails.ApplicationConfirmation(
            "https://lobsy.nl", "Alex de Tester", "Weekendhulp", "Bakkerij");
        Assert.Contains("Hoi Alex,", mail.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Hoi Alex de Tester,", mail.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void No_stub_klik_hier_emdash_or_pushbom_brand_in_any_language()
    {
        foreach (var lang in EmailStrings.Languages)
        {
            foreach (var (key, value) in EmailStrings.All[lang])
            {
                Assert.DoesNotContain("stub", value, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain('\u2014', value);
                Assert.DoesNotContain('\u2013', value);
                if (key.EndsWith(".Cta", StringComparison.Ordinal))
                {
                    Assert.False(
                        string.Equals(value.Trim(), "Klik hier", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(value.Trim(), "Click here", StringComparison.OrdinalIgnoreCase),
                        key);
                }

                if (!key.StartsWith("Email.PushBom", StringComparison.Ordinal))
                {
                    Assert.DoesNotContain("PushBom", value, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
    }

    [Fact]
    public void Nl_email_values_have_no_literal_minute_or_day_counts()
    {
        foreach (var (key, value) in EmailStrings.All["nl"])
        {
            Assert.DoesNotMatch(@"\b\d+\s*(minuten|dagen)\b", value);
        }
    }

    [Fact]
    public void ParentalConsent_in_registry_with_parent_reason_and_toestemming_link()
    {
        Assert.True(EmailTemplateRegistry.TryGet("ParentalConsent", out var def));
        Assert.Equal("ParentAsked", def.ReasonKey);
        var mail = TransactionalEmails.ParentalConsent(
            "https://lobsy.nl", "Sanne", "https://lobsy.nl/toestemming?t=abc",
            new DateTime(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc));
        Assert.Contains("/toestemming?t=", mail.Html, StringComparison.Ordinal);
        Assert.Contains("Sanne", mail.Text, StringComparison.Ordinal);
        Assert.Contains("ouder", mail.Text, StringComparison.OrdinalIgnoreCase);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var idx = 0;
        while ((idx = haystack.IndexOf(needle, idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += needle.Length;
        }

        return count;
    }
}

public class ApplicationVerificationCodeLifetimeTests
{
    [Fact]
    public void Mail_and_constant_share_ttl_minutes()
    {
        var previous = ApplicationRules.EmailVerificationCodeLifetime;
        try
        {
            ApplicationRules.EmailVerificationCodeLifetime = TimeSpan.FromMinutes(7);
            var mail = TransactionalEmails.ApplicationVerificationCode(
                "https://lobsy.nl", "Alex", "Weekendhulp", Guid.NewGuid(), "123456");
            Assert.Contains("7", mail.Text, StringComparison.Ordinal);
            Assert.DoesNotContain("10 minuten", mail.Text, StringComparison.Ordinal);
        }
        finally
        {
            ApplicationRules.EmailVerificationCodeLifetime = previous;
        }
    }
}
