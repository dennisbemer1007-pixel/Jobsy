namespace Jobsy.Tests;

/// <summary>
/// Corrupt sessionStorage basemap styles (background-only) must not be used; map should recover to a drawable style.
/// </summary>
public sealed class BanenkaartBasemapStyleGuardTests
{
    [Fact]
    public void Basemap_warm_and_mapLibre_validate_cached_style_before_use()
    {
        var root = FindRepoRoot();
        var warm = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "jobsy-basemap-warm.js"));
        var libre = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "jobsyMapLibre.js"));

        Assert.Contains("isUsableBasemapStyle", warm, StringComparison.Ordinal);
        Assert.Contains("clearStoredStyle", warm, StringComparison.Ordinal);
        Assert.Contains("drawable >= 4", warm, StringComparison.Ordinal);

        Assert.Contains("recoverBlankBasemap", libre, StringComparison.Ordinal);
        Assert.Contains("countRenderedBasemapFeatures", libre, StringComparison.Ordinal);
        Assert.Contains("LIBERTY_SAME_ORIGIN_URL", libre, StringComparison.Ordinal);
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
