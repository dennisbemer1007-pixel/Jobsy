using System.Globalization;
using System.Text.Json;
using Microsoft.Playwright;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Jobsy.Tests;

/// <summary>R4 review captures with visual map readiness (non-uniform map region pixels).</summary>
[Collection("PlaywrightSmoke")]
public sealed class CompactHeadersRolloutR4ScreenshotTests : IAsyncLifetime
{
    private const string ArtifactsDir = "/cursor/stores/self/artifacts/compact-headers-rollout-r4";

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
    public async Task Capture_compact_hero_rollout_r4_nl()
    {
        Directory.CreateDirectory(ArtifactsDir);
        var report = new List<object>();

        foreach (var (path, slug, width) in new[]
                 {
                     ("/bedrijvenkaart", "bedrijvenkaart", 390),
                     ("/bedrijvenkaart", "bedrijvenkaart", 1440),
                     ("/candidate/settings", "instellingen", 1440),
                     ("/banenkaart?weergave=kaart", "banenkaart", 390),
                 })
        {
            var file = $"{slug}-{width}-nl.png";
            var metrics = await CaptureAsync(_web!.ServerAddress, path, width, slug, Path.Combine(ArtifactsDir, file));
            report.Add(new { slug, width, file, metrics });
        }

        await File.WriteAllTextAsync(
            Path.Combine(ArtifactsDir, "r4-metrics.json"),
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    }

    private async Task<object> CaptureAsync(string serverAddress, string path, int width, string slug, string outPath)
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

        if (slug == "banenkaart")
        {
            await DismissBanenkaartStartPromptAsync(page);
            await WaitForBanenkaartMapReadyAsync(page);
        }
        else if (slug == "bedrijvenkaart")
        {
            await page.WaitForSelectorAsync("#bedrijvenkaart-map canvas, #bedrijvenkaart-map .maplibregl-canvas", new() { Timeout = 120_000 });
            await page.WaitForTimeoutAsync(1200);
        }

        var chromeCss = slug == "banenkaart"
            ? """
              .lobsy-assistant-tab, .pub-assistant-rail, .assistant-rail, [data-assistant-rail] { display: none !important; }
              """
            : """
              .pub-header, .bottom-nav, .candidate-bottom-nav,
              .lobsy-coach-dock, .pub-coach-dock { position: static !important; }
              .lobsy-assistant-tab, .pub-assistant-rail, .assistant-rail, [data-assistant-rail] { display: none !important; }
              """;
        await page.AddStyleTagAsync(new() { Content = chromeCss });

        if (slug == "banenkaart")
        {
            await page.EvaluateAsync(
                """
                () => {
                  const m = window.jobMap && window.jobMap.__testGetMap && window.jobMap.__testGetMap();
                  if (m && typeof m.resize === 'function') m.resize();
                  if (m && typeof m.triggerRepaint === 'function') m.triggerRepaint();
                }
                """);
            await page.WaitForTimeoutAsync(400);
            await WaitForBanenkaartMapReadyAsync(page);
        }

        await page.EvaluateAsync("() => window.scrollTo(0, 0)");
        await page.WaitForTimeoutAsync(350);

        bool? mapPixelsOk = null;
        double mapHeight = 0;
        if (slug == "banenkaart")
        {
            mapHeight = await page.EvaluateAsync<double>(
                "() => document.querySelector('#job-map')?.getBoundingClientRect().height ?? 0");
            Assert.True(mapHeight > 200, $"banenkaart map stage height too small: {mapHeight.ToString(CultureInfo.InvariantCulture)}px");

            mapPixelsOk = await MapRegionHasNonUniformPixelsAsync(page);
            if (mapPixelsOk == false)
            {
                Assert.Fail($"{slug}@{width}: map region has no rendered tile variation (blank map background).");
            }
        }

        await page.ScreenshotAsync(new() { Path = outPath, Type = ScreenshotType.Png, FullPage = false });

        return new
        {
            MapPixelsOk = mapPixelsOk,
            MapHeightPx = mapHeight,
            HeroHeightPx = await HeroHeightAsync(page, slug)
        };
    }

    private static async Task<int> HeroHeightAsync(IPage page, string slug)
    {
        var selector = slug switch
        {
            "bedrijvenkaart" => ".bk-page__hero-band > header.cand-compact-hero",
            "banenkaart" => ".bk-map-toggle--banen",
            _ => ".panel-page--cand-ill > header.cand-compact-hero"
        };
        var box = await page.Locator(selector).First.BoundingBoxAsync();
        return box is null ? 0 : (int)Math.Round(box.Height);
    }

    private static async Task DismissBanenkaartStartPromptAsync(IPage page)
    {
        var later = page.Locator(".kb-start-prompt__later");
        if (await later.CountAsync() > 0)
        {
            await later.ClickAsync();
            await page.WaitForTimeoutAsync(300);
        }
    }

    private static async Task WaitForBanenkaartMapReadyAsync(IPage page)
    {
        await page.WaitForSelectorAsync(".map-stage.is-live #job-map canvas", new() { Timeout = 120_000 });
        await page.WaitForFunctionAsync(
            """
            () => {
              const live = window.jobMap && window.jobMap.__testGetMap ? window.jobMap.__testGetMap() : null;
              const canvas = document.querySelector('#job-map canvas');
              if (!canvas || canvas.width < 50) return false;
              if (live && typeof live.isStyleLoaded === 'function' && !live.isStyleLoaded()) return false;
              return document.querySelector('.map-stage.is-live') !== null;
            }
            """,
            null,
            new() { Timeout = 120_000 });

        var pinsResponse = page.WaitForResponseAsync(
            r => r.Url.Contains("/api/vacancies/pins", StringComparison.OrdinalIgnoreCase) && r.Ok,
            new() { Timeout = 120_000 });

        await page.EvaluateAsync(
            """
            async () => {
              const map = window.jobMap;
              if (!map) return;
              const m = map.__testGetMap && map.__testGetMap();
              const needsRegion = !m || typeof m.getZoom !== 'function' || m.getZoom() < 9.5;
              if (needsRegion && typeof map.setOrigin === 'function') {
                map.setOrigin(52.011, 4.357, { maxMinutes: 30, transport: 'Fiets', radiusKm: 20 });
              }
              if (needsRegion && typeof map.reloadPins === 'function') {
                await map.reloadPins('/api/vacancies/pins?transport=Fiets&maxMinutes=30&originLat=52.011&originLng=4.357&radiusKm=20');
              }
              const live = map.__testGetMap && map.__testGetMap();
              if (live && typeof live.getZoom === 'function' && live.getZoom() < 9.5 && typeof live.jumpTo === 'function') {
                live.jumpTo({ center: [4.357, 52.011], zoom: 11, animate: false });
              }
              if (live && typeof live.resize === 'function') live.resize();
              if (live && typeof live.triggerRepaint === 'function') live.triggerRepaint();
            }
            """);

        try
        {
            await pinsResponse;
        }
        catch (TimeoutException)
        {
            /* pins may already be loaded from boot JSON */
        }

        await page.WaitForFunctionAsync(
            """
            () => {
              const map = window.jobMap;
              if (!map) return false;
              const count = map.__testGetPinCount ? map.__testGetPinCount() : 0;
              if (count > 0) return true;
              const boot = document.getElementById('jobsy-map-boot');
              return !!(boot && boot.textContent && boot.textContent.indexOf('"id"') >= 0);
            }
            """,
            null,
            new() { Timeout = 90_000 });

        await page.WaitForFunctionAsync(
            """
            () => {
              const live = window.jobMap && window.jobMap.__testGetMap ? window.jobMap.__testGetMap() : null;
              if (!live || typeof live.areTilesLoaded !== 'function') return true;
              return live.areTilesLoaded();
            }
            """,
            null,
            new() { Timeout = 45_000 });

        await page.WaitForTimeoutAsync(800);
    }

    /// <summary>Samples the visible #job-map screenshot — reliable when WebGL readPixels is blank in headless.</summary>
    private static async Task<bool> MapRegionHasNonUniformPixelsAsync(IPage page)
    {
        var map = page.Locator("#job-map");
        await map.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        var box = await map.BoundingBoxAsync();
        if (box is null || box.Height < 180 || box.Width < 180)
        {
            return false;
        }

        var png = await map.ScreenshotAsync(new() { Type = ScreenshotType.Png });
        return MapPngHasNonUniformPixels(png);
    }

    internal static bool MapPngHasNonUniformPixels(byte[] pngBytes)
    {
        using var img = Image.Load<Rgb24>(pngBytes);
        if (img.Width < 40 || img.Height < 40)
        {
            return false;
        }

        var minLum = 255;
        var maxLum = 0;
        var minR = 255;
        var maxR = 0;
        var minG = 255;
        var maxG = 0;
        var minB = 255;
        var maxB = 0;

        var yStart = img.Height / 5;
        var yEnd = 4 * img.Height / 5;
        var xStart = img.Width / 5;
        var xEnd = 4 * img.Width / 5;

        for (var y = yStart; y < yEnd; y += 6)
        {
            for (var x = xStart; x < xEnd; x += 6)
            {
                var p = img[x, y];
                var lum = (p.R + p.G + p.B) / 3;
                if (lum < minLum)
                {
                    minLum = lum;
                }

                if (lum > maxLum)
                {
                    maxLum = lum;
                }

                minR = Math.Min(minR, p.R);
                maxR = Math.Max(maxR, p.R);
                minG = Math.Min(minG, p.G);
                maxG = Math.Max(maxG, p.G);
                minB = Math.Min(minB, p.B);
                maxB = Math.Max(maxB, p.B);
            }
        }

        var lumSpread = maxLum - minLum;
        var channelSpread = Math.Max(maxR - minR, Math.Max(maxG - minG, maxB - minB));
        return lumSpread > 18 && channelSpread > 12;
    }
}
