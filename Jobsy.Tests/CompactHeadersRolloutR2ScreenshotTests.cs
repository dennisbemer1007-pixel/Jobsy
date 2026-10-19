using System.Text.Json;
using Jobsy.Web.Hosting;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>Rollout r2 review captures (NL; 390+1440 all pages, 1920 paspoort+carrière).</summary>
[Collection("PlaywrightSmoke")]
public sealed class CompactHeadersRolloutR2ScreenshotTests : IAsyncLifetime
{
    private const string ArtifactsDir = "/cursor/stores/self/artifacts/compact-headers-rollout-r2";

    private IllustratedPagesWebFactory? _web;
    private IllustratedPagesWebFactory? _onboardingWeb;
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
        IllustratedPagesApiStub.OnboardingWizardStep1Capture = true;
        _onboardingWeb = new IllustratedPagesWebFactory(emptyAppsAndLikes: true);
        IllustratedPagesApiStub.OnboardingWizardStep1Capture = false;
        _ = _web.ServerAddress;
        _ = _onboardingWeb.ServerAddress;
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        _web?.Dispose();
        _onboardingWeb?.Dispose();
    }

    [Fact]
    public async Task Capture_compact_hero_rollout_r2_nl()
    {
        Directory.CreateDirectory(ArtifactsDir);
        var report = new List<object>();
        var pages = new (string Path, string Slug, CompactHeadersRolloutScreenshotTests.HeroKind Kind)[]
        {
            ("/candidate/paspoort", "paspoort", CompactHeadersRolloutScreenshotTests.HeroKind.CompactBand),
            ("/carriere", "carriere", CompactHeadersRolloutScreenshotTests.HeroKind.CompactBand),
            ("/candidate/start", "onboarding", CompactHeadersRolloutScreenshotTests.HeroKind.Onboarding),
            ("/candidate/applications", "sollicitaties", CompactHeadersRolloutScreenshotTests.HeroKind.CompactBand),
            ("/candidate/liked", "bewaard", CompactHeadersRolloutScreenshotTests.HeroKind.CompactBand),
            ("/candidate/settings", "instellingen", CompactHeadersRolloutScreenshotTests.HeroKind.CompactBand),
            ("/bedrijvenkaart", "bedrijvenkaart", CompactHeadersRolloutScreenshotTests.HeroKind.CompactBand),
            ("/banenkaart?weergave=kaart", "banenkaart", CompactHeadersRolloutScreenshotTests.HeroKind.ChromeOnly),
            ("/candidate/ontdekkingsreis", "reis", CompactHeadersRolloutScreenshotTests.HeroKind.JourneyScene),
        };

        foreach (var width in new[] { 390, 1440, 1920 })
        {
            foreach (var (path, slug, kind) in pages)
            {
                if (width == 1920 && slug is not ("paspoort" or "carriere"))
                {
                    continue;
                }

                if (!PageAssetCatalog.RouteUsesCompactHeroCss(path.Split('?')[0]))
                {
                    continue;
                }

                var baseUrl = kind == CompactHeadersRolloutScreenshotTests.HeroKind.Onboarding
                    ? _onboardingWeb!.ServerAddress
                    : _web!.ServerAddress;
                var file = $"{slug}-{width}-nl.png";
                var (bytes, metrics) = await CaptureAsync(baseUrl, path, width, kind, slug);
                await File.WriteAllBytesAsync(Path.Combine(ArtifactsDir, file), bytes);
                report.Add(new { slug, width, file, metrics.HeroHeightPx, metrics.CompactHeroVisible });
            }
        }

        await File.WriteAllTextAsync(
            Path.Combine(ArtifactsDir, "r2-metrics.json"),
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    }

    private async Task<(byte[] Png, PageMetrics Metrics)> CaptureAsync(
        string serverAddress,
        string path,
        int width,
        CompactHeadersRolloutScreenshotTests.HeroKind kind,
        string slug)
    {
        var height = width <= 390 ? 844 : width >= 1920 ? 1080 : 900;
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
        if (kind == CompactHeadersRolloutScreenshotTests.HeroKind.Onboarding)
        {
            IllustratedPagesApiStub.OnboardingWizardStep1Capture = true;
        }

        try
        {
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
                await page.WaitForTimeoutAsync(2500);
            }

            if (kind == CompactHeadersRolloutScreenshotTests.HeroKind.Onboarding)
            {
                await page.Locator("[data-testid=onboarding-step-1-illustrated]").WaitForAsync(new() { Timeout = 120_000 });
            }

            var selector = HeroSelector(kind, width, slug);
            try
            {
                await page.Locator(selector).First.WaitForAsync(new() { Timeout = 60_000, State = WaitForSelectorState.Visible });
            }
            catch (TimeoutException) when (kind == CompactHeadersRolloutScreenshotTests.HeroKind.ChromeOnly)
            {
                // Mobile banenkaart hides jobsy-chrome__header; capture still valid.
            }
            await page.EvaluateAsync("() => window.scrollTo(0, 0)");
            await page.WaitForTimeoutAsync(900);

            var compactVisible = await page.Locator(".cand-compact-hero").CountAsync() > 0;
            var heroBox = await page.Locator(selector).First.BoundingBoxAsync();
            var png = await page.ScreenshotAsync(new() { Type = ScreenshotType.Png, FullPage = false });
            return (png, new PageMetrics
            {
                CompactHeroVisible = compactVisible,
                HeroHeightPx = heroBox is null ? 0 : (int)Math.Round(heroBox.Height)
            });
        }
        finally
        {
            if (kind == CompactHeadersRolloutScreenshotTests.HeroKind.Onboarding)
            {
                IllustratedPagesApiStub.OnboardingWizardStep1Capture = false;
            }
        }
    }

    private static string HeroSelector(CompactHeadersRolloutScreenshotTests.HeroKind kind, int width, string slug)
        => (kind, slug) switch
        {
            (CompactHeadersRolloutScreenshotTests.HeroKind.Onboarding, _) when width <= 390 =>
                ".ob-illustrated-step__mobile-cover",
            (CompactHeadersRolloutScreenshotTests.HeroKind.Onboarding, _) =>
                ".ob-illustrated-step--compact-preview-c .cand-compact-hero--onboarding",
            (CompactHeadersRolloutScreenshotTests.HeroKind.ChromeOnly, _) when width <= 390 =>
                ".bk-map-toggle--banen, .kb-filter-bar",
            (CompactHeadersRolloutScreenshotTests.HeroKind.ChromeOnly, _) => ".jobsy-chrome__header, .bk-map-toggle--banen",
            (CompactHeadersRolloutScreenshotTests.HeroKind.JourneyScene, _) when width <= 390 => ".journey-scene--mobile",
            (CompactHeadersRolloutScreenshotTests.HeroKind.JourneyScene, _) => ".journey-scene--desktop",
            (CompactHeadersRolloutScreenshotTests.HeroKind.CompactBand, "paspoort") =>
                ".passport-page--compact-preview-c > header.cand-compact-hero",
            (CompactHeadersRolloutScreenshotTests.HeroKind.CompactBand, "carriere") =>
                ".career-page--compact-preview-c > header.cand-compact-hero",
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
    }
}
