namespace Jobsy.Tests;

/// <summary>
/// OpenFreeMap planet tiles stop at zoom 14; banenkaart must not request or ease past that maxzoom.
/// </summary>
public sealed class BanenkaartBasemapZoomGuardTests
{
    [Fact]
    public void JobMap_and_jobsyMapLibre_cap_zoom_at_planet_maxzoom()
    {
        var root = FindRepoRoot();
        var jobMap = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "jobMap.js"));
        var libre = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "jobsyMapLibre.js"));
        var mapsBoot = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "banenkaart-maps-boot.js"));

        Assert.Contains("BASEMAP_MAX_ZOOM", libre, StringComparison.Ordinal);
        Assert.Contains("BASEMAP_MAX_ZOOM: BASEMAP_MAX_ZOOM", libre, StringComparison.Ordinal);
        Assert.Contains("disableClusteringAtZoom: BASEMAP_MAX_ZOOM", jobMap, StringComparison.Ordinal);
        Assert.Contains("clampBanenZoom", jobMap, StringComparison.Ordinal);
        Assert.Contains("applyBasemapZoomLimits", jobMap, StringComparison.Ordinal);
        Assert.DoesNotContain("disableClusteringAtZoom: 15", jobMap, StringComparison.Ordinal);
        Assert.DoesNotContain("maxZoom: 19", libre, StringComparison.Ordinal);

        AssetVersions.AssertVersionedRefMatchesManifest(mapsBoot, "js/jobMap.min.js");
        AssetVersions.AssertVersionedRefMatchesManifest(mapsBoot, "js/jobsyMapLibre.min.js");
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
