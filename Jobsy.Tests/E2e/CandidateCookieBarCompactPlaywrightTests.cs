using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
public sealed class CandidateCookieBarCompactPlaywrightTests : IAsyncLifetime
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

    [Fact]
    public async Task Cookie_bar_is_compact_above_bottom_nav_at_390_and_dismisses()
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IsMobile = true,
            HasTouch = true,
            IgnoreHTTPSErrors = true,
        });

        var page = await context.NewPageAsync();
        var signIn = $"{baseUrl}/__test/sign-in?role=candidate&return={Uri.EscapeDataString("/candidate/ontdekkingsreis")}";
        await page.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync($"{baseUrl}/candidate/ontdekkingsreis", new() { WaitUntil = WaitUntilState.Load });

        var bar = page.Locator(".cookie-consent");
        await bar.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });

        var metrics = await page.EvaluateAsync<CookieBarMetrics>(
            """
            () => {
              const bar = document.querySelector('.cookie-consent');
              const nav = document.querySelector('nav.bottom-nav');
              const main = document.querySelector('.app-main__content');
              if (!bar || !nav || !main) return null;
              const br = bar.getBoundingClientRect();
              const nr = nav.getBoundingClientRect();
              const mr = main.getBoundingClientRect();
              return {
                barHeight: br.height,
                barBottom: br.bottom,
                navTop: nr.top,
                mainVisibleHeight: Math.max(0, nr.top - mr.top),
              };
            }
            """);

        Assert.NotNull(metrics);
        Assert.True(metrics!.BarHeight <= 56, $"Cookie bar too tall: {metrics.BarHeight}px");
        Assert.True(metrics.BarBottom <= metrics.NavTop + 2, "Cookie bar overlaps bottom nav.");
        Assert.True(metrics.MainVisibleHeight >= 320, "Cookie bar covers too much main content.");

        await page.Locator(".cookie-consent [data-consent='necessary']").ClickAsync();
        await bar.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 5_000 });
        Assert.True(await page.EvaluateAsync<bool>("() => document.documentElement.classList.contains('cookie-consent-known')"));
    }

    [Fact]
    public async Task Http_cookie_alone_hides_bar_until_jobsyCookieConsent_clear()
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IsMobile = true,
            HasTouch = true,
            IgnoreHTTPSErrors = true,
        });

        var page = await context.NewPageAsync();
        var signIn = $"{baseUrl}/__test/sign-in?role=candidate&return={Uri.EscapeDataString("/candidate/ontdekkingsreis")}";
        await page.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.DOMContentLoaded });

        await page.EvaluateAsync("""
            () => {
              try { localStorage.removeItem('Jobsy.CookieConsent'); } catch (e) {}
              document.cookie = 'Jobsy.CookieConsent=' + encodeURIComponent('necessary')
                + '; Path=/; Max-Age=' + (60 * 60 * 24 * 365) + '; SameSite=Lax';
            }
            """);

        await page.GotoAsync($"{baseUrl}/candidate/ontdekkingsreis", new() { WaitUntil = WaitUntilState.Load });
        await page.WaitForTimeoutAsync(500);

        var hiddenWithCookieOnly = await page.EvaluateAsync<bool>(
            """
            () => {
              const bar = document.querySelector('.cookie-consent');
              if (!bar) return true;
              const style = window.getComputedStyle(bar);
              return style.display === 'none' || bar.offsetParent === null;
            }
            """);
        Assert.True(hiddenWithCookieOnly, "Banner should stay hidden when only the HTTP cookie is set.");

        await page.EvaluateAsync("() => window.jobsyCookieConsent.clear()");
        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.Load });

        await page.Locator(".cookie-consent").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });
    }

    private sealed class CookieBarMetrics
    {
        public double BarHeight { get; set; }
        public double BarBottom { get; set; }
        public double NavTop { get; set; }
        public double MainVisibleHeight { get; set; }
    }
}
