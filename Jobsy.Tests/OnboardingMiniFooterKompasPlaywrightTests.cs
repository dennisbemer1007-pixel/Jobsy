using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class OnboardingMiniFooterKompasPlaywrightTests : IAsyncLifetime
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

    [Fact]
    public async Task Mini_footer_next_kompas_and_double_submit_at_required_viewports()
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
            factory.FinishReached = true;
            factory.StubMatchingVacancyCount = 12;
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

            await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start?step=7&q=1"));
            await page.Locator("[data-ob-client-step=\"7\"]").WaitForAsync();
            await page.WaitForTimeoutAsync(1000);

            var back = page.GetByTestId("onboarding-back");
            var later = page.GetByTestId("onboarding-tests-later");
            var next = page.GetByTestId("onboarding-next");
            var backBox = await back.BoundingBoxAsync();
            var laterBox = await later.BoundingBoxAsync();
            var nextBox = await next.BoundingBoxAsync();
            Assert.NotNull(backBox);
            Assert.NotNull(laterBox);
            Assert.NotNull(nextBox);
            Assert.True(backBox!.X <= laterBox!.X + 1, $"[{width}px] Terug should stay left of Later doen");
            Assert.True(laterBox.X >= backBox.X, $"[{width}px] Later doen should sit after Terug");
            Assert.True(nextBox!.X > laterBox.X + laterBox.Width - 1, $"[{width}px] Volgende should stay on the right");

            await page.Locator("button[name=\"obMiniPick\"]").Filter(new() { HasText = "3" }).First.DblClickAsync();
            await page.WaitForURLAsync("**/candidate/start?step=7&q=2**");
            await Assertions.Expect(page.Locator(".ob-client-mini-q")).ToHaveCountAsync(1);

            await page.GotoAsync(root + "/candidate/start?step=7&q=1");
            for (var q = 1; q <= 3; q++)
            {
                await page.WaitForURLAsync($"**/candidate/start?step=7&q={q}**");
                await page.Locator("button[name=\"obMiniPick\"]").Filter(new() { HasText = "3" }).First.ClickAsync();
            }

            await page.WaitForURLAsync("**/candidate/start?step=7&q=4**");
            await page.GetByTestId("onboarding-next").ClickAsync();
            await page.WaitForURLAsync("**/candidate/start?step=7&q=5**");

            await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start/kompas"));
            await page.Locator("[data-ob-rustig-screen=\"kompas\"]").WaitForAsync();
            await Assertions.Expect(page.GetByTestId("onboarding-rustig-kompas")).ToBeVisibleAsync();
            await Assertions.Expect(page.Locator(".ob-rustig__kompas-test-card")).ToHaveCountAsync(4);
            await Assertions.Expect(page.Locator(".ob-rustig__kompas-dir-card")).ToHaveCountAsync(3);
            await Assertions.Expect(page.GetByTestId("onboarding-finale-match-bar")).ToBeVisibleAsync();
            await Assertions.Expect(page.GetByTestId("onboarding-finale-now-cards")).ToBeVisibleAsync();

            await Assertions.Expect(page.GetByTestId("onboarding-kompas-go-passport")).ToBeVisibleAsync();
            await page.GetByTestId("onboarding-kompas-complete").ClickAsync();
            await page.WaitForURLAsync("**/candidate/kompas**");
            await page.GetByTestId("candidate-kompas-ssr").WaitForAsync();

            await page.GotoAsync(root + "/candidate/start/kompas");
            await page.GetByTestId("onboarding-kompas-full-tests").ClickAsync();
            await page.WaitForURLAsync("**/candidate/kompas#kompas-full-tests**");

            await page.GotoAsync(root + "/candidate/start/kompas");
            await page.GetByTestId("onboarding-finale-match-bar").ClickAsync();
            await page.WaitForURLAsync("**/banenkaart**");
        }
    }
}
