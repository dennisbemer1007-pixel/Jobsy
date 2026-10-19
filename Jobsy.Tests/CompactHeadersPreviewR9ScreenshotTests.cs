using System.Text.Json;
using Jobsy.Web.Media;
using Microsoft.Playwright;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Jobsy.Tests;

/// <summary>Variant C r9: carrière lead wrap, prominent art, mobile spacing + art, onboarding top capture.</summary>
[Collection("PlaywrightSmoke")]
public sealed class CompactHeadersPreviewR9ScreenshotTests : IAsyncLifetime
{
    private const string ArtifactsDir = "/cursor/stores/self/artifacts/compact-headers-preview-r9";
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
    public async Task Capture_compact_header_preview_r9_variant_c()
    {
        Directory.CreateDirectory(ArtifactsDir);
        var report = new List<object>();
        var (baselineSteppers, baselineNowBlocks, baselineHaveBlocks) =
            await CountCareerOverviewBlocksAsync(_web!.ServerAddress);

        foreach (var width in new[] { 1440, 1920, 390 })
        {
            var m = await CaptureCareerAsync(width, "nl-NL", "");
            AssertCareer(width, "nl-NL", m, baselineSteppers, baselineNowBlocks, baselineHaveBlocks);
            report.Add(Row("carriere", width, "nl-NL", m));
        }

        var mEn = await CaptureCareerAsync(1440, "en-GB", "-en");
        AssertCareer(1440, "en-GB", mEn, baselineSteppers, baselineNowBlocks, baselineHaveBlocks);
        report.Add(Row("carriere", 1440, "en-GB", mEn));

        foreach (var width in new[] { 1440, 1920, 390 })
        {
            var top = await CaptureOnboardingAsync(width, "nl-NL", "", scrollToFooter: false, fileSuffix: width == 1440 ? "-top" : "");
            if (width >= 768)
            {
                AssertOnboardingTop(width, "nl-NL", top);
            }

            report.Add(Row("onboarding-kennismaken", width, "nl-NL", top, topCapture: width == 1440));

            if (width == 1440)
            {
                var footer = await CaptureOnboardingAsync(width, "nl-NL", "", scrollToFooter: true, fileSuffix: "");
                AssertOnboardingFooter(width, footer);
                report.Add(Row("onboarding-kennismaken", width, "nl-NL", footer, footerCapture: true));
            }
        }

        var obEn = await CaptureOnboardingAsync(1440, "en-GB", "-en", scrollToFooter: false, fileSuffix: "-top");
        AssertOnboardingTop(1440, "en-GB", obEn);
        report.Add(Row("onboarding-kennismaken", 1440, "en-GB", obEn, topCapture: true));

        await File.WriteAllTextAsync(
            Path.Combine(ArtifactsDir, "r9-metrics.json"),
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static object Row(string slug, int width, string locale, CaptureMetrics m, bool topCapture = false, bool footerCapture = false) => new
    {
        page = slug,
        width,
        locale,
        topCapture,
        footerCapture,
        m.HeroBandHeightPx,
        m.ArtWidthPercent,
        m.LeadHasLoslaat,
        m.LeadEllipsis,
        m.ImgLeftEdgeLumaStdDev,
        m.ImgRightEdgeLumaStdDev,
        m.HeroToCardGapPx,
        m.MobileArtVisible,
        m.WorkStatusChipClearancePx,
        m.HeroVisibleOnCapture
    };

    private static void AssertCareer(
        int width,
        string locale,
        CaptureMetrics m,
        int baselineSteppers,
        int baselineNowBlocks,
        int baselineHaveBlocks)
    {
        Assert.True(m.BandUsesPaspoortToken,
            $"carriere c@{width} {locale}: band must use token {CompactHeroPreviewBand.Token}.");
        if (width >= 768)
        {
            Assert.True(m.ImgLeftEdgeLumaStdDev <= ImgEdgeColumnMaxStdDev,
                $"carriere c@{width}: img left edge stddev {m.ImgLeftEdgeLumaStdDev:F1}.");
            Assert.True(m.ImgRightEdgeLumaStdDev <= ImgEdgeColumnMaxStdDev,
                $"carriere c@{width}: img right edge stddev {m.ImgRightEdgeLumaStdDev:F1}.");
            Assert.True(m.ArtWidthPercent >= 38 && m.ArtWidthPercent <= 52,
                $"carriere c@{width}: art {m.ArtWidthPercent:F1}% of band (38–52).");
        }

        if (locale.StartsWith("nl", StringComparison.OrdinalIgnoreCase))
        {
            Assert.True(m.LeadHasLoslaat, $"carriere c@{width}: lead must show full tagline (loslaat).");
        }

        Assert.False(m.LeadEllipsis, $"carriere c@{width}: lead must not use ellipsis truncation.");
        Assert.Equal(baselineSteppers, m.CareerStepperCount);
        Assert.Equal(baselineNowBlocks, m.CareerNowCount);
        Assert.Equal(baselineHaveBlocks, m.CareerHaveCount);

        if (width <= 390)
        {
            Assert.True(m.HeroToCardGapPx >= 16,
                $"carriere c@{width}: gap hero→card {m.HeroToCardGapPx}px (min 16).");
            Assert.True(m.MobileArtVisible, $"carriere c@{width}: mobile hero art must be visible.");
        }

        if (width is >= 1400 and <= 1480)
        {
            Assert.True(m.HeroBandHeightPx <= 188,
                $"carriere c@{width}: band height {m.HeroBandHeightPx}px (max 188 with wrapped lead).");
        }
    }

    private static void AssertOnboardingTop(int width, string locale, CaptureMetrics m)
    {
        Assert.True(m.HeroVisibleOnCapture,
            $"onboarding c@{width} {locale}: compact hero band must be visible on top capture.");
        Assert.True(m.BandUsesPaspoortToken);
    }

    private static void AssertOnboardingFooter(int width, CaptureMetrics m)
    {
        Assert.True(m.WorkStatusChipClearancePx >= 8,
            $"onboarding c@{width}: werkstatus chip clearance {m.WorkStatusChipClearancePx}px.");
    }

    private async Task<CaptureMetrics> CaptureCareerAsync(int width, string locale, string fileSuffix)
    {
        var (bytes, metrics) = await CapturePageAsync(
            _web!.ServerAddress,
            "/carriere",
            width,
            locale,
            $"carriere-c-{width}{fileSuffix}.png",
            onboarding: false,
            scrollToFooter: false);
        if (width >= 768 && metrics.ImgBox is not null)
        {
            (metrics.ImgLeftEdgeLumaStdDev, metrics.ImgRightEdgeLumaStdDev) =
                ImgEdgeColumnLumaStdDevs(bytes, metrics.ImgBox);
        }

        return metrics;
    }

    private async Task<CaptureMetrics> CaptureOnboardingAsync(
        int width,
        string locale,
        string nameSuffix,
        bool scrollToFooter,
        string fileSuffix)
    {
        var file = width == 1440 && scrollToFooter
            ? $"onboarding-kennismaken-c-{width}-footer{nameSuffix}.png"
            : width == 1440 && !string.IsNullOrEmpty(fileSuffix)
                ? $"onboarding-kennismaken-c-{width}{fileSuffix}{nameSuffix}.png"
                : $"onboarding-kennismaken-c-{width}{nameSuffix}.png";
        var (_, metrics) = await CapturePageAsync(
            _onboardingWeb!.ServerAddress,
            "/candidate/start",
            width,
            locale,
            file,
            onboarding: true,
            scrollToFooter: scrollToFooter);
        return metrics;
    }

    private async Task<(byte[] Png, CaptureMetrics Metrics)> CapturePageAsync(
        string server,
        string path,
        int width,
        string locale,
        string fileName,
        bool onboarding,
        bool scrollToFooter)
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
        if (onboarding)
        {
            IllustratedPagesApiStub.OnboardingWizardStep1Capture = true;
        }

        var pathWithQuery = path + "?heroPreview=c";
        try
        {
            await browserPage.GotoAsync(
                $"{server}/__test/sign-in?return={Uri.EscapeDataString(pathWithQuery)}",
                new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await browserPage.GotoAsync($"{server}{pathWithQuery}", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await browserPage.AddStyleTagAsync(new()
            {
                Content = """
                    .pub-header, .jobsy-chrome__header, .bottom-nav, .candidate-bottom-nav,
                    .lobsy-coach-dock, .pub-coach-dock { position: static !important; }
                    .lobsy-assistant-tab, .pub-assistant-rail, .assistant-rail, [data-assistant-rail] { display: none !important; }
                    """
            });

            if (onboarding)
            {
                await browserPage.Locator("[data-testid=onboarding-step-1-illustrated]").WaitForAsync(new() { Timeout = 120_000 });
                if (scrollToFooter)
                {
                    await browserPage.Locator(".work-status-chip").Last.WaitForAsync(new() { Timeout = 120_000 });
                    await browserPage.WaitForTimeoutAsync(600);
                    await browserPage.EvaluateAsync(
                        """
                        () => {
                          const chips = document.querySelectorAll('.work-status-chip');
                          if (chips.length) chips[chips.length - 1].scrollIntoView({ block: 'end' });
                        }
                        """);
                    await browserPage.WaitForTimeoutAsync(400);
                }
                else
                {
                    await browserPage.EvaluateAsync("() => window.scrollTo(0, 0)");
                }
            }
            else
            {
                await browserPage.Locator(".cand-compact-hero--career").WaitForAsync(new() { Timeout = 120_000 });
            }

            await browserPage.WaitForTimeoutAsync(900);
            var metrics = await FillMetricsAsync(browserPage, onboarding, width);
            var bytes = await browserPage.ScreenshotAsync(new() { Type = ScreenshotType.Png, FullPage = false });
            await File.WriteAllBytesAsync(Path.Combine(ArtifactsDir, fileName), bytes);
            return (bytes, metrics);
        }
        finally
        {
            if (onboarding)
            {
                IllustratedPagesApiStub.OnboardingWizardStep1Capture = false;
            }
        }
    }

    private static async Task<CaptureMetrics> FillMetricsAsync(IPage browserPage, bool onboarding, int viewportWidth)
    {
        var metrics = new CaptureMetrics();
        var heroSelector = onboarding
            ? viewportWidth <= 390
                ? ".ob-illustrated-step--compact-preview-c .ob-illustrated-step__mobile-cover"
                : ".ob-illustrated-step--compact-preview-c .cand-compact-hero--onboarding"
            : ".cand-compact-hero--career";
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

        if (onboarding)
        {
            metrics.HeroVisibleOnCapture = await browserPage.EvaluateAsync<bool>(
                """
                () => {
                  const hero = document.querySelector('.cand-compact-hero--onboarding')
                    || document.querySelector('.ob-illustrated-step__mobile-cover');
                  if (!hero) return false;
                  const r = hero.getBoundingClientRect();
                  return r.top >= -4 && r.top < 260 && r.height > 40;
                }
                """);
            metrics.WorkStatusChipClearancePx = await browserPage.EvaluateAsync<int>(
                """
                () => {
                  const chips = document.querySelectorAll('.work-status-chip');
                  const footer = document.querySelector('footer.ob-wizard__footer[data-sticky-footer]');
                  if (!chips.length || !footer) return 999;
                  const chip = chips[chips.length - 1];
                  return Math.round(footer.getBoundingClientRect().top - chip.getBoundingClientRect().bottom);
                }
                """);
            return metrics;
        }

        var heroBox = await browserPage.Locator(heroSelector).First.BoundingBoxAsync();
        var artBox = await browserPage.Locator($"{heroSelector} .cand-compact-hero__art").First.BoundingBoxAsync();
        var imgBox = await browserPage.Locator($"{heroSelector} .cand-compact-hero__art img").First.BoundingBoxAsync();
        if (heroBox is not null)
        {
            metrics.HeroBandHeightPx = (int)Math.Round(heroBox.Height);
        }

        if (imgBox is not null)
        {
            metrics.ImgBox = imgBox;
            if (viewportWidth <= 390)
            {
                metrics.MobileArtVisible = imgBox.Width > 24 && imgBox.Height > 24;
            }
        }

        var leadInfo = await browserPage.EvaluateAsync<LeadInfo>(
            """
            () => {
              const lead = document.querySelector('.cand-compact-hero--career .cand-hero__lead');
              if (!lead) return null;
              const style = getComputedStyle(lead);
              return {
                text: lead.textContent || '',
                ellipsis: style.textOverflow === 'ellipsis',
                nowrap: style.whiteSpace === 'nowrap'
              };
            }
            """);
        if (leadInfo is not null)
        {
            metrics.LeadHasLoslaat = leadInfo.Text.Contains("loslaat", StringComparison.OrdinalIgnoreCase);
            metrics.LeadEllipsis = leadInfo.Ellipsis || leadInfo.Nowrap;
        }

        if (heroBox is not null && artBox is not null)
        {
            var artPct = await browserPage.EvaluateAsync<double>(
                """
                () => {
                  const hero = document.querySelector('.cand-compact-hero--career');
                  const art = hero?.querySelector('.cand-compact-hero__art');
                  if (!hero || !art) return 0;
                  const hr = hero.getBoundingClientRect();
                  const ar = art.getBoundingClientRect();
                  return hr.width > 0 ? (ar.width / hr.width) * 100 : 0;
                }
                """);
            metrics.ArtWidthPercent = artPct;
        }

        metrics.HeroToCardGapPx = await browserPage.EvaluateAsync<int>(
            """
            () => {
              const hero = document.querySelector('.cand-compact-hero--career');
              const card = document.querySelector('.career-card--overview');
              if (!hero || !card) return 999;
              return Math.round(card.getBoundingClientRect().top - hero.getBoundingClientRect().bottom);
            }
            """);

        metrics.CareerStepperCount = await browserPage.Locator(".career-card--overview .career-stepper").CountAsync();
        metrics.CareerNowCount = await browserPage.Locator(".career-card--overview .career-now").CountAsync();
        metrics.CareerHaveCount = await browserPage.Locator(".career-card--overview .career-have").CountAsync();
        return metrics;
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
        var steppers = await browserPage.Locator(".career-card--overview .career-stepper").CountAsync();
        var nowBlocks = await browserPage.Locator(".career-card--overview .career-now").CountAsync();
        var haveBlocks = await browserPage.Locator(".career-card--overview .career-have").CountAsync();
        return (steppers, nowBlocks, haveBlocks);
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

        return (ColumnLumaStdDev(img, ix, iy, cols, ih), ColumnLumaStdDev(img, ix + iw - cols, iy, cols, ih));
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
        return Math.Sqrt(values.Select(v => (v - mean) * (v - mean)).Average());
    }

    private sealed class CaptureMetrics
    {
        public LocatorBoundingBoxResult? ImgBox { get; set; }
        public int HeroBandHeightPx { get; set; }
        public double ArtWidthPercent { get; set; }
        public bool LeadHasLoslaat { get; set; }
        public bool LeadEllipsis { get; set; }
        public double ImgLeftEdgeLumaStdDev { get; set; }
        public double ImgRightEdgeLumaStdDev { get; set; }
        public int HeroToCardGapPx { get; set; }
        public bool MobileArtVisible { get; set; }
        public int WorkStatusChipClearancePx { get; set; }
        public bool HeroVisibleOnCapture { get; set; }
        public bool BandUsesPaspoortToken { get; set; }
        public int CareerStepperCount { get; set; }
        public int CareerNowCount { get; set; }
        public int CareerHaveCount { get; set; }
    }

    private sealed class LeadInfo
    {
        public string Text { get; set; } = "";
        public bool Ellipsis { get; set; }
        public bool Nowrap { get; set; }
    }
}
