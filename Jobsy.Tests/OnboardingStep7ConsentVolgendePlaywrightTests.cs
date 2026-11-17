using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Regression (#1472): step 7 consent + Volgende must navigate (not hang on ssrMiniSaveChain).
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class OnboardingStep7ConsentVolgendePlaywrightTests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
    }

    [Theory]
    [InlineData(390)]
    [InlineData(1440)]
    public async Task Step7_consent_checkbox_then_volgende_reaches_first_mini_question(int width)
    {
        await using var factory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory
        {
            WelcomeDismissed = true,
            SavedStep = 7,
        };
        var root = factory.EnsureServerAddress();
        await using var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new() { Width = width, Height = width <= 400 ? 844 : 900 },
            HasTouch = width <= 400,
            IsMobile = width <= 400,
            Locale = "nl-NL",
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);

        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start?step=7"));
        await page.Locator("[data-ob-client-step=\"7\"]").WaitForAsync();

        var consent = page.Locator("[data-testid=onboarding-consent]");
        Assert.True(await consent.CountAsync() > 0, "Consent checkbox must be visible on step 7");
        await consent.CheckAsync();

        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=7&q=1**", new() { Timeout = 15_000 });
        await Assertions.Expect(page.Locator(".ob-client-mini-q")).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task Step7_consent_volgende_then_keyboard_enter_also_advances()
    {
        await using var factory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory
        {
            WelcomeDismissed = true,
            SavedStep = 7,
        };
        var root = factory.EnsureServerAddress();
        await using var context = await _browser!.NewContextAsync(MobileViewportLayoutAssertSupport.MobileContext(390));
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);

        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start?step=7"));
        await page.Locator("[data-ob-client-step=\"7\"]").WaitForAsync();
        await page.Locator("[data-testid=onboarding-consent]").CheckAsync();
        await page.Locator("[data-testid=onboarding-consent]").PressAsync("Enter");
        await page.WaitForURLAsync("**/candidate/start?step=7&q=1**", new() { Timeout = 15_000 });
    }

    [Fact]
    public async Task Step7_consent_through_mini_questions_reaches_step_8()
    {
        await using var factory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory
        {
            WelcomeDismissed = true,
            SavedStep = 7,
        };
        var root = factory.EnsureServerAddress();
        await using var context = await _browser!.NewContextAsync(MobileViewportLayoutAssertSupport.MobileContext(390));
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);

        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start?step=7"));
        await page.Locator("[data-ob-client-step=\"7\"]").WaitForAsync();
        await page.Locator("[data-testid=onboarding-consent]").CheckAsync();
        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=7&q=1**");

        for (var q = 1; q <= 5; q++)
        {
            await page.WaitForURLAsync($"**/candidate/start?step=7&q={q}**");
            var score = q == 5 ? 5 : 4;
            await page.Locator("button[name=\"obMiniPick\"]").Filter(new() { HasText = score.ToString() }).First.ClickAsync();
            if (q < 5)
            {
                await page.WaitForURLAsync($"**/candidate/start?step=7&q={q + 1}**");
            }
        }

        await page.WaitForURLAsync("**/candidate/start?step=8**", new() { Timeout = 30_000 });
        await page.Locator("[data-ob-client-step=\"8\"]").WaitForAsync();
    }
}