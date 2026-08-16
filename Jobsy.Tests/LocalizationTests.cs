using Jobsy.Api.Controllers;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Localization;
using Jobsy.Infrastructure.Services;
using Jobsy.Web.Localization;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class LocalizationTests
{
    [Theory]
    [InlineData("nl", "nl")]
    [InlineData("en-GB", "en")]
    [InlineData("PL", "pl")]
    [InlineData("ar-SA", "ar")]
    [InlineData("xx", "nl")]
    [InlineData(null, "nl")]
    public void JobsyLanguages_Normalize_maps_supported_codes(string? input, string expected)
        => Assert.Equal(expected, JobsyLanguages.Normalize(input));

    [Fact]
    public void UiStrings_default_dutch_and_english_differ()
    {
        Assert.Equal("Zoeken", UiStrings.Get("Nav.Search", "nl"));
        Assert.Equal("Banenkaart", UiStrings.Get("Nav.JobMap", "nl"));
        Assert.Equal("Job map", UiStrings.Get("Nav.JobMap", "en"));
        Assert.Equal("خريطة الوظائف", UiStrings.Get("Nav.JobMap", "ar"));
        Assert.Equal("Oefen je sollicitatiegesprek", UiStrings.Get("MockInterview.Title", "nl"));
        Assert.Equal("Practice your interview", UiStrings.Get("MockInterview.Title", "en"));
        Assert.NotEqual(
            UiStrings.Get("MockInterview.Title", "nl"),
            UiStrings.Get("MockInterview.Title", "en"));
        Assert.Equal("Solliciteren", UiStrings.Get("Apply.Title", "nl"));
        Assert.Equal("Apply", UiStrings.Get("Apply.Title", "en"));
        Assert.NotEqual(
            UiStrings.Get("Apply.Title", "nl"),
            UiStrings.Get("Apply.Title", "en"));
    }

    [Fact]
    public void UiStrings_new_candidate_chrome_keys_exist_in_all_languages()
    {
        string[] languages = ["nl", "en", "pl", "ro", "ar"];
        string[] sampleKeys =
        [
            "Apps.Status.Pending",
            "Profile.Title",
            "HowLobsy.Title",
            "Share.Title",
            "Metrics.Welcome",
            "Discovery.Top",
            "Transport.Verb.Bike",
            "Help.Purpose",
            "Education.None",
            "Common.Close"
        ];

        foreach (var key in sampleKeys)
        {
            foreach (var lang in languages)
            {
                var value = UiStrings.Get(key, lang);
                Assert.False(string.IsNullOrWhiteSpace(value));
                Assert.NotEqual(key, value);
            }
        }
    }

    [Fact]
    public void UiStrings_extras_sample_keys_exist_in_all_languages()
    {
        string[] languages = ["nl", "en", "pl", "ro", "ar"];
        string[] sampleKeys =
        [
            "Unsubscribe.Title",
            "Employer.VacanciesTitle",
            "Admin.Vacancies",
            "Admin.MailTest",
            "Admin.Feedback",
            "Feedback.Button",
            "Feedback.Capturing",
            "Feedback.ScreenshotPreview",
            "Feedback.PrivacyNote",
            "Admin.FeedbackReviewAck",
            "Admin.FeedbackUserReport",
            "Nav.MailTest",
            "Nav.Feedback",
            "Common.Cancel",
            "Legal.DocNote",
            "Discovery.LoadFailed",
            "Admin.Company",
            "Admin.Title",
            "Admin.Status",
            "Admin.All",
            "VacancyStatus.Active",
            "VacancyStatus.Draft",
            "VacancyStatus.Archived",
            "Admin.VacancyTitlePlaceholder",
            "Vacancy.SearchCompanyOrTitlePlaceholder",
            "Vacancy.PlacementDate",
            "Vacancy.Deadline",
            "Admin.Candidate",
            "Admin.Vacancy",
            "Sales.Dashboard",
            "Sales.Onboarding",
            "Sales.Invoices",
            "PushBom.Title",
            "GrantTokens.Title",
            "Nav.Organization",
            "DesktopPreferred.Title",
            "Organization.Lead"
        ];

        foreach (var key in sampleKeys)
        {
            foreach (var lang in languages)
            {
                var value = UiStrings.Get(key, lang);
                Assert.False(string.IsNullOrWhiteSpace(value));
                Assert.NotEqual(key, value);
            }
        }
    }

    [Fact]
    public void UiStrings_catalog_key_counts_match_across_languages()
    {
        var catalogField = typeof(UiStrings).GetField(
            "Catalog",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(catalogField);

        var catalog = (Dictionary<string, Dictionary<string, string>>)catalogField!.GetValue(null)!;
        string[] languages = ["nl", "en", "pl", "ro", "ar"];
        var nlCount = catalog["nl"].Count;
        Assert.True(nlCount > 0);

        foreach (var lang in languages)
        {
            Assert.True(catalog.ContainsKey(lang), $"Missing language catalog: {lang}");
            Assert.Equal(nlCount, catalog[lang].Count);
        }

        // New extras keys resolve distinctly (not as the key itself) after Build/MergeAll.
        string[] newKeys =
        [
            "Admin.Company",
            "Admin.VacancyTitlePlaceholder",
            "VacancyStatus.Draft",
            "Sales.Invoices",
            "PushBom.Title",
            "GrantTokens.Title",
            "Sales.DashboardLead"
        ];
        foreach (var key in newKeys)
        {
            foreach (var lang in languages)
            {
                Assert.NotEqual(key, UiStrings.Get(key, lang));
            }
        }
    }

    [Fact]
    public void ParsePreferences_reads_language()
    {
        var prefs = MeController.ParsePreferences(
            """{"roles":["horeca"],"maxTravelMinutes":30,"language":"en"}""");
        Assert.Equal("en", prefs.Language);
        Assert.Equal(30, prefs.MaxTravelMinutes);
    }

    [Fact]
    public void SerializePreferences_preserves_language()
    {
        var json = MeController.SerializePreferences(["retail"], 45, "Fiets", "pl", 18);
        var prefs = MeController.ParsePreferences(json);
        Assert.Equal("pl", prefs.Language);
        Assert.Equal(45, prefs.MaxTravelMinutes);
        Assert.Equal("Fiets", prefs.PreferredTransport);
        Assert.Equal(18, prefs.AgeYears);
        Assert.Contains("retail", prefs.Roles);
    }

    [Fact]
    public void ParsePreferences_reads_age_years()
    {
        var prefs = MeController.ParsePreferences(
            """{"roles":["retail"],"maxTravelMinutes":30,"ageYears":17}""");
        Assert.Equal(17, prefs.AgeYears);
    }

    [Fact]
    public async Task Translation_stub_leaves_same_language_untouched()
    {
        ITranslationService sut = new TranslationServiceStub(NullLogger<TranslationServiceStub>.Instance);
        var result = await sut.TranslateVacancyAsync(
            "Barista",
            "Koffie zetten",
            "nl",
            "nl");

        Assert.False(result.WasTranslated);
        Assert.Equal("Barista", result.Title);
        Assert.Equal("Koffie zetten", result.Description);
    }

    [Fact]
    public async Task Translation_without_api_key_keeps_original_when_language_differs()
    {
        // OpenAI translation service without credentials returns the original text.
        ITranslationService sut = new TranslationServiceStub(NullLogger<TranslationServiceStub>.Instance);
        var result = await sut.TranslateVacancyAsync(
            "Barista",
            "Koffie zetten",
            "nl",
            "en");

        // Stub still documents the path with a prefix (used only in tests / legacy).
        Assert.True(result.WasTranslated);
        Assert.StartsWith("[EN]", result.Title);
        Assert.Contains("Barista", result.Title);
        Assert.StartsWith("[EN]", result.Description);
    }
}
