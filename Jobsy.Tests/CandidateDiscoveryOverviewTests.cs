using Jobsy.Infrastructure.Services;

namespace Jobsy.Tests;

public class CandidateDiscoveryOverviewTests
{
    [Theory]
    [InlineData("{\"1\":5,\"2\":3}", 2)]
    [InlineData("{\"1\":0,\"2\":6}", 0)]
    [InlineData("{}", 0)]
    public void CountValidAnswers_counts_only_1_to_5(string json, int expected)
        => Assert.Equal(expected, CandidateDiscoveryOverviewService.CountValidAnswers(json));

    [Fact]
    public void CountValidAnswers_treats_null_as_zero()
        => Assert.Equal(0, CandidateDiscoveryOverviewService.CountValidAnswers(null));

    [Fact]
    public void BuildRevision_changes_when_any_stamp_changes()
    {
        var a = CandidateDiscoveryOverviewService.BuildRevision(
            DateTime.UtcNow,
            null,
            null,
            null,
            null,
            null);
        var b = CandidateDiscoveryOverviewService.BuildRevision(
            DateTime.UtcNow.AddMinutes(1),
            null,
            null,
            null,
            null,
            null);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Discovery_journey_uses_batch_overview_endpoint()
    {
        var root = FindRepoRoot();
        var journey = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Pages/Candidate/DiscoveryJourney.razor"));
        Assert.Contains("OverviewCache", journey, StringComparison.Ordinal);
        Assert.Contains("discovery-overview", File.ReadAllText(Path.Combine(root, "Jobsy.Web/Services/ApiClient/JobsyApiClient.Me.cs")), StringComparison.Ordinal);
        Assert.Contains("AnsweredCount(kind)", journey, StringComparison.Ordinal);
        Assert.Contains("OverviewCache.GetPanelAsync", journey, StringComparison.Ordinal);
    }

    [Fact]
    public void Journey_overview_rows_use_actions_column_for_buttons()
    {
        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/css/features/ontdekkingsreis.css"));
        Assert.Contains("grid-template-columns: minmax(0, 1fr) auto", css, StringComparison.Ordinal);
        var markup = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Candidate/Discovery/JourneyOverview.razor"));
        Assert.Contains("journey-overview__main", markup, StringComparison.Ordinal);
        Assert.Contains("journey-overview__actions", markup, StringComparison.Ordinal);
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
