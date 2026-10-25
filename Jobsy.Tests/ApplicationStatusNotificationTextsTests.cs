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
}
