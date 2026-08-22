using Microsoft.AspNetCore.Http;

namespace Jobsy.Tests;

/// <summary>
/// Guards the homepage against the TBT/DOM regression: prerendering every job card
/// (~6k nodes) and a late cookie-banner LCP.
/// </summary>
public class HomepagePerformanceGuardTests
{
    [Fact]
    public void First_paint_stylesheets_are_non_blocking()
    {
        var app = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "App.razor"));
        var appCssIdx = app.IndexOf("css/app.min.css", StringComparison.Ordinal);
        var appCssLinkStart = app.LastIndexOf("<link", appCssIdx, StringComparison.Ordinal);
        var appCssLinkEnd = app.IndexOf("/>", appCssIdx, StringComparison.Ordinal);
        var appCssLink = app[appCssLinkStart..(appCssLinkEnd + 2)];
        Assert.Contains("media=\"print\"", appCssLink);
        Assert.Contains("data-app-css", appCssLink);
        Assert.DoesNotContain("onload=", appCssLink);
        Assert.Contains("link.media = \"all\"", app);

        var home = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Pages", "Home.razor"));
        Assert.DoesNotContain("lib/maplibre/maplibre-gl.css", home);
        Assert.DoesNotContain("rel=\"preload\"", home);

        var maps = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "maps-loader.js"));
        Assert.Contains("link.media = \"print\"", maps);
        Assert.DoesNotContain("link.setAttribute(\"fetchpriority\"", maps);
        var bundle = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "app-core.js"));
        Assert.Contains("link.media = \"print\"", bundle);
        Assert.DoesNotContain("link.setAttribute(\"fetchpriority\"", bundle);
    }

    [Fact]
    public void Discovery_does_not_render_the_full_card_list_on_the_mobile_map()
    {
        var discovery = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "VacancyDiscovery.razor"));
        Assert.Contains("ShouldRenderVacancyCards", discovery);
        Assert.Contains("_wideViewport || (_mapPainted && !showMapOnMobile)", discovery);
        Assert.Contains("VisibleVacancies", discovery);
        Assert.Contains("RendererInfo.IsInteractive", discovery);
        Assert.DoesNotContain("photoEager", discovery);
        Assert.Contains("jobsyViewport.isWide", discovery);
        Assert.Contains("MeasureViewportAsync", discovery);
        Assert.Contains("VacancyCardPageSize = 12", discovery);
        Assert.DoesNotContain("@foreach (var vacancy in SortedVacancies)", discovery);
    }

    [Fact]
    public void First_paint_reserves_homepage_layout_to_avoid_cls()
    {
        var app = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "App.razor"));
        var criticalStart = app.IndexOf("<style>", StringComparison.Ordinal);
        var criticalEnd = app.IndexOf("</style>", StringComparison.Ordinal);
        Assert.True(criticalStart >= 0 && criticalEnd > criticalStart);
        var critical = app[criticalStart..criticalEnd];
        Assert.Contains(".visually-hidden", critical);
        Assert.Contains(".cookie-consent { position: fixed", critical);
        Assert.Contains(".cookie-consent a { color: #fff; text-decoration: underline", critical);
        Assert.Contains(".bottom-nav { position: fixed", critical);
        Assert.Contains(".lobsy-logo { display: block; width: 56px; height: 56px", critical);
        Assert.Contains("min-height: 10rem", critical);
        Assert.Contains(".app-header { min-height: 4.25rem; }", critical);
        Assert.Contains("contain: layout", critical);

        var logo = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "LobsyLogo.razor"));
        Assert.Contains("width=\"56\"", logo);
        Assert.Contains("height=\"56\"", logo);
        Assert.DoesNotContain("width=\"128\"", logo);
    }

    [Fact]
    public void Cookie_consent_hides_known_choice_before_paint()
    {
        var app = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "App.razor"));
        Assert.Contains("Jobsy.CookieConsent", app);
        Assert.Contains("cookie-consent-known", app);

        var banner = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "CookieConsentBanner.razor"));
        Assert.Contains("private bool _visible = true", banner);
        Assert.DoesNotContain("cookie-consent__mascot", banner);
        Assert.Contains("Accepteer cookies", banner);
        Assert.DoesNotContain("Accepteer analytics", banner);
        Assert.Contains("cookie-consent__privacy", banner);
        Assert.DoesNotContain("class=\"auth-link\"", banner);

        var consentJs = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "cookieConsent.js"));
        Assert.Contains("cookie-consent-known", consentJs);
        Assert.Contains("jobsyViewport", consentJs);

        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "css", "app.css"));
        Assert.Contains("html.cookie-consent-known .cookie-consent", css);
        Assert.Contains("contain: layout paint style", css);
        var privacyRule = css.IndexOf(".cookie-consent a.cookie-consent__privacy {", StringComparison.Ordinal);
        Assert.True(privacyRule > 0);
        var privacyBlock = css[privacyRule..(privacyRule + 180)];
        Assert.Contains("color: #fff", privacyBlock);
        Assert.Contains("text-decoration: underline", privacyBlock);
    }

    [Fact]
    public void Discovery_prerenders_a_map_shell_and_starts_maplibre_immediately()
    {
        var discovery = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "VacancyDiscovery.razor"));
        Assert.Contains("job-map-placeholder", discovery);
        Assert.DoesNotContain("images/maps/nl-preview.webp", discovery);
        Assert.DoesNotContain("job-map-placeholder__pins", discovery);
        Assert.DoesNotContain("MapPreviewPins", discovery);
        Assert.Contains("EnsureDiscoveryAsync", discovery);
        Assert.DoesNotContain("EnsureDiscoveryAfterPaintAsync", discovery);
        Assert.Contains("TryInitJobMapAsync", discovery);
        Assert.DoesNotContain("job-map-placeholder__status", discovery);
        Assert.Contains("if (!RendererInfo.IsInteractive)", discovery);
        Assert.Contains("jobsy-map-boot", discovery);
        Assert.Contains("MapBootPinsJson", discovery);
        Assert.Contains("OperationCanceledException", discovery);
        Assert.Contains("_mapPainted = true", discovery);
        Assert.Contains("OnMapTilesReady", discovery);
        Assert.Contains("_mapPainted = true", discovery);

        var maps = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "maps-loader.js"));
        Assert.DoesNotContain("ensureAfterPaint", maps);
        Assert.DoesNotContain("warmDiscovery", maps);
        Assert.DoesNotContain("jobsyMapsAfterFirstPaint", maps);
        Assert.DoesNotContain("IntersectionObserver", maps);
        Assert.DoesNotContain("requestIdleCallback", maps);
        Assert.Contains("fetchpriority", maps);
        Assert.Contains("Blazor calls ensure()", maps);

        var bundle = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "app-core.js"));
        Assert.DoesNotContain("ensureAfterPaint", bundle);
        Assert.DoesNotContain("warmDiscovery", bundle);
        Assert.DoesNotContain("jobsyMapsAfterFirstPaint", bundle);
        Assert.DoesNotContain("requestIdleCallback", bundle);
        Assert.Contains("fetchpriority", bundle);
        Assert.Contains("Blazor calls ensure()", bundle);

        var preview = Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "images", "maps", "nl-preview.webp");
        Assert.False(File.Exists(preview));

        var layout = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Layout", "MainLayout.razor"));
        Assert.Contains("RendererInfo.IsInteractive", layout);
        Assert.Contains("Never on first paint", layout);
        Assert.Contains("LobsyAssistantChat", layout);
        Assert.Contains("FeedbackWidget", layout);
    }

    [Fact]
    public void Map_loader_does_not_always_fetch_detail_and_discovery_scripts()
    {
        var maps = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "maps-loader.js"));
        Assert.Contains("ensure: ensure", maps);
        Assert.DoesNotContain("ensureAfterPaint", maps);
        Assert.Contains("discoveryScripts", maps);
        Assert.Contains("detailScripts", maps);
        Assert.Contains("setWorkerUrl", maps);
        Assert.Contains("jobMap.min.js", maps);
        Assert.DoesNotContain("/js/jobMap.js?", maps);

        var mapScripts = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Hosting", "MapScripts.cs"));
        Assert.Contains("EnsureDiscoveryAsync", mapScripts);
        Assert.DoesNotContain("EnsureDiscoveryAfterPaintAsync", mapScripts);
        Assert.Contains("EnsureDetailAsync", mapScripts);

        var discovery = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "VacancyDiscovery.razor"));
        Assert.Contains("MapScripts.EnsureDiscoveryAsync(Js)", discovery);
        Assert.DoesNotContain("EnsureDiscoveryAfterPaintAsync", discovery);
        Assert.DoesNotContain("MapScripts.EnsureAsync(Js)", discovery);

        var detail = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Pages", "VacancyDetail.razor"));
        Assert.Contains("EnsureDetailAsync", detail);
    }

    [Fact]
    public async Task Head_requests_are_rewritten_to_get_and_405_is_coerced()
    {
        string? seenMethod = null;
        var middleware = new Jobsy.Web.Hosting.HeadAsGetMiddleware(ctx =>
        {
            seenMethod = ctx.Request.Method;
            ctx.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Head;
        await middleware.Invoke(context);

        Assert.Equal(HttpMethods.Get, seenMethod);
        Assert.Equal(HttpMethods.Head, context.Request.Method);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public void Homepage_js_payload_skips_deferred_and_unminified_scripts()
    {
        var root = FindRepoRoot();
        var app = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "App.razor"));
        Assert.Contains("js/app-core.js", app);
        Assert.DoesNotContain("js/feedback.js", app);
        Assert.DoesNotContain("js/app-extras.js", app);

        var core = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "app-core.js"));
        Assert.Contains("jobsyExtras", core);
        Assert.Contains("lobsyFeedbackEnsure", core);
        Assert.Contains("setWorkerUrl", core);
        Assert.DoesNotContain("window.lobsySessionIdle =", core);
        Assert.DoesNotContain("window.jobsyDownload =", core);
        Assert.DoesNotContain("window.jobsyRichtext =", core);

        var extras = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "app-extras.js"));
        Assert.Contains("window.lobsySessionIdle =", extras);
        Assert.Contains("window.jobsyDownload =", extras);
        Assert.Contains("window.jobsyRichtext =", extras);

        var mapDir = Path.Combine(root, "Jobsy.Web", "wwwroot", "lib", "maplibre");
        Assert.False(File.Exists(Path.Combine(mapDir, "maplibre-gl.js")));
        Assert.True(File.Exists(Path.Combine(mapDir, "maplibre-gl-csp.js")));
        Assert.True(File.Exists(Path.Combine(mapDir, "maplibre-gl-csp-worker.js")));
        Assert.True(new FileInfo(Path.Combine(mapDir, "maplibre-gl-csp.js")).Length < 1_000_000);

        Assert.True(new FileInfo(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "jobMap.min.js")).Length
            < new FileInfo(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "jobMap.js")).Length);
        Assert.True(new FileInfo(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "jobsyMapLibre.min.js")).Length
            < new FileInfo(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "jobsyMapLibre.js")).Length);
        Assert.True(new FileInfo(Path.Combine(root, "Jobsy.Web", "wwwroot", "css", "app.min.css")).Length
            < new FileInfo(Path.Combine(root, "Jobsy.Web", "wwwroot", "css", "app.css")).Length);

        var appRazor = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "App.razor"));
        Assert.Contains("css/app.min.css", appRazor);
        Assert.DoesNotContain("href=\"css/app.css?", appRazor);

        var home = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "Pages", "Home.razor"));
        Assert.DoesNotContain("rel=\"preload\"", home);
        Assert.DoesNotContain("lib/maplibre/", home);
        Assert.DoesNotContain("jobMap.min.js", home);

        Assert.False(Directory.Exists(Path.Combine(root, "Jobsy.Web", "wwwroot", "lib", "leaflet")));
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
