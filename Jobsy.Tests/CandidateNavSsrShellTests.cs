namespace Jobsy.Tests;

public class CandidateNavSsrShellTests
{
    [Fact]
    public void Static_bottom_nav_includes_working_links_and_testid()
    {
        var root = TestRepo.FindRoot();
        var shell = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Navigation", "CandidateNavSsrShell.cs"));
        Assert.Contains("data-lobsy-candidate-nav=", shell, StringComparison.Ordinal);
        Assert.Contains("candidate-bottom-nav-ssr", shell, StringComparison.Ordinal);
        Assert.Contains("BuildBottomNav", shell, StringComparison.Ordinal);
        var layout = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Layout", "MainLayout.razor"));
        Assert.Contains("NoBlazorCandidateNav", layout, StringComparison.Ordinal);
    }
}
