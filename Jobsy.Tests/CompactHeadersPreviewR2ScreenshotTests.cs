using Microsoft.Playwright;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Jobsy.Tests;

/// <summary>Re-capture A/B after soft-edge mask fix; pixel-check art boundaries.</summary>
[Collection("PlaywrightSmoke")]
public sealed class CompactHeadersPreviewR2ScreenshotTests : IAsyncLifetime
{
    private const string ArtifactsDir = "/cursor/stores/self/artifacts/compact-headers-preview-r2";
    private static readonly Rgb24 Pearl = new(247, 244, 240);

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
    public async Task Capture_compact_header_preview_r2_ab_with_soft_edge_check()
    {
        Directory.CreateDirectory(ArtifactsDir);
        var pages = new (string Slug, string Server, string Path, bool Onboarding)[]
        {
            ("onboarding-kennismaken", _onboardingWeb!.ServerAddress, "/candidate/start", true),
            ("carriere", _web!.ServerAddress, "/carriere", false),
            ("paspoort", _web.ServerAddress, "/candidate/paspoort", false),
        };

        foreach (var page in pages)
        {
            foreach (var (variant, query) in new[] { ("a", "?heroPreview=a"), ("b", "?heroPreview=b") })
            {
                foreach (var width in new[] { 1440, 390 })
                {
                    var png = await CaptureAsync(page, variant, query, width);
                    if (width == 1440)
                    {
                        AssertNoHardVerticalArtEdge(png, page.Slug, variant);
                    }
                }
            }

            await BuildContactSheetAsync(page.Slug);
        }
    }

    private async Task<byte[]> CaptureAsync(
        (string Slug, string Server, string Path, bool Onboarding) page,
        string variant,
        string query,
        int width)
    {
        var height = width <= 390 ? 844 : 900;
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

        var pathWithQuery = page.Path + query;
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

            await browserPage.WaitForTimeoutAsync(800);
            var file = Path.Combine(ArtifactsDir, $"{page.Slug}-{variant}-{width}.png");
            var bytes = await browserPage.ScreenshotAsync(new() { Type = ScreenshotType.Png, FullPage = false });
            await File.WriteAllBytesAsync(file, bytes);
            return bytes;
        }
        finally
        {
            if (page.Onboarding)
            {
                IllustratedPagesApiStub.OnboardingWizardStep1Capture = false;
            }
        }
    }

    /// <summary>Fail if a full-height vertical step (&gt;55 RGB delta) appears in the art column band.</summary>
    private static void AssertNoHardVerticalArtEdge(byte[] pngBytes, string slug, string variant)
    {
        using var img = Image.Load<Rgb24>(pngBytes);
        var heroTop = slug == "onboarding-kennismaken" ? 105 : 64;
        var heroBottom = heroTop + (variant == "b" ? 132 : 172);
        heroBottom = Math.Min(heroBottom, img.Height - 2);
        var scanXStart = (int)(img.Width * 0.42);
        var scanXEnd = (int)(img.Width * 0.72);
        var worstDelta = 0;
        var worstX = 0;
        for (var x = scanXStart; x < scanXEnd; x++)
        {
            var strongSteps = 0;
            var samples = 0;
            for (var y = heroTop + 8; y < heroBottom - 8; y++)
            {
                var left = img[x, y];
                var right = img[x + 1, y];
                var delta = Math.Abs(left.R - right.R) + Math.Abs(left.G - right.G) + Math.Abs(left.B - right.B);
                if (delta > 55)
                {
                    strongSteps++;
                }

                samples++;
            }

            if (samples > 0 && strongSteps > samples * 0.55)
            {
                var colDelta = ColumnPearlToArtDelta(img, x, heroTop, heroBottom);
                if (colDelta > worstDelta)
                {
                    worstDelta = colDelta;
                    worstX = x;
                }
            }
        }

        Assert.True(
            worstDelta < 120,
            $"{slug} variant={variant}: hard vertical art edge near x={worstX} (delta={worstDelta}).");
    }

    private static int ColumnPearlToArtDelta(Image<Rgb24> img, int x, int y0, int y1)
    {
        var max = 0;
        for (var y = y0; y < y1; y++)
        {
            var p = img[x, y];
            var d = Math.Abs(p.R - Pearl.R) + Math.Abs(p.G - Pearl.G) + Math.Abs(p.B - Pearl.B);
            if (d > max)
            {
                max = d;
            }
        }

        return max;
    }

    private static async Task BuildContactSheetAsync(string slug)
    {
        foreach (var width in new[] { 1440, 390 })
        {
            var tiles = new List<Image<Rgb24>>();
            foreach (var variant in new[] { "a", "b" })
            {
                var path = Path.Combine(ArtifactsDir, $"{slug}-{variant}-{width}.png");
                Assert.True(File.Exists(path), path);
                tiles.Add(await Image.LoadAsync<Rgb24>(path));
            }

            var tileW = tiles[0].Width;
            var tileH = tiles[0].Height;
            using var sheet = new Image<Rgb24>(tileW * 2, tileH + 40);
            sheet.Mutate(ctx => ctx.BackgroundColor(Pearl));
            for (var i = 0; i < tiles.Count; i++)
            {
                sheet.Mutate(ctx => ctx.DrawImage(tiles[i], new Point(i * tileW, 40), 1f));
            }

            await sheet.SaveAsPngAsync(Path.Combine(ArtifactsDir, $"{slug}-contact-{width}.png"));
            foreach (var t in tiles)
            {
                t.Dispose();
            }
        }
    }
}
