namespace Jobsy.Tests;

/// <summary>Guard: travel rings stay thin (kandidaat-polish 04).</summary>
public class BanenkaartTravelRingCssTests
{
    [Fact]
    public void EnsureTravelRingLayers_uses_thinner_lines_and_lighter_fills()
    {
        var js = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "jobMap.js"));
        var start = js.IndexOf("function ensureTravelRingLayers", StringComparison.Ordinal);
        Assert.True(start >= 0, "ensureTravelRingLayers missing");
        var end = js.IndexOf("\n    function applyTravelRingData", start, StringComparison.Ordinal);
        Assert.True(end > start, "applyTravelRingData after ensureTravelRingLayers");
        var fn = js[start..end];

        Assert.Contains("2.5", fn, StringComparison.Ordinal);
        Assert.Contains("1.5", fn, StringComparison.Ordinal);
        Assert.Contains("4.5", fn, StringComparison.Ordinal);
        Assert.Contains("\n                        3\n", fn, StringComparison.Ordinal);
        Assert.Contains("0.12", fn, StringComparison.Ordinal);
        Assert.Contains("0.09", fn, StringComparison.Ordinal);
        Assert.Contains("0.06", fn, StringComparison.Ordinal);
        Assert.Contains("0.05", fn, StringComparison.Ordinal);
        Assert.Contains("[\"literal\", [2, 2]]", fn, StringComparison.Ordinal);
        Assert.DoesNotContain("5.5", fn, StringComparison.Ordinal);
        Assert.DoesNotContain("3.5", fn, StringComparison.Ordinal);
        Assert.DoesNotContain("0.22", fn, StringComparison.Ordinal);
        Assert.DoesNotContain("0.14", fn, StringComparison.Ordinal);

        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "css", "features", "banenkaart.css"));
        Assert.Contains(".map-iso-label", css, StringComparison.Ordinal);
        Assert.Contains("font-size: 12px", css, StringComparison.Ordinal);
        Assert.Contains("border: 1.5px solid", css, StringComparison.Ordinal);
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
