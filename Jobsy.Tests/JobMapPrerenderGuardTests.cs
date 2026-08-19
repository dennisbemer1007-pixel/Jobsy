namespace Jobsy.Tests;

/// <summary>
/// Homepage prerenders a map shell. MapLibre binds only after hydrate + idle,
/// so replacing the prerendered #job-map node cannot leave a dead map.
/// </summary>
public class JobMapPrerenderGuardTests
{
    [Fact]
    public void Home_prerenders_the_map_shell_without_binding_maplibre()
    {
        var home = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Pages", "Home.razor"));
        Assert.Contains("InteractiveServerRenderMode(prerender: true)", home);
        Assert.DoesNotContain("images/maps/nl-preview.webp", home);
        Assert.Contains("jobsyMaps", home);
        Assert.DoesNotContain("prerender: false", home);
        Assert.DoesNotContain("/lib/leaflet/leaflet.min.js", home);
        Assert.DoesNotContain("/lib/leaflet/leaflet.css", home);
        Assert.DoesNotContain("/lib/maplibre/maplibre-gl.css", home);
        Assert.DoesNotContain("/lib/maplibre/maplibre-gl.js", home);
        Assert.DoesNotContain("fetchpriority=\"high\"", home);
    }

    [Fact]
    public void Job_map_exposes_isAlive_for_detached_map_containers()
    {
        var js = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "jobMap.js"));
        Assert.Contains("function isAlive()", js);
        Assert.Contains("NL_BOUNDS", js);
        Assert.Contains("removeOutsideVisibleBounds: false", js);
        Assert.Contains("NL_CENTER", js);
        Assert.DoesNotContain("map.setView([52.07, 4.28], 11)", js);
        Assert.DoesNotContain("center: NL_CENTER", js);
        Assert.DoesNotContain("map.fitBounds(NL_BOUNDS", js);
        Assert.DoesNotContain("map.setView(NL_CENTER", js);
        Assert.Contains("fitMapToVacancies", js);
        Assert.Contains("collectVacancyPoints", js);
        Assert.Contains("No default NL view", js);
        Assert.Contains("ensureVacancyTiles", js);
        Assert.Contains("openingViewUntil", js);
        var initIdx = js.IndexOf("function init(elementId, vacancies, options)", StringComparison.Ordinal);
        var setVacanciesInInit = js.IndexOf("setVacancies(vacancies || []);", initIdx, StringComparison.Ordinal);
        var tilesAfterVacancies = js.IndexOf("ensureVacancyTiles();", setVacanciesInInit, StringComparison.Ordinal);
        Assert.True(initIdx > 0 && setVacanciesInInit > initIdx && tilesAfterVacancies > setVacanciesInInit);
        Assert.Contains("container.isConnected", js);
        Assert.Contains("isAlive", js[(js.LastIndexOf("return {", StringComparison.Ordinal))..]);
        Assert.Contains("maplibregl", js);
        Assert.Contains("Solliciteer", js);
    }

    [Fact]
    public void Job_map_uses_openfreemap_vector_styles_and_hides_attribution()
    {
        var helper = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "jobsyMapLibre.js"));
        Assert.Contains("https://tiles.openfreemap.org/styles/liberty", helper);
        Assert.Contains("https://tiles.openfreemap.org/styles/bright", helper);
        Assert.Contains("map.setStyle", helper);
        Assert.Contains("attributionControl: false", helper);
        Assert.Contains("cooperativeGestures: false", helper);
        Assert.Contains("dragPan: true", helper);
        Assert.Contains("touchAction", helper);
        Assert.Contains("3D / Bright", helper);
        Assert.Contains("ev.touches.length", helper);

        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "css", "app.css"));
        Assert.Contains("maplibregl-ctrl-attrib", css);
        Assert.Contains("display: none !important", css);
        Assert.Contains("job-map-style-switch", css);
        Assert.Contains("touch-action: none", css);
        Assert.Contains("#job-map", css);
        Assert.Contains("height: 300px", css);
        Assert.Contains("min-height: 300px", css);
        Assert.Contains("height: 100% !important", css);
        Assert.DoesNotContain("min-height: 55dvh", css);
        Assert.DoesNotContain("/lib/leaflet/", helper);
        Assert.Contains("pinReservedBox", helper);
        Assert.Contains("min-width: 769px", helper);
        Assert.Contains("style.height = \"100%\"", helper);
    }

    [Fact]
    public void Logo_images_have_a_lobsy_onerror_fallback()
    {
        var app = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "App.razor"));
        Assert.Contains("jobsyLogoFallback", app);
        Assert.Contains("/images/lobsy-256.webp", app);
        Assert.Contains("/images/brand/lobsy-128.png", app);
        Assert.Contains("data:image/svg+xml", app);

        var photo = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "VacancyPhoto.razor"));
        Assert.Contains("data-logo-fallback", photo);
        Assert.Contains("jobsyLogoFallback", photo);

        var logo = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "LobsyLogo.razor"));
        Assert.Contains("jobsyLogoFallback", logo);

        var js = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "jobMap.js"));
        Assert.Contains("jobsyLogoFallback", js);
        Assert.Contains("/images/lobsy-256.webp", js);
        Assert.True(File.Exists(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "images", "lobsy-256.webp")));
    }

    [Fact]
    public void App_shell_does_not_load_unpkg_or_map_engine_on_every_page()
    {
        var app = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "App.razor"));
        Assert.DoesNotContain("unpkg.com", app);
        Assert.Contains("js/app-core.js", app);
        Assert.Contains("defer", app);
        Assert.DoesNotContain("app-core.js?v=20260816-perf\" defer", app);
        Assert.Contains("css/app.css", app);
        Assert.Contains("media=\"print\"", app);
        Assert.Contains("jobsyLogoFallback", app);
        Assert.Contains("#job-map", app);
        Assert.Contains("height: 300px", app);
        Assert.Contains("min-height: 300px", app);
        Assert.Contains("min-width: 769px", app);
        Assert.Contains("components-reconnect-overlay", app);
        Assert.Contains("translateZ(0)", app);
        Assert.Contains("will-change: transform, opacity", app);
        Assert.Contains(".jobsy-chrome { display: none; }", app);
        Assert.Contains(".app-shell", app);
        Assert.Contains("padding-bottom: 4.75rem", app);
        Assert.DoesNotContain("min-height: 55dvh", app);
        Assert.Contains("<script src=\"_framework/blazor.web.js\" defer>", app);
        Assert.DoesNotContain("bootBlazor", app);
        Assert.Contains("data-jobsy-reconnect", app);
        Assert.Contains("jobsy-wide", app);
        Assert.DoesNotContain("lib/leaflet/leaflet.min.js", app);
        Assert.DoesNotContain("lib/leaflet/leaflet.css", app);
        Assert.DoesNotContain("lib/maplibre/maplibre-gl.js", app);
        Assert.DoesNotContain("lib/maplibre/maplibre-gl.css", app);
        Assert.DoesNotContain("<script src=\"https://unpkg.com/leaflet", app);

        var maps = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "maps-loader.js"));
        Assert.Contains("pending[kind] = null", maps);
        Assert.Contains("discovery", maps);
        Assert.Contains("requestIdleCallback", maps);
        Assert.Contains("addEventListener(\"load\"", maps);
        Assert.Contains("document.readyState === \"complete\"", maps);
        Assert.Contains("DISCOVERY_DELAY_MS", maps);
        Assert.DoesNotContain("LOAD_DELAY_MS", maps);
        var bundle = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "app-core.js"));
        Assert.Contains("pending[kind] = null", bundle);
        Assert.Contains("maplibre-gl.js", bundle);
        Assert.Contains("requestIdleCallback", bundle);
        Assert.Contains("DISCOVERY_DELAY_MS", bundle);
        Assert.DoesNotContain("LOAD_DELAY_MS", bundle);
    }

    [Fact]
    public void Discovery_reinitializes_map_when_leaflet_node_is_dead()
    {
        var discovery = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "VacancyDiscovery.razor"));
        Assert.Contains("jobMap.isAlive", discovery);
        Assert.Contains("Keep going — map init must not wait on geo helpers.", discovery);

        var afterRender = discovery.IndexOf("OnAfterRenderAsync(bool firstRender)", StringComparison.Ordinal);
        Assert.True(afterRender > 0);
        Assert.Contains("_mapHostReady", discovery);
        var tryInit = discovery.IndexOf("await TryInitJobMapAsync();", afterRender, StringComparison.Ordinal);
        var isWide = discovery.IndexOf("jobsyViewport.isWide", afterRender, StringComparison.Ordinal);
        Assert.True(isWide > afterRender && tryInit > isWide);
        var geoHydrate = discovery.IndexOf("ensureLocationOnLaunch", afterRender, StringComparison.Ordinal);
        Assert.True(geoHydrate > afterRender);
        var hydrateCatch = discovery.IndexOf("catch (JSException)", geoHydrate, StringComparison.Ordinal);
        Assert.True(hydrateCatch > geoHydrate);
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
