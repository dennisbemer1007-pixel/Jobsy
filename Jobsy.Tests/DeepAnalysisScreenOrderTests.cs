using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class DeepAnalysisScreenOrderTests
{
    [Fact]
    public void Career_grouped_screen_order_places_second_realistic_block_after_first()
    {
        var screen = DeepAnalysisScreenOrder.For(AssessmentKind.Career).ToList();
        Assert.Equal(DeepAnalysisCatalog.CareerQuestionCount, screen.Count);

        var idx25 = screen.FindIndex(q => q.Id == 25);
        Assert.True(idx25 >= 0);
        Assert.Equal(151, screen[idx25 + 1].Id);
        Assert.Equal(26, DeepAnalysisScreenOrder.NumberOf(DeepAnalysisScreenOrder.Ids(screen), 151));
        Assert.Equal(25, DeepAnalysisScreenOrder.NumberOf(DeepAnalysisScreenOrder.Ids(screen), 25));
    }

    [Fact]
    public void Next_and_first_unanswered_follow_grouped_screen_order_not_raw_id()
    {
        var orderedIds = DeepAnalysisScreenOrder.Ids(DeepAnalysisScreenOrder.For(AssessmentKind.Career));
        var answers = Enumerable.Range(1, 25).ToDictionary(i => i, _ => 4);

        // Id order would jump to 26; screen order continues Realistic at 151.
        Assert.Equal(151, DeepAnalysisScreenOrder.FirstUnanswered(orderedIds, answers));
        Assert.Equal(151, DeepAnalysisScreenOrder.NextUnansweredAfter(orderedIds, answers, 25));

        answers[151] = 5;
        Assert.Equal(152, DeepAnalysisScreenOrder.NextUnansweredAfter(orderedIds, answers, 151));
    }

    [Fact]
    public void Screen_numbers_are_dense_1_through_N()
    {
        var orderedIds = DeepAnalysisScreenOrder.Ids(DeepAnalysisScreenOrder.For(AssessmentKind.Career));
        for (var i = 0; i < orderedIds.Count; i++)
        {
            Assert.Equal(i + 1, DeepAnalysisScreenOrder.NumberOf(orderedIds, orderedIds[i]));
        }
    }

    [Fact]
    public void DeepAnalysis_page_uses_screen_number_helpers()
    {
        var root = FindRepoRoot();
        var page = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/DeepAnalysis.razor"));
        Assert.Contains("DeepAnalysisScreenOrder", page, StringComparison.Ordinal);
        Assert.Contains("ScreenNumber(", page, StringComparison.Ordinal);
        Assert.Contains("OrderedScreenIds", page, StringComparison.Ordinal);
        Assert.Contains("ScrollRequestVersion", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"@($\"{question.Id}.", page, StringComparison.Ordinal);
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

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}
