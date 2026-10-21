using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
public sealed class OnboardingStartJourneyMockCaptureTests : IAsyncLifetime
{
    private IllustratedPagesWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
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
    [InlineData(1280, 720, false, "1280x720")]
    [InlineData(390, 844, true, "390x844")]
    public async Task Viewport_shows_cta_above_fold(int width, int height, bool mobile, string tag)
    {
        var outDir = ArtifactsDir();
        Directory.CreateDirectory(outDir);
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IsMobile = mobile,
            HasTouch = mobile,
            IgnoreHTTPSErrors = true,
        });
        var page = await OpenMockAsync(context, baseUrl);
        await page.Locator("[data-testid=onboarding-begin]").WaitForAsync(new() { Timeout = 60_000 });

        var ctaVisible = await page.EvaluateAsync<bool>("""
            () => {
              const btn = document.querySelector('[data-testid=onboarding-begin]');
              if (!btn) return false;
              const r = btn.getBoundingClientRect();
              return r.top >= 0 && r.bottom <= window.innerHeight && r.height > 20;
            }
            """);

        Assert.True(ctaVisible, $"Aan de slag must be fully visible above the fold at {tag}.");

        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(outDir, $"onboarding-start-journey-mock-{tag}.png"),
            FullPage = false,
        });
    }

    [Fact]
    public async Task Desktop_full_page_capture()
    {
        var outDir = ArtifactsDir();
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 720 },
            IgnoreHTTPSErrors = true,
        });
        var page = await OpenMockAsync(context, baseUrl);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(outDir, "onboarding-start-journey-mock-1280-full.png"),
            FullPage = true,
        });
    }

    private static async Task<IPage> OpenMockAsync(IBrowserContext context, string baseUrl)
    {
        var page = await context.NewPageAsync();
        await PlaywrightCookieConsent.AcceptAsync(context);
        await page.GotoAsync($"{baseUrl}/preview/onboarding-start-journey", new()
        {
            WaitUntil = WaitUntilState.Load,
            Timeout = 90_000,
        });
        await page.Locator("[data-testid=onboarding-start-journey-route]").WaitForAsync(new() { Timeout = 60_000 });
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 60_000 });
        return page;
    }

    private static string ArtifactsDir()
    {
        var dir = "/opt/cursor/artifacts/screenshots";
        if (!Directory.Exists("/opt/cursor"))
        {
            dir = Path.Combine(FindRepoRoot(), "artifacts");
        }

        return dir;
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "Jobsy.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? "";
        }

        return Directory.GetCurrentDirectory();
    }
}
