using System.Text.Json;
using Jobsy.Web.Hosting;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>R3 review captures: bedrijvenkaart, banenkaart toggle+map, instellingen alignment.</summary>
[Collection("PlaywrightSmoke")]
public sealed class CompactHeadersRolloutR3ScreenshotTests : IAsyncLifetime
{
    private const string ArtifactsDir = "/cursor/stores/self/artifacts/compact-headers-rollout-r3";

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
        IllustratedPagesApiStub.OnboardingWizardStep1Capture = false;
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
    public async Task Capture_compact_hero_rollout_r3_nl()
    {
        Directory.CreateDirectory(ArtifactsDir);
        var report = new List<object>();
        var pages = new (string Path, string Slug, CompactHeadersRolloutScreenshotTests.HeroKind Kind)[]
        {
            ("/candidate/liked", "bewaard", CompactHeadersRolloutScreenshotTests.HeroKind.CompactBand),
            ("/candidate/settings", "instellingen", CompactHeadersRolloutScreenshotTests.HeroKind.CompactBand),
            ("/bedrijvenkaart", "bedrijvenkaart", CompactHeadersRolloutScreenshotTests.HeroKind.CompactBand),
            ("/banenkaart?weergave=kaart", "banenkaart", CompactHeadersRolloutScreenshotTests.HeroKind.ChromeOnly),
        };

        foreach (var width in new[] { 390, 1440 })
        {
            foreach (var (path, slug, kind) in pages)
            {
                if (width == 1440 && slug is "banenkaart")
                {
                    continue;
                }

                var file = $"{slug}-{width}-nl.png";
                var (bytes, metrics) = await CaptureAsync(_web!.ServerAddress, path, width, kind, slug);
                await File.WriteAllBytesAsync(Path.Combine(ArtifactsDir, file), bytes);
                report.Add(new { slug, width, file, metrics.MapCanvasVisible, metrics.HeroHeightPx, metrics.CompactHeroVisible });
            }
        }

        await File.WriteAllTextAsync(
            Path.Combine(ArtifactsDir, "r3-metrics.json"),
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    }

    private async Task<(byte[] Png, PageMetrics Metrics)> CaptureAsync(
        string serverAddress,
        string path,
        int width,
        CompactHeadersRolloutScreenshotTests.HeroKind kind,
        string slug)
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
            $"{serverAddress}/__test/sign-in?return={Uri.EscapeDataString(path)}",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync($"{serverAddress}{path}", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.AddStyleTagAsync(new()
        {
            Content = """
                .pub-header, .bottom-nav, .candidate-bottom-nav,
                .lobsy-coach-dock, .pub-coach-dock { position: static !important; }
                .lobsy-assistant-tab, .pub-assistant-rail, .assistant-rail, [data-assistant-rail] { display: none !important; }
                """
        });

        if (slug == "bedrijvenkaart")
        {
            await page.WaitForSelectorAsync("#bedrijvenkaart-map canvas, #bedrijvenkaart-map .maplibregl-canvas", new() { Timeout = 120_000 });
            await page.WaitForTimeoutAsync(800);
        }

        if (slug == "banenkaart")
        {
            await page.WaitForSelectorAsync(".map-stage.is-live #job-map canvas, #job-map .maplibregl-canvas", new() { Timeout = 120_000 });
            await page.WaitForFunctionAsync(
                """
                () => {
                  const canvas = document.querySelector('#job-map canvas, .maplibregl-canvas');
                  return canvas instanceof HTMLCanvasElement && canvas.width > 0 && canvas.height > 0;
                }
                """,
                null,
                new() { Timeout = 120_000 });
            await page.WaitForTimeoutAsync(600);
        }

        var selector = HeroSelector(kind, width, slug);
        try
        {
            await page.Locator(selector).First.WaitForAsync(new() { Timeout = 60_000, State = WaitForSelectorState.Visible });
        }
        catch (TimeoutException) when (kind == CompactHeadersRolloutScreenshotTests.HeroKind.ChromeOnly)
        {
            // Mobile banenkaart hides jobsy-chrome__header; toggle + map still valid.
        }

        await page.EvaluateAsync("() => window.scrollTo(0, 0)");
        await page.WaitForTimeoutAsync(400);

        var compactVisible = await page.Locator(".cand-compact-hero").CountAsync() > 0;
        var heroBox = slug == "banenkaart"
            ? await page.Locator(".bk-map-toggle--banen").First.BoundingBoxAsync()
            : await page.Locator(selector).First.BoundingBoxAsync();
        var mapCanvas = slug is "banenkaart" or "bedrijvenkaart"
            && await page.Locator("canvas.maplibregl-canvas").CountAsync() > 0;

        var png = await page.ScreenshotAsync(new() { Type = ScreenshotType.Png, FullPage = false });
        return (png, new PageMetrics
        {
            CompactHeroVisible = compactVisible,
            HeroHeightPx = heroBox is null ? 0 : (int)Math.Round(heroBox.Height),
            MapCanvasVisible = mapCanvas
        });
    }

    private static string HeroSelector(CompactHeadersRolloutScreenshotTests.HeroKind kind, int width, string slug)
        => (kind, slug) switch
        {
            (CompactHeadersRolloutScreenshotTests.HeroKind.ChromeOnly, _) when width <= 390 =>
                ".bk-map-toggle--banen, .kb-filter-bar",
            (CompactHeadersRolloutScreenshotTests.HeroKind.ChromeOnly, _) => ".jobsy-chrome__header, .bk-map-toggle--banen",
            (CompactHeadersRolloutScreenshotTests.HeroKind.CompactBand, "bedrijvenkaart") =>
                ".bk-page__hero-band > header.cand-compact-hero",
            (CompactHeadersRolloutScreenshotTests.HeroKind.CompactBand, _) =>
                ".panel-page--cand-ill > header.cand-compact-hero",
            _ => ".cand-compact-hero"
        };

    private sealed class PageMetrics
    {
        public bool CompactHeroVisible { get; set; }
        public int HeroHeightPx { get; set; }
        public bool MapCanvasVisible { get; set; }
    }
}
