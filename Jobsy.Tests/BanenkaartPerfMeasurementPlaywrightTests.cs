using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Local proof run for banenkaart perf (requires CI stack + 3000+ seed).
/// Run: JOBSY_E2E_BASE_URL=http://127.0.0.1:5201 JOBSY_BANENKAART_PERF_LABEL=after dotnet test --filter BanenkaartPerfMeasurement
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class BanenkaartPerfMeasurementPlaywrightTests
{
    private sealed record RequestStat(string Url, int Status, int ResponseBytes, double DurationMs);

    private sealed record ApiPayloadMetrics(
        long CatalogBytes,
        long DiscoverBytes,
        long PinsNoBboxBytes,
        long PinsBboxBytes,
        int CatalogItemCount,
        int DiscoverItemCount,
        int PinsNoBboxCount,
        int PinsBboxCount);

    private sealed record ViewportMetrics(
        int Width,
        int Height,
        string Label,
        double FirstRenderMs,
        int BrowserVacancyApiRequests,
        int BrowserPinsRequests,
        long BrowserPinsBytes,
        int BrowserPinsRequestsAfterPan,
        int PinsNetworkFetchAfterPan,
        int PinCountOnMap,
        int PinCountAfterPan,
        int ClusterFeatureCount,
        bool LoadMoreTriggered,
        int VacancyCardsAfterLoadMore,
        string ScreenshotPath);

    [Fact]
    public async Task BanenkaartPerfMeasurement_capture_metrics_and_screenshots()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        var runLabel = (Environment.GetEnvironmentVariable("JOBSY_BANENKAART_PERF_LABEL") ?? "run").Trim();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("Set JOBSY_E2E_BASE_URL (e.g. http://127.0.0.1:5201).");
        }

        if (!await IsReachableAsync(baseUrl))
        {
            throw new InvalidOperationException($"Stack not reachable at {baseUrl}.");
        }

        await EmployersPlaywrightGuard.SkipIfEmployersOffAsync(baseUrl);

        var outDir = Path.Combine(FindRepoRoot(), "banenkaart");
        Directory.CreateDirectory(outDir);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        var viewports = new[] { (1280, 900, "desktop"), (390, 844, "mobile") };
        var all = new List<ViewportMetrics>();

        foreach (var (width, height, suffix) in viewports)
        {
            all.Add(await MeasureViewportAsync(browser, baseUrl, outDir, runLabel, suffix, width, height));
        }

        var api = await MeasureApiPayloadsAsync(baseUrl);

        var report = new
        {
            capturedAtUtc = DateTime.UtcNow,
            branchLabel = runLabel,
            baseUrl,
            api,
            viewports = all
        };
        var jsonPath = Path.Combine(outDir, $"metrics-{runLabel}.json");
        await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));

        foreach (var v in all)
        {
            Assert.True(v.FirstRenderMs < 120_000, $"Map first render too slow ({v.Label}): {v.FirstRenderMs}ms");
            Assert.True(v.PinCountOnMap > 0, $"No pins on map ({v.Label}).");
            Assert.True(v.BrowserPinsRequests >= 1, $"Expected at least one pins request ({v.Label}).");
        }

        if (string.Equals(runLabel, "after", StringComparison.OrdinalIgnoreCase))
        {
            Assert.True(all.Any(v => v.BrowserPinsRequestsAfterPan >= 2),
                "Expected viewport pan/zoom to trigger a second pins HTTP response on PR branch.");
        }
    }

    private static async Task<ViewportMetrics> MeasureViewportAsync(
        IBrowser browser,
        string baseUrl,
        string outDir,
        string runLabel,
        string suffix,
        int width,
        int height)
    {
        var stats = new ConcurrentBag<RequestStat>();
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IsMobile = width < 500,
            HasTouch = width < 500,
            Locale = "nl-NL"
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();

        page.Response += async (_, resp) =>
        {
            var url = resp.Url;
            if (!url.Contains("/api/vacancies", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var bytes = 0;
            try
            {
                var body = await resp.BodyAsync();
                bytes = body.Length;
            }
            catch (PlaywrightException)
            {
            }

            stats.Add(new RequestStat(url, resp.Status, bytes, 0));
        };

        var sw = Stopwatch.StartNew();
        await page.GotoAsync(baseUrl + E2eRoutes.Banenkaart, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        var later = page.GetByRole(AriaRole.Link, new() { Name = "Later" });
        if (await later.CountAsync() > 0)
        {
            try { await later.First.ClickAsync(new() { Timeout = 3000 }); } catch (PlaywrightException) { }
        }

        await page.WaitForFunctionAsync(
            "() => !!(window.jobMap && document.querySelector('#job-map canvas'))",
            null,
            new() { Timeout = 120_000 });
        await page.WaitForTimeoutAsync(3500);
        sw.Stop();
        var firstRenderMs = sw.Elapsed.TotalMilliseconds;

        var list = stats.ToList();
        var pinsBeforePan = CountMatching(list, "pins");
        var pinCount = await page.EvaluateAsync<int>("() => (window.jobMap && window.jobMap.__testGetPinCount) ? window.jobMap.__testGetPinCount() : 0");
        var netBefore = await page.EvaluateAsync<int>("() => (window.jobMap && window.jobMap.__testGetPinsNetworkFetchCount) ? window.jobMap.__testGetPinsNetworkFetchCount() : 0");

        var clusterCount = await page.EvaluateAsync<int>("""
            () => {
              const m = window.jobMap && window.jobMap.__testGetMap && window.jobMap.__testGetMap();
              if (!m || !m.queryRenderedFeatures) return 0;
              try {
                const f = m.queryRenderedFeatures({ layers: ['jobsy-pins-clusters'] });
                return f ? f.length : 0;
              } catch (e) { return 0; }
            }
            """);

        await page.EvaluateAsync("""
            async () => {
              const m = window.jobMap && window.jobMap.__testGetMap && window.jobMap.__testGetMap();
              if (!m || typeof m.panBy !== 'function') return;
              await new Promise(r => {
                m.once('moveend', r);
                m.panBy([-220, 140], { duration: 350 });
              });
            }
            """);

        await page.WaitForTimeoutAsync(2500);

        list = stats.ToList();
        var pinsAfterPan = CountMatching(list, "pins");
        var netAfter = await page.EvaluateAsync<int>("() => (window.jobMap && window.jobMap.__testGetPinsNetworkFetchCount) ? window.jobMap.__testGetPinsNetworkFetchCount() : 0");
        var pinCountAfterPan = await page.EvaluateAsync<int>("() => (window.jobMap && window.jobMap.__testGetPinCount) ? window.jobMap.__testGetPinCount() : 0");

        var loadMore = false;
        var cards = await page.Locator(".job-card").CountAsync();
        if (width >= 1024)
        {
            var more = page.Locator(".vacancy-list__more");
            if (await more.CountAsync() > 0 && await more.First.IsVisibleAsync())
            {
                await more.First.ClickAsync();
                loadMore = true;
                await page.WaitForTimeoutAsync(1500);
                cards = await page.Locator(".job-card").CountAsync();
            }
        }
        else
        {
            var listTab = page.Locator("button, a").Filter(new() { HasText = "Lijst" });
            if (await listTab.CountAsync() > 0)
            {
                await listTab.First.ClickAsync();
                await page.WaitForTimeoutAsync(800);
            }

            var more = page.Locator(".vacancy-list__more");
            if (await more.CountAsync() > 0)
            {
                try
                {
                    await more.First.ClickAsync(new() { Timeout = 5000 });
                    loadMore = true;
                    await page.WaitForTimeoutAsync(1500);
                    cards = await page.Locator(".job-card").CountAsync();
                }
                catch (PlaywrightException) { }
            }
        }

        var shotPath = Path.Combine(outDir, $"{runLabel}-{suffix}.png");
        await page.ScreenshotAsync(new() { Path = shotPath, FullPage = false });

        await Task.Delay(800);
        list = stats.ToList();

        return new ViewportMetrics(
            width,
            height,
            suffix,
            firstRenderMs,
            list.Count,
            pinsBeforePan,
            SumBytes(list, "pins"),
            pinsAfterPan,
            netAfter,
            pinCount,
            pinCountAfterPan,
            clusterCount,
            loadMore,
            cards,
            shotPath);
    }

    private static async Task<ApiPayloadMetrics> MeasureApiPayloadsAsync(string webBaseUrl)
    {
        var apiBase = webBaseUrl.Replace(":5201", ":5200", StringComparison.Ordinal);
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };

        async Task<(byte[] Body, long Ms)> GetAsync(string path)
        {
            var sw = Stopwatch.StartNew();
            var body = await http.GetByteArrayAsync(apiBase + path);
            sw.Stop();
            return (body, sw.ElapsedMilliseconds);
        }

        var (catalogBody, _) = await GetAsync("/api/vacancies");
        var (discoverBody, _) = await GetAsync("/api/vacancies/discover?transport=Fiets&maxMinutes=60");
        var (pinsBody, _) = await GetAsync("/api/vacancies/pins?transport=Fiets&maxMinutes=60");
        var (pinsBboxBody, _) = await GetAsync(
            "/api/vacancies/pins?transport=Fiets&maxMinutes=60&minLat=51.9&minLng=4.2&maxLat=52.2&maxLng=4.8&zoom=10");

        using var catalogDoc = JsonDocument.Parse(catalogBody);
        using var discoverDoc = JsonDocument.Parse(discoverBody);
        using var pinsDoc = JsonDocument.Parse(pinsBody);
        using var pinsBboxDoc = JsonDocument.Parse(pinsBboxBody);

        var catalogItems = catalogDoc.RootElement.ValueKind == JsonValueKind.Array
            ? catalogDoc.RootElement.GetArrayLength()
            : catalogDoc.RootElement.GetProperty("items").GetArrayLength();
        var discoverItems = discoverDoc.RootElement.ValueKind == JsonValueKind.Array
            ? discoverDoc.RootElement.GetArrayLength()
            : discoverDoc.RootElement.GetProperty("items").GetArrayLength();

        return new ApiPayloadMetrics(
            catalogBody.LongLength,
            discoverBody.LongLength,
            pinsBody.LongLength,
            pinsBboxBody.LongLength,
            catalogItems,
            discoverItems,
            pinsDoc.RootElement.GetArrayLength(),
            pinsBboxDoc.RootElement.GetArrayLength());
    }

    private static int CountMatching(List<RequestStat> stats, string fragment)
        => stats.Count(s => s.Url.Contains(fragment, StringComparison.OrdinalIgnoreCase));

    private static long SumBytes(List<RequestStat> stats, string fragment)
        => stats.Where(s => s.Url.Contains(fragment, StringComparison.OrdinalIgnoreCase)).Sum(s => (long)s.ResponseBytes);

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var resp = await http.GetAsync(baseUrl + "/");
            return resp.IsSuccessStatusCode || (int)resp.StatusCode is 301 or 302;
        }
        catch
        {
            return false;
        }
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
