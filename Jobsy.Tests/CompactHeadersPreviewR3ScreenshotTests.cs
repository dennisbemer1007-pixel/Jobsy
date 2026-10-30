using System.Text.Json;
using Jobsy.Web.Media;
using Microsoft.Playwright;

using Jobsy.Tests.Testing;

namespace Jobsy.Tests;

/// <summary>Variant C r3: left-edge cream, ~50% art, mask on img, pixel gates + boundary crops.</summary>
[Collection("PlaywrightSmoke")]
public sealed class CompactHeadersPreviewR3ScreenshotTests : IAsyncLifetime
{
    private const string ArtifactsDir = "/cursor/stores/self/artifacts/compact-headers-preview-r3";

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
    public async Task Capture_compact_header_preview_r3_variant_c()
    {
        Directory.CreateDirectory(ArtifactsDir);
        var report = new List<object>();
        var pages = new (string Slug, string Server, string Path, bool Onboarding, TestRgb Cream)[]
        {
            ("onboarding-kennismaken", _onboardingWeb!.ServerAddress, "/candidate/start", true, ParseHex(CompactHeroPreviewCream.OnboardingFinaleHero)),
            ("carriere", _web!.ServerAddress, "/carriere", false, ParseHex(CompactHeroPreviewCream.CarriereLearnGrowWide)),
            ("paspoort", _web.ServerAddress, "/candidate/paspoort", false, ParseHex(CompactHeroPreviewCream.PaspoortKompasWide)),
        };

        foreach (var page in pages)
        {
            foreach (var width in new[] { 1440, 1920, 390 })
            {
                var (png, artStartLayout, artSharePct) = await CaptureAsync(
                    (page.Slug, page.Server, page.Path, page.Onboarding),
                    width);
                if (width >= 768)
                {
                    var (heroTop, heroBottom) = HeroBandY(page.Slug, png);
                    var artStart = artStartLayout ?? DetectArtStartX(png, heroTop, heroBottom, page.Cream);
                    var artWidthPct = artSharePct ?? MeasureArtWidthPercent(png, heroTop, heroBottom, page.Cream, artStart);
                    var maxColDelta = MaxColumnDeltaBeforeArtStart(png, heroTop, heroBottom, artStart, windowPx: 40);

                    Assert.True(maxColDelta < 6,
                        $"{page.Slug} c@{width}: max column delta {maxColDelta} within 40px before art start x={artStart}.");
                    var artMin = width >= 1800 ? 48.0 : 42.0;
                    var artMax = width >= 1800 ? 62.0 : 58.0;
                    Assert.True(artWidthPct >= artMin && artWidthPct <= artMax,
                        $"{page.Slug} c@{width}: art width {artWidthPct:F1}% of band (expected ~{(width >= 1800 ? 55 : 50)}%, {artMin}–{artMax}%).");

                    SaveBoundaryCrop(png, artStart, heroTop, heroBottom, page.Slug, width);

                    report.Add(new
                    {
                        page = page.Slug,
                        width,
                        artStartX = artStart,
                        artWidthPercent = Math.Round(artWidthPct, 1),
                        maxColumnDeltaNearStart = maxColDelta
                    });
                }
            }
        }

        await File.WriteAllTextAsync(
            Path.Combine(ArtifactsDir, "r3-metrics.json"),
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    }

    private async Task<(byte[] Png, int? ArtStartX, double? ArtSharePercent)> CaptureAsync(
        (string Slug, string Server, string Path, bool Onboarding) page,
        int width)
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
                var stepClass = await browserPage.Locator("[data-testid=onboarding-step-1-illustrated]").GetAttributeAsync("class");
                Assert.Contains("ob-illustrated-step--compact-preview-c", stepClass ?? "", StringComparison.Ordinal);
                if (width >= 768)
                {
                    var imgSrc = await browserPage.Locator("[data-testid=onboarding-illustration-img]").GetAttributeAsync("src");
                    Assert.Contains("wie-ben-jij-profession-wide", imgSrc ?? "", StringComparison.OrdinalIgnoreCase);
                    var bandBox = await browserPage.Locator(".ob-illustrated-step__hero-band").BoundingBoxAsync();
                    Assert.NotNull(bandBox);
                    Assert.True(bandBox!.Height <= 140, $"hero band height {bandBox.Height}px (expected compact ~130px).");
                }
            }
            else
            {
                await browserPage.Locator(".cand-hero").First.WaitForAsync(new() { Timeout = 120_000 });
            }

            await browserPage.WaitForTimeoutAsync(900);
            int? artStart = null;
            double? artShare = null;
            if (width >= 768)
            {
                var artSelector = page.Onboarding
                    ? ".ob-illustrated-step__art-img-wrap img, .ob-illustrated-step__art img"
                    : ".cand-hero__compact-c-art img";
                var bandSelector = page.Onboarding ? ".ob-illustrated-step__hero-band" : ".cand-hero";
                var artBox = await browserPage.Locator(artSelector).First.BoundingBoxAsync();
                var bandBox = await browserPage.Locator(bandSelector).First.BoundingBoxAsync();
                if (artBox is not null)
                {
                    artStart = (int)Math.Round(artBox.X);
                }

                if (artBox is not null && bandBox is not null && bandBox.Width > 0)
                {
                    artShare = artBox.Width / bandBox.Width * 100.0;
                }
            }

            var file = Path.Combine(ArtifactsDir, $"{page.Slug}-c-{width}.png");
            var bytes = await browserPage.ScreenshotAsync(new() { Type = ScreenshotType.Png, FullPage = false });
            await File.WriteAllBytesAsync(file, bytes);
            return (bytes, artStart, artShare);
        }
        finally
        {
            if (page.Onboarding)
            {
                IllustratedPagesApiStub.OnboardingWizardStep1Capture = false;
            }
        }
    }

    private static (int Top, int Bottom) HeroBandY(string slug, byte[] pngBytes)
    {
        using var img = SkiaTestImage.LoadRgb(pngBytes);
        var heroTop = slug == "onboarding-kennismaken" ? 105 : 64;
        var heroBottom = Math.Min(heroTop + 132, img.Height - 2);
        return (heroTop, heroBottom);
    }

    private static int DetectArtStartX(byte[] pngBytes, int heroTop, int heroBottom, TestRgb cream)
    {
        using var img = SkiaTestImage.LoadRgb(pngBytes);
        var yMid = (heroTop + heroBottom) / 2;
        var xScanStart = (int)(img.Width * 0.35);
        var xScanEnd = (int)(img.Width * 0.75);
        for (var x = xScanStart; x < xScanEnd; x++)
        {
            var col = ColumnAverage(img, x, heroTop + 6, heroBottom - 6);
            if (ColorDrift(col, cream) > 14)
            {
                return x;
            }
        }

        return xScanStart;
    }

    private static double MeasureArtWidthPercent(byte[] pngBytes, int heroTop, int heroBottom, TestRgb cream, int artStartX)
    {
        using var img = SkiaTestImage.LoadRgb(pngBytes);
        var bandWidth = img.Width;
        var right = bandWidth - 17;
        var left = artStartX;
        var threshold = 18;
        for (var x = artStartX; x < bandWidth - 8; x++)
        {
            var col = ColumnAverage(img, x, heroTop + 4, heroBottom - 4);
            if (ColorDrift(col, cream) > threshold)
            {
                right = Math.Max(right, x);
            }
        }

        return (right - left + 1) * 100.0 / bandWidth;
    }

    /// <summary>Adjacent column RGB drift in the band, from artStart-40px up to artStart (seam zone).</summary>
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
            var delta = ColorDrift(left, right);
            if (delta > max)
            {
                max = delta;
            }
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

    private static void SaveBoundaryCrop(byte[] pngBytes, int artStartX, int heroTop, int heroBottom, string slug, int width)
    {
        using var img = SkiaTestImage.LoadRgb(pngBytes);
        var x0 = Math.Max(0, artStartX - 24);
        var x1 = Math.Min(img.Width, artStartX + 48);
        var y0 = Math.Max(0, heroTop);
        var y1 = Math.Min(img.Height, heroBottom);
        var cropW = x1 - x0;
        var cropH = y1 - y0;
        using var src = img.ToBitmap();
        using var crop = SkiaTestImage.Crop(src, x0, y0, cropW, cropH);
        SkiaTestImage.SavePng(crop, Path.Combine(ArtifactsDir, $"{slug}-c-{width}-art-left-edge.png"));
    }

    private static TestRgb ParseHex(string hex)
    {
        hex = hex.TrimStart('#');
        return new TestRgb(
            Convert.ToByte(hex[..2], 16),
            Convert.ToByte(hex[2..4], 16),
            Convert.ToByte(hex[4..6], 16));
    }

    private static int ColorDrift(TestRgb a, TestRgb b)
        => Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B);
}
