using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
public sealed class CandidatePassportMobileLoadPlaywrightTests : IAsyncLifetime
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
    public async Task Paspoort_shows_real_content_within_2s_at_390()
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
        await PlaywrightCookieConsent.AcceptAsync(context);
        var signIn = $"{baseUrl}/__test/sign-in?role=candidate&return={Uri.EscapeDataString("/candidate/paspoort")}";
        await page.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.DOMContentLoaded });

        var started = DateTime.UtcNow;
        await page.GotoAsync($"{baseUrl}/candidate/paspoort", new() { WaitUntil = WaitUntilState.DOMContentLoaded });

        var ok = await WaitForPassportContentAsync(page, timeoutMs: 2_000);
        var elapsed = DateTime.UtcNow - started;
        Assert.True(ok, $"Paspoort content not visible within 2s (elapsed {elapsed.TotalSeconds:F1}s).");
        await Assertions.Expect(page.Locator(".page-content-skeleton")).ToHaveCountAsync(0, new() { Timeout = 500 });
        await Assertions.Expect(page.Locator(".panel-inline-error")).ToHaveCountAsync(0, new() { Timeout = 500 });

        var navPinned = await page.EvaluateAsync<bool>(
            """
            () => {
              const nav = document.querySelector('nav.bottom-nav');
              if (!nav) return false;
              const r = nav.getBoundingClientRect();
              return r.bottom <= window.innerHeight + 2 && r.top >= window.innerHeight - r.height - 4;
            }
            """);
        Assert.True(navPinned, "Bottom nav should sit on the viewport bottom, not mid-page.");
    }

    private static async Task<bool> WaitForPassportContentAsync(IPage page, int timeoutMs)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            var ready = await page.EvaluateAsync<bool>(
                """
                () => {
                  if (!document.querySelector('.passport-page')) return false;
                  if (document.querySelector('.page-content-skeleton')) return false;
                  const card = document.querySelector('.passport-layout__card--mobile, .passport-layout__sidebar');
                  const overview = document.querySelector('.passport-overview');
                  return !!(card || overview);
                }
                """);
            if (ready)
            {
                return true;
            }

            await page.WaitForTimeoutAsync(50);
        }

        return false;
    }
}
