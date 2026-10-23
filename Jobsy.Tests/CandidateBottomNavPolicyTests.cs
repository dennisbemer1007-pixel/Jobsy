namespace Jobsy.Tests;

public sealed class CandidateBottomNavPolicyTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(RepoRoot, relativePath));

    [Fact]
    public void Candidate_pages_keep_bottom_nav_on_desktop()
    {
        var bottomNav = Read("Jobsy.Web/Components/Layout/BottomNav.razor");
        Assert.Contains("_isCandidate", bottomNav, StringComparison.Ordinal);
        Assert.Contains("_hideForDesktopViewport = false", bottomNav, StringComparison.Ordinal);

        var appCss = Read("Jobsy.Web/wwwroot/css/app.css");
        Assert.Contains(".app-shell:not(.app-shell--candidate) .bottom-nav", appCss, StringComparison.Ordinal);
        Assert.DoesNotContain(".app-shell--candidate:has(.jobsy-discovery) .bottom-nav", appCss, StringComparison.Ordinal);

        var noBlazor = Read("Jobsy.Web/wwwroot/css/features/candidate-no-blazor-chrome.css");
        Assert.Contains(".bottom-nav[data-testid=\"candidate-bottom-nav-ssr\"]", noBlazor, StringComparison.Ordinal);
        Assert.Contains("app-shell--candidate", noBlazor, StringComparison.Ordinal);
    }

    [Fact]
    public void App_header_hides_candidate_top_nav_links()
    {
        var header = Read("Jobsy.Web/Components/Layout/AppShellHeader.razor");
        Assert.DoesNotContain("CandidateHeaderNav", header, StringComparison.Ordinal);
        Assert.DoesNotContain("NoBlazorCandidateNav", header, StringComparison.Ordinal);
        Assert.Contains("_hideCandidateBrandTagline", header, StringComparison.Ordinal);
    }
}
