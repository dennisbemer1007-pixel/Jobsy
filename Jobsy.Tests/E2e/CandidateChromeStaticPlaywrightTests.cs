using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Candidate header chrome: primary nav and account menu must respond without Blazor click round-trips.
/// Runs on desktop and mobile (390px, touch).
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class CandidateChromeStaticPlaywrightTests : IAsyncLifetime
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
    [InlineData(PlaywrightViewportContexts.DesktopWidth, PlaywrightViewportContexts.DesktopHeight)]
    [InlineData(PlaywrightViewportContexts.MobileWidth, PlaywrightViewportContexts.MobileHeight)]
    public async Task Header_menus_and_primary_nav_respond_without_blazor_post_storm(int width, int height)
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(PlaywrightViewportContexts.ForSize(width, height));
        var page = await context.NewPageAsync();
        await PlaywrightCookieConsent.AcceptAsync(context);

        var blazorPosts = 0;
        await page.RouteAsync("**/_blazor/**", route =>
        {
            if (route.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                blazorPosts++;
            }

            return route.ContinueAsync();
        });

        var signIn = $"{baseUrl}/__test/sign-in?role=candidate&return={Uri.EscapeDataString("/candidate/settings?pane=mail")}";
        await page.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync($"{baseUrl}/candidate/settings?pane=mail", new() { WaitUntil = WaitUntilState.Load, Timeout = 90_000 });

        var postsBefore = blazorPosts;

        var accountToggle = page.Locator(".account-menu [data-menu-trigger]").First;
        for (var i = 0; i < 5; i++)
        {
            await accountToggle.ClickAsync(new() { Timeout = 8_000 });
            await page.WaitForFunctionAsync(
                "() => document.documentElement.getAttribute('data-header-menu') === 'account'",
                null,
                new() { Timeout = 3_000 });
            await page.Keyboard.PressAsync("Escape");
            await page.WaitForFunctionAsync(
                "() => !document.documentElement.hasAttribute('data-header-menu')",
                null,
                new() { Timeout = 3_000 });
        }

        var navLink = width >= 500
            ? page.Locator(".candidate-header-nav a[href='/carriere']").First
            : page.Locator("nav.bottom-nav a[href='/carriere']").First;
        await navLink.ClickAsync(new() { Timeout = 8_000 });
        await page.WaitForURLAsync(
            u => u.Contains("/carriere", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 10_000 });
        await page.Locator(".career-page, .career-card").First.WaitForAsync(new() { Timeout = 15_000 });

        var postsAfterNav = blazorPosts - postsBefore;
        Assert.True(
            postsAfterNav <= 2,
            $"Expected at most 2 Blazor POSTs during menu/nav interaction at {width}px, saw {postsAfterNav}.");
    }
}
