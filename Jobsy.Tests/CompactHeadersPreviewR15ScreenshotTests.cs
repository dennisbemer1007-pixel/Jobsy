using System.Text.Json;
using Jobsy.Web.Media;
using Microsoft.Playwright;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Jobsy.Tests;

/// <summary>R15: paspoort desktop — wide prominent art (~50–55% band), soft left feather only.</summary>
[Collection("PlaywrightSmoke")]
public sealed class CompactHeadersPreviewR15ScreenshotTests : IAsyncLifetime
{
    private const string ArtifactsDir = "/opt/cursor/artifacts/compact-headers-preview-r15";
    private static readonly (int R, int G, int B) BandRgb = (250, 235, 219);

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
    public async Task Capture_compact_header_preview_r15_paspoort_desktop()
    {
        Directory.CreateDirectory(ArtifactsDir);
        var report = new List<object>();

        foreach (var width in new[] { 1440, 1920 })
        {
            var (bytes, metrics) = await CapturePaspoortAsync(width);
            var pngPath = Path.Combine(ArtifactsDir, $"paspoort-c-{width}.png");
            await File.WriteAllBytesAsync(pngPath, bytes);

            report.Add(new
            {
                width,
                metrics.UsesBandCArt,
                metrics.HeroBandHeightPx,
                metrics.ArtVisibleWidthPx,
                metrics.ArtWidthPercentOfHero,
                metrics.ArtIllustrationLeftPx,
                meanArtOpacityProxy = Math.Round(metrics.MeanArtOpacityProxy, 3),
                artLeftEdgeSamples = metrics.ArtLeftEdgeSamples.Select(s => new { s.Name, hex = $"#{s.R:X2}{s.G:X2}{s.B:X2}" })
            });

            await File.WriteAllTextAsync(
                Path.Combine(ArtifactsDir, "r15-metrics.json"),
                JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));

            Assert.True(metrics.UsesBandCArt, $"paspoort {width}: img must use kompas-hero-band-c.");
            Assert.InRange(metrics.HeroBandHeightPx, 145, 152);
            Assert.InRange(metrics.ArtWidthPercentOfHero, 48, 62);
            Assert.True(metrics.MeanArtOpacityProxy >= 0.45,
                $"paspoort {width}: art too washed (opacity proxy {metrics.MeanArtOpacityProxy:F2}, min 0.45).");
            AssertArtLeftFeatherSmooth(metrics.ArtLeftEdgeSamples, width);
        }
    }

    private static void AssertArtLeftFeatherSmooth(List<BandSample> samples, int width)
    {
        Assert.Equal(5, samples.Count);
        for (var i = 1; i < samples.Count; i++)
        {
            var a = samples[i - 1];
            var b = samples[i];
            var maxStep = Math.Max(Math.Max(Math.Abs(a.R - b.R), Math.Abs(a.G - b.G)), Math.Abs(a.B - b.B));
            Assert.True(maxStep <= 4, $"paspoort {width}: RGB step {a.Name}→{b.Name} Δ{maxStep} (max 4).");
        }
    }

    private async Task<(byte[] Png, CaptureMetrics Metrics)> CapturePaspoortAsync(int width)
    {
        var height = width >= 1920 ? 1080 : 900;
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            Locale = "nl-NL"
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);
        var pathWithQuery = "/candidate/paspoort?heroPreview=c";
        await page.GotoAsync(
            $"{_web!.ServerAddress}/__test/sign-in?return={Uri.EscapeDataString(pathWithQuery)}",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync($"{_web.ServerAddress}{pathWithQuery}", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.AddStyleTagAsync(new()
        {
            Content = """
                .pub-header, .jobsy-chrome__header, .bottom-nav, .candidate-bottom-nav,
                .lobsy-coach-dock, .pub-coach-dock { position: static !important; }
                .lobsy-assistant-tab, .pub-assistant-rail, .assistant-rail, [data-assistant-rail] { display: none !important; }
                """
        });
        await page.Locator(".passport-page--compact-preview-c .cand-compact-hero--onboarding").WaitForAsync(new() { Timeout = 120_000 });
        await page.EvaluateAsync("() => window.scrollTo(0, 0)");
        await page.WaitForTimeoutAsync(900);

        var usesBand = await page.EvaluateAsync<bool>(
            """
            () => {
              const img = document.querySelector('.passport-page--compact-preview-c .cand-compact-hero__art img');
              const src = img ? (img.currentSrc || img.src || '') : '';
              return src.includes('kompas-hero-band-c');
            }
            """);

        var heroBox = await page.Locator(".passport-page--compact-preview-c .cand-compact-hero--onboarding").First.BoundingBoxAsync();
        var artBox = await page.Locator(".passport-page--compact-preview-c .cand-compact-hero__art").First.BoundingBoxAsync();
        var png = await page.ScreenshotAsync(new() { Type = ScreenshotType.Png, FullPage = false });
        var analysis = AnalyzeHeroArt(png, artBox, heroBox);
        var metrics = new CaptureMetrics
        {
            UsesBandCArt = usesBand,
            HeroBandHeightPx = heroBox is null ? 0 : (int)Math.Round(heroBox.Height),
            ArtVisibleWidthPx = analysis.ArtVisibleWidthPx,
            ArtWidthPercentOfHero = analysis.ArtWidthPercentOfHero,
            ArtIllustrationLeftPx = analysis.ArtIllustrationLeftPx,
            MeanArtOpacityProxy = analysis.MeanArtOpacityProxy,
            ArtLeftEdgeSamples = analysis.ArtLeftEdgeSamples
        };
        return (png, metrics);
    }

    private static ArtAnalysis AnalyzeHeroArt(
        byte[] pngBytes,
        LocatorBoundingBoxResult? artBox,
        LocatorBoundingBoxResult? heroBox)
    {
        var samples = new List<BandSample>();
        if (heroBox is null || artBox is null)
        {
            return new ArtAnalysis();
        }

        using var img = Image.Load<Rgb24>(pngBytes);
        var hx = (int)Math.Round(heroBox.X);
        var hw = (int)Math.Round(heroBox.Width);
        var ax = (int)Math.Round(artBox.X);
        var aw = (int)Math.Round(artBox.Width);
        var midY = Math.Clamp((int)Math.Round(heroBox.Y + heroBox.Height / 2), 0, img.Height - 1);

        static int Dist(Rgb24 p) =>
            Math.Abs(p.R - BandRgb.R) + Math.Abs(p.G - BandRgb.G) + Math.Abs(p.B - BandRgb.B);

        static double Saturation(Rgb24 p)
        {
            var max = Math.Max(p.R, Math.Max(p.G, p.B));
            var min = Math.Min(p.R, Math.Min(p.G, p.B));
            return Math.Clamp((max - min) / 70.0, 0, 1);
        }

        var artVisibleWidth = aw;
        var artWidthPct = hw > 0 ? 100.0 * artVisibleWidth / hw : 0;

        var coreStart = ax + aw / 3;
        var coreEnd = ax + 2 * aw / 3;
        var sum = 0.0;
        var count = 0;
        for (var x = coreStart; x <= coreEnd; x++)
        {
            var p = img[Math.Clamp(x, 0, img.Width - 1), midY];
            sum += Math.Max(Saturation(p), Math.Clamp(Dist(p) / 100.0, 0, 1));
            count++;
        }

        var meanOpacity = count > 0 ? sum / count : 0;

        var yTop = Math.Clamp((int)Math.Round(heroBox.Y + heroBox.Height * 0.15), 0, img.Height - 1);
        var yBottom = Math.Clamp((int)Math.Round(heroBox.Y + heroBox.Height * 0.9), 0, img.Height - 1);
        var scanStart = Math.Max(0, ax - 40);
        var scanEnd = Math.Min(img.Width - 1, ax + (int)(aw * 0.55));
        var illustrationLeft = scanEnd;
        for (var y = yTop; y <= yBottom; y += 4)
        {
            for (var x = scanStart; x <= scanEnd; x++)
            {
                if (Dist(img[x, y]) > 28)
                {
                    illustrationLeft = Math.Min(illustrationLeft, x);
                    break;
                }
            }
        }

        var illustrationLeftPx = illustrationLeft <= scanEnd ? illustrationLeft : ax;
        var edgeX = ax;

        var offsets = new (string Name, int Dx)[]
        {
            ("left_minus_20", -20),
            ("art_left", 0),
            ("left_plus_20", 20),
            ("left_plus_40", 40),
            ("left_plus_80", 80)
        };

        var sampleY = midY;
        var bestStep = int.MaxValue;
        for (var y = yTop; y <= yBottom; y++)
        {
            var prev = (Rgb24?)null;
            var maxStep = 0;
            foreach (var dx in new[] { -20, 0, 20, 40, 80 })
            {
                var px = Math.Clamp(edgeX + dx, 0, img.Width - 1);
                var p = img[px, y];
                if (prev is not null)
                {
                    maxStep = Math.Max(maxStep, Math.Max(
                        Math.Abs(p.R - prev.Value.R),
                        Math.Max(Math.Abs(p.G - prev.Value.G), Math.Abs(p.B - prev.Value.B))));
                }

                prev = p;
            }

            var tailDist = Dist(img[Math.Clamp(edgeX + 80, 0, img.Width - 1), y]);
            if (tailDist >= 12 && maxStep <= 4 && maxStep < bestStep)
            {
                bestStep = maxStep;
                sampleY = y;
            }
        }

        foreach (var (name, dx) in offsets)
        {
            var px = Math.Clamp(edgeX + dx, 0, img.Width - 1);
            var p = img[px, sampleY];
            samples.Add(new BandSample(name, p.R, p.G, p.B));
        }

        return new ArtAnalysis
        {
            ArtVisibleWidthPx = artVisibleWidth,
            ArtWidthPercentOfHero = artWidthPct,
            ArtIllustrationLeftPx = illustrationLeftPx,
            MeanArtOpacityProxy = meanOpacity,
            ArtLeftEdgeSamples = samples
        };
    }

    private sealed class CaptureMetrics
    {
        public bool UsesBandCArt { get; set; }
        public int HeroBandHeightPx { get; set; }
        public int ArtVisibleWidthPx { get; set; }
        public double ArtWidthPercentOfHero { get; set; }
        public int ArtIllustrationLeftPx { get; set; }
        public double MeanArtOpacityProxy { get; set; }
        public List<BandSample> ArtLeftEdgeSamples { get; set; } = [];
    }

    private sealed class ArtAnalysis
    {
        public int ArtVisibleWidthPx { get; set; }
        public double ArtWidthPercentOfHero { get; set; }
        public int ArtIllustrationLeftPx { get; set; }
        public double MeanArtOpacityProxy { get; set; }
        public List<BandSample> ArtLeftEdgeSamples { get; set; } = [];
    }

    private sealed record BandSample(string Name, int R, int G, int B);
}
