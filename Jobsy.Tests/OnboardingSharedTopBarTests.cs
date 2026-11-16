using Jobsy.Web.Hosting;

namespace Jobsy.Tests;

public sealed class OnboardingSharedTopBarTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    [Fact]
    public void Onboarding_ssr_layout_renders_shared_admin_style_top_bar()
    {
        var layout = File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/Components/Layout/OnboardingStartSsrLayout.razor"));
        var shared = File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/Components/Shell/LobsyShellTopBar.razor"));
        var admin = File.ReadAllText(Path.Combine(RepoRoot, "Jobsy.Web/Components/Admin/Shell/AdminTopBar.razor"));

        Assert.Contains("LobsyShellTopBar", layout, StringComparison.Ordinal);
        Assert.Contains("LobsyShellTopBarKind.CandidateOnboarding", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("AppShellHeader", layout, StringComparison.Ordinal);
        Assert.Contains("admin-topbar", shared, StringComparison.Ordinal);
        Assert.Contains("LobsyShellTopBar", admin, StringComparison.Ordinal);
    }

    [Fact]
    public void Candidate_start_route_includes_admin_and_v3_stylesheets()
    {
        var sheets = PageAssetCatalog.RouteStylesheets("/candidate/start");
        Assert.Contains(sheets, s => s.Contains("admin.css", StringComparison.Ordinal));
        Assert.Contains(sheets, s => s.Contains("onboarding-wizard-v3.css", StringComparison.Ordinal));
    }
}
