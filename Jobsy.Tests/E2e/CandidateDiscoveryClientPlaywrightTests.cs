using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
public sealed class CandidateDiscoveryClientPlaywrightTests : IAsyncLifetime
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
    public async Task Step1_taps_stay_client_side_until_next_puts_once(int width, int height)
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        IllustratedPagesApiStub.ResetProfilePutCount();

        await using var context = await _browser!.NewContextAsync(PlaywrightViewportContexts.ForSize(width, height));
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        var signIn = $"{baseUrl}/__test/sign-in?role=candidate&return={Uri.EscapeDataString("/candidate/ontdekkingsreis?stap=1")}";
        await page.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync($"{baseUrl}/candidate/ontdekkingsreis?stap=1", new() { WaitUntil = WaitUntilState.Load, Timeout = 90_000 });

        await page.Locator("[data-ob-client-step=\"1\"]").WaitForAsync(new() { Timeout = 60_000 });
        await WaitForClientStep1MountedAsync(page);
        await FillValidClientStep1Async(page);

        var scholier = page.Locator(".work-status-chips .lobsy-choice-tile").First;
        for (var i = 0; i < 12; i++)
        {
            await scholier.ClickAsync(new() { Force = width < 500 });
            await page.WaitForTimeoutAsync(40);
        }

        Assert.Equal(0, IllustratedPagesApiStub.ProfilePutCount);

        await page.GetByTestId("onboarding-next").ClickAsync(new() { Force = width < 500, Timeout = 15_000 });
        await page.Locator("[data-discovery-client-step=\"2\"]").WaitForAsync(new() { Timeout = 60_000 });

        Assert.Equal(1, IllustratedPagesApiStub.ProfilePutCount);
    }

    private static async Task WaitForClientStep1MountedAsync(IPage page)
    {
        await page.WaitForFunctionAsync(
            """
            () => typeof window.jobsyOnboardingClient !== 'undefined'
                && document.querySelector('[data-ob-client-step="1"] [data-ob-client=workStatusGrid] button')
            """,
            null,
            new() { Timeout = 60_000 });
    }

    private static async Task FillValidClientStep1Async(IPage page)
    {
        await page.Locator("#onboarding-field-firstname input").FillAsync("Alex");
        await page.Locator("#onboarding-field-lastname input").FillAsync("Jansen");
        await page.Locator("#onboarding-home-address").FillAsync("2671 GB Naaldwijk");
        await page.WaitForTimeoutAsync(500);
        var suggestion = page.Locator("button.address-suggestions__item").First;
        await suggestion.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });
        await suggestion.ClickAsync(new() { Timeout = 5_000 });

        var birth = page.Locator("[data-ob-client=birthDate]");
        await birth.FillAsync("15-06-2000");
        await birth.BlurAsync();
        await page.WaitForTimeoutAsync(200);
    }
}
