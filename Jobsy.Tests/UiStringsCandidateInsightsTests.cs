using Jobsy.Web.Localization;

namespace Jobsy.Tests;

public class UiStringsCandidateInsightsTests
{
    [Fact]
    public void All_five_languages_contain_every_Insights_key()
    {
        var nlKeys = CollectInsightsKeys();
        Assert.NotEmpty(nlKeys);

        foreach (var lang in new[] { "nl", "en", "pl", "ro", "ar" })
        {
            foreach (var key in nlKeys)
            {
                var value = UiStrings.Get(key, lang);
                Assert.False(string.IsNullOrWhiteSpace(value), $"{lang} missing {key}");
                Assert.NotEqual(key, value);
            }
        }
    }

    private static List<string> CollectInsightsKeys()
    {
        var nl = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var en = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var pl = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var ro = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var ar = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        UiStringsCandidateInsights.MergeAll(nl, en, pl, ro, ar);
        return nl.Keys.Where(k => k.StartsWith("Insights.", StringComparison.Ordinal)
                                  || k == "Nav.CandidateInsights")
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToList();
    }
}
