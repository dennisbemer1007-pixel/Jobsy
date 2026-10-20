using System.Text.Json;
using Microsoft.Playwright;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class CompactHeadersRolloutR7ScreenshotTests : IAsyncLifetime
{
    private const string ArtifactsDir = "/cursor/stores/self/artifacts/compact-headers-rollout-r7";

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
    public async Task Capture_compact_hero_rollout_r7_nl()
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
            Path.Combine(ArtifactsDir, "r7-metrics.json"),
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

        var chromeCss = slug is "banenkaart" or "onboarding-step1" or "onboarding-tests"
            ? """
              .lobsy-assistant-tab, .pub-assistant-rail, .assistant-rail, [data-assistant-rail] { display: none !important; }
              """
            : PanelChromeCss;
        await page.AddStyleTagAsync(new() { Content = chromeCss });
        await EnsureScrollTopAsync(page);

        if (slug == "banenkaart")
        {
            await CompactHeadersRolloutR4ScreenshotTests.DismissBanenkaartStartPromptAsync(page);
            await CompactHeadersRolloutR4ScreenshotTests.WaitForBanenkaartMapReadyAsync(page);
        }
        else if (slug == "bedrijvenkaart")
        {
            await page.Locator(".bk-page__hero-band").WaitForAsync(new() { Timeout = 60_000 });
            await CompactHeadersRolloutR6ScreenshotTests.WaitForBedrijvenkaartMapReadyAsync(page);
            await page.EvaluateAsync("() => window.jobMap && window.jobMap.invalidate && window.jobMap.invalidate()");
            await page.WaitForTimeoutAsync(300);
        }
        else if (slug == "reis")
        {
            await page.Locator(".journey-overview, .journey-step").First.WaitForAsync(new() { Timeout = 60_000 });
        }

        if (!string.IsNullOrWhiteSpace(extraWaitSelector))
        {
            await page.Locator(extraWaitSelector).First.WaitForAsync(new() { Timeout = 120_000 });
        }

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

        await EnsureScrollTopAsync(page);
        await page.WaitForTimeoutAsync(350);

        if (slug == "bedrijvenkaart")
        {
            await AssertBedrijvenkaartHeroInViewportAsync(page, width);
            var mapOk = width >= 1440 && await CompactHeadersRolloutR6ScreenshotTests.BedrijvenkaartMapHasNonUniformPixelsAsync(page);
            if (width >= 1440)
            {
                Assert.True(mapOk, "bedrijvenkaart map pane must show rendered tiles at 1440.");
            }
        }

        if (slug == "banenkaart" && width <= 390)
        {
            var mapOk = await CompactHeadersRolloutR4ScreenshotTests.MapRegionHasNonUniformPixelsAsync(page);
            Assert.True(mapOk, "banenkaart map region must show rendered tiles.");
        }

        if (slug is "onboarding-step1" or "onboarding-tests")
        {
            Assert.Equal(0, await page.Locator(".passport-page .cand-hero__lead").CountAsync());
        }

        await page.ScreenshotAsync(new() { Path = outPath, Type = ScreenshotType.Png, FullPage = false });

        return new { Path = path, Width = width, Slug = slug, ScrollY = await page.EvaluateAsync<double>("() => window.scrollY") };
    }

    private static async Task EnsureScrollTopAsync(IPage page)
    {
        await page.EvaluateAsync(
            """
            () => {
              window.scrollTo(0, 0);
              document.documentElement.scrollTop = 0;
              document.body.scrollTop = 0;
            }
            """);
    }

    private static async Task AssertBedrijvenkaartHeroInViewportAsync(IPage page, int width)
    {
        var scrollY = await page.EvaluateAsync<double>("() => window.scrollY");
        Assert.True(scrollY < 4, $"bedrijvenkaart capture must start at scrollY=0 (was {scrollY}).");

        var band = page.Locator(".bk-page__hero-band").First;
        await band.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        var bandBox = await band.BoundingBoxAsync();
        Assert.NotNull(bandBox);
        var maxBandTop = width <= 390 ? 220 : 200;
        Assert.True(bandBox!.Y >= 0 && bandBox.Y < maxBandTop,
            $"hero band should be in viewport below chrome (y={bandBox.Y}, maxTop={maxBandTop}).");
        Assert.True(bandBox.Height >= 72, $"hero band too short ({bandBox.Height}px).");

        var hero = page.Locator(".bk-page__hero-band > header.cand-compact-hero").First;
        var box = await hero.BoundingBoxAsync();
        Assert.NotNull(box);
        Assert.True(box!.Height >= 72, $"compact hero too short ({box.Height}px).");

        var filters = page.Locator(".bk-info-banner, .bk-filters").First;
        var filtersBox = await filters.BoundingBoxAsync();
        if (filtersBox is not null)
        {
            Assert.True(filtersBox.Y < 900, "filters should be visible in above-the-fold capture.");
        }
    }
}
