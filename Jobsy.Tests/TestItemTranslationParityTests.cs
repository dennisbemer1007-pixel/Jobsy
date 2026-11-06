using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Tests.Uat;
using Jobsy.Web.Localization;

namespace Jobsy.Tests;

public sealed class TestItemTranslationParityTests
{
    private static readonly string[] Langs = ["en", "pl", "ro", "ar"];

    [Fact]
    public void Free_test_item_keys_exist_in_all_five_languages_and_differ_from_nl()
    {
        var prefixes = new[] { "Competency.Q", "Career.Q", "CultureScan.Q", "ValuesScan.Q" };
        var allow = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Proper nouns / codes that may match across languages
            "RIASEC", "OCEAN", "IPIP", "Mollie", "PDF", "Lobsy"
        };

        foreach (var prefix in prefixes)
        {
            for (var i = 1; i <= 25; i++)
            {
                var key = $"{prefix}{i:00}";
                if (prefix == "CultureScan.Q" && i > 18) continue;
                var nl = UiStrings.Get(key, "nl");
                Assert.False(string.IsNullOrWhiteSpace(nl), key);
                foreach (var lang in Langs)
                {
                    var value = UiStrings.Get(key, lang);
                    Assert.False(string.IsNullOrWhiteSpace(value), $"{key}/{lang}");
                    if (allow.Contains(value)) continue;
                    Assert.False(
                        string.Equals(value, nl, StringComparison.Ordinal),
                        $"{key}/{lang} still equals nl");
                }
            }
        }
    }

    [Theory]
    [InlineData(AssessmentKind.Competence)]
    [InlineData(AssessmentKind.Career)]
    [InlineData(AssessmentKind.Culture)]
    [InlineData(AssessmentKind.Values)]
    public void Deep_items_localized_for_shipped_languages_with_zero_fallback(AssessmentKind kind)
    {
        DeepItemLocalizations.ResetFallbackCountsForTests();
        var questions = DeepAnalysisCatalog.QuestionsFor(kind);
        foreach (var lang in Langs)
        {
            var map = DeepItemLocalizations.Load(kind, lang);
            Assert.Equal(questions.Count, map.Count);
            foreach (var q in questions)
            {
                var text = DeepItemLocalizations.Resolve(kind, q.Id, q.PromptNl, "ex", lang);
                Assert.False(string.IsNullOrWhiteSpace(text.Prompt));
                Assert.False(string.Equals(text.Prompt, q.PromptNl, StringComparison.Ordinal),
                    $"{kind}/{q.Id}/{lang} equals nl");
            }

            Assert.Equal(0, DeepItemLocalizations.FallbackCount(kind, lang));
        }
    }

    [Fact]
    public void TestFlow_Scale_and_Depth_exist_in_five_languages()
    {
        foreach (var key in new[]
                 {
                     "TestFlow.Scale.Low", "TestFlow.Scale.High",
                     "TestDepth.First", "TestDepth.Deeper", "TestDepth.Full", "TestDepth.Bottom",
                     "DeepPay.Title", "Deep.Offer.Title"
                 })
        {
            var nl = UiStrings.Get(key, "nl");
            foreach (var lang in Langs)
            {
                var v = UiStrings.Get(key, lang);
                Assert.False(string.IsNullOrWhiteSpace(v));
                Assert.False(string.Equals(nl, v, StringComparison.Ordinal));
            }
        }
    }

    [Fact]
    public void Tests_css_uses_no_physical_left_right()
    {
        var root = RepoRoot.Find();
        var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/features/tests.css"));
        Assert.DoesNotContain("margin-left:", css, StringComparison.Ordinal);
        Assert.DoesNotContain("margin-right:", css, StringComparison.Ordinal);
        Assert.DoesNotContain("padding-left:", css, StringComparison.Ordinal);
        Assert.DoesNotContain("padding-right:", css, StringComparison.Ordinal);
        Assert.DoesNotContain("\n  left:", css, StringComparison.Ordinal);
        Assert.DoesNotContain("\n  right:", css, StringComparison.Ordinal);
        Assert.DoesNotContain("float: left", css, StringComparison.Ordinal);
        Assert.DoesNotContain("float: right", css, StringComparison.Ordinal);
        Assert.Contains("[dir=\"rtl\"]", css, StringComparison.Ordinal);
    }

    [Fact]
    public void Review_csvs_exist_for_dennis()
    {
        var root = RepoRoot.Find();
        Assert.True(File.Exists(Path.Combine(root, "docs/i18n/tests-deep-items-review.csv")));
        Assert.True(File.Exists(Path.Combine(root, "docs/i18n/tests-items-review.csv")));
    }
}
