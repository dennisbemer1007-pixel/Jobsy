using System.Text.Json;
using Jobsy.Web.Media;
using Microsoft.Playwright;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Jobsy.Tests;

/// <summary>R14: paspoort desktop art left fade + ~150px band; 5-point edge colour ramp.</summary>
[Collection("PlaywrightSmoke")]
public sealed class CompactHeadersPreviewR14ScreenshotTests : IAsyncLifetime
{
    private const string ArtifactsDir = "/workspace/artifacts/compact-headers-preview-r14";
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
    public async Task Capture_compact_header_preview_r14_paspoort_desktop()
    {
        Directory.CreateDirectory(ArtifactsDir);
        var report = new List<object>();

        foreach (var width in new[] { 1440, 1920 })
        {
            var (bytes, metrics) = await CapturePaspoortAsync(width);
            await File.WriteAllBytesAsync(Path.Combine(ArtifactsDir, $"paspoort-c-{width}.png"), bytes);
            report.Add(new
            {
                width,
                metrics.UsesBandCArt,
                metrics.HeroBandHeightPx,
                artLeftEdgeSamples = metrics.ArtLeftEdgeSamples.Select(s => new { s.Name, hex = $"#{s.R:X2}{s.G:X2}{s.B:X2}" })
            });
            await File.WriteAllTextAsync(
                Path.Combine(ArtifactsDir, "r14-metrics.json"),
                JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));

            Assert.True(metrics.UsesBandCArt, $"paspoort {width}: img must use kompas-hero-band-c.");
            Assert.InRange(metrics.HeroBandHeightPx, 145, 152);
            AssertArtLeftEdgeSmooth(metrics.ArtLeftEdgeSamples, width);
        }

        var metricsPath = Path.Combine(ArtifactsDir, "r14-metrics.json");
        await File.WriteAllTextAsync(
            metricsPath,
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void AssertArtLeftEdgeSmooth(List<BandSample> samples, int width)
    {
        Assert.Equal(5, samples.Count);
        for (var i = 1; i < samples.Count; i++)
        {
            var a = samples[i - 1];
            var b = samples[i];
            Assert.True(Math.Abs(a.R - b.R) <= 3, $"paspoort {width}: R step {a.Name}→{b.Name} Δ{Math.Abs(a.R - b.R)}.");
            Assert.True(Math.Abs(a.G - b.G) <= 3, $"paspoort {width}: G step {a.Name}→{b.Name} Δ{Math.Abs(a.G - b.G)}.");
            Assert.True(Math.Abs(a.B - b.B) <= 3, $"paspoort {width}: B step {a.Name}→{b.Name} Δ{Math.Abs(a.B - b.B)}.");
        }

        var first = samples[0];
        Assert.True(Math.Abs(first.R - BandRgb.R) <= 8 && Math.Abs(first.G - BandRgb.G) <= 8 && Math.Abs(first.B - BandRgb.B) <= 8,
            $"paspoort {width}: sample left-20 should match band {CompactHeroPreviewBand.Token}.");
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
        var png = await page.ScreenshotAsync(new() { Type = ScreenshotType.Png, FullPage = false });
        var samples = SampleArtLeftEdge(png, width, heroBox);
        var metrics = new CaptureMetrics
        {
            UsesBandCArt = usesBand,
            HeroBandHeightPx = heroBox is null ? 0 : (int)Math.Round(heroBox.Height),
            ArtLeftEdgeSamples = samples
        };
        return (png, metrics);
    }

    private static List<BandSample> SampleArtLeftEdge(byte[] pngBytes, int viewportWidth, LocatorBoundingBoxResult? heroBox)
    {
        var samples = new List<BandSample>();
        if (heroBox is null)
        {
            return samples;
        }

        using var img = Image.Load<Rgb24>(pngBytes);
        var midY = Math.Clamp((int)Math.Round(heroBox.Y + heroBox.Height / 2), 0, img.Height - 1);
        var edgeX = (int)Math.Round(512.0 * viewportWidth / 1440.0);

        var offsets = new (string Name, int Dx)[]
        {
            ("left_minus_20", -20),
            ("art_left", 0),
            ("left_plus_20", 20),
            ("left_plus_40", 40),
            ("left_plus_80", 80)
        };

        foreach (var (name, dx) in offsets)
        {
            var px = Math.Clamp(edgeX + dx, 0, img.Width - 1);
            var p = img[px, midY];
            samples.Add(new BandSample(name, p.R, p.G, p.B));
        }

        return samples;
    }

    private sealed class CaptureMetrics
    {
        public bool UsesBandCArt { get; set; }
        public int HeroBandHeightPx { get; set; }
        public List<BandSample> ArtLeftEdgeSamples { get; set; } = [];
    }

    private sealed record BandSample(string Name, int R, int G, int B);
}
