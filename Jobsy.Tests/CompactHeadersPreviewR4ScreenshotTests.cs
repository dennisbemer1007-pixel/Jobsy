using System.Text.Json;
using Jobsy.Web.Media;
using Microsoft.Playwright;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Jobsy.Tests;

/// <summary>Variant C r4: seamless band edges, art scale, copy/art separation, EN sample.</summary>
[Collection("PlaywrightSmoke")]
public sealed class CompactHeadersPreviewR4ScreenshotTests : IAsyncLifetime
{
    private const string ArtifactsDir = "/cursor/stores/self/artifacts/compact-headers-preview-r4";
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
    public async Task Capture_compact_header_preview_r4_variant_c()
    {
        Directory.CreateDirectory(ArtifactsDir);
        var report = new List<object>();
        var pages = new (string Slug, string Server, string Path, bool Onboarding, Rgb24 Cream)[]
        {
            ("onboarding-kennismaken", _onboardingWeb!.ServerAddress, "/candidate/start", true, ParseHex(CompactHeroPreviewCream.OnboardingFinaleHero)),
            ("carriere", _web!.ServerAddress, "/carriere", false, ParseHex(CompactHeroPreviewCream.CarriereLearnGrowWide)),
            ("paspoort", _web.ServerAddress, "/candidate/paspoort", false, ParseHex(CompactHeroPreviewCream.PaspoortKompasWide)),
        };

        foreach (var page in pages)
        {
            foreach (var width in new[] { 1440, 1920, 390 })
            {
                var locale = "nl-NL";
                var suffix = "";
                var (png, metrics) = await CaptureAsync(
                    (page.Slug, page.Server, page.Path, page.Onboarding),
                    width,
                    locale,
                    suffix);
                if (width >= 768)
                {
                    AssertR4Pixels(page.Slug, width, metrics);
                    SaveBoundaryCrop(png, metrics.ArtStartX, metrics.HeroTop, metrics.HeroBottom, page.Slug, width, suffix);
                }

                report.Add(new { page = page.Slug, width, locale, metrics.ArtWidthPercent, metrics.MaxSeamDelta, metrics.MaxBandEdgeDelta });
            }

            var (enPng, enMetrics) = await CaptureAsync(
                (page.Slug, page.Server, page.Path, page.Onboarding),
                1440,
                "en-GB",
                "-en");
            AssertR4Pixels(page.Slug, 1440, enMetrics);
            await File.WriteAllBytesAsync(Path.Combine(ArtifactsDir, $"{page.Slug}-c-1440-en.png"), enPng);
        }

        await File.WriteAllTextAsync(
            Path.Combine(ArtifactsDir, "r4-metrics.json"),
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void AssertR4Pixels(string slug, int width, CaptureMetrics metrics)
    {
        Assert.True(metrics.MaxSeamDelta < 6,
            $"{slug} c@{width}: seam column delta {metrics.MaxSeamDelta} (max 6).");
        Assert.True(metrics.MaxBandEdgeDelta < 6,
            $"{slug} c@{width}: band edge vs page delta {metrics.MaxBandEdgeDelta} (max 6).");
        var artMin = width >= 1800 ? 48.0 : 45.0;
        var artMax = width >= 1800 ? 62.0 : 58.0;
        Assert.True(metrics.ArtWidthPercent >= artMin && metrics.ArtWidthPercent <= artMax,
            $"{slug} c@{width}: art {metrics.ArtWidthPercent:F1}% of band (expected {artMin}-{artMax}%).");
        Assert.False(metrics.CopyIntersectsArt,
            $"{slug} c@{width}: hero copy bbox intersects art bbox.");
        if (slug == "carriere")
        {
            Assert.True(metrics.CareerCardVisibleH1Count == 0,
                $"carriere c@{width}: expected no visible h1 in overview card, got {metrics.CareerCardVisibleH1Count}.");
        }
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
                var artSelector = page.Onboarding
                    ? ".ob-illustrated-step__art"
                    : ".cand-hero__compact-c-art";
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
                }
            }

            var file = Path.Combine(ArtifactsDir, $"{page.Slug}-c-{width}{fileSuffix}.png");
            var bytes = await browserPage.ScreenshotAsync(new() { Type = ScreenshotType.Png, FullPage = false });
            await File.WriteAllBytesAsync(file, bytes);

            if (width >= 768)
            {
                var (heroTop, heroBottom) = HeroBandY(page.Slug, bytes);
                metrics.HeroTop = heroTop;
                metrics.HeroBottom = heroBottom;
                var artStart = metrics.ArtStartX > 0 ? metrics.ArtStartX : (int)(bytes.Length > 0 ? 680 : 680);
                metrics.MaxSeamDelta = MaxColumnDeltaBeforeArtStart(bytes, heroTop, heroBottom, artStart, 40);
                metrics.MaxBandEdgeDelta = MaxBandEdgeDelta(
                    bytes,
                    heroTop,
                    heroBottom,
                    metrics.BandLeftX,
                    metrics.BandRightX,
                    PageBackgroundFor(page.Slug),
                    metrics.SkipRightBandEdge);
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

    private static bool BoxesIntersect(LocatorBoundingBoxResult a, LocatorBoundingBoxResult b)
    {
        var ax2 = a.X + a.Width;
        var ay2 = a.Y + a.Height;
        var bx2 = b.X + b.Width;
        var by2 = b.Y + b.Height;
        return a.X < bx2 && ax2 > b.X && a.Y < by2 && ay2 > b.Y;
    }

    private static Rgb24 PageBackgroundFor(string slug)
        => slug == "onboarding-kennismaken"
            ? ParseHex("#f5f2ee")
            : ParseHex("#f7f4f0");

    private static int MaxBandEdgeDelta(
        byte[] pngBytes,
        int heroTop,
        int heroBottom,
        int bandLeftX,
        int bandRightX,
        Rgb24 pageBackground,
        bool skipRightEdge)
    {
        using var img = Image.Load<Rgb24>(pngBytes);
        var y0 = heroTop + 10;
        var y1 = Math.Min(heroBottom - 8, heroTop + 24);
        var max = 0;

        static void SampleLeftEdge(Image<Rgb24> image, int edgeX, int yStart, int yEnd, Rgb24 reference, ref int peak)
        {
            for (var dx = 0; dx <= 2; dx++)
            {
                var x = edgeX + dx;
                if (x < 1 || x >= image.Width - 2)
                {
                    continue;
                }

                var col = ColumnAverage(image, x, yStart, yEnd);
                peak = Math.Max(peak, ColorDrift(col, reference));
            }
        }

        static void SampleRightEdge(Image<Rgb24> image, int edgeXInclusive, int yStart, int yEnd, Rgb24 reference, ref int peak)
        {
            for (var dx = 0; dx <= 2; dx++)
            {
                var x = edgeXInclusive - dx;
                if (x < 1 || x >= image.Width - 2)
                {
                    continue;
                }

                var col = ColumnAverage(image, x, yStart, yEnd);
                peak = Math.Max(peak, ColorDrift(col, reference));
            }
        }

        if (bandLeftX > 0)
        {
            SampleLeftEdge(img, bandLeftX, y0, y1, pageBackground, ref max);
        }

        if (!skipRightEdge && bandRightX > 1)
        {
            SampleRightEdge(img, bandRightX - 1, y0, y1, pageBackground, ref max);
        }
        return max;
    }

    private static (int Top, int Bottom) HeroBandY(string slug, byte[] pngBytes)
    {
        using var img = Image.Load<Rgb24>(pngBytes);
        var heroTop = slug == "onboarding-kennismaken" ? 105 : 64;
        var heroBottom = Math.Min(heroTop + 132, img.Height - 2);
        return (heroTop, heroBottom);
    }

    private static int MaxColumnDeltaBeforeArtStart(byte[] pngBytes, int heroTop, int heroBottom, int artStartX, int windowPx)
    {
        using var img = Image.Load<Rgb24>(pngBytes);
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

    private static Rgb24 ColumnAverage(Image<Rgb24> img, int x, int y0, int y1)
    {
        long r = 0, g = 0, b = 0;
        var n = 0;
        for (var y = y0; y < y1; y++)
        {
            var p = img[x, y];
            r += p.R;
            g += p.G;
            b += p.B;
            n++;
        }

        return n == 0 ? new Rgb24(0, 0, 0) : new Rgb24((byte)(r / n), (byte)(g / n), (byte)(b / n));
    }

    private static void SaveBoundaryCrop(byte[] pngBytes, int artStartX, int heroTop, int heroBottom, string slug, int width, string suffix)
    {
        using var img = Image.Load<Rgb24>(pngBytes);
        var x0 = Math.Max(0, artStartX - 24);
        var x1 = Math.Min(img.Width, artStartX + 48);
        using var crop = img.Clone(ctx => ctx.Crop(new Rectangle(x0, heroTop, x1 - x0, heroBottom - heroTop)));
        crop.SaveAsPng(Path.Combine(ArtifactsDir, $"{slug}-c-{width}{suffix}-art-left-edge.png"));
    }

    private static Rgb24 ParseHex(string hex)
    {
        hex = hex.TrimStart('#');
        return new Rgb24(
            Convert.ToByte(hex[..2], 16),
            Convert.ToByte(hex[2..4], 16),
            Convert.ToByte(hex[4..6], 16));
    }

    private static int ColorDrift(Rgb24 a, Rgb24 b)
        => Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B);

    private sealed class CaptureMetrics
    {
        public int ArtStartX { get; set; }
        public double ArtWidthPercent { get; set; }
        public int MaxSeamDelta { get; set; }
        public int MaxBandEdgeDelta { get; set; }
        public bool CopyIntersectsArt { get; set; }
        public int CareerCardVisibleH1Count { get; set; }
        public int HeroTop { get; set; }
        public int HeroBottom { get; set; }
        public int BandLeftX { get; set; }
        public int BandRightX { get; set; }
        public bool SkipRightBandEdge { get; set; }
    }
}
