namespace Jobsy.Tests;

/// <summary>
/// Homepage prerenders a sized map shell and starts MapLibre immediately
/// so the first paint is a filled map, not an empty placeholder.
/// </summary>
public class JobMapPrerenderGuardTests
{
    [Fact]
    public void Home_does_not_chain_map_assets_from_the_document()
    {
        var home = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Pages", "Home.razor"));
        Assert.Contains("InteractiveServerRenderMode(prerender: true)", home);
        Assert.DoesNotContain("images/maps/nl-preview.webp", home);
        Assert.DoesNotContain("prerender: false", home);
        Assert.DoesNotContain("/lib/leaflet/leaflet.min.js", home);
        Assert.DoesNotContain("/lib/leaflet/leaflet.css", home);
        Assert.DoesNotContain("rel=\"preload\"", home);
        Assert.DoesNotContain("lib/maplibre/maplibre-gl.css", home);
        Assert.DoesNotContain("lib/maplibre/maplibre-gl-csp.js", home);
        Assert.DoesNotContain("jobMap.min.js", home);
        Assert.DoesNotContain("fetchpriority=\"high\"", home);

        var maps = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "maps-loader.js"));
        Assert.Contains("lib/maplibre/maplibre-gl-csp.js", maps);
        Assert.Contains("jobMap.min.js", maps);
        Assert.Contains("ensure: ensure", maps);
        Assert.Contains("link.media = \"print\"", maps);
        Assert.Contains("fetchpriority\", \"low\"", maps);
        Assert.DoesNotContain("warmDiscovery", maps);
        Assert.DoesNotContain("jobsyMapsAfterFirstPaint", maps);
        Assert.DoesNotContain("fetchpriority\", \"high\"", maps);
        Assert.DoesNotContain("jobMap.boot", maps);
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
        Assert.Contains("function boot(", js);
        Assert.Contains("readBootPayload", js);
        Assert.Contains("jobsy-map-boot", js);
        Assert.Contains("const live", js);
        Assert.Contains("Paint the basemap immediately", js);
        Assert.Contains("fitMapToVacancies", js);
        Assert.Contains("lockCamera", js);
        Assert.Contains("openingCamera", js);
        Assert.Contains("readFilledOrigin", js);
        Assert.Contains("FILLED_LOCATION_ZOOM = 11", js);
        Assert.Contains("preferFilledLocation", js);
        Assert.Contains("jumpToLocation", js);
        Assert.Contains("safeJumpTo", js);
        Assert.Contains("safeEaseTo", js);
        Assert.Contains("fitToOriginRings", js);
        Assert.Contains("overlayFitPadding", js);
        Assert.Contains("scheduleTravelRingRedraw", js);
        Assert.Contains("onTravelRingStyleData", js);
        Assert.Contains("deferMapReveal", js);
        Assert.Contains("finishOpeningFrame", js);
        Assert.Contains("jobsy-travel-rings", js);
        Assert.Contains("sourceId + \"-halo\"", js);
        Assert.Contains("highlight-carousel--map", js);
        Assert.Contains("collectVacancyPoints", js);
        Assert.Contains("ensureVacancyTiles", js);
        Assert.Contains("firstSizedFit", js);
        Assert.DoesNotContain("openingViewUntil", js);
        Assert.DoesNotContain("No default NL view", js);
        var initIdx = js.IndexOf("function init(elementId, vacancies, options)", StringComparison.Ordinal);
        var initEnd = js.IndexOf("function locateIconHtml", initIdx, StringComparison.Ordinal);
        Assert.True(initIdx > 0 && initEnd > initIdx);
        var initFn = js[initIdx..initEnd];
        Assert.DoesNotContain("jumpToLocation", initFn);
        Assert.DoesNotContain("fitMapToVacancies", initFn);
        Assert.Contains("setOrigin(filledOrigin.lat", initFn);
        var setVacanciesInInit = js.IndexOf("setVacancies(vacancies || []);", initIdx, StringComparison.Ordinal);
        var tilesAfterVacancies = js.IndexOf("ensureVacancyTiles();", setVacanciesInInit, StringComparison.Ordinal);
        Assert.True(setVacanciesInInit > initIdx && tilesAfterVacancies > setVacanciesInInit);

        var setStart = js.IndexOf("function setVacancies(vacancies)", StringComparison.Ordinal);
        var setEnd = js.IndexOf("function ensureOriginMarker", StringComparison.Ordinal);
        Assert.True(setStart > 0 && setEnd > setStart);
        Assert.DoesNotContain("fitMapToVacancies", js[setStart..setEnd]);

        var originStart = js.IndexOf("function setOrigin(lat, lng, travel)", StringComparison.Ordinal);
        var originEnd = js.IndexOf("function setTravelOptions", StringComparison.Ordinal);
        Assert.True(originStart > 0 && originEnd > originStart);
        Assert.DoesNotContain("fitMapToVacancies", js[originStart..originEnd]);
        Assert.Contains("fitToOriginRings", js[originStart..originEnd]);
        Assert.Contains("container.isConnected", js);
        Assert.Contains("isAlive", js[(js.LastIndexOf("return {", StringComparison.Ordinal))..]);
        Assert.Contains("maplibregl", js);
        Assert.Contains("Solliciteer", js);
    }

    [Fact]
    public void Job_map_popups_anchor_bottom_and_center_on_the_marker()
    {
        var js = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "jobMap.js"));
        Assert.Contains("anchor: \"bottom\"", js);
        Assert.Contains("JOB_POPUP_OFFSET", js);
        Assert.Contains("CLUSTER_POPUP_OFFSET", js);
        Assert.Contains("bottom: [0, -18]", js);
        Assert.Contains("bottom: [0, -23]", js);
        Assert.Contains("popup.setOffset", js);
        Assert.DoesNotContain("offset: 22", js);
        Assert.Contains("function centerPopupInView", js);
        Assert.Contains("map.panBy", js);
        Assert.Contains("highlight-carousel--map", js);
        Assert.Contains("map-popup__type-chip", js);
        Assert.Contains("job-map-popup--with-type", js);
        Assert.DoesNotContain("map-popup__badge--type", js);

        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "css", "app.css"));
        Assert.Contains("maplibregl-popup-anchor-bottom .maplibregl-popup-tip", css);
        Assert.Contains("align-self: center", css);
        Assert.Contains("border-top-color: var(--surface)", css);
        Assert.Contains(".map-popup__type-chip", css);
        Assert.Contains(".job-map-popup--with-wages > .map-popup__type-chip", css);
        Assert.Contains("right: 62px", css);

        var bindStart = js.IndexOf("function bindClusterPopupInteractions", StringComparison.Ordinal);
        var bindEnd = js.IndexOf("function eventTargetInsidePopup", StringComparison.Ordinal);
        Assert.True(bindStart >= 0 && bindEnd > bindStart);
        var bindFn = js[bindStart..bindEnd];
        Assert.DoesNotContain("centerPopupInView", bindFn);
        Assert.Contains(".job-map-popup--cluster .map-popup__main", css);
        Assert.Contains("max-height: 252px", css);
    }

    [Fact]
    public void Job_map_uses_openfreemap_vector_styles_and_hides_attribution()
    {
        var helper = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "jobsyMapLibre.js"));
        Assert.Contains("https://tiles.openfreemap.org/styles/liberty", helper);
        Assert.Contains("https://tiles.openfreemap.org/styles/bright", helper);
        Assert.Contains("map.setStyle", helper);
        Assert.Contains("attributionControl: false", helper);
        Assert.Contains("maplibreLogo: false", helper);
        Assert.Contains("cooperativeGestures: false", helper);
        Assert.Contains("dragPan: true", helper);
        Assert.Contains("touchAction", helper);
        Assert.Contains("3D-kaart", helper);
        Assert.DoesNotContain("3D / Bright", helper);
        Assert.Contains("syncStyleToggle", helper);
        Assert.Contains("return \"liberty\"", helper);

        var createStart = helper.IndexOf("function createMap(container, options)", StringComparison.Ordinal);
        var createEnd = helper.IndexOf("return {", createStart, StringComparison.Ordinal);
        Assert.True(createStart > 0 && createEnd > createStart);
        var createFn = helper[createStart..createEnd];
        Assert.Contains("center: options.center", createFn);
        Assert.Contains("zoom: options.zoom", createFn);
        Assert.Contains("controlsPosition", createFn);
        Assert.Contains("options.controls !== false", createFn);
        Assert.Contains("options.touchZoomRotate !== false", createFn);
        Assert.DoesNotContain("applyCameraForStyle", createFn);

        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "css", "app.css"));
        Assert.Contains("maplibregl-ctrl-attrib", css);
        Assert.Contains("display: none !important", css);
        Assert.Contains("job-map-style-switch", css);
        Assert.Contains("maplibregl-ctrl-bottom-right", css);
        Assert.Contains("bottom: 58px", css);
        Assert.Contains(".job-map-style-switch__btn.is-on", css);
        Assert.Contains("touch-action: none", css);
        Assert.Contains("#job-map", css);
        Assert.Contains("min-height: 55dvh", css);
    }

    [Fact]
    public void Logo_images_have_a_lobsy_onerror_fallback()
    {
        var app = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "App.razor"));
        Assert.Contains("jobsyLogoFallback", app);
        Assert.Contains("/images/brand/lobsy-256.webp", app);
        Assert.Contains("/images/brand/lobsy-128.png", app);

        var photo = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "VacancyPhoto.razor"));
        Assert.Contains("data-logo-fallback", photo);
        Assert.Contains("jobsyLogoFallback", photo);

        var logo = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "LobsyLogo.razor"));
        Assert.Contains("jobsyLogoFallback", logo);

        var js = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "jobMap.js"));
        Assert.Contains("jobsyLogoFallback", js);
    }

    [Fact]
    public void App_shell_does_not_load_unpkg_or_map_engine_on_every_page()
    {
        var app = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "App.razor"));
        Assert.DoesNotContain("unpkg.com", app);
        Assert.Contains("js/app-core.js", app);
        Assert.Contains("defer", app);
        Assert.DoesNotContain("app-core.js?v=20260816-perf\" defer", app);
        Assert.Contains("css/app.min.css", app);
        Assert.Contains("media=\"print\"", app);
        Assert.Contains("jobsyLogoFallback", app);
        Assert.Contains("#job-map", app);
        Assert.Contains("min-height: 55dvh", app);
        Assert.Contains("--map-land:#f8f4f0", app);
        Assert.Contains(".map-stage.is-live .job-map-placeholder", app);
        Assert.Contains(".lobsy-watermarks { display: none; }", app);
        Assert.Contains(".app-shell:has(.jobsy-discovery) .lobsy-watermarks", app);
        Assert.Contains("maplibregl-ctrl-logo", app);
        Assert.Contains(".jobsy-chrome { display: none; }", app);
        Assert.DoesNotContain("lib/leaflet/leaflet.min.js", app);
        Assert.DoesNotContain("lib/leaflet/leaflet.css", app);
        Assert.DoesNotContain("lib/maplibre/maplibre-gl.js", app);
        Assert.DoesNotContain("lib/maplibre/maplibre-gl.css", app);
        Assert.DoesNotContain("<script src=\"https://unpkg.com/leaflet", app);

        var maps = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "maps-loader.js"));
        Assert.Contains("pending[kind] = null", maps);
        Assert.Contains("discovery", maps);
        Assert.Contains("fetchpriority", maps);
        Assert.DoesNotContain("requestIdleCallback", maps);
        Assert.DoesNotContain("ensureAfterPaint", maps);
        var bundle = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "app-core.js"));
        Assert.Contains("pending[kind] = null", bundle);
        Assert.Contains("maplibre-gl-csp.js", bundle);
        Assert.Contains("fetchpriority", bundle);
        Assert.DoesNotContain("requestIdleCallback", bundle);
        Assert.DoesNotContain("ensureAfterPaint", bundle);
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
        Assert.Contains("ResolveOpeningView", discovery);
        Assert.Contains("LoadMapViewAsync", discovery);
        Assert.Contains("[\"view\"]", discovery);
        Assert.Contains("preferFilledLocation", discovery);
        Assert.Contains("VacancyMapViewCalculator.ResolveOpening", discovery);
        Assert.Contains("CenterMapOnFilledLocationAsync", discovery);
        Assert.Contains("fitToOriginRings", discovery);
        Assert.Contains("RegionHost.EnsureInitializedAsync", discovery);
        Assert.DoesNotContain("await RegionHost.EnsureInitializedAsync();", discovery);
        Assert.Contains("FilledLocationZoom", discovery);
        var afterRenderEnd = discovery.IndexOf("private async Task MeasureViewportAsync", afterRender, StringComparison.Ordinal);
        Assert.True(afterRenderEnd > afterRender);
        var afterRenderFn = discovery[afterRender..afterRenderEnd];
        var measureCall = afterRenderFn.IndexOf("await MeasureViewportAsync();", StringComparison.Ordinal);
        var firstInit = afterRenderFn.IndexOf("await TryInitJobMapAsync();", StringComparison.Ordinal);
        var extraInit = afterRenderFn.IndexOf("await TryInitJobMapAsync();", firstInit + 1, StringComparison.Ordinal);
        Assert.True(measureCall >= 0 && firstInit > measureCall && extraInit < 0);
        var geoHydrate = discovery.IndexOf("ensureLocationOnLaunch", afterRender, StringComparison.Ordinal);
        Assert.True(geoHydrate > afterRender);
        var launchDone = discovery.IndexOf("_launchHydrationDone = true", geoHydrate, StringComparison.Ordinal);
        Assert.True(launchDone > geoHydrate);
        Assert.DoesNotContain("CenterMapOnFilledLocationAsync", discovery[geoHydrate..launchDone]);
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

    [Fact]
    public void Vacancy_detail_map_is_a_square_block_centered_on_the_vacancy()
    {
        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "css", "app.css"));
        var wrapStart = css.IndexOf("\n.vacancy-location__map-wrap {", StringComparison.Ordinal);
        Assert.True(wrapStart > 0);
        var wrapEnd = css.IndexOf("}", wrapStart, StringComparison.Ordinal);
        var wrap = css[wrapStart..wrapEnd];
        Assert.Contains("aspect-ratio: 1 / 1", wrap);
        Assert.Contains("min(100%, 16.5rem)", wrap);
        Assert.DoesNotContain("height: 240px", wrap);

        Assert.Contains(".vacancy-media--split", css);
        Assert.Contains("minmax(0, 1fr) 16.5rem", css);
        Assert.Contains(".vacancy-media:not(.vacancy-media--split) .detail-card__image", css);
        Assert.Contains("max-height: 16.5rem", css);
        Assert.Contains("align-items: center", css[css.IndexOf(".vacancy-location__actions {", StringComparison.Ordinal)..]);
        Assert.Contains(".vacancy-location__eta", css);
        Assert.Contains(".apply-panel--guest", css);
        Assert.DoesNotContain("text-transform: uppercase", css[css.IndexOf(".vacancy-location__transport {", StringComparison.Ordinal)..(css.IndexOf(".vacancy-location__transport {", StringComparison.Ordinal) + 400)]);

        var detail = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Pages", "VacancyDetail.razor"));
        Assert.Contains("vacancy-media--split", detail);
        Assert.Contains("HasPhoto && HasLocation", detail);
        Assert.Contains("detail-card--pending", detail);
        Assert.Contains("PersistentComponentState", detail);
        Assert.Contains("panel-page--vacancy", detail);
        Assert.Contains("keepPrerendered", detail);
        Assert.Contains("vacancy-location__eta", detail);
        Assert.Contains("GetVacancyTravelAsync", detail);
        Assert.Contains("apply-panel--guest", detail);
        Assert.DoesNotContain("Vacancy.ProximityWink", detail);
        Assert.DoesNotContain("± @_vacancy.TravelMinutes", detail);
        var mediaIdx = detail.IndexOf("class=\"vacancy-media", StringComparison.Ordinal);
        var titleIdx = detail.IndexOf("detail-card__top", StringComparison.Ordinal);
        var bodyIdx = detail.IndexOf("detail-card__body", StringComparison.Ordinal);
        Assert.True(mediaIdx > 0 && titleIdx > mediaIdx && bodyIdx > titleIdx, "Photo and map must sit together above the title and description.");

        var js = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "vacancyDetailMap.js"));
        Assert.Contains("styleKey: \"liberty\"", js);
        Assert.Contains("controls: false", js);
        Assert.Contains("touchZoomRotate: false", js);
        Assert.Contains("vacancy-detail-marker", js);
        Assert.Contains("center: [useLng, useLat]", js);
        Assert.Contains("setLngLat([currentLng, currentLat])", js);
        Assert.DoesNotContain("minHeight = \"200px\"", js);
    }

    [Fact]
    public void Job_map_keeps_3d_toggle_with_zoom_above_locate()
    {
        var js = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "jobMap.js"));
        Assert.Contains("controlsPosition: \"bottom-right\"", js);

        var helper = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "jobsyMapLibre.js"));
        Assert.Contains("StyleSwitchControl", helper);
        Assert.Contains("maplibregl-ctrl-group job-map-style-switch", helper);
        Assert.Contains("bottom-right", helper);
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
