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
    public void Prematch_self_sm02b_sm03b_copy()
    {
        var cv = ApplicationStatusNotificationTexts.CvViewedSelf("Jan", "Kassier", "nl");
        Assert.Equal("Bevestiging", cv.Title);
        Assert.Contains("Jan", cv.Body, StringComparison.Ordinal);

        var yes = ApplicationStatusNotificationTexts.MutualYesSelf("Jan", "Kassier", "en");
        Assert.Equal("Confirmation", yes.Title);
    }

    [Fact]
    public void ResolveLanguageFromPreferencesJson_reads_language_field()
    {
        Assert.Equal("en", ApplicationStatusNotifier.ResolveLanguageFromPreferencesJson("{\"language\":\"en\"}"));
        Assert.Equal("nl", ApplicationStatusNotifier.ResolveLanguageFromPreferencesJson(null));
    }

    [Fact]
    public void AfterMatch_sm04_sm10_copy_matches_doc_no_em_dash()
    {
        var nl = ApplicationStatusNotificationTexts.InterviewSlotsProposed("Acme", "Kassier", "nl");
        var en = ApplicationStatusNotificationTexts.InterviewSlotsProposed("Acme", "Cashier", "en");
        Assert.Equal("Momenten voorgesteld", nl.Title);
        Assert.Equal("Times proposed", en.Title);
        Assert.DoesNotContain("—", nl.Body, StringComparison.Ordinal);

        var reschedule = ApplicationStatusNotificationTexts.InterviewRescheduleRequested("Functie", "nl");
        Assert.Equal("Gesprek verzet", reschedule.Title);
        Assert.DoesNotContain("%", reschedule.Body, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplicationSubmitted_sm01_copy()
    {
        var employer = ApplicationStatusNotificationTexts.EmployerNewApplication("Acme", "Kassier", "nl");
        Assert.Equal("Nieuwe sollicitatie", employer.Title);

        var self = ApplicationStatusNotificationTexts.ApplicationSubmittedSelf("Acme", "Kassier", "en");
        Assert.Equal("Application sent", self.Title);
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
    public void ApplicationOutcome_sm12_sm14_copy()
    {
        var offer = ApplicationStatusNotificationTexts.ApplicationOutcomeOffer("Acme", "Kassier", "nl");
        Assert.Equal("Aanbod", offer.Title);
        Assert.DoesNotContain("—", offer.Body, StringComparison.Ordinal);

        var notNow = ApplicationStatusNotificationTexts.ApplicationOutcomeNotNow("Acme", "Kassier", "en");
        Assert.Equal("Not now", notNow.Title);
    }
}
