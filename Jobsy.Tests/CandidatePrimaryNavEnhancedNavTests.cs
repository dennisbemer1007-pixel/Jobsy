namespace Jobsy.Tests;

public sealed class CandidatePrimaryNavEnhancedNavTests
{
    [Fact]
    public void Candidate_primary_nav_links_are_plain_anchors_without_blazor_click_handlers()
    {
        var root = FindRepoRoot();
        var header = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Layout", "CandidateHeaderNav.razor"));
        var bottom = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Layout", "BottomNav.razor"));
        var link = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Layout", "CandidateNavLink.razor"));
        Assert.Contains("<CandidateNavLink", header, StringComparison.Ordinal);
        Assert.Contains("<CandidateNavLink", bottom, StringComparison.Ordinal);
        Assert.Contains("data-lobsy-candidate-nav=\"true\"", link, StringComparison.Ordinal);
        Assert.Contains("data-lobsy-document-title=\"", link, StringComparison.Ordinal);
        Assert.DoesNotContain("@onclick", link, StringComparison.Ordinal);
        Assert.DoesNotContain("InteractiveServerRenderMode", header, StringComparison.Ordinal);
        Assert.DoesNotContain("InteractiveServerRenderMode", bottom, StringComparison.Ordinal);
        Assert.DoesNotContain("InteractiveServerRenderMode", link, StringComparison.Ordinal);
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
