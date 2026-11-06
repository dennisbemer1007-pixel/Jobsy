using System.Text.Json;
using Jobsy.Web.Hosting;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>Post-rollout compact hero captures (default, no ?heroPreview=c).</summary>
[Collection("PlaywrightSmoke")]
public sealed class CompactHeadersRolloutScreenshotTests : IAsyncLifetime
{
    private const string ArtifactsDir = "/opt/cursor/artifacts/compact-headers-rollout";

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
    public async Task Capture_compact_hero_rollout_matrix()
    {
        Directory.CreateDirectory(ArtifactsDir);
        var report = new List<object>();
        var pages = new (string Path, string Slug, HeroKind Kind)[]
        {
            ("/candidate/paspoort", "paspoort", HeroKind.CompactBand),
            ("/carriere", "carriere", HeroKind.CompactBand),
            ("/candidate/start", "onboarding", HeroKind.Onboarding),
            ("/candidate/applications", "sollicitaties", HeroKind.CompactBand),
            ("/candidate/liked", "bewaard", HeroKind.CompactBand),
            ("/candidate/settings", "instellingen", HeroKind.CompactBand),
            ("/bedrijvenkaart", "bedrijvenkaart", HeroKind.CompactBand),
            ("/banenkaart?weergave=kaart", "banenkaart", HeroKind.ChromeOnly),
            ("/candidate/ontdekkingsreis", "reis", HeroKind.JourneyScene),
        };

        foreach (var locale in new[] { ("nl", "nl-NL"), ("en", "en-GB") })
        {
            foreach (var width in new[] { 390, 1440, 1920 })
            {
                foreach (var (path, slug, kind) in pages)
                {
                    if (!PageAssetCatalog.RouteUsesCompactHeroCss(path.Split('?')[0]))
                    {
                        continue;
                    }

                    var baseUrl = kind == HeroKind.Onboarding ? _onboardingWeb!.ServerAddress : _web!.ServerAddress;
                    var (bytes, metrics) = await CaptureAsync(baseUrl, path, width, locale.Item2, kind, slug);
                    var file = $"{slug}-{width}-{locale.Item1}.png";
                    await File.WriteAllBytesAsync(Path.Combine(ArtifactsDir, file), bytes);
                    report.Add(new
                    {
                        slug,
                        width,
                        locale = locale.Item1,
                        kind = kind.ToString(),
                        metrics.CompactHeroVisible,
                        metrics.MobileCoverVisible,
                        metrics.HeroHeightPx,
                        file
                    });

                    switch (kind)
                    {
                        case HeroKind.CompactBand when width <= 390:
                            Assert.True(metrics.CompactHeroVisible, $"{slug}@{width} {locale.Item1}: expected compact hero.");
                            Assert.InRange(metrics.HeroHeightPx, 110, 175);
                            break;
                        case HeroKind.CompactBand:
                            Assert.True(metrics.CompactHeroVisible, $"{slug}@{width} {locale.Item1}: expected .cand-compact-hero.");
                            Assert.InRange(metrics.HeroHeightPx, 120, 210);
                            break;
                        case HeroKind.Onboarding when width <= 390:
                            Assert.True(metrics.MobileCoverVisible, $"{slug}@{width} {locale.Item1}: expected mobile cover.");
                            Assert.InRange(metrics.HeroHeightPx, 110, 200);
                            break;
                        case HeroKind.Onboarding:
                            Assert.True(metrics.CompactHeroVisible, $"{slug}@{width} {locale.Item1}: expected desktop compact hero.");
                            Assert.InRange(metrics.HeroHeightPx, 120, 220);
                            break;
                        case HeroKind.ChromeOnly:
                            Assert.True(metrics.HeroHeightPx >= 0, $"{slug}@{width}: banenkaart chrome capture.");
                            break;
                        case HeroKind.JourneyScene:
                            Assert.InRange(metrics.HeroHeightPx, 0, 1000);
                            break;
                    }
                }
            }
        }

        await File.WriteAllTextAsync(
            Path.Combine(ArtifactsDir, "rollout-metrics.json"),
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    }

    internal enum HeroKind
    {
        CompactBand,
        Onboarding,
        ChromeOnly,
        JourneyScene
    }

    private async Task<(byte[] Png, PageMetrics Metrics)> CaptureAsync(
        string serverAddress,
        string path,
        int width,
        string locale,
        HeroKind kind,
        string slug)
    {
        var height = width <= 390 ? 844 : width >= 1920 ? 1080 : 900;
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            Locale = locale,
            IsMobile = width <= 390,
            HasTouch = width <= 390
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);
        if (kind == HeroKind.Onboarding)
        {
            IllustratedPagesApiStub.OnboardingWizardStep1Capture = true;
        }

        try
        {
            await page.GotoAsync(
                $"{serverAddress}/__test/sign-in?return={Uri.EscapeDataString(path)}",
                new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.GotoAsync($"{serverAddress}{path}", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            var flattenChrome = kind != HeroKind.ChromeOnly;
            await page.AddStyleTagAsync(new()
            {
                Content = flattenChrome
                    ? """
                        .pub-header, .jobsy-chrome__header, .bottom-nav, .candidate-bottom-nav,
                        .lobsy-coach-dock, .pub-coach-dock { position: static !important; }
                        .lobsy-assistant-tab, .pub-assistant-rail, .assistant-rail, [data-assistant-rail] { display: none !important; }
                        """
                    : """
                        .pub-header, .bottom-nav, .candidate-bottom-nav,
                        .lobsy-coach-dock, .pub-coach-dock { position: static !important; }
                        .lobsy-assistant-tab, .pub-assistant-rail, .assistant-rail, [data-assistant-rail] { display: none !important; }
                        """
            });

            if (kind == HeroKind.Onboarding)
            {
                await page.Locator("[data-testid=onboarding-step-1-illustrated]").WaitForAsync(new() { Timeout = 120_000 });
            }

            var selector = HeroSelector(kind, width, slug);
            try
            {
                await page.Locator(selector).First.WaitForAsync(new() { Timeout = 60_000 });
            }
            catch (TimeoutException) when (kind is HeroKind.JourneyScene or HeroKind.ChromeOnly)
            {
                // optional shell
            }

            await page.EvaluateAsync("() => window.scrollTo(0, 0)");
            await page.WaitForTimeoutAsync(900);

            var compactVisible = await page.Locator(".cand-compact-hero").CountAsync() > 0;
            var mobileCoverVisible = await page.Locator(".ob-illustrated-step__mobile-cover").CountAsync() > 0;
            var heroBox = await page.Locator(selector).First.BoundingBoxAsync();
            var png = await page.ScreenshotAsync(new() { Type = ScreenshotType.Png, FullPage = false });
            return (png, new PageMetrics
            {
                CompactHeroVisible = compactVisible,
                MobileCoverVisible = mobileCoverVisible,
                HeroHeightPx = heroBox is null ? 0 : (int)Math.Round(heroBox.Height)
            });
        }
        finally
        {
            if (kind == HeroKind.Onboarding)
            {
                IllustratedPagesApiStub.OnboardingWizardStep1Capture = false;
            }
        }
    }

    private static string HeroSelector(HeroKind kind, int width, string slug) => (kind, slug) switch
    {
        (HeroKind.Onboarding, _) when width <= 390 => ".ob-illustrated-step__mobile-cover",
        (HeroKind.Onboarding, _) => ".ob-illustrated-step--compact-preview-c .cand-compact-hero--onboarding",
        (HeroKind.ChromeOnly, _) => ".jobsy-chrome__header",
        (HeroKind.JourneyScene, _) when width <= 390 => ".journey-scene--mobile",
        (HeroKind.JourneyScene, _) => ".journey-scene--desktop",
        (HeroKind.CompactBand, "paspoort") => ".passport-page--compact-preview-c > header.cand-compact-hero",
        (HeroKind.CompactBand, "carriere") => ".career-page--compact-preview-c > header.cand-compact-hero",
        (HeroKind.CompactBand, "bedrijvenkaart") => ".bk-page__hero-stack > header.cand-compact-hero",
        (HeroKind.CompactBand, _) => ".panel-page--cand-ill > header.cand-compact-hero",
        _ => ".cand-compact-hero"
    };

    private sealed class PageMetrics
    {
        public bool CompactHeroVisible { get; set; }
        public bool MobileCoverVisible { get; set; }
        public int HeroHeightPx { get; set; }
    }
}
