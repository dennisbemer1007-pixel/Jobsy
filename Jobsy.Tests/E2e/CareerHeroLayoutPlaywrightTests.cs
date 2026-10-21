using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Carrière compact hero: desktop eyebrow spacing, header nav at 1024, mobile art strip at 390.
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
    [InlineData(1024, 900)]
    [InlineData(1280, 900)]
    public async Task Desktop_carriere_header_nav_and_hero_spacing(int width, int height)
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IgnoreHTTPSErrors = true,
        });
        var page = await OpenCarrierePageAsync(context, baseUrl, expectDesktopNav: true);
        await page.Locator("header.uwv-carriere-showcase-hero.cand-compact-hero--page h1").First
            .WaitForAsync(new() { Timeout = 30_000 });

        var navSingleRow = await page.EvaluateAsync<bool>("""
            () => {
              const nav = document.querySelector('.candidate-header-nav');
              if (!nav) return false;
              const links = Array.from(nav.querySelectorAll('.candidate-header-nav__link'));
              if (links.length < 2) return true;
              const tops = links.map(a => a.getBoundingClientRect().top);
              return Math.max(...tops) - Math.min(...tops) < 8;
            }
            """);
        Assert.True(navSingleRow, $"Candidate header nav wrapped at {width}px.");

        if (width == 1024)
        {
            await page.EvaluateAsync("""
                () => {
                  const actions = document.querySelector('.app-header__actions');
                  if (!actions || actions.querySelector('[data-testid="pwa-install-entry-btn"]')) return;
                  const btn = document.createElement('button');
                  btn.type = 'button';
                  btn.className = 'pwa-entry-btn';
                  btn.dataset.testid = 'pwa-install-entry-btn';
                  btn.textContent = 'Installeer Lobsy';
                  actions.prepend(btn);
                }
                """);
            var installHidden = await page.EvaluateAsync<bool>("""
                () => {
                  const btn = document.querySelector('[data-testid="pwa-install-entry-btn"]');
                  if (!btn) return true;
                  const style = getComputedStyle(btn);
                  return style.display === 'none' || style.visibility === 'hidden' || btn.offsetParent === null;
                }
                """);
            Assert.True(installHidden, "Installeer Lobsy must be hidden at 1024px when candidate header nav is shown.");
        }

        var eyebrowGapOk = await page.EvaluateAsync<bool>("""
            () => {
              const header = document.querySelector('header.uwv-carriere-showcase-hero.cand-compact-hero--page');
              const copy = header && header.querySelector('.cand-compact-hero__copy, .cand-hero__copy');
              if (!header || !copy) return false;
              const padTop = parseFloat(getComputedStyle(copy).paddingTop) || 0;
              const gap = copy.getBoundingClientRect().top - header.getBoundingClientRect().top + padTop;
              return gap >= 16 || padTop >= 16;
            }
            """);
        Assert.True(eyebrowGapOk, $"Carrière eyebrow needs ~20px top breathing room at {width}px.");

        var copyOk = await page.EvaluateAsync<bool>("""
            () => {
              const header = document.querySelector('header.uwv-carriere-showcase-hero.cand-compact-hero--page');
              const copy = header && header.querySelector('.cand-compact-hero__copy, .cand-hero__copy');
              const h1 = copy && copy.querySelector('h1');
              if (!header || !copy || !h1) return false;
              const h1b = h1.getBoundingClientRect();
              return h1b.width > 48 && h1b.left >= 0 && h1b.right <= window.innerWidth + 1;
            }
            """);
        Assert.True(copyOk, $"UWV showcase hero title not visible at {width}px.");
    }

    [Fact]
    public async Task Mobile_carriere_hero_shows_figures_not_empty_band()
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IsMobile = true,
            HasTouch = true,
            IgnoreHTTPSErrors = true,
        });
        var page = await OpenCarrierePageAsync(context, baseUrl, expectDesktopNav: false);
        await page.Locator("header.uwv-carriere-showcase-hero.cand-compact-hero--page").First
            .WaitForAsync(new() { Timeout = 60_000 });

        var mobileArtOk = await page.EvaluateAsync<bool>("""
            async () => {
              const img = document.querySelector('[data-testid="cand-compact-hero-mobile-art"]');
              if (!img) return false;
              if (img.complete && img.naturalWidth > 0) {
                const r = img.getBoundingClientRect();
                return r.height >= 40 && r.width >= 120;
              }
              await new Promise((resolve, reject) => {
                img.addEventListener('load', resolve, { once: true });
                img.addEventListener('error', reject, { once: true });
              });
              const r = img.getBoundingClientRect();
              return img.naturalWidth > 0 && r.height >= 40 && r.width >= 120;
            }
            """);
        Assert.True(mobileArtOk, "Mobile Carrière hero must show watercolor figures in the art strip (no empty pale band).");
    }

    private static async Task<IPage> OpenCarrierePageAsync(
        IBrowserContext context,
        string baseUrl,
        bool expectDesktopNav)
    {
        var page = await context.NewPageAsync();
        await PlaywrightCookieConsent.AcceptAsync(context);
        var signIn = $"{baseUrl}/__test/sign-in?role=candidate&return={Uri.EscapeDataString("/carriere")}";
        await page.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync($"{baseUrl}/carriere", new() { WaitUntil = WaitUntilState.Load, Timeout = 90_000 });
        await page.Locator(".career-page--compact-preview-c").First.WaitForAsync(new() { Timeout = 60_000 });
        if (expectDesktopNav)
        {
            await page.Locator("[data-testid=candidate-header-nav]").WaitForAsync(new() { Timeout = 60_000 });
        }

        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 60_000 });
        return page;
    }
}
