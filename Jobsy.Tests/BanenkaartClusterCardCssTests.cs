namespace Jobsy.Tests;

public class BanenkaartClusterCardCssTests
{
    [Fact]
    public void Cluster_sheet_uses_auto_height_without_fixed_223px()
    {
        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "css", "features", "banenkaart.css"));

        Assert.DoesNotContain("--map-popup-sheet-h", css, StringComparison.Ordinal);
        Assert.DoesNotContain("223px", css, StringComparison.Ordinal);
        Assert.DoesNotContain("height: 252px", css, StringComparison.Ordinal);
        Assert.DoesNotContain("--map-popup-content-h", css, StringComparison.Ordinal);

        Assert.Contains(".map-cluster-sheet", css, StringComparison.Ordinal);
        Assert.Contains("height: auto", css, StringComparison.Ordinal);
        Assert.Contains("max-height: min(60vh, 420px)", css, StringComparison.Ordinal);
        Assert.Contains("overflow-y: auto", css, StringComparison.Ordinal);

        Assert.Contains("-webkit-line-clamp: 2", css, StringComparison.Ordinal);
        Assert.Contains("line-clamp: 2", css, StringComparison.Ordinal);
        Assert.Contains("88px", css, StringComparison.Ordinal);
        Assert.Contains(".map-popup__view", css, StringComparison.Ordinal);
        Assert.Contains(".map-popup__heart", css, StringComparison.Ordinal);
        Assert.Contains(".map-cluster-card__dots", css, StringComparison.Ordinal);
        Assert.Contains(".map-cluster-card__grab", css, StringComparison.Ordinal);
        Assert.Contains("width: min(360px", css, StringComparison.Ordinal);
        Assert.Contains("--map-cluster-selected: #f54a1b", css, StringComparison.Ordinal);
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
