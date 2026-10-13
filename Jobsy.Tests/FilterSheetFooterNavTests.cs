namespace Jobsy.Tests;

/// <summary>
/// Guard: mobile filter-sheet footer (Toon n banen / Annuleren) must not sit under .bottom-nav.
/// </summary>
public class FilterSheetFooterNavTests
{
    [Fact]
    public void App_css_hides_bottom_nav_while_discovery_filters_are_open_on_mobile()
    {
        var root = FindRepoRoot();
        var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "css", "app.css"));

        Assert.Contains("@media (max-width: 768px)", css, StringComparison.Ordinal);
        Assert.Contains(
            ".app-shell:has(.jobsy-discovery.filters-open) .bottom-nav",
            css,
            StringComparison.Ordinal);
        Assert.Contains(
            ".app-shell:has(.jobsy-discovery.filters-open) .cookie-consent",
            css,
            StringComparison.Ordinal);
        Assert.Contains("display: none !important;", css, StringComparison.Ordinal);

        // Full-height sheet with safe-area padding (sticky footer remains reachable).
        Assert.Contains("max-height: min(100dvh, 100vh);", css, StringComparison.Ordinal);
        Assert.Contains("padding-bottom: max(0.35rem, env(safe-area-inset-bottom));", css, StringComparison.Ordinal);
        Assert.Contains("position: sticky;", css, StringComparison.Ordinal);

        // Do not change .app-main stacking globally for this fix.
        var appMainRules = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "css", "app.css"));
        Assert.Contains(".app-main", appMainRules, StringComparison.Ordinal);
    }

    [Fact]
    public void VacancyDiscovery_filter_sheet_footer_applies_with_Toon_banen()
    {
        var discovery = File.ReadAllText(
            Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "VacancyDiscovery.razor"));

        Assert.Contains("filters-open", discovery, StringComparison.Ordinal);
        Assert.Contains("class=\"filter-sheet__footer\"", discovery, StringComparison.Ordinal);
        Assert.Contains("class=\"filter-sheet__apply\"", discovery, StringComparison.Ordinal);
        Assert.Contains("@onclick=\"ApplySearch\"", discovery, StringComparison.Ordinal);
        Assert.Contains("class=\"filter-sheet__cancel\"", discovery, StringComparison.Ordinal);
        Assert.Contains("ApplyFiltersLabel", discovery, StringComparison.Ordinal);
        Assert.Contains("Kb.Filter.Show", discovery, StringComparison.Ordinal);

        var applyIdx = discovery.IndexOf("class=\"filter-sheet__apply\"", StringComparison.Ordinal);
        Assert.True(applyIdx > 0);
        var applySnippet = discovery.Substring(applyIdx, Math.Min(220, discovery.Length - applyIdx));
        Assert.Contains("ApplySearch", applySnippet, StringComparison.Ordinal);
        Assert.Contains("ApplyFiltersLabel", applySnippet, StringComparison.Ordinal);
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
