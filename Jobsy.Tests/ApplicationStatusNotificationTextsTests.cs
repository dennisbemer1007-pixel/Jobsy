using Jobsy.Core.Notifications;
using Jobsy.Infrastructure.Services;

namespace Jobsy.Tests;

public sealed class ApplicationStatusNotificationTextsTests
{
    [Fact]
    public void CvViewed_nl_en_no_em_dash()
    {
        var nl = ApplicationStatusNotificationTexts.CvViewed("Acme", "Kassamedewerker", "nl");
        var en = ApplicationStatusNotificationTexts.CvViewed("Acme", "Cashier", "en");
        Assert.Equal("CV bekeken", nl.Title);
        Assert.Contains("Acme", nl.Body, StringComparison.Ordinal);
        Assert.Equal("CV viewed", en.Title);
        Assert.DoesNotContain("—", nl.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("—", en.Body, StringComparison.Ordinal);
    }

    [Fact]
    public void MutualYes_matches_doc_deeplink_copy()
    {
        var nl = ApplicationStatusNotificationTexts.MutualYes("Acme", "Kassamedewerker", "nl");
        Assert.Equal("Wederzijds ja", nl.Title);
        Assert.Contains("gesprek plannen", nl.Body, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolveLanguageFromPreferencesJson_reads_language_field()
    {
        Assert.Equal("en", ApplicationStatusNotifier.ResolveLanguageFromPreferencesJson("{\"language\":\"en\"}"));
        Assert.Equal("nl", ApplicationStatusNotifier.ResolveLanguageFromPreferencesJson(null));
    }

    [Fact]
    public void ApplicationNewMessage_sm11_copy_nl_en()
    {
        var nl = ApplicationStatusNotificationTexts.ApplicationNewMessage("Kassier", "nl");
        var en = ApplicationStatusNotificationTexts.ApplicationNewMessage("Cashier", "en");
        Assert.Equal("Nieuw bericht", nl.Title);
        Assert.Equal("New message", en.Title);
        Assert.DoesNotContain("—", nl.Body, StringComparison.Ordinal);

        var self = ApplicationStatusNotificationTexts.ApplicationNewMessageSelf("Kassier", "nl");
        Assert.Equal("Bevestiging", self.Title);
        Assert.Contains("verstuurd", self.Body, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplicationWithdrawn_sm15_copy()
    {
        var employer = ApplicationStatusNotificationTexts.CandidateWithdrawn("Jan", "Kassier", "nl");
        Assert.Equal("Sollicitatie ingetrokken", employer.Title);

        var self = ApplicationStatusNotificationTexts.ApplicationWithdrawnSelf("Acme", "Kassier", "en");
        Assert.Equal("Confirmation", self.Title);
    }

    [Fact]
    public void ApplicationOutcome_sm12_sm14_copy()
    {
        var offer = ApplicationStatusNotificationTexts.ApplicationOutcomeOffer("Acme", "Kassier", "nl");
        Assert.Equal("Aanbod", offer.Title);
        Assert.DoesNotContain("—", offer.Body, StringComparison.Ordinal);

        var notNow = ApplicationStatusNotificationTexts.ApplicationOutcomeNotNow("Acme", "Kassier", "en");
        Assert.Equal("Not now", notNow.Title);
    }
}
