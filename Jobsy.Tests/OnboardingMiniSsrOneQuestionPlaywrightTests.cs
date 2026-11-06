using Jobsy.Tests.E2e;
using Jobsy.Web.Onboarding;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class OnboardingMiniSsrOneQuestionPlaywrightTests : IAsyncLifetime
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
        OnboardingSsrE2eOverrides.DreamTitles = null;
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
    }

    [Fact]
    public async Task Step2_opslaan_and_step7_one_question_ssr_flow_at_required_viewports()
    {
        var viewports = new (bool Mobile, int Width, int Height)[]
        {
            (false, 1024, 700),
            (false, 1280, 720),
            (true, 390, 844),
        };

        foreach (var (mobile, width, height) in viewports)
        {
            await using var factory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory();
            factory.WelcomeDismissed = true;
            factory.SavedStep = 7;
            var root = factory.EnsureServerAddress();
            await using var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new() { Width = width, Height = height },
                HasTouch = mobile,
                IsMobile = mobile,
                Locale = "nl-NL",
            });
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(90_000);

            await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start?step=2"));
            await page.Locator("[data-ob-client-step=\"2\"]").WaitForAsync();
            await page.Locator("input[name=\"obStartMode\"][value=\"from\"]").CheckAsync();
            var save = page.GetByTestId("onboarding-start-save");
            var saveBox = await save.BoundingBoxAsync();
            var next = page.GetByTestId("onboarding-next");
            var nextBox = await next.BoundingBoxAsync();
            Assert.NotNull(saveBox);
            Assert.NotNull(nextBox);
            Assert.InRange(saveBox!.Height, 43, 46);
            Assert.True(nextBox!.Y - (saveBox.Y + saveBox.Height) >= 16, $"[{width}px] Start block should sit at least 16px above Volgende");

            await page.GotoAsync(root + "/candidate/start?step=7");
            await page.Locator("[data-ob-client-step=\"7\"]").WaitForAsync();
            await Assertions.Expect(page.Locator(".ob-client-mini-q")).ToHaveCountAsync(1);

            await page.Locator("button[name=\"obMiniPick\"]").Filter(new() { HasText = "3" }).First.ClickAsync();
            await page.WaitForURLAsync("**/candidate/start?step=7&q=2**");
            await Assertions.Expect(page.Locator("[data-testid=onboarding-mini-likert].is-selected")).ToHaveCountAsync(0);

            await page.GetByTestId("onboarding-back").ClickAsync();
            await page.WaitForURLAsync("**/candidate/start?step=7&q=1**");
            await Assertions.Expect(page.Locator("[data-testid=onboarding-mini-likert].is-selected")).ToHaveCountAsync(1);
            var pressed = await page.Locator("[data-testid=onboarding-mini-likert].is-selected").GetAttributeAsync("aria-pressed");
            Assert.Equal("true", pressed);

            for (var q = 1; q <= 5; q++)
            {
                await page.WaitForURLAsync($"**/candidate/start?step=7&q={q}**");
                await Assertions.Expect(page.Locator(".ob-client-mini-q")).ToHaveCountAsync(1);
                var score = q == 5 ? 5 : 4;
                await page.Locator("button[name=\"obMiniPick\"]").Filter(new() { HasText = score.ToString() }).First.ClickAsync();
                if (q < 5)
                {
                    await page.WaitForURLAsync($"**/candidate/start?step=7&q={q + 1}**");
                }
            }

            await page.WaitForURLAsync("**/candidate/start?step=8**");
            await page.Locator("[data-ob-client-step=\"8\"]").WaitForAsync();
            await Assertions.Expect(page.Locator(".ob-client-mini-q")).ToHaveCountAsync(1);

            if (!mobile)
            {
                var backBox = await page.GetByTestId("onboarding-back").BoundingBoxAsync();
                var nextStep8 = await page.GetByTestId("onboarding-next").BoundingBoxAsync();
                Assert.NotNull(backBox);
                Assert.NotNull(nextStep8);
                Assert.True(nextStep8!.X > backBox!.X + backBox.Width, "Volgende should stay to the right of Terug on step 8");
            }
        }
    }
}
