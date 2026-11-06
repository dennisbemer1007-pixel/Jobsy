using Jobsy.Infrastructure.Data;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
public sealed class CandidateBottomNavAfterMatchPlaywrightTests : IAsyncLifetime
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
    [InlineData(390)]
    [InlineData(1440)]
    public async Task After_match_messages_marks_sollicitaties_active_in_bottom_nav(int width)
    {
        var appId = AfterMatchDemoIds.ScheduledApp;
        var path =
            $"/candidate/applications/{appId:D}/after-match?panel=messages";
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = 900 },
            IsMobile = width < 600,
            IgnoreHTTPSErrors = true,
        });

        var page = await context.NewPageAsync();
        await PlaywrightCookieConsent.AcceptAsync(context);
        var signIn = $"{baseUrl}/__test/sign-in?role=candidate&return={Uri.EscapeDataString(path)}";
        await page.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync($"{baseUrl}{path}", new() { WaitUntil = WaitUntilState.Load, Timeout = 90_000 });

        await page.WaitForSelectorAsync("[data-testid=candidate-bottom-nav-ssr]", new() { Timeout = 30_000 });

        var activeOk = await page.EvaluateAsync<bool>(
            """
            () => {
              const nav = document.querySelector('[data-testid=candidate-bottom-nav-ssr]');
              if (!nav) return false;
              const link = nav.querySelector('a.bottom-nav__item[href="/candidate/applications"]');
              if (!link) return false;
              return link.classList.contains('is-active')
                && link.getAttribute('aria-current') === 'page';
            }
            """);

        Assert.True(activeOk, "Sollicitaties bottom nav item must stay active on after-match messages.");
    }
}
