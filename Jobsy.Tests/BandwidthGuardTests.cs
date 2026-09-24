using Jobsy.Infrastructure.Jobs;

namespace Jobsy.Tests;

/// <summary>
/// Guards Render egress: compressed API JSON, compact discover payloads,
/// no background polling in hidden tabs, and a same-origin image Cache API.
/// </summary>
public class BandwidthGuardTests
{
    [Fact]
    public void Api_enables_brotli_gzip_and_omits_null_json()
    {
        var program = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Api/Program.cs"));
        Assert.Contains("AddJobsyApiPerformance", program);
        Assert.Contains("UseResponseCompression", program);
        Assert.Contains("JsonIgnoreCondition.WhenWritingNull", program);

        var hosting = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Api/Hosting/ApiPerformanceExtensions.cs"));
        Assert.Contains("BrotliCompressionProvider", hosting);
        Assert.Contains("application/json", hosting);
        Assert.Contains("EnableForHttps = true", hosting);
    }

    [Fact]
    public void Discover_supports_take_and_strips_inline_images_from_list_json()
    {
        var controller = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Api/Controllers/VacanciesController.cs"));
        Assert.Contains("[FromQuery] int? take = null", controller);
        Assert.Contains("ApplyPublicListCacheHeaders", controller);
        Assert.Contains("VacancyImageUrls.ForPublicList", controller);
        Assert.Contains("compact ? null : r.ScheduleJson", controller);
        Assert.Contains("GetPublicImage", controller);
        Assert.Contains("{id:guid}/image", controller);

        var client = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Services/JobsyApiClient.cs"));
        Assert.Contains("int? take = null", client);
        Assert.Contains("&take={cap}", client);
        Assert.Contains("AutomaticDecompression", File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Services/JobsyApiClientFactory.cs")));
    }

    [Fact]
    public void Hidden_tabs_do_not_keep_polling_session_or_notifications()
    {
        var bell = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/Layout/NotificationBell.razor"));
        Assert.Contains("PageIsVisibleAsync", bell);
        Assert.Contains("jobsyPageVisible", bell);

        var idle = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/SessionIdleGuard.razor"));
        Assert.Contains("jobsyPageVisible", idle);

        foreach (var relative in new[]
                 {
                     "Jobsy.Web/wwwroot/js/sessionIdle.js",
                     "Jobsy.Web/wwwroot/js/app-extras.js"
                 })
        {
            var js = File.ReadAllText(Path.Combine(FindRepoRoot(), relative));
            Assert.Contains("document.visibilityState === \"hidden\"", js);
            Assert.Contains("refreshTimeout();", js);
        }
    }

    [Fact]
    public void Pwa_service_worker_caches_shell_and_images_but_skips_api()
    {
        var sw = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/service-worker.js"));
        Assert.Contains("pathname.indexOf(\"/images/\") === 0", sw);
        Assert.Contains("cache.put", sw);
        Assert.Contains("addEventListener(\"push\"", sw);
        Assert.Contains("url.pathname.indexOf(\"/api/\") === 0", sw);
        Assert.Contains("request.mode === \"navigate\"", sw);
        Assert.Contains("showNotification", sw);

        var published = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/service-worker.published.js"));
        Assert.Contains("lobsy-shell-published", published);
        Assert.Contains("showNotification", published);

        var core = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/js/app-core.js"));
        Assert.Contains("window.jobsyPageVisible", core);
        Assert.Contains("service-worker", core);
        Assert.Contains("serviceWorker.register", core);
        Assert.DoesNotContain("window.lobsySessionIdle =", core);

        var manifest = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/wwwroot/manifest.webmanifest"));
        Assert.Contains("\"display\": \"standalone\"", manifest);
        Assert.Contains("icon-512.png", manifest);
    }

    [Fact]
    public void Discovery_index_refresh_is_not_sub_minute_and_impressions_are_paged()
    {
        Assert.Equal(TimeSpan.FromSeconds(60), VacancyDiscoveryIndexHostedService.RefreshInterval);

        var discovery = File.ReadAllText(Path.Combine(FindRepoRoot(), "Jobsy.Web/Components/VacancyDiscovery.razor"));
        Assert.Contains("RecordImpressionsAsync(Js, VisibleVacancies.Select(v => v.Id))", discovery);
        Assert.DoesNotContain("RecordImpressionsAsync(Js, _vacancies.Select(v => v.Id))", discovery);
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
