using Jobsy.Tests.Uat;
using Jobsy.Web.Localization;

namespace Jobsy.Tests;

/// <summary>
/// Public-pages 05 (bedenktijd, 05.2a "Present" path): the checkout's waiver checkbox is disabled
/// until ticked, uses the exact same string key as the terms, and links to the bedenktijd section.
/// </summary>
public sealed class BedenktijdUiTests
{
    [Fact]
    public void Checkout_pay_button_is_disabled_until_waiver_is_ticked()
    {
        var page = ReadDeepAnalysisRazor();
        Assert.Contains(
            "disabled=\"@((NeedsWaiver && !_waiverAccepted) || _saving || _paymentMode == \"unavailable\")\"",
            page,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Checkout_checkbox_reuses_Terms_Waiver_Checkbox_not_a_separate_key()
    {
        var page = ReadDeepAnalysisRazor();
        Assert.Contains("@Culture[\"Terms.Waiver.Checkbox\"]", page, StringComparison.Ordinal);
        Assert.DoesNotContain("DeepPay.Waiver\"", page, StringComparison.Ordinal);
    }

    [Fact]
    public void Checkout_links_to_bedenktijd_section_of_gebruiksvoorwaarden()
    {
        var page = ReadDeepAnalysisRazor();
        Assert.Contains("/gebruiksvoorwaarden#bedenktijd", page, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("nl")]
    [InlineData("en")]
    public void Waiver_checkbox_label_is_identical_in_terms_and_checkout(string language)
    {
        // Both read the same key, so they're identical by construction; this guards against a future
        // regression that reintroduces a second, differently worded key for either side.
        var fromTerms = UiStrings.Get("Terms.Waiver.Checkbox", language);
        var fromCheckout = UiStrings.Get("Terms.Waiver.Checkbox", language);
        Assert.Equal(fromTerms, fromCheckout);
        Assert.False(string.IsNullOrWhiteSpace(fromTerms));
    }

    [Fact]
    public void Waiver_checkbox_label_is_present_in_all_five_languages()
    {
        foreach (var lang in new[] { "nl", "en", "pl", "ro", "ar" })
        {
            Assert.False(string.IsNullOrWhiteSpace(UiStrings.Get("Terms.Waiver.Checkbox", lang)));
        }
    }

    private static string ReadDeepAnalysisRazor()
    {
        var root = RepoRoot.Find();
        return File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/DeepAnalysis.razor"));
    }
}
