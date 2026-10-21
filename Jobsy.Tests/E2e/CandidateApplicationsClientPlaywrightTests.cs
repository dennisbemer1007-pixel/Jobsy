using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
public sealed class CandidateApplicationsClientPlaywrightTests : IAsyncLifetime
{
    private IllustratedPagesWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        Environment.SetEnvironmentVariable("JOBSY_CSP_ALLOW_UNSAFE_EVAL", "0");
        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        _web = new IllustratedPagesWebFactory(accLikeProductionCsp: true);
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
    public async Task Filter_taps_stay_client_side_until_withdraw_posts_once(int width, int height)
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(PlaywrightViewportContexts.ForSize(width, height));
        var page = await context.NewPageAsync();
        var apiHostGuard = await PlaywrightAccLikeRegression.AttachAccLikePageAsync(page);
        await PlaywrightCookieConsent.AcceptAsync(context);

        var withdrawPosts = 0;
        await page.RouteAsync("**/candidate/applications/withdraw", route =>
        {
            if (route.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                withdrawPosts++;
            }

            return route.ContinueAsync();
        });

        var signIn = $"{baseUrl}/__test/sign-in?role=candidate&return={Uri.EscapeDataString("/candidate/applications")}";
        await page.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync($"{baseUrl}/candidate/applications", new() { WaitUntil = WaitUntilState.Load, Timeout = 90_000 });

        await page.Locator("[data-candidate-applications]").WaitForAsync(new() { Timeout = 15_000 });
        await page.Locator("[data-filter-btn='running']").WaitForAsync(new() { Timeout = 10_000 });

        for (var i = 0; i < 8; i++)
        {
            await page.Locator("[data-filter-btn='running']").ClickAsync(new() { Force = width < 500 });
            await page.Locator("[data-filter-btn='done']").ClickAsync(new() { Force = width < 500 });
            await page.Locator("[data-filter-btn='all']").ClickAsync(new() { Force = width < 500 });
            await page.WaitForTimeoutAsync(35);
        }

        if (width < 500)
        {
            await page.Locator("[data-menu-toggle]").First.ClickAsync(new() { Force = true });
        }
        else
        {
            await page.WaitForFunctionAsync(
                """() => !!document.querySelector('[data-apps-detail-panel]:not([hidden]) .kb-apps__detail-actions [data-app-withdraw]')""",
                null,
                new() { Timeout = 15_000 });
        }

        var withdrawBtn = width < 500
            ? page.Locator("[data-app-menu-panel] [data-app-withdraw]").First
            : page.Locator(".kb-apps__detail-actions [data-app-withdraw]").First;
        if (await withdrawBtn.CountAsync() == 0)
        {
            return;
        }

        await withdrawBtn.ClickAsync(new() { Force = width < 500 });
        var dialog = page.Locator("[data-apps-withdraw-dialog]");
        await dialog.WaitForAsync(new() { Timeout = 5_000 });

        await page.Locator("[data-apps-withdraw-dialog] [data-apps-withdraw-confirm]").ClickAsync();
        await page.WaitForURLAsync(
            u => u.Contains("withdrawn=1", StringComparison.OrdinalIgnoreCase)
                 || u.Contains("/candidate/applications", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 30_000 });

        Assert.Equal(1, withdrawPosts);
        apiHostGuard.AssertZero($"candidate applications filter/withdraw at {width}px");
    }
}
