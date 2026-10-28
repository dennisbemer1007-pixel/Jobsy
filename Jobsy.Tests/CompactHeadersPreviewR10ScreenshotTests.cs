using System.Text.Json;
using Jobsy.Web.Media;
using Microsoft.Playwright;

using Jobsy.Tests.Testing;

namespace Jobsy.Tests;

/// <summary>Variant C r10: Carrière uses onboarding compact hero + band art; 5-point band colour match.</summary>
[Collection("PlaywrightSmoke")]
public sealed class CompactHeadersPreviewR10ScreenshotTests : IAsyncLifetime
{
    private const string ArtifactsDir = "/cursor/stores/self/artifacts/compact-headers-preview-r10";
    private static readonly (int R, int G, int B) BandRgb = (250, 235, 219);

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
    public async Task Capture_compact_header_preview_r10_variant_c()
    {
        Directory.CreateDirectory(ArtifactsDir);
        var report = new List<object>();

        foreach (var width in new[] { 1440, 1920, 390 })
        {
            var (bytes, m) = await CaptureCareerAsync(width, "nl-NL", "");
            AssertCareer(width, "nl-NL", bytes, m);
            report.Add(Row("carriere", width, "nl-NL", m));
        }

        var (bytesEn, mEn) = await CaptureCareerAsync(1440, "en-GB", "-en");
        AssertCareer(1440, "en-GB", bytesEn, mEn);
        report.Add(Row("carriere", 1440, "en-GB", mEn));

        var (_, ob390) = await CaptureOnboardingAsync(390, "nl-NL", "", fileSuffix: "-top");
        AssertOnboardingTop(390, "nl-NL", ob390);
        report.Add(Row("onboarding-kennismaken", 390, "nl-NL", ob390, topCapture: true));

        await File.WriteAllTextAsync(
            Path.Combine(ArtifactsDir, "r10-metrics.json"),
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static object Row(string slug, int width, string locale, CaptureMetrics m, bool topCapture = false) => new
    {
        page = slug,
        width,
        locale,
        topCapture,
        m.HeroBandHeightPx,
        m.BandUsesPaspoortToken,
        bandSamples = m.BandSamplePoints.Select(p => new { p.Name, hex = $"#{p.R:X2}{p.G:X2}{p.B:X2}" }).ToList(),
        m.LeadHasLoslaat,
        m.LeadEllipsis,
        m.HeroVisibleOnCapture
    };

    private static void AssertCareer(int width, string locale, byte[] pngBytes, CaptureMetrics m)
    {
        Assert.True(m.BandUsesPaspoortToken,
            $"carriere c@{width} {locale}: band must use token {CompactHeroPreviewBand.Token}.");
        Assert.False(m.UsesLegacyCareerLayout,
            $"carriere c@{width}: must use cand-compact-hero--onboarding, not --career.");
        AssertBandSamples(m, $"carriere c@{width} {locale}");

        if (locale.StartsWith("nl", StringComparison.OrdinalIgnoreCase))
        {
            Assert.True(m.LeadHasLoslaat, $"carriere c@{width}: lead must show full tagline (loslaat).");
        }

        Assert.False(m.LeadEllipsis, $"carriere c@{width}: lead must not use ellipsis truncation.");

        if (width <= 390)
        {
            Assert.True(m.HeroBandHeightPx <= 172,
                $"carriere c@{width}: band height {m.HeroBandHeightPx}px (max ~170).");
        }

        if (width is >= 1400 and <= 1480)
        {
            Assert.True(m.HeroBandHeightPx <= 240,
                $"carriere c@{width}: band height {m.HeroBandHeightPx}px (onboarding layout, wrapped lead).");
        }

        _ = pngBytes;
    }

    private static void AssertOnboardingTop(int width, string locale, CaptureMetrics m)
    {
        Assert.True(m.HeroVisibleOnCapture,
            $"onboarding c@{width} {locale}: mobile cover must be visible on top capture.");
    }

    private static void AssertBandSamples(CaptureMetrics m, string label)
    {
        Assert.Equal(5, m.BandSamplePoints.Count);
        foreach (var pt in m.BandSamplePoints)
        {
            var dr = Math.Abs(pt.R - BandRgb.R);
            var dg = Math.Abs(pt.G - BandRgb.G);
            var db = Math.Abs(pt.B - BandRgb.B);
            Assert.True(dr <= 2 && dg <= 2 && db <= 2,
                $"{label}: sample {pt.Name} #{pt.R:X2}{pt.G:X2}{pt.B:X2} vs #FAEBDB (Δ {dr},{dg},{db}).");
        }
    }

    private async Task<(byte[] Png, CaptureMetrics Metrics)> CaptureCareerAsync(int width, string locale, string fileSuffix)
    {
        var fileName = $"carriere-c-{width}{fileSuffix}.png";
        return await CapturePageAsync(
            _web!.ServerAddress,
            "/carriere",
            width,
            locale,
            fileName,
            onboarding: false);
    }

    private async Task<(byte[] Png, CaptureMetrics Metrics)> CaptureOnboardingAsync(
        int width,
        string locale,
        string nameSuffix,
        string fileSuffix)
    {
        var file = $"onboarding-kennismaken-c-{width}{fileSuffix}{nameSuffix}.png";
        return await CapturePageAsync(
            _onboardingWeb!.ServerAddress,
            "/candidate/start",
            width,
            locale,
            file,
            onboarding: true);
    }

    private async Task<(byte[] Png, CaptureMetrics Metrics)> CapturePageAsync(
        string server,
        string path,
        int width,
        string locale,
        string fileName,
        bool onboarding)
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
                await browserPage.EvaluateAsync("() => window.scrollTo(0, 0)");
            }
            else
            {
                await browserPage.Locator(".career-page--compact-preview-c .cand-compact-hero--onboarding").WaitForAsync(new() { Timeout = 120_000 });
            }

            await browserPage.WaitForTimeoutAsync(900);
            var bytes = await browserPage.ScreenshotAsync(new() { Type = ScreenshotType.Png, FullPage = false });
            await File.WriteAllBytesAsync(Path.Combine(ArtifactsDir, fileName), bytes);
            var metrics = await FillMetricsAsync(browserPage, bytes, onboarding);
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

    private static async Task<CaptureMetrics> FillMetricsAsync(IPage browserPage, byte[] pngBytes, bool onboarding)
    {
        var metrics = new CaptureMetrics();
        var heroSelector = onboarding
            ? ".ob-illustrated-step--compact-preview-c .ob-illustrated-step__mobile-cover"
            : ".career-page--compact-preview-c .cand-compact-hero--onboarding";
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
                  const hero = document.querySelector('.ob-illustrated-step__mobile-cover');
                  if (!hero) return false;
                  const r = hero.getBoundingClientRect();
                  return r.top >= -4 && r.top < 420 && r.height > 40;
                }
                """);
            return metrics;
        }

        metrics.UsesLegacyCareerLayout = await browserPage.Locator(".cand-compact-hero--career").CountAsync() > 0;

        var heroBox = await browserPage.Locator(heroSelector).First.BoundingBoxAsync();
        var artBox = await browserPage.Locator($"{heroSelector} .cand-compact-hero__art").First.BoundingBoxAsync();
        if (heroBox is not null)
        {
            metrics.HeroBandHeightPx = (int)Math.Round(heroBox.Height);
        }

        metrics.BandSamplePoints = SampleBandPoints(pngBytes, heroBox, artBox);

        var leadInfo = await browserPage.EvaluateAsync<LeadInfo>(
            """
            () => {
              const lead = document.querySelector('.career-page--compact-preview-c .cand-hero__lead');
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

        return metrics;
    }

    private static List<BandSample> SampleBandPoints(byte[] pngBytes, LocatorBoundingBoxResult? heroBox, LocatorBoundingBoxResult? artBox)
    {
        var points = new List<BandSample>();
        if (heroBox is null)
        {
            return points;
        }

        using var img = SkiaTestImage.LoadRgb(pngBytes);
        var hx = (int)Math.Round(heroBox.X);
        var hy = (int)Math.Round(heroBox.Y);
        var hw = (int)Math.Round(heroBox.Width);
        var hh = (int)Math.Round(heroBox.Height);

        if (artBox is null)
        {
            return points;
        }

        if (hw <= 420)
        {
            var samplesMobile = new (string Name, int X, int Y)[]
            {
                ("band_left", hx + 16, hy + hh / 2),
                ("band_top", hx + (int)(hw * 0.28), hy + 10),
                ("band_bottom", hx + (int)(hw * 0.28), hy + hh - 8),
                ("band_mid", hx + (int)(hw * 0.22), hy + hh / 2),
                ("band_upper", hx + (int)(hw * 0.32), hy + (int)(hh * 0.28))
            };
            foreach (var (name, x, y) in samplesMobile)
            {
                var px = Math.Clamp(x, 0, img.Width - 1);
                var py = Math.Clamp(y, 0, img.Height - 1);
                var p = img.GetRgb(px, py);
                points.Add(new BandSample(name, p.R, p.G, p.B));
            }

            return points;
        }

        var ax = (int)Math.Round(artBox.X);
        var ay = (int)Math.Round(artBox.Y);
        var aw = (int)Math.Round(artBox.Width);
        var ah = (int)Math.Round(artBox.Height);

        // Band token checks on art margins (pearl ::before covers the copy column in the render).
        var samples = new (string Name, int X, int Y)[]
        {
            ("art_margin_left_mid", ax + 8, ay + ah / 2),
            ("art_margin_left_upper", ax + 8, ay + (int)(ah * 0.22)),
            ("art_margin_left_lower", ax + 8, ay + (int)(ah * 0.78)),
            ("right_of_copy", ax - 14, hy + hh / 2),
            ("art_margin_left_top", ax + 8, ay + 10)
        };

        foreach (var (name, x, y) in samples)
        {
            var px = Math.Clamp(x, 0, img.Width - 1);
            var py = Math.Clamp(y, 0, img.Height - 1);
            var p = img.GetRgb(px, py);
            points.Add(new BandSample(name, p.R, p.G, p.B));
        }

        return points;
    }

    private sealed class CaptureMetrics
    {
        public int HeroBandHeightPx { get; set; }
        public bool BandUsesPaspoortToken { get; set; }
        public bool UsesLegacyCareerLayout { get; set; }
        public bool LeadHasLoslaat { get; set; }
        public bool LeadEllipsis { get; set; }
        public bool HeroVisibleOnCapture { get; set; }
        public List<BandSample> BandSamplePoints { get; set; } = [];
    }

    private sealed record BandSample(string Name, int R, int G, int B);

    private sealed class LeadInfo
    {
        public string Text { get; set; } = "";
        public bool Ellipsis { get; set; }
        public bool Nowrap { get; set; }
    }
}
