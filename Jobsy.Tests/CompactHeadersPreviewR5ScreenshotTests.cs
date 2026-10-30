using System.Text.Json;
using Jobsy.Web.Media;
using Microsoft.Playwright;

using Jobsy.Tests.Testing;

namespace Jobsy.Tests;

/// <summary>Variant C r5: layout regression gates + captures (preview only).</summary>
[Collection("PlaywrightSmoke")]
public sealed class CompactHeadersPreviewR5ScreenshotTests : IAsyncLifetime
{
    private const string ArtifactsDir = "/cursor/stores/self/artifacts/compact-headers-preview-r5";
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
    public async Task Capture_compact_header_preview_r5_variant_c()
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
                var locale = "nl-NL";
                var suffix = "";
                var metrics = await CaptureAndAssertAsync(
                    page, width, locale, suffix, baselineSteppers, baselineNowBlocks, baselineHaveBlocks);
                report.Add(new
                {
                    page = page.Slug,
                    width,
                    locale,
                    metrics.ArtWidthPercent,
                    metrics.MaxSeamDelta,
                    metrics.FormTitleDeltaPx,
                    metrics.HeroTextClipped,
                    metrics.CareerStepperCount,
                    metrics.CareerNowCount
                });
            }

            var enMetrics = await CaptureAndAssertAsync(
                page, 1440, "en-GB", "-en", baselineSteppers, baselineNowBlocks, baselineHaveBlocks);
            report.Add(new
            {
                page = page.Slug,
                width = 1440,
                locale = "en-GB",
                enMetrics.ArtWidthPercent,
                enMetrics.MaxSeamDelta,
                enMetrics.FormTitleDeltaPx,
                enMetrics.HeroTextClipped,
                enMetrics.CareerStepperCount,
                enMetrics.CareerNowCount
            });
        }

        await File.WriteAllTextAsync(
            Path.Combine(ArtifactsDir, "r5-metrics.json"),
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    }

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
            AssertR5(page.Slug, width, locale, metrics, baselineSteppers, baselineNowBlocks, baselineHaveBlocks);
            if (metrics.ArtStartX > 0)
            {
                SaveBoundaryCrop(png, metrics.ArtStartX, metrics.HeroTop, metrics.HeroBottom, page.Slug, width, fileSuffix);
            }
        }

        return metrics;
    }

    private static void AssertR5(
        string slug,
        int width,
        string locale,
        CaptureMetrics metrics,
        int baselineSteppers,
        int baselineNowBlocks,
        int baselineHaveBlocks)
    {
        Assert.True(metrics.MaxSeamDelta < 6,
            $"{slug} c@{width} {locale}: seam column delta {metrics.MaxSeamDelta} (max 6).");
        Assert.False(metrics.HeroTextClipped,
            $"{slug} c@{width} {locale}: hero copy clipped inside band.");
        Assert.False(metrics.CopyIntersectsArt,
            $"{slug} c@{width} {locale}: hero copy bbox intersects art bbox.");

        if (slug == "onboarding-kennismaken")
        {
            Assert.True(metrics.FormTitleDeltaPx <= 2,
                $"{slug} c@{width}: form vs title left delta {metrics.FormTitleDeltaPx}px (max 2).");
        }

        if (slug == "carriere")
        {
            var artMin = width >= 1800 ? 44.0 : 42.0;
            var artMax = width >= 1800 ? 54.0 : 52.0;
            Assert.True(metrics.ArtWidthPercent >= artMin && metrics.ArtWidthPercent <= artMax,
                $"{slug} c@{width}: art {metrics.ArtWidthPercent:F1}% of band (expected {artMin}-{artMax}%).");
            Assert.True(metrics.CareerCardVisibleH1Count == 0,
                $"carriere c@{width}: duplicate visible h1 in card.");
            Assert.Equal(baselineSteppers, metrics.CareerStepperCount);
            Assert.Equal(baselineNowBlocks, metrics.CareerNowCount);
            Assert.Equal(baselineHaveBlocks, metrics.CareerHaveCount);
        }

        if (slug == "paspoort")
        {
            Assert.True(metrics.HeroBandHeightPx >= 130,
                $"paspoort c@{width}: band height {metrics.HeroBandHeightPx}px (min 130).");
            Assert.True(metrics.ArtWidthPercent >= 40 && metrics.ArtWidthPercent <= 58,
                $"paspoort c@{width}: art {metrics.ArtWidthPercent:F1}% of band.");
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
            }
            else
            {
                await browserPage.Locator(".cand-hero").First.WaitForAsync(new() { Timeout = 120_000 });
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
                var (heroTop, heroBottom) = HeroBandY(page.Slug, bytes);
                metrics.HeroTop = heroTop;
                metrics.HeroBottom = heroBottom;
                metrics.HeroBandHeightPx = heroBottom - heroTop;
                var artStart = metrics.ArtStartX > 0 ? metrics.ArtStartX : 680;
                metrics.MaxSeamDelta = MaxColumnDeltaBeforeArtStart(bytes, heroTop, heroBottom, artStart, 40);
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
        var artSelector = page.Onboarding ? ".ob-illustrated-step__art" : ".cand-hero__compact-c-art";
        var copySelector = page.Onboarding ? ".ob-illustrated-step__head" : ".cand-hero__copy";
        var bandSelector = page.Onboarding ? ".ob-illustrated-step__hero-band" : ".cand-hero";

        var artBox = await browserPage.Locator(artSelector).First.BoundingBoxAsync();
        var copyBox = await browserPage.Locator(copySelector).First.BoundingBoxAsync();
        var bandBox = await browserPage.Locator(bandSelector).First.BoundingBoxAsync();
        if (artBox is not null && bandBox is not null && bandBox.Width > 0)
        {
            metrics.ArtStartX = (int)Math.Round(artBox.X);
            metrics.ArtWidthPercent = artBox.Width / bandBox.Width * 100.0;
            metrics.BandLeftX = (int)Math.Round(bandBox.X);
            metrics.BandRightX = (int)Math.Round(bandBox.X + bandBox.Width);
            var artRight = artBox.X + artBox.Width;
            metrics.SkipRightBandEdge = artRight >= bandBox.X + bandBox.Width * 0.62;
        }

        if (copyBox is not null && artBox is not null)
        {
            metrics.CopyIntersectsArt = BoxesIntersect(copyBox, artBox);
        }

        if (page.Slug == "carriere")
        {
            metrics.CareerCardVisibleH1Count = await browserPage.Locator(".career-card--overview h1:not(.visually-hidden)").CountAsync();
            metrics.CareerStepperCount = await browserPage.Locator(".career-card--overview .career-stepper").CountAsync();
            metrics.CareerNowCount = await browserPage.Locator(".career-card--overview .career-now").CountAsync();
            metrics.CareerHaveCount = await browserPage.Locator(".career-card--overview .career-have").CountAsync();
        }

        if (page.Onboarding)
        {
            var titleBox = await browserPage.Locator(".ob-illustrated-step__head h1").First.BoundingBoxAsync();
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
              const band = document.querySelector('.ob-illustrated-step__hero-band, .cand-hero--compact-preview-c');
              if (!band) return false;
              const bandRect = band.getBoundingClientRect();
              const nodes = band.querySelectorAll('h1, .cand-hero__lead, .ob-wizard__lead, .cand-hero__eyebrow');
              for (const el of nodes) {
                if (el.scrollHeight > el.clientHeight + 1 || el.scrollWidth > el.clientWidth + 1) return true;
                const r = el.getBoundingClientRect();
                if (r.width < 120) return true;
                if (r.bottom > bandRect.bottom + 4) return true;
              }
              return false;
            }
            """);
    }

    private static bool BoxesIntersect(LocatorBoundingBoxResult a, LocatorBoundingBoxResult b)
    {
        var ax2 = a.X + a.Width;
        var ay2 = a.Y + a.Height;
        var bx2 = b.X + b.Width;
        var by2 = b.Y + b.Height;
        return a.X < bx2 && ax2 > b.X && a.Y < by2 && ay2 > b.Y;
    }

    private static (int Top, int Bottom) HeroBandY(string slug, byte[] pngBytes)
    {
        using var img = SkiaTestImage.LoadRgb(pngBytes);
        var heroTop = slug == "onboarding-kennismaken" ? 105 : 64;
        var heroBottom = slug == "paspoort"
            ? Math.Min(heroTop + 160, img.Height - 2)
            : Math.Min(heroTop + 132, img.Height - 2);
        return (heroTop, heroBottom);
    }

    private static int MaxColumnDeltaBeforeArtStart(byte[] pngBytes, int heroTop, int heroBottom, int artStartX, int windowPx)
    {
        using var img = SkiaTestImage.LoadRgb(pngBytes);
        var x0 = Math.Max(1, artStartX - windowPx);
        var x1 = Math.Min(img.Width - 2, artStartX);
        var y0 = heroTop + 8;
        var y1 = Math.Min(heroBottom - 4, heroTop + 22);
        var max = 0;
        for (var x = x0; x < x1; x++)
        {
            var left = ColumnAverage(img, x, y0, y1);
            var right = ColumnAverage(img, x + 1, y0, y1);
            max = Math.Max(max, ColorDrift(left, right));
        }

        return max;
    }

    private static TestRgb ColumnAverage(SkiaTestImage img, int x, int y0, int y1)
    {
        long r = 0, g = 0, b = 0;
        var n = 0;
        for (var y = y0; y < y1; y++)
        {
            var p = img.GetRgb(x, y);
            r += p.R;
            g += p.G;
            b += p.B;
            n++;
        }

        return n == 0 ? new TestRgb(0, 0, 0) : new TestRgb((byte)(r / n), (byte)(g / n), (byte)(b / n));
    }

    private static void SaveBoundaryCrop(byte[] pngBytes, int artStartX, int heroTop, int heroBottom, string slug, int width, string suffix)
    {
        using var img = SkiaTestImage.LoadRgb(pngBytes);
        var x0 = Math.Max(0, artStartX - 24);
        var x1 = Math.Min(img.Width, artStartX + 48);
        using var src = img.ToBitmap();
        using var crop = SkiaTestImage.Crop(src, x0, heroTop, x1 - x0, heroBottom - heroTop);
        SkiaTestImage.SavePng(crop, Path.Combine(ArtifactsDir, $"{slug}-c-{width}{suffix}-art-left-edge.png"));
    }

    private static int ColorDrift(TestRgb a, TestRgb b)
        => Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B);

    private sealed class CaptureMetrics
    {
        public int ArtStartX { get; set; }
        public double ArtWidthPercent { get; set; }
        public int MaxSeamDelta { get; set; }
        public bool CopyIntersectsArt { get; set; }
        public int CareerCardVisibleH1Count { get; set; }
        public int CareerStepperCount { get; set; }
        public int CareerNowCount { get; set; }
        public int CareerHaveCount { get; set; }
        public int HeroTop { get; set; }
        public int HeroBottom { get; set; }
        public int HeroBandHeightPx { get; set; }
        public int BandLeftX { get; set; }
        public int BandRightX { get; set; }
        public bool SkipRightBandEdge { get; set; }
        public int FormTitleDeltaPx { get; set; }
        public bool HeroTextClipped { get; set; }
    }
}
