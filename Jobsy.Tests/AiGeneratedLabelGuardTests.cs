namespace Jobsy.Tests;

public sealed class AiGeneratedLabelGuardTests
{
    [Theory]
    [InlineData("Jobsy.Web/Components/Candidate/DnaPanel.razor")]
    [InlineData("Jobsy.Web/Components/Candidate/Passport/PassportDnaTab.razor")]
    [InlineData("Jobsy.Web/Components/Candidate/RoleFitCheckPanel.razor")]
    [InlineData("Jobsy.Web/Components/Candidate/Career/CareerOverviewCard.razor")]
    [InlineData("Jobsy.Web/Components/Candidate/HonestAdviceBlock.razor")]
    [InlineData("Jobsy.Web/Components/Pages/Candidate/OccupationDayPage.razor")]
    [InlineData("Jobsy.Web/Components/MockInterviewModal.razor")]
    public void Candidate_ai_surfaces_include_generated_label(string relativePath)
    {
        var root = FindRepoRoot();
        var text = File.ReadAllText(Path.Combine(root, relativePath));
        Assert.True(
            text.Contains("AiGeneratedLabel", StringComparison.Ordinal)
                || text.Contains("ai-generated-label", StringComparison.Ordinal),
            $"Expected AI disclosure in {relativePath}");
    }

    [Fact]
    public void Passport_ssr_dna_story_includes_ai_label_when_from_open_ai()
    {
        var root = FindRepoRoot();
        var panels = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Passport/PassportSsrPanelsHtml.cs"));
        Assert.Contains("Ai.GeneratedLabel", panels, StringComparison.Ordinal);
        Assert.Contains("StoryFromOpenAi", panels, StringComparison.Ordinal);
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

        throw new InvalidOperationException("Could not find Jobsy.sln");
    }
}