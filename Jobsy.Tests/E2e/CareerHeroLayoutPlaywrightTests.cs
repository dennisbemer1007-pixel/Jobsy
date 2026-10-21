using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Carrière compact hero must not clip copy at tablet/desktop widths (1024–1440 audit).
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class CareerHeroLayoutPlaywrightTests : IAsyncLifetime
{
    private IllustratedPagesWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        Environment.SetEnvironmentVariable("JOBSY_UWV_FEATURES", "0");
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
    [InlineData(1024, 900, "1024")]
    [InlineData(1280, 900, "1280")]
    [InlineData(1440, 900, "1440")]
    public async Task Plan_hero_copy_is_not_clipped(int width, int height, string tag)
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        var outDir = Path.Combine(FindRepoRoot(), "artifacts", "carriere-hero-layout");
        Directory.CreateDirectory(outDir);

        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IgnoreHTTPSErrors = true
        });

        var page = await context.NewPageAsync();
        await PlaywrightCookieConsent.AcceptAsync(context);
        var signIn = $"{baseUrl}/__test/sign-in?role=candidate&return={Uri.EscapeDataString("/carriere")}";
        await page.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync($"{baseUrl}/carriere", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.Locator(".career-page--illustrated .career-card:not(.career-card--skeleton)").First
            .WaitForAsync(new() { Timeout = 60_000 });
        await page.Locator("header.cand-compact-hero--onboarding h1").First
            .WaitForAsync(new() { Timeout = 30_000 });

        var ok = await page.EvaluateAsync<bool>("""
            () => {
              const header = document.querySelector('header.cand-compact-hero--onboarding');
              const copy = header && header.querySelector('.cand-hero__copy');
              const eyebrow = copy && copy.querySelector('.cand-hero__eyebrow');
              const h1 = copy && copy.querySelector('h1');
              if (!header || !copy || !h1) {
                return false;
              }
              const hb = header.getBoundingClientRect();
              const eb = eyebrow ? eyebrow.getBoundingClientRect() : h1.getBoundingClientRect();
              const h1b = h1.getBoundingClientRect();
              const copyLeft = copy.getBoundingClientRect().left;
              return eb.left >= hb.left + 4 && h1b.left >= hb.left + 4 && copyLeft >= hb.left;
            }
            """);

        Assert.True(ok, $"Hero copy clipped at {width}px (check eyebrow/h1 vs header band).");

        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(outDir, $"carriere-hero-{tag}.png"),
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
