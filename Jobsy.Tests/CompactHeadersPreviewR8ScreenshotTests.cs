using System.Text.Json;
using Jobsy.Web.Media;
using Microsoft.Playwright;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Jobsy.Tests;

/// <summary>Variant C r8: career compact band, rail alignment, img edge luma, onboarding sticky padding.</summary>
[Collection("PlaywrightSmoke")]
public sealed class CompactHeadersPreviewR8ScreenshotTests : IAsyncLifetime
{
    private const string ArtifactsDir = "/cursor/stores/self/artifacts/compact-headers-preview-r8";
    private const double ImgEdgeColumnMaxStdDev = 22.0;
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
    public async Task Capture_compact_header_preview_r8_variant_c()
    {
        Directory.CreateDirectory(ArtifactsDir);
        var report = new List<object>();
        var pages = new (string Slug, string Server, string Path, bool Onboarding)[]
        {
            ("onboarding-kennismaken", _onboardingWeb!.ServerAddress, "/candidate/start", true),
            ("carriere", _web!.ServerAddress, "/carriere", false),
        };

        var (baselineSteppers, baselineNowBlocks, baselineHaveBlocks) =
            await CountCareerOverviewBlocksAsync(_web!.ServerAddress);

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
            Path.Combine(ArtifactsDir, "r8-metrics.json"),
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static object MetricRow(string slug, int width, string locale, CaptureMetrics m) => new
    {
        page = slug,
        width,
        locale,
        m.HeroBandHeightPx,
        m.HeroRailAlignDeltaPx,
        m.ImgLeftEdgeLumaStdDev,
        m.ImgRightEdgeLumaStdDev,
        m.WorkStatusChipClearancePx,
        m.FormTitleDeltaPx,
        m.BandUsesPaspoortToken,
        m.ArtWidthPercent
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
        var (_, metrics) = await CaptureAsync(page, width, locale, fileSuffix);
        if (width >= 768)
        {
            AssertR8(page.Slug, width, locale, metrics, baselineSteppers, baselineNowBlocks, baselineHaveBlocks);
        }

        return metrics;
    }

    private static void AssertR8(
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

        Assert.True(metrics.ImgLeftEdgeLumaStdDev <= ImgEdgeColumnMaxStdDev,
            $"{slug} c@{width}: img left 6px luma stddev {metrics.ImgLeftEdgeLumaStdDev:F1} (max {ImgEdgeColumnMaxStdDev}).");
        if (slug == "carriere")
        {
            Assert.True(metrics.ImgRightEdgeLumaStdDev <= ImgEdgeColumnMaxStdDev,
                $"{slug} c@{width}: img right 6px luma stddev {metrics.ImgRightEdgeLumaStdDev:F1} (max {ImgEdgeColumnMaxStdDev}).");
        }

        if (slug == "carriere")
        {
            if (width is >= 1400 and <= 1480)
            {
                Assert.True(metrics.HeroBandHeightPx <= 152,
                    $"carriere c@{width}: band height {metrics.HeroBandHeightPx}px (max 152 at 1440).");
            }

            Assert.True(metrics.HeroRailAlignDeltaPx <= 4,
                $"carriere c@{width}: hero vs rail left delta {metrics.HeroRailAlignDeltaPx}px (max 4).");
            Assert.Equal(baselineSteppers, metrics.CareerStepperCount);
            Assert.Equal(baselineNowBlocks, metrics.CareerNowCount);
            Assert.Equal(baselineHaveBlocks, metrics.CareerHaveCount);
            if (width >= 1024)
            {
                var artMax = width >= 1800 ? 82.0 : 78.0;
                Assert.True(metrics.ArtWidthPercent >= 48 && metrics.ArtWidthPercent <= artMax,
                    $"carriere c@{width}: art column {metrics.ArtWidthPercent:F1}% of band (rail grid, max {artMax}).");
            }
            else
            {
                Assert.True(metrics.ArtWidthPercent >= 38 && metrics.ArtWidthPercent <= 58,
                    $"carriere c@{width}: art {metrics.ArtWidthPercent:F1}% of band.");
            }
        }

        if (slug == "onboarding-kennismaken")
        {
            Assert.True(metrics.FormTitleDeltaPx <= 2,
                $"onboarding c@{width}: form/title delta {metrics.FormTitleDeltaPx}px.");
            if (width is >= 1400 and <= 1480)
            {
                Assert.True(metrics.WorkStatusChipClearancePx >= 8,
                    $"onboarding c@{width}: last werkstatus chip clearance above footer {metrics.WorkStatusChipClearancePx}px.");
            }
        }
    }

    private async Task<(int Steppers, int NowBlocks, int HaveBlocks)> CountCareerOverviewBlocksAsync(string server)
    {
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
            Locale = "nl-NL"
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var browserPage = await context.NewPageAsync();
        browserPage.SetDefaultTimeout(120_000);
        await browserPage.GotoAsync(
            $"{server}/__test/sign-in?return={Uri.EscapeDataString("/carriere")}",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await browserPage.GotoAsync($"{server}/carriere", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
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
                    .lobsy-assistant-tab, .pub-assistant-rail, .assistant-rail, [data-assistant-rail] { display: none !important; }
                    """
            });

            if (page.Onboarding)
            {
                await browserPage.Locator("[data-testid=onboarding-step-1-illustrated]").WaitForAsync(new() { Timeout = 120_000 });
                await browserPage.Locator(".cand-compact-hero--onboarding").WaitForAsync(new() { Timeout = 120_000 });
                await browserPage.Locator(".work-status-chip").Last.WaitForAsync(new() { Timeout = 120_000 });
                await browserPage.WaitForTimeoutAsync(600);
                await browserPage.EvaluateAsync(
                    """
                    () => {
                      const chips = document.querySelectorAll('.work-status-chip');
                      if (chips.length) {
                        chips[chips.length - 1].scrollIntoView({ block: 'end', behavior: 'instant' });
                      }
                    }
                    """);
                await browserPage.WaitForTimeoutAsync(400);
            }
            else
            {
                await browserPage.Locator(".cand-compact-hero--career").WaitForAsync(new() { Timeout = 120_000 });
            }

            await browserPage.WaitForTimeoutAsync(900);

            var metrics = new CaptureMetrics();
            if (width >= 768)
            {
                await FillLayoutMetricsAsync(browserPage, page, width, metrics);
            }

            var file = Path.Combine(ArtifactsDir, $"{page.Slug}-c-{width}{fileSuffix}.png");
            var bytes = await browserPage.ScreenshotAsync(new() { Type = ScreenshotType.Png, FullPage = false });
            await File.WriteAllBytesAsync(file, bytes);

            if (width >= 768 && metrics.ImgBox is not null)
            {
                var (leftStd, rightStd) = ImgEdgeColumnLumaStdDevs(bytes, metrics.ImgBox);
                metrics.ImgLeftEdgeLumaStdDev = leftStd;
                metrics.ImgRightEdgeLumaStdDev = rightStd;
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
        int viewportWidth,
        CaptureMetrics metrics)
    {
        var heroSelector = page.Onboarding
            ? ".ob-illustrated-step--compact-preview-c .cand-compact-hero"
            : ".cand-compact-hero--career";
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
            metrics.ImgBox = imgBox;
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

        if (page.Slug == "carriere" && viewportWidth >= 1024)
        {
            var heroTitleBox = await browserPage.Locator($"{heroSelector} h1").First.BoundingBoxAsync();
            var railTitleBox = await browserPage.Locator("#career-rail-title").First.BoundingBoxAsync();
            if (heroTitleBox is not null && railTitleBox is not null)
            {
                metrics.HeroRailAlignDeltaPx = (int)Math.Round(Math.Abs(heroTitleBox.X - railTitleBox.X));
            }

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

            var clearance = await browserPage.EvaluateAsync<double>(
                """
                () => {
                  const chips = document.querySelectorAll('.work-status-chip');
                  const footer = document.querySelector('footer.ob-wizard__footer[data-sticky-footer]');
                  if (!chips.length || !footer) return 999;
                  const chip = chips[chips.length - 1];
                  const cr = chip.getBoundingClientRect();
                  const fr = footer.getBoundingClientRect();
                  return fr.top - cr.bottom;
                }
                """);
            metrics.WorkStatusChipClearancePx = (int)Math.Round(clearance);
        }

        metrics.HeroTextClipped = await browserPage.EvaluateAsync<bool>(
            """
            () => {
              const band = document.querySelector('.cand-compact-hero--career, .cand-compact-hero--onboarding');
              if (!band) return false;
              const bandRect = band.getBoundingClientRect();
              const nodes = band.querySelectorAll('h1, .cand-hero__lead, .ob-wizard__lead, .cand-hero__eyebrow');
              for (const el of nodes) {
                const style = getComputedStyle(el);
                const ellipses = style.textOverflow === 'ellipsis' || style.overflow === 'hidden';
                if (el.scrollHeight > el.clientHeight + 1) return true;
                if (!ellipses && el.scrollWidth > el.clientWidth + 1) return true;
                const r = el.getBoundingClientRect();
                if (r.width < 80) return true;
                if (r.bottom > bandRect.bottom + 4) return true;
              }
              return false;
            }
            """);
    }

    private static (double Left, double Right) ImgEdgeColumnLumaStdDevs(byte[] pngBytes, LocatorBoundingBoxResult imgBox)
    {
        using var img = Image.Load<Rgb24>(pngBytes);
        var ix = Math.Clamp((int)Math.Floor(imgBox.X), 0, img.Width - 1);
        var iy = Math.Clamp((int)Math.Floor(imgBox.Y), 0, img.Height - 1);
        var iw = Math.Clamp((int)Math.Ceiling(imgBox.Width), 1, img.Width - ix);
        var ih = Math.Clamp((int)Math.Ceiling(imgBox.Height), 1, img.Height - iy);
        var cols = Math.Min(6, iw / 2);
        if (cols < 1)
        {
            return (0, 0);
        }

        var left = ColumnLumaStdDev(img, ix, iy, cols, ih);
        var right = ColumnLumaStdDev(img, ix + iw - cols, iy, cols, ih);
        return (left, right);
    }

    private static double ColumnLumaStdDev(Image<Rgb24> img, int x0, int y0, int colCount, int rowCount)
    {
        var values = new List<double>(colCount * rowCount);
        for (var x = x0; x < x0 + colCount && x < img.Width; x++)
        {
            for (var y = y0; y < y0 + rowCount && y < img.Height; y++)
            {
                var p = img[x, y];
                values.Add(0.299 * p.R + 0.587 * p.G + 0.114 * p.B);
            }
        }

        if (values.Count == 0)
        {
            return 0;
        }

        var mean = values.Average();
        var variance = values.Select(v => (v - mean) * (v - mean)).Average();
        return Math.Sqrt(variance);
    }

    private sealed class CaptureMetrics
    {
        public LocatorBoundingBoxResult? ImgBox { get; set; }
        public double ArtWidthPercent { get; set; }
        public int HeroBandHeightPx { get; set; }
        public int HeroRailAlignDeltaPx { get; set; }
        public double ImgLeftEdgeLumaStdDev { get; set; }
        public double ImgRightEdgeLumaStdDev { get; set; }
        public int WorkStatusChipClearancePx { get; set; }
        public int FormTitleDeltaPx { get; set; }
        public bool BandUsesPaspoortToken { get; set; }
        public bool HeroTextClipped { get; set; }
        public int CareerStepperCount { get; set; }
        public int CareerNowCount { get; set; }
        public int CareerHaveCount { get; set; }
    }
}
