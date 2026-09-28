namespace Jobsy.Tests;

/// <summary>
/// Guard: mobile filter-sheet footer (Toepassen/Annuleren) must not sit under .bottom-nav.
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
        Assert.Contains("display: none;", css, StringComparison.Ordinal);

        // Sheet stays viewport-bound with safe-area padding (footer remains reachable).
        Assert.Contains("max-height: min(92dvh, 92vh);", css, StringComparison.Ordinal);
        Assert.Contains("padding-bottom: max(0.35rem, env(safe-area-inset-bottom));", css, StringComparison.Ordinal);

        // Do not change .app-main stacking globally for this fix.
        var appMainRules = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "css", "app.css"));
        Assert.Contains(".app-main", appMainRules, StringComparison.Ordinal);
    }

    [Fact]
    public void VacancyDiscovery_still_renders_filter_sheet_apply_with_ApplySearch()
    {
        var discovery = File.ReadAllText(
            Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "VacancyDiscovery.razor"));

        Assert.Contains("filters-open", discovery, StringComparison.Ordinal);
        Assert.Contains("class=\"filter-sheet__footer\"", discovery, StringComparison.Ordinal);
        Assert.Contains("class=\"filter-sheet__apply\"", discovery, StringComparison.Ordinal);
        Assert.Contains("@onclick=\"ApplySearch\"", discovery, StringComparison.Ordinal);
        Assert.Contains("class=\"filter-sheet__cancel\"", discovery, StringComparison.Ordinal);
        Assert.Contains("class=\"filter-bar__search\"", discovery, StringComparison.Ordinal);

        var applyIdx = discovery.IndexOf("class=\"filter-sheet__apply\"", StringComparison.Ordinal);
        Assert.True(applyIdx > 0);
        var applySnippet = discovery.Substring(applyIdx, Math.Min(160, discovery.Length - applyIdx));
        Assert.Contains("ApplySearch", applySnippet, StringComparison.Ordinal);
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
