namespace Jobsy.Tests;

/// <summary>
/// Homepage prerenders a map shell. Leaflet binds only after hydrate + idle,
/// so replacing the prerendered #job-map node cannot leave a dead map.
/// </summary>
public class JobMapPrerenderGuardTests
{
    [Fact]
    public void Home_prerenders_the_map_shell_without_binding_leaflet()
    {
        var home = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Pages", "Home.razor"));
        Assert.Contains("InteractiveServerRenderMode(prerender: true)", home);
        Assert.DoesNotContain("prerender: false", home);
        Assert.Contains("images/maps/nl-preview.webp", home);
    }

    [Fact]
    public void Job_map_exposes_isAlive_for_detached_leaflet_containers()
    {
        var js = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "jobMap.js"));
        Assert.Contains("function isAlive()", js);
        Assert.Contains("NL_BOUNDS", js);
        Assert.Contains("removeOutsideVisibleBounds: false", js);
        Assert.Contains("NL_CENTER", js);
        Assert.DoesNotContain("map.setView([52.07, 4.28], 11)", js);
        Assert.Contains("container.isConnected", js);
        Assert.Contains("isAlive", js[(js.LastIndexOf("return {", StringComparison.Ordinal))..]);
    }

    [Fact]
    public void App_shell_does_not_load_unpkg_or_leaflet_on_every_page()
    {
        var app = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "App.razor"));
        Assert.DoesNotContain("unpkg.com", app);
        Assert.Contains("js/app-core.js", app);
        Assert.DoesNotContain("app-core.js?v=20260816-perf\" defer", app);
        Assert.Contains("rel=\"preload\"", app);
        Assert.Contains("css/app.css", app);
        Assert.DoesNotContain("lib/leaflet/leaflet.min.js", app);
        Assert.DoesNotContain("lib/leaflet/leaflet.css", app);
        Assert.DoesNotContain("<script src=\"https://unpkg.com/leaflet", app);

        var maps = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "maps-loader.js"));
        Assert.Contains("pending[kind] = null", maps);
        Assert.Contains("discovery", maps);
        var bundle = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "app-core.js"));
        Assert.Contains("pending[kind] = null", bundle);
    }

    [Fact]
    public void Discovery_reinitializes_map_when_leaflet_node_is_dead()
    {
        var discovery = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "VacancyDiscovery.razor"));
        Assert.Contains("jobMap.isAlive", discovery);
        Assert.Contains("Keep going — map init must not wait on geo helpers.", discovery);

        var afterRender = discovery.IndexOf("OnAfterRenderAsync(bool firstRender)", StringComparison.Ordinal);
        Assert.True(afterRender > 0);
        var hydrateCatch = discovery.IndexOf("catch (JSException)", afterRender, StringComparison.Ordinal);
        Assert.True(hydrateCatch > afterRender);
        var hydrateSlice = discovery[hydrateCatch..(hydrateCatch + 180)];
        Assert.DoesNotContain("return;", hydrateSlice);

        var isAliveIdx = discovery.IndexOf("jobMap.isAlive", afterRender, StringComparison.Ordinal);
        Assert.True(isAliveIdx > afterRender);
        var aliveCatch = discovery.IndexOf("catch (JSException)", isAliveIdx, StringComparison.Ordinal);
        Assert.True(aliveCatch > isAliveIdx);
        var aliveCatchSlice = discovery[aliveCatch..(aliveCatch + 220)];
        Assert.Contains("do not tear down a live map", aliveCatchSlice);
        Assert.DoesNotContain("_mapReady = false", aliveCatchSlice);
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
