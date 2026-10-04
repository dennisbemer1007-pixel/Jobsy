using Jobsy.Web.Localization;

namespace Jobsy.Tests;

public class TestResultCopyTests
{
    [Fact]
    public void Score_chip_names_questions_and_upsell_drops_vacancies_when_employers_are_off()
    {
        var chip = string.Format(UiStrings.Get("TestResult.ScoresFromQuestions", "nl"), 5, 25);
        Assert.Equal("5 scores uit 25 vragen", chip);
        var self = UiStrings.Get("TestResult.Upsell.Self", "nl");
        Assert.DoesNotContain("vacature", self, StringComparison.OrdinalIgnoreCase);

        var root = FindRepoRoot();
        var locked = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Candidate/TestResult/LockedDeepReportSection.razor"));
        var gold = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Candidate/TestResult/TestResultPremiumBlock.razor"));
        Assert.Contains("TestResult.Upsell.Self", locked, StringComparison.Ordinal);
        Assert.Contains("EmployersEnabled", locked, StringComparison.Ordinal);
        Assert.Contains("TestResult.Upsell.Self", gold, StringComparison.Ordinal);
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
