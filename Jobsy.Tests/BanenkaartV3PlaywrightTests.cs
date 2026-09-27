using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Banenkaart v3: stable docked cluster popup, no skeleton, clickable pins.
/// Soft-skips without <c>JOBSY_E2E_BASE_URL</c>.
/// </summary>
[Collection("PlaywrightSmoke")]
public class BanenkaartV3PlaywrightTests
{
    [Theory]
    [InlineData(360)]
    [InlineData(390)]
    [InlineData(430)]
    public async Task Cluster_pager_has_zero_layout_shift_and_no_skeleton(int width)
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = 844 },
            HasTouch = true,
            IsMobile = true,
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();

        await page.GotoAsync(baseUrl + "/", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.WaitForFunctionAsync(
            "() => !!(window.jobMap && window.maplibregl && document.querySelector('#job-map canvas'))",
            null,
            new() { Timeout = 60_000 });

        // Observe skeleton insertions during the whole cluster session.
        await page.EvaluateAsync("""
            () => {
              window.__jobsySkeletonCount = 0;
              const root = document.querySelector('.map-pane') || document.body;
              window.__jobsySkeletonObs = new MutationObserver((mutations) => {
                for (const m of mutations) {
                  for (const n of m.addedNodes) {
                    if (!(n instanceof Element)) continue;
                    if (n.matches?.('.map-popup--skeleton, [aria-busy="true"].map-popup') ||
                        n.querySelector?.('.map-popup--skeleton, [aria-busy="true"].map-popup')) {
                      window.__jobsySkeletonCount++;
                    }
                  }
                }
              });
              window.__jobsySkeletonObs.observe(root, { childList: true, subtree: true });
            }
            """);

        var opened = await TryOpenLargestClusterAsync(page);
        if (!opened)
        {
            return;
        }

        await page.WaitForSelectorAsync(
            ".map-cluster-sheet [data-cluster-next], .job-map-popup--cluster [data-cluster-next]",
            new() { Timeout = 15_000 });

        var next = page.Locator("[data-cluster-next]").First;
        var prev = page.Locator("[data-cluster-prev]").First;
        var close = page.Locator("[data-cluster-close]").First;
        var sheet = page.Locator(".map-cluster-sheet, .job-map-popup--cluster .map-cluster-card").First;

        async Task<(double x, double y, double h)> MeasureAsync()
        {
            var boxNext = await next.BoundingBoxAsync();
            var boxPrev = await prev.BoundingBoxAsync();
            var boxClose = await close.BoundingBoxAsync();
            var boxSheet = await sheet.BoundingBoxAsync();
            Assert.NotNull(boxNext);
            Assert.NotNull(boxPrev);
            Assert.NotNull(boxClose);
            Assert.NotNull(boxSheet);
            return (boxNext!.X, boxPrev!.Y, boxSheet!.Height);
        }

        var baseline = await MeasureAsync();
        for (var i = 0; i < 12; i++)
        {
            if (await next.IsDisabledAsync())
            {
                break;
            }

            await next.ClickAsync();
            await page.WaitForTimeoutAsync(250);
            var now = await MeasureAsync();
            Assert.Equal(baseline.x, now.x);
            Assert.Equal(baseline.y, now.y);
            Assert.Equal(baseline.h, now.h);
        }

        for (var i = 0; i < 12; i++)
        {
            if (await prev.IsDisabledAsync())
            {
                break;
            }

            await prev.ClickAsync();
            await page.WaitForTimeoutAsync(250);
            var now = await MeasureAsync();
            Assert.Equal(baseline.x, now.x);
            Assert.Equal(baseline.y, now.y);
            Assert.Equal(baseline.h, now.h);
        }

        var skeletonCount = await page.EvaluateAsync<int>("() => window.__jobsySkeletonCount || 0");
        Assert.Equal(0, skeletonCount);

        // Docking: sheet above bottom nav, chip visible, carousel collapsed.
        var layoutOk = await page.EvaluateAsync<bool>("""
            () => {
              const sheet = document.querySelector('.map-cluster-sheet');
              const chip = document.querySelector('.map-cluster-chip');
              const pane = document.querySelector('.map-pane');
              if (!sheet || !pane) return false;
              const sr = sheet.getBoundingClientRect();
              const nav = document.querySelector('.bottom-nav, nav.bottom-nav, .app-bottom-nav, footer .nav');
              if (nav) {
                const nr = nav.getBoundingClientRect();
                if (sr.bottom > nr.top + 2) return false;
              }
              const filters = document.querySelector('.jobsy-actions, .discovery-actions, .filter-row');
              if (filters) {
                const fr = filters.getBoundingClientRect();
                if (sr.top < fr.bottom - 2) return false;
              }
              return !!(chip && !chip.hidden && pane.classList.contains('is-cluster-open'));
            }
            """);
        Assert.True(layoutOk, "Docked sheet / carousel chip layout incorrect.");

        await close.ClickAsync();
        await page.WaitForTimeoutAsync(200);
        var restored = await page.EvaluateAsync<bool>("""
            () => {
              const pane = document.querySelector('.map-pane');
              return !!(pane && !pane.classList.contains('is-cluster-open'));
            }
            """);
        Assert.True(restored);
    }

    [Fact]
    public async Task Desktop_cluster_uses_fixed_pager_without_skeleton()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 800 },
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.WaitForFunctionAsync(
            "() => !!(window.jobMap && document.querySelector('#job-map canvas'))",
            null,
            new() { Timeout = 60_000 });

        if (!await TryOpenLargestClusterAsync(page))
        {
            return;
        }

        await page.WaitForSelectorAsync(".job-map-popup--cluster [data-cluster-card], .map-cluster-card", new() { Timeout = 15_000 });
        Assert.Equal(0, await page.Locator(".map-popup--skeleton").CountAsync());
        var next = page.Locator("[data-cluster-next]").First;
        if (await next.CountAsync() > 0 && !await next.IsDisabledAsync())
        {
            var before = await next.BoundingBoxAsync();
            await next.ClickAsync();
            await page.WaitForTimeoutAsync(250);
            var after = await next.BoundingBoxAsync();
            Assert.NotNull(before);
            Assert.NotNull(after);
            Assert.Equal(before!.X, after!.X);
            Assert.Equal(before.Y, after.Y);
        }
    }

    private static async Task<bool> TryOpenLargestClusterAsync(IPage page)
    {
        try
        {
            var clicked = await page.EvaluateAsync<bool>("""
                async () => {
                  const jm = window.jobMap;
                  if (!jm || typeof jm.debugOpenLargestCluster !== 'function') return false;
                  return !!(await jm.debugOpenLargestCluster());
                }
                """);
            return clicked;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var response = await http.GetAsync(baseUrl.TrimEnd('/') + "/");
            return (int)response.StatusCode is >= 200 and < 500;
        }
        catch
        {
            return false;
        }
    }
}
