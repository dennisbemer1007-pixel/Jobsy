using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
public sealed class PassportMobileTabsPlaywrightTests : IAsyncLifetime
{
    private IllustratedPagesWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
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
    [InlineData(360)]
    [InlineData(390)]
    [InlineData(412)]
    public async Task Illustrated_passport_tabs_stick_flush_to_viewport_top(int width)
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = 844 },
            IsMobile = true,
            HasTouch = true,
            IgnoreHTTPSErrors = true,
        });

        var page = await context.NewPageAsync();
        await PlaywrightCookieConsent.AcceptAsync(context);
        var signIn = $"{baseUrl}/__test/sign-in?role=candidate&return={Uri.EscapeDataString("/candidate/paspoort")}";
        await page.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync($"{baseUrl}/candidate/paspoort", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.Locator(".passport-page--illustrated .passport-tabs-shell").WaitForAsync(new() { Timeout = 60_000 });

        await page.EvaluateAsync("""
            () => {
              const shell = document.querySelector('.passport-page--illustrated .passport-tabs-shell');
              if (shell) shell.scrollIntoView({ block: 'start' });
              window.scrollBy(0, 420);
            }
            """);

        await page.WaitForTimeoutAsync(300);

        var metrics = await page.EvaluateAsync<(double top, double stickyTop)>("""
            () => {
              const shell = document.querySelector('.passport-page--illustrated .passport-tabs-shell');
              if (!shell) return { top: -1, stickyTop: -1 };
              const cs = getComputedStyle(shell);
              return { top: shell.getBoundingClientRect().top, stickyTop: parseFloat(cs.top) || 0 };
            }
            """);

        Assert.True(metrics.stickyTop < 1, $"Expected sticky top ~0, got {metrics.stickyTop}px at {width}px.");
        Assert.True(metrics.top < 4, $"Tabs shell should sit flush while stuck (top={metrics.top}px at {width}px).");

        var outDir = Path.Combine(FindRepoRoot(), "artifacts", "passport-mobile-tabs");
        Directory.CreateDirectory(outDir);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(outDir, $"paspoort-tabs-{width}.png"),
            FullPage = false
        });
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}
