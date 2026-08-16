using Microsoft.AspNetCore.Http;

namespace Jobsy.Tests;

/// <summary>
/// Guards the homepage against the TBT/DOM regression: prerendering every job card
/// (~6k nodes) and a late cookie-banner LCP.
/// </summary>
public class HomepagePerformanceGuardTests
{
    [Fact]
    public void Discovery_does_not_render_the_full_card_list_on_the_mobile_map()
    {
        var discovery = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "VacancyDiscovery.razor"));
        Assert.Contains("ShouldRenderVacancyCards", discovery);
        Assert.Contains("VisibleVacancies", discovery);
        Assert.Contains("RendererInfo.IsInteractive", discovery);
        Assert.Contains("jobsyViewport.isWide", discovery);
        Assert.Contains("VacancyCardPageSize = 12", discovery);
        Assert.DoesNotContain("@foreach (var vacancy in SortedVacancies)", discovery);
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

        var consentJs = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "cookieConsent.js"));
        Assert.Contains("cookie-consent-known", consentJs);
        Assert.Contains("jobsyViewport", consentJs);

        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "css", "app.css"));
        Assert.Contains("html.cookie-consent-known .cookie-consent", css);
        Assert.Contains("contain: layout paint style", css);
    }

    [Fact]
    public void Discovery_prerenders_a_map_placeholder_and_defers_leaflet()
    {
        var discovery = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "VacancyDiscovery.razor"));
        Assert.Contains("job-map-placeholder", discovery);
        Assert.Contains("images/maps/nl-preview.webp", discovery);
        Assert.DoesNotContain("job-map-placeholder__cluster", discovery);
        Assert.Contains("EnsureDiscoveryAfterPaintAsync", discovery);
        Assert.Contains("_vacancies.Count == 0 && _loading", discovery);
        Assert.Contains("if (!RendererInfo.IsInteractive)", discovery);
        Assert.Contains("OnMapTilesReady", discovery);

        var maps = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "maps-loader.js"));
        Assert.Contains("ensureAfterPaint", maps);
        Assert.Contains("requestIdleCallback", maps);
        Assert.Contains("IntersectionObserver", maps);

        var bundle = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "app-core.js"));
        Assert.Contains("ensureAfterPaint", bundle);
        Assert.Contains("requestIdleCallback", bundle);

        var preview = Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "images", "maps", "nl-preview.webp");
        Assert.True(File.Exists(preview));

        var layout = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "Layout", "MainLayout.razor"));
        Assert.Contains("RendererInfo.IsInteractive", layout);
        Assert.Contains("LobsyAssistantChat", layout);
        Assert.Contains("FeedbackWidget", layout);
    }

    [Fact]
    public void Map_loader_does_not_always_fetch_detail_and_discovery_scripts()
    {
        var maps = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "maps-loader.js"));
        Assert.Contains("ensure: ensure", maps);
        Assert.Contains("ensureAfterPaint", maps);
        Assert.Contains("discoveryScripts", maps);
        Assert.Contains("detailScripts", maps);

        var mapScripts = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Hosting", "MapScripts.cs"));
        Assert.Contains("EnsureDiscoveryAsync", mapScripts);
        Assert.Contains("EnsureDiscoveryAfterPaintAsync", mapScripts);
        Assert.Contains("EnsureDetailAsync", mapScripts);

        var discovery = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web", "Components", "VacancyDiscovery.razor"));
        Assert.Contains("EnsureDiscoveryAfterPaintAsync", discovery);
        Assert.DoesNotContain("MapScripts.EnsureAsync(Js)", discovery);
        Assert.DoesNotContain("await MapScripts.EnsureDiscoveryAsync(Js)", discovery);

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
