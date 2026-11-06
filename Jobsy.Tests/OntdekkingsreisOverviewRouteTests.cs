namespace Jobsy.Tests;

/// <summary>
/// Documents which Ontdekkingsreis screen matches #791 (overview route) vs the welcome start screen.
/// </summary>
public class OntdekkingsreisOverviewRouteTests
{
    [Fact]
    public void Discovery_journey_markup_distinguishes_start_and_overview_routes()
    {
        var journey = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Pages", "Candidate", "DiscoveryJourney.razor"));
        Assert.Contains("_screen == \"start\"", journey, StringComparison.Ordinal);
        Assert.Contains("_screen == \"overview\"", journey, StringComparison.Ordinal);
        Assert.Contains("journey--overview-route", journey, StringComparison.Ordinal);
        Assert.Contains("JourneyOverview", journey, StringComparison.Ordinal);
        Assert.Contains("journey-zones", journey, StringComparison.Ordinal);
    }

    [Fact]
    public void Overview_hero_requires_compact_preview_variant_c()
    {
        var journey = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Pages", "Candidate", "DiscoveryJourney.razor"));
        Assert.Contains("CandidateHeroCompactPreviewVariant.C", journey, StringComparison.Ordinal);
        Assert.Contains("cand-hero--discovery-overview-v8", journey, StringComparison.Ordinal);
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
