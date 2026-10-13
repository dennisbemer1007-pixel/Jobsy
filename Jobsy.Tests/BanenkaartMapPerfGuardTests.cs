namespace Jobsy.Tests;

/// <summary>
/// Banenkaart main-thread / pin-fetch perf: transport switches must not re-hit
/// /api/vacancies/pins; MapLibre boot waits for circuit/idle; popup paints skeleton first.
/// </summary>
public class BanenkaartMapPerfGuardTests
{
    [Fact]
    public void JobMap_caches_pins_by_filter_key_excluding_transport()
    {
        var root = FindRepoRoot();
        var js = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "jobMap.js"));
        var discovery = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "VacancyDiscovery.razor"));
        var maps = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "app-core.js"));

        Assert.Contains("function pinsFilterKey", js, StringComparison.Ordinal);
        Assert.Contains("searchParams.delete(\"transport\")", js, StringComparison.Ordinal);
        Assert.Contains("pinsByFilterKey", js, StringComparison.Ordinal);
        Assert.Contains("pinsNetworkFetchCount", js, StringComparison.Ordinal);
        Assert.Contains("__testGetPinsNetworkFetchCount", js, StringComparison.Ordinal);

        // Same filter, different travel mode → same cache key.
        Assert.Equal(
            PinsFilterKey("/api/vacancies/pins?transport=Fiets&maxMinutes=30&q=hovenier"),
            PinsFilterKey("/api/vacancies/pins?transport=Auto&maxMinutes=30&q=hovenier"));
        Assert.NotEqual(
            PinsFilterKey("/api/vacancies/pins?transport=Fiets&maxMinutes=30"),
            PinsFilterKey("/api/vacancies/pins?transport=Fiets&maxMinutes=45"));

        Assert.Contains("reloadMapPins: false", discovery, StringComparison.Ordinal);
        Assert.Contains("OnTransportChanged", discovery, StringComparison.Ordinal);
        Assert.Contains("whenCircuitOrIdle", maps, StringComparison.Ordinal);
        Assert.Contains("requestIdleCallback", maps, StringComparison.Ordinal);
        Assert.Contains("timeout: 1500", maps, StringComparison.Ordinal);
        Assert.Contains("script.defer = true", maps, StringComparison.Ordinal);
        AssetVersions.AssertVersionedRefMatchesManifest(maps, "js/jobMap.min.js");

        Assert.Contains("function afterFirstPaint", js, StringComparison.Ordinal);
        Assert.Contains("skeletonPopupHtml", js, StringComparison.Ordinal);
        Assert.Contains("panClusterAboveSheet(ll)", js, StringComparison.Ordinal);
        // Fly/pan must be scheduled after the skeleton paint, not before sheet creation.
        var openCluster = js.IndexOf("function openClusterList", StringComparison.Ordinal);
        Assert.True(openCluster >= 0);
        var clusterSlice = js[openCluster..];
        var skeletonIdx = clusterSlice.IndexOf("skeletonPopupHtml", StringComparison.Ordinal);
        var afterPaintIdx = clusterSlice.IndexOf("afterFirstPaint", StringComparison.Ordinal);
        var panIdx = clusterSlice.IndexOf("panClusterAboveSheet(ll)", StringComparison.Ordinal);
        Assert.True(skeletonIdx >= 0 && afterPaintIdx > skeletonIdx && panIdx > afterPaintIdx,
            "cluster sheet must show skeleton, then afterFirstPaint, then pan/fly");
    }

    /// <summary>Mirrors jobMap.js pinsFilterKey for regression coverage without a JS harness.</summary>
    internal static string PinsFilterKey(string url)
    {
        var uri = new Uri(new Uri("https://lobsy.local"), url);
        var bag = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var query = uri.Query.TrimStart('?');
        if (!string.IsNullOrEmpty(query))
        {
            foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var eq = pair.IndexOf('=');
                var key = Uri.UnescapeDataString(eq >= 0 ? pair[..eq] : pair);
                var value = Uri.UnescapeDataString(eq >= 0 ? pair[(eq + 1)..] : "");
                if (string.Equals(key, "transport", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!bag.TryGetValue(key, out var list))
                {
                    list = [];
                    bag[key] = list;
                }

                list.Add(value);
            }
        }

        var parts = new List<string>();
        foreach (var key in bag.Keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            foreach (var value in bag[key].OrderBy(v => v, StringComparer.Ordinal))
            {
                parts.Add($"{key}={value}");
            }
        }

        return uri.AbsolutePath + "?" + string.Join("&", parts);
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
