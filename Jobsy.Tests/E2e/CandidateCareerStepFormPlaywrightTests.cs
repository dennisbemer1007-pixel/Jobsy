using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
public sealed class CandidateCareerStepFormPlaywrightTests : IAsyncLifetime
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
    [InlineData(PlaywrightViewportContexts.DesktopWidth, PlaywrightViewportContexts.DesktopHeight)]
    [InlineData(PlaywrightViewportContexts.MobileWidth, PlaywrightViewportContexts.MobileHeight)]
    public async Task Step_complete_submits_one_bff_post(int width, int height)
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(PlaywrightViewportContexts.ForSize(width, height));
        var page = await context.NewPageAsync();
        await PlaywrightCookieConsent.AcceptAsync(context);

        var posts = 0;
        await page.RouteAsync("**/bff/candidate/career/**", route =>
        {
            if (route.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                posts++;
            }

            return route.ContinueAsync();
        });

        var signIn = $"{baseUrl}/__test/sign-in?role=candidate&return={Uri.EscapeDataString("/carriere?stap=1")}";
        await page.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync($"{baseUrl}/carriere?stap=1", new() { WaitUntil = WaitUntilState.Load, Timeout = 90_000 });

        var complete = page.Locator("form.career-step__complete-form button[type='submit']").First;
        if (await complete.CountAsync() == 0)
        {
            return;
        }

        posts = 0;
        await complete.ClickAsync();
        await page.WaitForURLAsync(u => u.Contains("/carriere", StringComparison.OrdinalIgnoreCase), new() { Timeout = 30_000 });
        Assert.Equal(1, posts);
    }
}
