namespace Jobsy.Tests;

public sealed class CandidateBottomNavPolicyTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(RepoRoot, relativePath));

    [Fact]
    public void Candidate_desktop_uses_header_nav_ssr_bottom_nav_mobile_only()
    {
        var bottomNav = Read("Jobsy.Web/Components/Layout/BottomNav.razor");
        Assert.Contains("jobsyViewport.isKompasWide", bottomNav, StringComparison.Ordinal);

        var noBlazor = Read("Jobsy.Web/wwwroot/css/features/candidate-no-blazor-chrome.css");
        Assert.Contains(".bottom-nav[data-testid=\"candidate-bottom-nav-ssr\"]", noBlazor, StringComparison.Ordinal);
        Assert.Contains("display: none !important", noBlazor, StringComparison.Ordinal);
    }

    [Fact]
    public void App_header_renders_candidate_primary_nav()
    {
        var header = Read("Jobsy.Web/Components/Layout/AppShellHeader.razor");
        Assert.Contains("CandidateHeaderNav", header, StringComparison.Ordinal);
        Assert.Contains("NoBlazorCandidateNav", header, StringComparison.Ordinal);
        Assert.Contains("candidate-header-nav-slot", header, StringComparison.Ordinal);
    }
}
