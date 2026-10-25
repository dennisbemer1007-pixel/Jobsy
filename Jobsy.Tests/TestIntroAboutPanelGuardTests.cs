namespace Jobsy.Tests;

public sealed class TestIntroAboutPanelGuardTests
{
    [Theory]
    [InlineData("CompetencyTest.razor")]
    [InlineData("CareerTest.razor")]
    [InlineData("CultureScan.razor")]
    [InlineData("ValuesScan.razor")]
    public void Candidate_test_intro_renders_TestAboutPanel_before_questions(string pageFile)
    {
        var root = FindRepoRoot();
        var path = Path.Combine(root, "Jobsy.Web", "Components", "Pages", "Candidate", pageFile);
        var text = File.ReadAllText(path);
        var introIdx = text.IndexOf("ShellMode.Intro", StringComparison.Ordinal);
        var aboutIdx = text.IndexOf("<TestAboutPanel", StringComparison.Ordinal);
        var questionIdx = text.IndexOf("ShellMode.Question", StringComparison.Ordinal);
        Assert.True(introIdx >= 0, pageFile + " must define intro shell mode.");
        Assert.True(aboutIdx > introIdx, pageFile + " must include TestAboutPanel on intro.");
        Assert.True(questionIdx < 0 || aboutIdx < questionIdx, pageFile + " TestAboutPanel must appear before question shell.");
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "Jobsy.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? "";
        }

        throw new InvalidOperationException("Repo root not found.");
    }
}
