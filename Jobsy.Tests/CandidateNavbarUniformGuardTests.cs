using Jobsy.Core.Features;
using Jobsy.Web.Components.Layout;
using Jobsy.Web.Navigation;

namespace Jobsy.Tests;

/// <summary>
/// Logged-in candidates must use one shared app header (MainLayout AppShellHeader), never a duplicate discovery chrome header.
/// </summary>
public sealed class CandidateNavbarUniformGuardTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(RepoRoot, relativePath));

    [Fact]
    public void MainLayout_uses_shared_AppShellHeader_with_bottom_nav_only()
    {
        var layout = Read("Jobsy.Web/Components/Layout/MainLayout.razor");
        var header = Read("Jobsy.Web/Components/Layout/AppShellHeader.razor");
        var bottom = Read("Jobsy.Web/Components/Layout/BottomNav.razor");

        Assert.Contains("<AppShellHeader", layout, StringComparison.Ordinal);
        Assert.Contains("NoBlazorCandidateNav", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("jobsy-chrome__header", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("CandidateHeaderNav", header, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"app-shell-header\"", header, StringComparison.Ordinal);
        Assert.Contains("bottom-nav", bottom, StringComparison.Ordinal);
        Assert.Contains("<PageHelp />", header, StringComparison.Ordinal);
    }

    [Fact]
    public void VacancyDiscovery_does_not_embed_duplicate_top_nav()
    {
        var discovery = Read("Jobsy.Web/Components/VacancyDiscovery.razor");
        Assert.DoesNotContain("jobsy-chrome__header", discovery, StringComparison.Ordinal);
        Assert.DoesNotContain("<AuthHeader />", discovery, StringComparison.Ordinal);
        Assert.DoesNotContain("<NotificationBell />", discovery, StringComparison.Ordinal);
        Assert.DoesNotContain("<LanguageSelector />", discovery, StringComparison.Ordinal);
    }

    [Fact]
    public void Map_routes_use_banenkaart_candidate_shell_class()
    {
        foreach (var path in new[] { "banenkaart", "bedrijvenkaart", "banen" })
        {
            Assert.True(MainLayout.IsBanenkaartPath(path));
        }
    }

    [Fact]
    public void Candidate_primary_routes_share_header_nav_catalog()
    {
        var flags = new FeatureFlagSnapshot(EmployersEnabled: true, CandidatePassportEnabled: true);
        var items = RoleNavCatalog.CandidateItems(flags);
        Assert.Contains(items, i => i.Href == RoleNavCatalog.SearchItem.Href);
        Assert.Contains(items, i => i.Href == RoleNavCatalog.PassportItem.Href);
        Assert.Contains(items, i => i.Href == RoleNavCatalog.DiscoveryItem.Href);
    }

    [Fact]
    public void Css_no_longer_hides_app_header_on_discovery_for_anonymous()
    {
        var critical = Read("Jobsy.Web/wwwroot/css/critical.css");
        var app = Read("Jobsy.Web/wwwroot/css/app.css");
        Assert.DoesNotContain(":not(.app-shell--banenkaart-shell) .app-header", critical, StringComparison.Ordinal);
        Assert.DoesNotContain(":not(.app-shell--banenkaart-shell) .app-header", app, StringComparison.Ordinal);
    }
}
