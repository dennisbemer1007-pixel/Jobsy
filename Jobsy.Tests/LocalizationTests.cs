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
    public async Task Translation_stub_prefixes_when_language_differs()
    {
        ITranslationService sut = new TranslationServiceStub(NullLogger<TranslationServiceStub>.Instance);
        var result = await sut.TranslateVacancyAsync(
            "Barista",
            "Koffie zetten",
            "nl",
            "en");

        Assert.True(result.WasTranslated);
        Assert.StartsWith("[EN]", result.Title);
        Assert.Contains("Barista", result.Title);
        Assert.StartsWith("[EN]", result.Description);
    }
}
