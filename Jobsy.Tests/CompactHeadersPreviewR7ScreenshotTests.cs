using System.Text.Json;
using Jobsy.Web.Media;
using Microsoft.Playwright;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Jobsy.Tests;

/// <summary>Variant C r7: unified CandidateCompactHero vs paspoort pattern.</summary>
[Collection("PlaywrightSmoke")]
public sealed class CompactHeadersPreviewR7ScreenshotTests : IAsyncLifetime
{
    private const string ArtifactsDir = "/cursor/stores/self/artifacts/compact-headers-preview-r7";
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
    public async Task Capture_compact_header_preview_r7_variant_c()
    {
        Directory.CreateDirectory(ArtifactsDir);
        var report = new List<object>();
        var pages = new (string Slug, string Server, string Path, bool Onboarding)[]
        {
            ("onboarding-kennismaken", _onboardingWeb!.ServerAddress, "/candidate/start", true),
            ("carriere", _web!.ServerAddress, "/carriere", false),
            ("paspoort", _web.ServerAddress, "/candidate/paspoort", false),
        };

        var (baselineSteppers, baselineNowBlocks, baselineHaveBlocks) =
            await CountCareerOverviewBlocksAsync(_web!.ServerAddress, heroPreviewC: false);

        foreach (var page in pages)
        {
            foreach (var width in new[] { 1440, 1920, 390 })
            {
                var metrics = await CaptureAndAssertAsync(
                    page, width, "nl-NL", "", baselineSteppers, baselineNowBlocks, baselineHaveBlocks);
                report.Add(MetricRow(page.Slug, width, "nl-NL", metrics));
            }

            var enMetrics = await CaptureAndAssertAsync(
                page, 1440, "en-GB", "-en", baselineSteppers, baselineNowBlocks, baselineHaveBlocks);
            report.Add(MetricRow(page.Slug, 1440, "en-GB", enMetrics));
        }

        await File.WriteAllTextAsync(
            Path.Combine(ArtifactsDir, "r7-metrics.json"),
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static object MetricRow(string slug, int width, string locale, CaptureMetrics m) => new
    {
        page = slug,
        width,
        locale,
        m.ArtWidthPercent,
        m.ArtEdgeSeamDelta,
        m.BandUsesPaspoortToken,
        m.CareerArtHeightRatio,
        m.FormTitleDeltaPx,
        m.HeroTextClipped,
        m.HeroBandHeightPx
    };

    private async Task<CaptureMetrics> CaptureAndAssertAsync(
        (string Slug, string Server, string Path, bool Onboarding) page,
        int width,
        string locale,
        string fileSuffix,
        int baselineSteppers,
        int baselineNowBlocks,
        int baselineHaveBlocks)
    {
        var (png, metrics) = await CaptureAsync(page, width, locale, fileSuffix);
        if (width >= 768)
        {
            AssertR7(page.Slug, width, locale, metrics, baselineSteppers, baselineNowBlocks, baselineHaveBlocks);
            if (metrics.ImgLeftX > 0)
            {
                SaveBoundaryCrop(png, metrics.ImgLeftX, metrics.HeroTop, metrics.HeroBottom, page.Slug, width, fileSuffix);
            }
        }

        return metrics;
    }

    private static void AssertR7(
        string slug,
        int width,
        string locale,
        CaptureMetrics metrics,
        int baselineSteppers,
        int baselineNowBlocks,
        int baselineHaveBlocks)
    {
        Assert.True(metrics.BandUsesPaspoortToken,
            $"{slug} c@{width}: band must use paspoort token {CompactHeroPreviewBand.Token}.");
        Assert.False(metrics.HeroTextClipped,
            $"{slug} c@{width} {locale}: hero copy clipped.");
        Assert.True(metrics.HeroBandHeightPx >= 130,
            $"{slug} c@{width}: band height {metrics.HeroBandHeightPx}px (min 130).");

        if (slug is "onboarding-kennismaken" or "carriere")
        {
            Assert.True(metrics.ArtEdgeSeamDelta < 6,
                $"{slug} c@{width}: art-edge seam {metrics.ArtEdgeSeamDelta}.");
            if (slug == "carriere" && width >= 1024)
            {
                var artMax = width >= 1800 ? 82.0 : 78.0;
                Assert.True(metrics.ArtWidthPercent >= 48 && metrics.ArtWidthPercent <= artMax,
                    $"{slug} c@{width}: art column {metrics.ArtWidthPercent:F1}% (rail grid).");
            }
            else
            {
                Assert.True(metrics.ArtWidthPercent >= 42 && metrics.ArtWidthPercent <= 58,
                    $"{slug} c@{width}: art column {metrics.ArtWidthPercent:F1}% (42–58).");
            }
        }

        if (slug == "onboarding-kennismaken")
        {
            Assert.True(metrics.FormTitleDeltaPx <= 2,
                $"onboarding c@{width}: form/title delta {metrics.FormTitleDeltaPx}px.");
        }

        if (slug == "carriere")
        {
            if (width is >= 1400 and <= 1480)
            {
                Assert.True(metrics.HeroBandHeightPx <= 152,
                    $"carriere c@{width}: band height {metrics.HeroBandHeightPx}px (max 152).");
            }

            Assert.True(metrics.CareerArtHeightRatio >= 0.72,
                $"carriere c@{width}: art height ratio {metrics.CareerArtHeightRatio:F2} (min 0.72 of band).");
            Assert.Equal(baselineSteppers, metrics.CareerStepperCount);
            Assert.Equal(baselineNowBlocks, metrics.CareerNowCount);
            Assert.Equal(baselineHaveBlocks, metrics.CareerHaveCount);
        }
    }

    private async Task<(int Steppers, int NowBlocks, int HaveBlocks)> CountCareerOverviewBlocksAsync(
        string server,
        bool heroPreviewC)
    {
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
            Locale = "nl-NL"
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var browserPage = await context.NewPageAsync();
        browserPage.SetDefaultTimeout(120_000);
        var path = heroPreviewC ? "/carriere?heroPreview=c" : "/carriere";
        await browserPage.GotoAsync(
            $"{server}/__test/sign-in?return={Uri.EscapeDataString(path)}",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await browserPage.GotoAsync($"{server}{path}", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await browserPage.Locator(".career-card--overview").WaitForAsync(new() { Timeout = 120_000 });
        await browserPage.WaitForTimeoutAsync(600);
        var steppers = await browserPage.Locator(".career-card--overview .career-stepper").CountAsync();
        var nowBlocks = await browserPage.Locator(".career-card--overview .career-now").CountAsync();
        var haveBlocks = await browserPage.Locator(".career-card--overview .career-have").CountAsync();
        return (steppers, nowBlocks, haveBlocks);
    }

    private async Task<(byte[] Png, CaptureMetrics Metrics)> CaptureAsync(
        (string Slug, string Server, string Path, bool Onboarding) page,
        int width,
        string locale,
        string fileSuffix)
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
        var browserPage = await context.NewPageAsync();
        browserPage.SetDefaultTimeout(120_000);
        if (page.Onboarding)
        {
            IllustratedPagesApiStub.OnboardingWizardStep1Capture = true;
        }

        var pathWithQuery = page.Path + "?heroPreview=c";
        try
        {
            await browserPage.GotoAsync(
                $"{page.Server}/__test/sign-in?return={Uri.EscapeDataString(pathWithQuery)}",
                new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await browserPage.GotoAsync($"{page.Server}{pathWithQuery}", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await browserPage.AddStyleTagAsync(new()
            {
                Content = """
                    .pub-header, .jobsy-chrome__header, .bottom-nav, .candidate-bottom-nav,
                    .lobsy-coach-dock, .pub-coach-dock { position: static !important; }
                    .passport-layout__card--desktop { position: static !important; }
                    .lobsy-assistant-tab, .pub-assistant-rail, .assistant-rail, [data-assistant-rail] { display: none !important; }
                    """
            });

            if (page.Onboarding)
            {
                await browserPage.Locator("[data-testid=onboarding-step-1-illustrated]").WaitForAsync(new() { Timeout = 120_000 });
                await browserPage.Locator(".cand-compact-hero--onboarding").WaitForAsync(new() { Timeout = 120_000 });
            }
            else
            {
                await browserPage.Locator(".cand-compact-hero").First.WaitForAsync(new() { Timeout = 120_000 });
            }

            await browserPage.WaitForTimeoutAsync(900);

            var metrics = new CaptureMetrics();
            if (width >= 768)
            {
                await FillLayoutMetricsAsync(browserPage, page, metrics);
            }

            var file = Path.Combine(ArtifactsDir, $"{page.Slug}-c-{width}{fileSuffix}.png");
            var bytes = await browserPage.ScreenshotAsync(new() { Type = ScreenshotType.Png, FullPage = false });
            await File.WriteAllBytesAsync(file, bytes);

            if (width >= 768)
            {
                var (heroTop, heroBottom) = HeroBandY(page.Slug, bytes, metrics);
                metrics.HeroTop = heroTop;
                metrics.HeroBottom = heroBottom;
                metrics.HeroBandHeightPx = heroBottom - heroTop;
                if (metrics.ImgLeftX > 0 && page.Slug is "onboarding-kennismaken" or "carriere")
                {
                    metrics.ArtEdgeSeamDelta = SeamDeltaAtArtEdge(bytes, metrics.ImgLeftX, heroTop, heroBottom);
                }
            }

            return (bytes, metrics);
        }
        finally
        {
            if (page.Onboarding)
            {
                IllustratedPagesApiStub.OnboardingWizardStep1Capture = false;
            }
        }
    }

    private static async Task FillLayoutMetricsAsync(
        IPage browserPage,
        (string Slug, string Server, string Path, bool Onboarding) page,
        CaptureMetrics metrics)
    {
        var heroSelector = page.Onboarding
            ? ".ob-illustrated-step--compact-preview-c .cand-compact-hero"
            : ".cand-compact-hero";
        var artSelector = $"{heroSelector} .cand-compact-hero__art";
        var imgSelector = $"{heroSelector} .cand-compact-hero__art img";

        var heroBox = await browserPage.Locator(heroSelector).First.BoundingBoxAsync();
        var artBox = await browserPage.Locator(artSelector).First.BoundingBoxAsync();
        var imgBox = await browserPage.Locator(imgSelector).First.BoundingBoxAsync();

        if (artBox is not null && heroBox is not null && heroBox.Width > 0)
        {
            metrics.ArtWidthPercent = artBox.Width / heroBox.Width * 100.0;
        }

        if (heroBox is not null)
        {
            metrics.HeroBandHeightPx = (int)Math.Round(heroBox.Height);
        }

        if (imgBox is not null)
        {
            metrics.ImgLeftX = (int)Math.Round(imgBox.X);
            if (heroBox is not null && heroBox.Height > 0)
            {
                metrics.CareerArtHeightRatio = imgBox.Height / heroBox.Height;
            }
        }

        var token = CompactHeroPreviewBand.Token.ToLowerInvariant();
        metrics.BandUsesPaspoortToken = await browserPage.EvaluateAsync<bool>(
            """
            ({ heroSel, tokenHex }) => {
              const el = document.querySelector(heroSel);
              if (!el) return false;
              const bg = getComputedStyle(el).backgroundColor;
              const probe = document.createElement('div');
              probe.style.backgroundColor = tokenHex;
              document.body.appendChild(probe);
              const expected = getComputedStyle(probe).backgroundColor;
              probe.remove();
              return bg === expected;
            }
            """,
            new { heroSel = heroSelector, tokenHex = token });

        if (page.Slug == "carriere")
        {
            metrics.CareerStepperCount = await browserPage.Locator(".career-card--overview .career-stepper").CountAsync();
            metrics.CareerNowCount = await browserPage.Locator(".career-card--overview .career-now").CountAsync();
            metrics.CareerHaveCount = await browserPage.Locator(".career-card--overview .career-have").CountAsync();
        }

        if (page.Onboarding)
        {
            var titleBox = await browserPage.Locator(".cand-compact-hero--onboarding h1").First.BoundingBoxAsync();
            var formEdge = await browserPage.Locator(
                ".ob-illustrated-step__form input, .ob-illustrated-step__form .ob-wizard__card--illustrated, .ob-illustrated-step__form fieldset"
            ).First.BoundingBoxAsync();
            var formBox = formEdge ?? await browserPage.Locator(".ob-illustrated-step__form").First.BoundingBoxAsync();
            if (titleBox is not null && formBox is not null)
            {
                metrics.FormTitleDeltaPx = (int)Math.Round(Math.Abs(titleBox.X - formBox.X));
            }
        }

        metrics.HeroTextClipped = await browserPage.EvaluateAsync<bool>(
            """
            () => {
              const band = document.querySelector('.cand-compact-hero');
              if (!band) return false;
              const bandRect = band.getBoundingClientRect();
              const nodes = band.querySelectorAll('h1, .cand-hero__lead, .ob-wizard__lead, .cand-hero__eyebrow');
              for (const el of nodes) {
                const style = getComputedStyle(el);
                const ellipses = style.textOverflow === 'ellipsis' || style.overflow === 'hidden';
                if (el.scrollHeight > el.clientHeight + 1) return true;
                if (!ellipses && el.scrollWidth > el.clientWidth + 1) return true;
                const r = el.getBoundingClientRect();
                if (r.width < 120) return true;
                if (r.bottom > bandRect.bottom + 4) return true;
              }
              return false;
            }
            """);
    }

    private static (int Top, int Bottom) HeroBandY(string slug, byte[] pngBytes, CaptureMetrics metrics)
    {
        using var img = Image.Load<Rgb24>(pngBytes);
        var heroTop = slug == "onboarding-kennismaken" ? 105 : 64;
        var heroBottom = metrics.HeroBandHeightPx > 0
            ? Math.Min(heroTop + metrics.HeroBandHeightPx, img.Height - 2)
            : Math.Min(heroTop + 160, img.Height - 2);
        return (heroTop, heroBottom);
    }

    private static int SeamDeltaAtArtEdge(byte[] pngBytes, int imgLeftX, int heroTop, int heroBottom)
    {
        using var img = Image.Load<Rgb24>(pngBytes);
        var h = heroBottom - heroTop;
        var ys = new[] { heroTop + h / 4, heroTop + h / 2, heroTop + 3 * h / 4 };
        var xLeft = Math.Clamp(imgLeftX - 10, 1, img.Width - 2);
        var xRight = Math.Clamp(imgLeftX + 10, 1, img.Width - 2);
        var max = 0;
        foreach (var y in ys)
        {
            var yy = Math.Clamp(y, heroTop + 2, heroBottom - 3);
            max = Math.Max(max, ColorDrift(img[xLeft, yy], img[xRight, yy]));
        }

        return max;
    }

    private static void SaveBoundaryCrop(byte[] pngBytes, int artStartX, int heroTop, int heroBottom, string slug, int width, string suffix)
    {
        using var img = Image.Load<Rgb24>(pngBytes);
        var x0 = Math.Max(0, artStartX - 24);
        var x1 = Math.Min(img.Width, artStartX + 48);
        using var crop = img.Clone(ctx => ctx.Crop(new Rectangle(x0, heroTop, x1 - x0, heroBottom - heroTop)));
        crop.SaveAsPng(Path.Combine(ArtifactsDir, $"{slug}-c-{width}{suffix}-art-left-edge.png"));
    }

    private static int ColorDrift(Rgb24 a, Rgb24 b)
        => Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B);

    private sealed class CaptureMetrics
    {
        public int ImgLeftX { get; set; }
        public double ArtWidthPercent { get; set; }
        public int ArtEdgeSeamDelta { get; set; }
        public bool BandUsesPaspoortToken { get; set; }
        public double CareerArtHeightRatio { get; set; }
        public int HeroTop { get; set; }
        public int HeroBottom { get; set; }
        public int HeroBandHeightPx { get; set; }
        public int FormTitleDeltaPx { get; set; }
        public bool HeroTextClipped { get; set; }
        public int CareerStepperCount { get; set; }
        public int CareerNowCount { get; set; }
        public int CareerHaveCount { get; set; }
    }
}
