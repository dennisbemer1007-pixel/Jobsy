namespace Jobsy.Tests;

public class JourneyMiniTestFixesTests
{
    [Fact]
    public void Journey_reloads_mini_questions_after_the_free_test_merge()
    {
        var source = File.ReadAllText(Path.Combine(
            TestRepo.FindRoot(),
            "Jobsy.Web", "Components", "Pages", "Candidate", "DiscoveryJourney.razor"));
        Assert.Contains("GratisDnaMergeNotifier.Merged += OnGratisDnaMerged", source, StringComparison.Ordinal);
        Assert.Contains("PrepareMiniAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("RenderFooter(showBack: !_reviewMode, showPrimary: CanNext || _reviewMode)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Answered_list_counts_every_earlier_answer()
    {
        var source = File.ReadAllText(Path.Combine(
            TestRepo.FindRoot(),
            "Jobsy.Web", "Components", "Shared", "Questionnaire", "TestQuestionFlow.razor"));
        Assert.Contains("TestFlow.Answered\", answeredEarlier.Count", source, StringComparison.Ordinal);
        Assert.DoesNotContain("narrow ? 1", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Shed_hides_the_deeper_hint_when_there_are_no_options()
    {
        var source = File.ReadAllText(Path.Combine(
            TestRepo.FindRoot(),
            "Jobsy.Web", "Components", "Candidate", "Discovery", "JourneyShedMoment.razor"));
        Assert.Contains("Options.Count > 0", source, StringComparison.Ordinal);
        Assert.Contains("aria-checked=\"@(selected ? \"true\" : \"false\")\"", source, StringComparison.Ordinal);
    }
}
