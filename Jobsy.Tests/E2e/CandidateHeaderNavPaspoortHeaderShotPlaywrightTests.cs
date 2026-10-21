using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>Manual PR evidence: cropped header on paspoort at 1024 and 1280.</summary>
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
    [InlineData(1024, 900)]
    [InlineData(1280, 900)]
    public async Task Capture_paspoort_header_clip(int width, int height)
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
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

        var label = Environment.GetEnvironmentVariable("LOBSY_HEADER_SHOT_LABEL") ?? "after";
        var fileName = $"candidate-nav-header-{label}-{width}.png";
        await header.ScreenshotAsync(new()
        {
            Path = Path.Combine(ArtifactsDir, fileName),
        });
    }
}
