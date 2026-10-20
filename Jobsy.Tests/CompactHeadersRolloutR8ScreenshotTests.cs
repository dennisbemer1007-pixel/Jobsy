using System.Text.Json;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class CompactHeadersRolloutR8ScreenshotTests : IAsyncLifetime
{
    private const string ArtifactsDir = "/cursor/stores/self/artifacts/compact-headers-rollout-r8";

    private const string PanelChromeCss = """
        .app-header, .pub-header, .bottom-nav, .candidate-bottom-nav,
        .lobsy-coach-dock, .pub-coach-dock { position: static !important; top: auto !important; }
        .lobsy-assistant-tab, .pub-assistant-rail, .assistant-rail, [data-assistant-rail] { display: none !important; }
        """;

    private IllustratedPagesWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "true");
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        _web = new IllustratedPagesWebFactory(emptyAppsAndLikes: true);
        _ = _web.ServerAddress;
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        _web?.Dispose();
    }

    [Fact]
    public async Task Capture_compact_hero_rollout_r8_nl()
    {
        Directory.CreateDirectory(ArtifactsDir);
        var report = new List<object>();

        foreach (var (path, slug) in new (string Path, string Slug)[]
                 {
                     ("/bedrijvenkaart", "bedrijvenkaart"),
                     ("/candidate/ontdekkingsreis", "reis"),
                 })
        {
            foreach (var width in new[] { 390, 1440 })
            {
                var file = $"{slug}-{width}-nl.png";
                var metrics = await CapturePageAsync(path, width, slug, Path.Combine(ArtifactsDir, file));
                report.Add(new { slug, width, file, metrics });
            }
        }

        await File.WriteAllTextAsync(
            Path.Combine(ArtifactsDir, "r8-metrics.json"),
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    }

    private async Task<object> CapturePageAsync(string path, int width, string slug, string outPath)
    {
        var height = width <= 390 ? 844 : 900;
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            Locale = "nl-NL",
            IsMobile = width <= 390,
            HasTouch = width <= 390
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await page.GotoAsync(
            $"{_web!.ServerAddress}/__test/sign-in?return={Uri.EscapeDataString(path)}",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync($"{_web.ServerAddress}{path}", new() { WaitUntil = WaitUntilState.DOMContentLoaded });

        await page.AddStyleTagAsync(new() { Content = PanelChromeCss });

        if (slug == "bedrijvenkaart")
        {
            await page.Locator(".bk-page__hero-band").WaitForAsync(new() { Timeout = 60_000 });
            await CompactHeadersRolloutR6ScreenshotTests.WaitForBedrijvenkaartMapReadyAsync(page);
            await page.EvaluateAsync("() => window.jobMap && window.jobMap.invalidate && window.jobMap.invalidate()");
            await page.WaitForTimeoutAsync(400);
        }
        else if (slug == "reis")
        {
            await page.Locator(".journey-overview, .journey-step").First.WaitForAsync(new() { Timeout = 60_000 });
        }

        await ScrollToTopAndSettleAsync(page);

        if (slug == "bedrijvenkaart")
        {
            await AssertBedrijvenkaartCaptureAsync(page, width);
            if (width >= 1440)
            {
                var mapOk = await CompactHeadersRolloutR6ScreenshotTests.BedrijvenkaartMapHasNonUniformPixelsAsync(page);
                Assert.True(mapOk, "bedrijvenkaart map pane must show rendered tiles at 1440.");
            }
        }
        else if (slug == "reis" && width >= 900)
        {
            await AssertReisOverviewRowsFillCardAsync(page);
        }

        await ScrollToTopAndSettleAsync(page);
        await page.ScreenshotAsync(new() { Path = outPath, Type = ScreenshotType.Png, FullPage = false });

        return new
        {
            Path = path,
            Width = width,
            Slug = slug,
            ScrollY = await page.EvaluateAsync<double>("() => window.scrollY")
        };
    }

    private static async Task ScrollToTopAndSettleAsync(IPage page)
    {
        await page.EvaluateAsync(
            """
            () => new Promise(resolve => {
              const top = () => {
                if (window.jobsyDom && typeof jobsyDom.scrollToTop === 'function') {
                  jobsyDom.scrollToTop();
                } else {
                  window.scrollTo(0, 0);
                  document.documentElement.scrollTop = 0;
                  document.body.scrollTop = 0;
                }
              };
              top();
              requestAnimationFrame(() => {
                top();
                requestAnimationFrame(() => resolve());
              });
            })
            """);
        await page.WaitForTimeoutAsync(200);
    }

    private static async Task AssertBedrijvenkaartCaptureAsync(IPage page, int width)
    {
        var scrollY = await page.EvaluateAsync<double>("() => window.scrollY");
        Assert.True(scrollY < 4, $"bedrijvenkaart capture must start at scrollY=0 (was {scrollY}).");

        var header = page.Locator(".app-header").First;
        await header.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        var headerBox = await header.BoundingBoxAsync();
        Assert.NotNull(headerBox);
        Assert.True(headerBox!.Y >= 0 && headerBox.Y < 24,
            $"app header must be visible at top of viewport (y={headerBox.Y}).");

        var band = page.Locator(".bk-page__hero-band").First;
        var bandBox = await band.BoundingBoxAsync();
        Assert.NotNull(bandBox);
        Assert.True(bandBox!.Y >= headerBox.Y && bandBox.Y < (width <= 390 ? 240 : 220),
            $"hero band should sit below header in viewport (y={bandBox.Y}).");
        Assert.True(bandBox.Height >= 72, $"hero band too short ({bandBox.Height}px).");

        var filters = page.Locator(".bk-info-banner, .bk-filters").First;
        var filtersBox = await filters.BoundingBoxAsync();
        if (filtersBox is not null)
        {
            Assert.True(filtersBox.Y < 900, "filters should be visible in above-the-fold capture.");
        }
    }

    private static async Task AssertReisOverviewRowsFillCardAsync(IPage page)
    {
        var card = page.Locator(".journey-step:has(.journey-overview)").First;
        var row = page.Locator(".journey-overview__row:not(.journey-overview__row--skeleton)").First;
        await row.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        var cardBox = await card.BoundingBoxAsync();
        var rowBox = await row.BoundingBoxAsync();
        Assert.NotNull(cardBox);
        Assert.NotNull(rowBox);
        var cardInnerWidth = cardBox!.Width - 64;
        Assert.True(rowBox!.Width >= cardInnerWidth * 0.92,
            $"overview row should span the card (row={rowBox.Width}px, card~inner={cardInnerWidth}px).");
    }
}
