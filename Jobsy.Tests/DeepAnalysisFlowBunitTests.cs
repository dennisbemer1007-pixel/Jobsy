using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Tests.Uat;
using Jobsy.Web.Localization;

namespace Jobsy.Tests;

public sealed class DeepAnalysisFlowBunitTests
{
    [Fact]
    public void DeepAnalysis_Uses_TestQuestionFlow_And_No_Modal_Boosters()
    {
        var root = RepoRoot.Find();
        var page = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/DeepAnalysis.razor"));
        Assert.Contains("TestQuestionFlow", page, StringComparison.Ordinal);
        Assert.Contains("TestPageShell", page, StringComparison.Ordinal);
        Assert.Contains("DeepTestMotivation", page, StringComparison.Ordinal);
        Assert.DoesNotContain("DeepAnalysisBoosters", page, StringComparison.Ordinal);
        Assert.DoesNotContain("LobsyFriendlyDialog", page, StringComparison.Ordinal);
        Assert.DoesNotContain("DeepTestOrderDialog", page, StringComparison.Ordinal);
        Assert.DoesNotContain("QuestionnaireShell", page, StringComparison.Ordinal);
        Assert.Contains("Deep.UnknownTitle", page, StringComparison.Ordinal);
        Assert.Contains(
            "role=\"radiogroup\"",
            File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Shared/Questionnaire/LikertRadioGroup.razor")),
            StringComparison.Ordinal);
    }

    [Fact]
    public void TestDetail_Free_Primary_Paid_Goes_To_Offer()
    {
        var root = RepoRoot.Find();
        var page = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/TestDetail.razor"));
        Assert.DoesNotContain("DeepTestOrderDialog", page, StringComparison.Ordinal);
        Assert.Contains("GoToOfferAsync", page, StringComparison.Ordinal);
        Assert.Contains("Tests.StartFree", page, StringComparison.Ordinal);
        var freeIdx = page.IndexOf("Tests.StartFree", StringComparison.Ordinal);
        var paidIdx = page.IndexOf("Tests.ExtendedLabel", StringComparison.Ordinal);
        Assert.True(freeIdx > 0 && paidIdx > freeIdx);
    }

    [Fact]
    public void Checkout_Uses_TestPageShell_Three_States()
    {
        var root = RepoRoot.Find();
        var page = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/DeepAnalysisCheckout.razor"));
        Assert.Contains("TestPageShell", page, StringComparison.Ordinal);
        Assert.Contains("DeepPay.Checking", page, StringComparison.Ordinal);
        Assert.Contains("DeepPay.PaidTitle", page, StringComparison.Ordinal);
        Assert.Contains("DeepPay.FailedTitle", page, StringComparison.Ordinal);
        Assert.Contains("role=\"status\"", page, StringComparison.Ordinal);
    }

    [Fact]
    public void Offer_Strings_And_SecureMollie_Key_Exist()
    {
        Assert.Equal("Duik tot de bodem", UiStrings.Get("Deep.Offer.Title", "nl"));
        Assert.Equal("Veilig betalen via Mollie", UiStrings.Get("DeepPay.SecureMollie", "nl"));
        Assert.Contains("{0}", UiStrings.Get("DeepPay.Upsell", "nl"), StringComparison.Ordinal);
    }

    [Fact]
    public void OrderDialog_Deleted()
    {
        var root = RepoRoot.Find();
        Assert.False(File.Exists(Path.Combine(root, "Jobsy.Web/Components/Candidate/Tests/DeepTestOrderDialog.razor")));
    }

    [Theory]
    [InlineData(AssessmentKind.Competence)]
    [InlineData(AssessmentKind.Career)]
    public void Parts_Ordering_Preserves_Ids(AssessmentKind kind)
    {
        var parts = TestDepthRules.Parts(kind);
        Assert.Equal(5, parts.Count);
        var all = parts.SelectMany(p => p.QuestionIds).ToList();
        Assert.Equal(all.Count, all.Distinct().Count());
    }
}
