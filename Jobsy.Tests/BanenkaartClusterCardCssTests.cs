namespace Jobsy.Tests;

public class BanenkaartClusterCardCssTests
{
    [Fact]
    public void Mobile_cluster_card_uses_taller_fixed_sheet_and_two_line_title()
    {
        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "css", "features", "banenkaart.css"));

        Assert.Contains("--map-popup-content-h: 176px", css, StringComparison.Ordinal);
        Assert.Contains("--map-popup-sheet-h: 223px", css, StringComparison.Ordinal);
        Assert.DoesNotContain("--map-popup-content-h: 152px", css, StringComparison.Ordinal);
        Assert.DoesNotContain("--map-popup-sheet-h: 199px", css, StringComparison.Ordinal);

        Assert.Contains(".map-cluster-card .map-popup__body", css, StringComparison.Ordinal);
        Assert.Contains("min-height: 0", css, StringComparison.Ordinal);
        Assert.Contains("max-height: none", css, StringComparison.Ordinal);

        Assert.Contains("-webkit-line-clamp: 2", css, StringComparison.Ordinal);
        Assert.Contains("line-clamp: 2", css, StringComparison.Ordinal);
        Assert.Contains("max-width: 100%", css, StringComparison.Ordinal);

        // Media fills the card height; legacy app.css max-height:152px must not clip.
        Assert.Contains(".map-cluster-card .map-popup__media", css, StringComparison.Ordinal);
        Assert.Contains("max-height: none !important", css, StringComparison.Ordinal);

        // Desktop sizes stay on the explicit 252/205 block.
        Assert.Contains(".job-map-popup--cluster .map-cluster-card", css, StringComparison.Ordinal);
        Assert.Contains("height: 252px", css, StringComparison.Ordinal);
        Assert.Contains("height: 205px", css, StringComparison.Ordinal);
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
