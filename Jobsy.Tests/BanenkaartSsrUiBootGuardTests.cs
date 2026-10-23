namespace Jobsy.Tests;

public sealed class BanenkaartSsrUiBootGuardTests
{
    [Fact]
    public void Banenkaart_ssr_emits_hero_strip_before_filter_chrome()
    {
        var root = FindRepoRoot();
        var shells = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Banenkaart", "BanenkaartSsrShells.cs"));
        var heroIdx = shells.IndexOf("AppendNearbyHeroStrip", StringComparison.Ordinal);
        var chromeIdx = shells.IndexOf("AppendChrome(sb, culture, model)", StringComparison.Ordinal);
        Assert.True(heroIdx >= 0 && chromeIdx > heroIdx,
            "Illustration strip must be rendered before filter/search chrome in SSR markup.");
    }

    [Fact]
    public void Banenkaart_desktop_list_uses_two_column_card_grid()
    {
        var root = FindRepoRoot();
        var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "css", "features", "banenkaart-page.css"));
        Assert.Contains("grid-template-columns: repeat(2, minmax(0, 1fr))", css, StringComparison.Ordinal);
        Assert.Contains("kb-list-row--card", css, StringComparison.Ordinal);

        var listHtml = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Banenkaart", "BanenkaartSsrListHtml.cs"));
        Assert.Contains("kb-list-row__sector", listHtml, StringComparison.Ordinal);
        Assert.Contains("kb-list-row__overlay", listHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("kb-why-line", listHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void Banenkaart_ssr_closes_filters_and_defers_map_boot_until_ui_ready()
    {
        var root = FindRepoRoot();
        var ssr = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "banenkaart-ssr.js"));
        var boot = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "banenkaart-maps-boot.js"));

        Assert.Contains("applyInitialUiState", ssr, StringComparison.Ordinal);
        Assert.Contains("jobsy-bk-ssr-ui-ready", ssr, StringComparison.Ordinal);
        Assert.DoesNotContain("if (state.filtersOpen)", ssr, StringComparison.Ordinal);
        Assert.Contains("scheduleWarmBoot", boot, StringComparison.Ordinal);
        Assert.Contains("jobsy-bk-ssr-ui-ready", boot, StringComparison.Ordinal);
        Assert.Contains("bindViewportLayout", ssr, StringComparison.Ordinal);
        Assert.Contains("syncLayoutForViewport", ssr, StringComparison.Ordinal);
        Assert.Contains("bindClientFilterApply", ssr, StringComparison.Ordinal);
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
