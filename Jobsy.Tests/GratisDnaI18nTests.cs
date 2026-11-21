using Jobsy.Core.Rules;
using Jobsy.Web.Features;
using Jobsy.Web.Localization;

namespace Jobsy.Tests;

public class GratisDnaI18nTests
{
    [Fact]
    public void All_GratisDna_keys_exist_in_five_languages()
    {
        var nl = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var en = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var pl = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var ro = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var ar = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        UiStringsGratisDna.MergeAll(nl, en, pl, ro, ar);

        Assert.NotEmpty(nl);
        foreach (var key in nl.Keys.Where(k => k.StartsWith("GratisDna.", StringComparison.OrdinalIgnoreCase)))
        {
            Assert.True(en.ContainsKey(key), $"Missing EN: {key}");
            Assert.True(pl.ContainsKey(key), $"Missing PL: {key}");
            Assert.True(ro.ContainsKey(key), $"Missing RO: {key}");
            Assert.True(ar.ContainsKey(key), $"Missing AR: {key}");
            Assert.False(string.IsNullOrWhiteSpace(nl[key]), $"Empty NL: {key}");
        }
    }

    [Fact]
    public void Landing_consent_privacy_block_is_not_dutch_in_non_nl_languages()
    {
        var keys = new[] { "GratisDna.Landing.Consent", "GratisDna.Questions.Privacy" };
        foreach (var lang in new[] { "en", "pl", "ro", "ar" })
        {
            foreach (var key in keys)
            {
                var text = LandingText.For(key, LandingVariant.On, lang);
                Assert.DoesNotContain("Je antwoorden blijven", text, StringComparison.Ordinal);
                Assert.DoesNotContain("Werkgevers zien ze nooit", text, StringComparison.Ordinal);
            }
        }

        Assert.Contains(
            "device",
            LandingText.For("GratisDna.Landing.Consent", LandingVariant.On, "en"),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Gratis_css_reserves_space_for_cookie_banner_on_landing()
    {
        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "css", "features", "gratis-dna.css"));
        Assert.Contains("html:not(.cookie-consent-known) .pub-theme .gd-page--warm .gd-page__inner", css, StringComparison.Ordinal);
        Assert.Contains("--pub-cookie-banner-height", css, StringComparison.Ordinal);
    }

    [Fact]
    public void Nl_tile_sentences_match_OnboardingImpressionLibrary()
    {
        var nl = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var en = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var pl = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var ro = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var ar = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        UiStringsGratisDna.MergeAll(nl, en, pl, ro, ar);

        void AssertStrength(string code)
            => Assert.Equal(
                OnboardingImpressionLibrary.StrengthSentence(code),
                nl[$"GratisDna.Tile.Strength.{code}"]);

        void AssertRiasec(string code)
            => Assert.Equal(
                OnboardingImpressionLibrary.RiasecSentence(code),
                nl[$"GratisDna.Tile.Riasec.{code}"]);

        void AssertCulture(string code)
            => Assert.Equal(
                OnboardingImpressionLibrary.CultureSentence(code),
                nl[$"GratisDna.Tile.Culture.{code}"]);

        void AssertValue(string code)
            => Assert.Equal(
                OnboardingImpressionLibrary.ValueSentence(code),
                nl[$"GratisDna.Tile.Value.{code}"]);

        foreach (var code in CompetencyTestCatalog.CategoryCodes.Concat([CompetencyTestCatalog.Extraversie]))
        {
            AssertStrength(code);
        }

        foreach (var code in new[]
                 {
                     CareerTestCatalog.Realistic, CareerTestCatalog.Investigative, CareerTestCatalog.Artistic,
                     CareerTestCatalog.Social, CareerTestCatalog.Enterprising, CareerTestCatalog.Conventional
                 })
        {
            AssertRiasec(code);
        }

        foreach (var code in OnboardingWizardCatalog.CultureDimensionCodes.Concat([CulturePersonalityCatalog.Innovation]))
        {
            AssertCulture(code);
        }

        foreach (var code in SchwartzValuesCatalog.CategoryCodes)
        {
            AssertValue(code);
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}
