namespace Jobsy.Tests;

public sealed class CandidatePrimaryNavEnhancedNavTests
{
    [Fact]
    public void Candidate_primary_nav_uses_interactive_nav_link_without_enhance_nav_false()
    {
        var root = FindRepoRoot();
        var header = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Layout", "CandidateHeaderNav.razor"));
        var bottom = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Layout", "BottomNav.razor"));
        var link = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Layout", "CandidateNavLink.razor"));
        Assert.Contains("<CandidateNavLink", header, StringComparison.Ordinal);
        Assert.Contains("<CandidateNavLink", bottom, StringComparison.Ordinal);
        Assert.DoesNotContain("data-enhance-nav=\"false\"", header, StringComparison.Ordinal);
        Assert.DoesNotContain("data-enhance-nav=\"false\"", bottom, StringComparison.Ordinal);
        Assert.Contains("NavigateTo(Href, forceLoad: false)", link, StringComparison.Ordinal);
        Assert.Contains("data-lobsy-candidate-nav=\"true\"", link, StringComparison.Ordinal);
        Assert.Contains("@onclick:preventDefault=\"true\"", link, StringComparison.Ordinal);
        Assert.Contains("InteractiveServerRenderMode(prerender: false)", link, StringComparison.Ordinal);
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
