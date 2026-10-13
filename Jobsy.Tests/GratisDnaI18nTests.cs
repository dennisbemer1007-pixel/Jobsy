using Jobsy.Core.Rules;
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
}
