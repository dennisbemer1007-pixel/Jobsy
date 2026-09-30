using Jobsy.Tests.Uat;
using Jobsy.Web.Localization;

namespace Jobsy.Tests;

public sealed class TestsStackCleanupGuards
{
    [Fact]
    public void Candidate_test_ui_keys_avoid_legacy_product_names()
    {
        // Keys introduced/owned by the tests stack (01–06) must not use legacy product names.
        var forbidden = new[] { "diepte-analyse", "Diepteanalyse", "Quick-Scan", "officiële" };
        var keys = new[]
        {
            "DeepPay.Title", "Deep.Offer.Title", "Deep.Offer.Lead", "TestDepth.Bottom",
            "TestFlow.Begin", "DeepPay.Upsell", "Deep.Done.Lead"
        };
        foreach (var key in keys)
        {
            foreach (var lang in new[] { "nl", "en", "pl", "ro", "ar" })
            {
                var value = UiStrings.Get(key, lang);
                foreach (var bad in forbidden)
                {
                    Assert.DoesNotContain(bad, value, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
    }

    [Fact]
    public void DeepAnalysisBoosters_and_OrderDialog_are_gone()
    {
        var root = RepoRoot.Find();
        Assert.False(File.Exists(Path.Combine(root, "Jobsy.Core/Rules/DeepAnalysisBoosters.cs")));
        Assert.False(File.Exists(Path.Combine(root, "Jobsy.Web/Components/Candidate/Tests/DeepTestOrderDialog.razor")));
    }

    [Fact]
    public void FormatUpsellCopy_returns_key_not_official_copy()
    {
        var copy = Jobsy.Infrastructure.Services.DeepAnalysisService.FormatUpsellCopy(2.99m);
        Assert.Equal("DeepPay.Upsell", copy);
        Assert.DoesNotContain("officiële", copy, StringComparison.OrdinalIgnoreCase);
    }
}
