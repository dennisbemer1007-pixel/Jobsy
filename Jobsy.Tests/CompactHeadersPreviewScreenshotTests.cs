using Microsoft.Playwright;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Jobsy.Tests;

/// <summary>Owner preview: before | heroPreview=a | heroPreview=b on three candidate pages.</summary>
[Collection("PlaywrightSmoke")]
public sealed class CompactHeadersPreviewScreenshotTests : IAsyncLifetime
{
    private const string ArtifactsDir = "/cursor/stores/self/artifacts/compact-headers-preview";
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
    public async Task Capture_compact_header_preview_matrix()
    {
        Directory.CreateDirectory(ArtifactsDir);
        var pages = new (string Slug, string Server, string Path, string HeroSelector, bool Onboarding)[]
        {
            ("onboarding-kennismaken", _onboardingWeb!.ServerAddress, "/candidate/start", ".ob-illustrated-step__mobile-cover, .ob-illustrated-step__hero-band", true),
            ("carriere", _web!.ServerAddress, "/carriere", ".cand-hero", false),
            ("paspoort", _web.ServerAddress, "/candidate/paspoort", ".cand-hero", false),
        };

        foreach (var page in pages)
        {
            foreach (var variant in new[] { ("before", ""), ("a", "?heroPreview=a"), ("b", "?heroPreview=b") })
            {
                await CaptureAsync(page, variant.Item1, variant.Item2, width: 1440);
                await CaptureAsync(page, variant.Item1, variant.Item2, width: 390);
            }

            await BuildContactSheetAsync(page.Slug);
        }
    }

    private async Task CaptureAsync(
        (string Slug, string Server, string Path, string HeroSelector, bool Onboarding) page,
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
            if (width > 390)
            {
                await browserPage.Locator("[data-testid=onboarding-step-1-illustrated] .ob-illustrated-step__hero-band")
                    .WaitForAsync(new() { Timeout = 120_000 });
            }
            else
            {
                await browserPage.Locator("[data-testid=onboarding-mobile-hero]").WaitForAsync(new() { Timeout = 120_000 });
            }
        }
        else
        {
            await browserPage.Locator(".cand-hero").First.WaitForAsync(new() { Timeout = 120_000 });
        }

        await browserPage.WaitForTimeoutAsync(800);
        var file = Path.Combine(ArtifactsDir, $"{page.Slug}-{variant}-{width}.png");
        await browserPage.ScreenshotAsync(new() { Path = file, FullPage = false });
        Assert.True(File.Exists(file));

        await WriteHeroMetaAsync(browserPage, page, variant, width, file);
        }
        finally
        {
            if (page.Onboarding)
            {
                IllustratedPagesApiStub.OnboardingWizardStep1Capture = false;
            }
        }
    }

    private static async Task WriteHeroMetaAsync(
        IPage browserPage,
        (string Slug, string Server, string Path, string HeroSelector, bool Onboarding) page,
        string variant,
        int width,
        string pngPath)
    {
        var selector = width <= 390 && page.Onboarding
            ? ".ob-illustrated-step__mobile-cover"
            : page.Onboarding
                ? ".ob-illustrated-step__hero-band"
                : ".cand-hero";
        var box = await browserPage.Locator(selector).First.BoundingBoxAsync();
        Assert.NotNull(box);
        var metaPath = pngPath.Replace(".png", "-meta.txt", StringComparison.Ordinal);
        await File.WriteAllTextAsync(
            metaPath,
            FormattableString.Invariant(
                $"variant={variant}\nwidth={width}\nheroHeight={box!.Height:F1}\nheroY={box.Y:F1}"));
    }

    private static async Task BuildContactSheetAsync(string slug)
    {
        var labels = new[] { ("before", "Before"), ("a", "A (~170px)"), ("b", "B (~130px)") };
        foreach (var width in new[] { 1440, 390 })
        {
            var tiles = new List<Image<Rgb24>>();
            foreach (var (variant, _) in labels)
            {
                var path = Path.Combine(ArtifactsDir, $"{slug}-{variant}-{width}.png");
                Assert.True(File.Exists(path), path);
                tiles.Add(await Image.LoadAsync<Rgb24>(path));
            }

            var tileW = tiles[0].Width;
            var tileH = tiles[0].Height;
            using var sheet = new Image<Rgb24>(tileW * 3, tileH + 40);
            sheet.Mutate(ctx => ctx.BackgroundColor(new Rgb24(251, 245, 236)));
            for (var i = 0; i < tiles.Count; i++)
            {
                sheet.Mutate(ctx => ctx.DrawImage(tiles[i], new Point(i * tileW, 40), 1f));
            }

            var outPath = Path.Combine(ArtifactsDir, $"{slug}-contact-{width}.png");
            await sheet.SaveAsPngAsync(outPath);
            foreach (var t in tiles)
            {
                t.Dispose();
            }
        }
    }
}
