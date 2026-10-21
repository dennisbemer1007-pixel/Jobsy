using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>Manual PR evidence: cropped header on paspoort at 390, 1024, and 1280.</summary>
[Collection("PlaywrightSmoke")]
public sealed class CandidateHeaderNavPaspoortHeaderShotPlaywrightTests : IAsyncLifetime
{
    private const string ArtifactsDir = "/opt/cursor/artifacts/screenshots";

    private IllustratedPagesWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(ArtifactsDir);
        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        _web = new IllustratedPagesWebFactory();
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

    [Theory]
    [InlineData(390, 844)]
    [InlineData(1024, 900)]
    [InlineData(1280, 900)]
    public async Task Capture_paspoort_header_clip(int width, int height)
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IsMobile = width < 1024,
            IgnoreHTTPSErrors = true,
        });

        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{baseUrl}/__test/sign-in?role=candidate&return=/candidate/paspoort");
        await page.GotoAsync($"{baseUrl}/candidate/paspoort", new() { WaitUntil = WaitUntilState.Load, Timeout = 90_000 });

        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 60_000 });

        var header = page.Locator("[data-testid=app-shell-header]");
        await header.WaitForAsync(new() { Timeout = 30_000 });
        await WaitForHeaderBrandAndLanguageImagesAsync(page);

        var label = Environment.GetEnvironmentVariable("LOBSY_HEADER_SHOT_LABEL") ?? "after";
        var fileName = $"candidate-nav-header-{label}-{width}.png";
        await header.ScreenshotAsync(new()
        {
            Path = Path.Combine(ArtifactsDir, fileName),
        });
    }

    /// <summary>Logo and NL flag are img tags; capture must wait for decode, not only circuit ready.</summary>
    private static async Task WaitForHeaderBrandAndLanguageImagesAsync(IPage page)
    {
        await page.WaitForFunctionAsync(
            """
            () => {
              const logo = document.querySelector('.app-header .lobsy-logo__img');
              const flag = document.querySelector('.app-header .language-selector__toggle img.language-selector__flag');
              if (!logo || !flag) {
                return false;
              }
              const ok = (img) => img.complete && img.naturalWidth > 0 && img.naturalHeight > 0;
              return ok(logo) && ok(flag);
            }
            """,
            null,
            new() { Timeout = 30_000 });
    }
}
