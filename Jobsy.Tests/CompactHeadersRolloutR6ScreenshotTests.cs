using System.Globalization;
using System.Text.Json;
using Microsoft.Playwright;

using Jobsy.Tests.Testing;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class CompactHeadersRolloutR6ScreenshotTests : IAsyncLifetime
{
    private const string ArtifactsDir = "/cursor/stores/self/artifacts/compact-headers-rollout-r6";

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
        IllustratedPagesApiStub.OnboardingWizardTestsStepCapture = false;
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
    public async Task Capture_compact_hero_rollout_r6_nl()
    {
        Directory.CreateDirectory(ArtifactsDir);
        var report = new List<object>();

        var pages = new (string Path, string Slug)[]
        {
            ("/bedrijvenkaart", "bedrijvenkaart"),
            ("/banenkaart?weergave=kaart", "banenkaart"),
            ("/candidate/settings", "instellingen"),
            ("/candidate/applications", "sollicitaties"),
            ("/candidate/ontdekkingsreis", "reis"),
            ("/candidate/paspoort", "paspoort"),
        };

        foreach (var width in new[] { 390, 1440 })
        {
            foreach (var (path, slug) in pages)
            {
                var file = $"{slug}-{width}-nl.png";
                var metrics = await CapturePageAsync(path, width, slug, Path.Combine(ArtifactsDir, file));
                report.Add(new { slug, width, file, metrics });
            }
        }

        foreach (var (step1Capture, stepSlug, waitSelector) in new (bool Step1, string Slug, string Wait)[]
                 {
                     (true, "onboarding-step1", "[data-testid=onboarding-step-1-illustrated]"),
                     (false, "onboarding-tests", ".test-flow, .test-question-flow"),
                 })
        {
            IllustratedPagesApiStub.OnboardingWizardStep1Capture = step1Capture;
            IllustratedPagesApiStub.OnboardingWizardTestsStepCapture = !step1Capture;
            try
            {
                foreach (var width in new[] { 390, 1440 })
                {
                    var file = $"{stepSlug}-{width}-nl.png";
                    var metrics = await CapturePageAsync(
                        "/candidate/start",
                        width,
                        stepSlug,
                        Path.Combine(ArtifactsDir, file),
                        waitSelector);
                    report.Add(new { slug = stepSlug, width, file, metrics });
                }
            }
            finally
            {
                IllustratedPagesApiStub.OnboardingWizardStep1Capture = false;
                IllustratedPagesApiStub.OnboardingWizardTestsStepCapture = false;
            }
        }

        await File.WriteAllTextAsync(
            Path.Combine(ArtifactsDir, "r6-metrics.json"),
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    }

    private async Task<object> CapturePageAsync(
        string path,
        int width,
        string slug,
        string outPath,
        string? extraWaitSelector = null)
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

        if (slug == "banenkaart")
        {
            await CompactHeadersRolloutR4ScreenshotTests.DismissBanenkaartStartPromptAsync(page);
            await CompactHeadersRolloutR4ScreenshotTests.WaitForBanenkaartMapReadyAsync(page);
        }
        else if (slug == "bedrijvenkaart")
        {
            await WaitForBedrijvenkaartMapReadyAsync(page);
        }
        else if (slug == "reis")
        {
            await page.Locator(".journey-overview, .journey-step").First.WaitForAsync(new() { Timeout = 60_000 });
        }

        if (!string.IsNullOrWhiteSpace(extraWaitSelector))
        {
            await page.Locator(extraWaitSelector).First.WaitForAsync(new() { Timeout = 120_000 });
        }

        var chromeCss = slug is "banenkaart" or "onboarding-step1" or "onboarding-tests"
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
                }
                """);
            await CompactHeadersRolloutR4ScreenshotTests.WaitForBanenkaartMapReadyAsync(page);
        }
        else if (slug == "bedrijvenkaart")
        {
            await page.EvaluateAsync("() => window.jobMap && window.jobMap.invalidate && window.jobMap.invalidate()");
            await page.WaitForTimeoutAsync(400);
            await WaitForBedrijvenkaartMapReadyAsync(page);
        }

        await page.EvaluateAsync("() => window.scrollTo(0, 0)");
        await page.WaitForTimeoutAsync(400);

        if (slug == "banenkaart" && width <= 390)
        {
            var mapOk = await CompactHeadersRolloutR4ScreenshotTests.MapRegionHasNonUniformPixelsAsync(page);
            Assert.True(mapOk, "banenkaart map region must show rendered tiles.");
        }

        if (slug == "bedrijvenkaart" && width >= 1440)
        {
            var mapOk = await BedrijvenkaartMapHasNonUniformPixelsAsync(page);
            Assert.True(mapOk, "bedrijvenkaart map pane must show rendered tiles at 1440.");
        }

        if (slug is "onboarding-step1" or "onboarding-tests")
        {
            var paspoortLead = await page.Locator(".passport-page .cand-hero__lead").CountAsync();
            Assert.Equal(0, paspoortLead);
        }

        await page.ScreenshotAsync(new() { Path = outPath, Type = ScreenshotType.Png, FullPage = false });

        return new { Path = path, Width = width, Slug = slug };
    }

    internal static async Task WaitForBedrijvenkaartMapReadyAsync(IPage page)
    {
        await page.WaitForSelectorAsync("#bedrijvenkaart-map canvas, #bedrijvenkaart-map .maplibregl-canvas", new() { Timeout = 120_000 });
        await page.WaitForFunctionAsync(
            """
            () => {
              const canvas = document.querySelector('#bedrijvenkaart-map canvas');
              return canvas && canvas.width > 80 && canvas.height > 80;
            }
            """,
            null,
            new() { Timeout = 120_000 });
        await page.WaitForTimeoutAsync(600);
    }

    internal static async Task<bool> BedrijvenkaartMapHasNonUniformPixelsAsync(IPage page)
    {
        var box = await page.Locator("#bedrijvenkaart-map").BoundingBoxAsync();
        if (box is null || box.Width < 80 || box.Height < 80)
        {
            return false;
        }

        var png = await page.Locator("#bedrijvenkaart-map").ScreenshotAsync(new() { Type = ScreenshotType.Png });
        using var image = SkiaTestImage.LoadRgba(png);
        if (image.Width < 8 || image.Height < 8)
        {
            return false;
        }

        var sample = image.GetRgb(image.Width / 2, image.Height / 2);
        var uniform = true;
        for (var y = 0; y < image.Height && uniform; y += Math.Max(1, image.Height / 12))
        {
            for (var x = 0; x < image.Width; x += Math.Max(1, image.Width / 12))
            {
                var p = image.GetRgb(x, y);
                if (Math.Abs(p.R - sample.R) > 8 || Math.Abs(p.G - sample.G) > 8 || Math.Abs(p.B - sample.B) > 8)
                {
                    uniform = false;
                    break;
                }
            }
        }

        return !uniform;
    }
}
