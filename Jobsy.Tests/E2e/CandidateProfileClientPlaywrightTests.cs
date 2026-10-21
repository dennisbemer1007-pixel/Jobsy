using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
public sealed class CandidateProfileClientPlaywrightTests : IAsyncLifetime
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
        _web = new IllustratedPagesWebFactory(accLikeProductionCsp: true, candidatePassportOff: true);
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
    public async Task Personal_inputs_stay_client_side_until_section_save_post(int width, int height)
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(PlaywrightViewportContexts.ForSize(width, height));
        var page = await context.NewPageAsync();
        var apiHostGuard = new PlaywrightAccLikeRegression.BrowserApiHostCounter();
        apiHostGuard.Attach(page);
        await PlaywrightCookieConsent.AcceptAsync(context);

        var sectionPosts = 0;
        await page.RouteAsync("**/candidate/profile/sections/**", route =>
        {
            if (route.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                sectionPosts++;
            }

            return route.ContinueAsync();
        });

        var profileUrl = "/candidate/profile?tab=profile&profileSection=personal";
        var signIn = $"{baseUrl}/__test/sign-in?role=candidate&return={Uri.EscapeDataString(profileUrl)}";
        await page.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync($"{baseUrl}{profileUrl}", new() { WaitUntil = WaitUntilState.Load, Timeout = 90_000 });

        await page.Locator("[data-candidate-profile]").WaitForAsync(new() { Timeout = 15_000 });
        var firstName = page.Locator("[data-testid='profile-first-name']").First;
        await firstName.WaitForAsync(new() { Timeout = 15_000 });

        await firstName.ClickAsync(new() { Force = width < 500 });
        await firstName.PressSequentiallyAsync("abc", new() { Delay = 25 });
        await page.WaitForTimeoutAsync(120);

        Assert.Equal(0, sectionPosts);

        await page.Locator("form[data-profile-section-form='personal'] button[type='submit']").ClickAsync(new() { Force = width < 500 });
        await page.WaitForURLAsync(
            u => u.Contains("profileSaved=1", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 30_000 });

        Assert.Equal(1, sectionPosts);
        await page.GetByText("Profiel opgeslagen.").WaitForAsync(new() { Timeout = 10_000 });
        apiHostGuard.AssertZero($"candidate profile personal save at {width}px");
    }
}
