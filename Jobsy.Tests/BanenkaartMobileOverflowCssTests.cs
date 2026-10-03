namespace Jobsy.Tests;

/// <summary>
/// Kandidaat polish 01+02: mobile discovery grid must use minmax(0,1fr); filter bar
/// is a two-row layout without requiring horizontal chip scrolling.
/// </summary>
public class BanenkaartMobileOverflowCssTests
{
    [Fact]
    public void Mobile_discovery_grid_uses_minmax_zero_fr_and_chrome_min_width_zero()
    {
        var appCss = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "css", "app.css"));
        var kbCss = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "css", "features", "kandidaat-banen.css"));

        Assert.Contains("grid-template-columns: minmax(0, 1fr)", appCss, StringComparison.Ordinal);
        Assert.Contains("/* minmax(0,1fr): plain 1fr", appCss, StringComparison.Ordinal);
        Assert.Contains("min-width: 0;\n        background: var(--surface);", appCss, StringComparison.Ordinal);
        Assert.Contains(".kb-chrome-search {\n        min-width: 0;\n    }", appCss, StringComparison.Ordinal);

        Assert.Contains(".kb-filter-bar", kbCss, StringComparison.Ordinal);
        Assert.Contains("flex-direction: column", kbCss, StringComparison.Ordinal);
        Assert.Contains(".kb-filters-button", kbCss, StringComparison.Ordinal);
        // Desktop chip row may still scroll; keep helpers.
        Assert.Contains("overscroll-behavior-x: contain", kbCss, StringComparison.Ordinal);
        Assert.Contains(".kb-filter-chips.has-more-end", kbCss, StringComparison.Ordinal);
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

        throw new InvalidOperationException("Jobsy.sln not found from test base directory.");
    }
}
