namespace Jobsy.Tests;

public sealed class CandidatePrimaryNavEnhancedNavTests
{
    [Fact]
    public void Candidate_primary_nav_links_disable_blazor_enhanced_navigation()
    {
        var root = FindRepoRoot();
        var header = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Layout", "CandidateHeaderNav.razor"));
        var bottom = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Layout", "BottomNav.razor"));
        Assert.Contains("data-enhance-nav=\"false\"", header, StringComparison.Ordinal);
        Assert.Contains("data-enhance-nav=\"false\"", bottom, StringComparison.Ordinal);
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
