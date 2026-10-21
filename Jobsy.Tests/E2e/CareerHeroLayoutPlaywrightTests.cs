using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Carrière UWV showcase compact hero (--page) must not clip copy at tablet/desktop widths (1024–1440 audit).
/// Matches live Acceptatie DOM: <c>uwv-carriere-showcase-hero</c> + <c>cand-compact-hero--page</c>.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class CareerHeroLayoutPlaywrightTests : IAsyncLifetime
{
    private IllustratedPagesWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        Environment.SetEnvironmentVariable("JOBSY_UWV_FEATURES", "1");
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
    [InlineData(1920, 900, "1920")]
    public async Task Uwv_showcase_hero_copy_is_not_clipped(int width, int height, string tag)
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
        await page.Locator(".career-page--compact-preview-c").First
            .WaitForAsync(new() { Timeout = 60_000 });
        await page.Locator("header.uwv-carriere-showcase-hero.cand-compact-hero--page h1").First
            .WaitForAsync(new() { Timeout = 30_000 });

        var ok = await page.EvaluateAsync<bool>("""
            () => {
              const header = document.querySelector('header.uwv-carriere-showcase-hero.cand-compact-hero--page');
              const copy = header && header.querySelector('.cand-compact-hero__copy, .cand-hero__copy');
              const eyebrow = copy && copy.querySelector('.cand-hero__eyebrow');
              const h1 = copy && copy.querySelector('h1');
              if (!header || !copy || !h1) {
                return false;
              }
              const cs = getComputedStyle(copy);
              const padStart = parseFloat(cs.paddingLeft) || parseFloat(cs.paddingInlineStart) || 0;
              const hb = header.getBoundingClientRect();
              const eb = eyebrow ? eyebrow.getBoundingClientRect() : h1.getBoundingClientRect();
              const h1b = h1.getBoundingClientRect();
              const minInset = 12;
              return padStart >= minInset
                && h1b.left >= minInset
                && eb.left >= minInset
                && hb.left + padStart <= h1b.left + 2;
            }
            """);

        Assert.True(ok, $"UWV showcase hero copy clipped at {width}px (padding/h1/eyebrow vs viewport).");

        if (width == 1024)
        {
            var navSingleRow = await page.EvaluateAsync<bool>("""
                () => {
                  const nav = document.querySelector('.candidate-header-nav');
                  if (!nav) return false;
                  const links = Array.from(nav.querySelectorAll('.candidate-header-nav__link'));
                  if (links.length < 2) return true;
                  const tops = links.map(a => a.getBoundingClientRect().top);
                  const minTop = Math.min(...tops);
                  const maxTop = Math.max(...tops);
                  return maxTop - minTop < 8;
                }
                """);
            Assert.True(navSingleRow, "Candidate header nav wrapped to two rows at 1024px.");
        }

        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(outDir, $"carriere-hero-after-{tag}.png"),
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
