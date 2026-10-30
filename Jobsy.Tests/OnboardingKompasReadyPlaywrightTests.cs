using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class OnboardingKompasReadyPlaywrightTests : IAsyncLifetime
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

    [Fact(Skip = "kreeftstrook verwijderd")]
    public async Task Start_kompas_hero_directions_and_ctas_at_required_viewports()
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
            factory.FinishReached = true;
            factory.CandidatePassportEnabled = true;
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

            await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start/kompas"));
            await page.Locator("[data-ob-rustig-screen=\"kompas\"]").WaitForAsync();

            var heroImg = page.GetByTestId("ob-rustig-hero-img");
            await heroImg.WaitForAsync();
            await Assertions.Expect(heroImg).ToHaveAttributeAsync("loading", "eager");
            await page.WaitForFunctionAsync(
                """() => { const img = document.querySelector('[data-testid="ob-rustig-hero-img"]'); return img && img.complete && img.naturalHeight > 0; }""");
            var heroBox = await heroImg.BoundingBoxAsync();
            Assert.NotNull(heroBox);
            Assert.InRange(heroBox!.Height, 72, mobile ? 220 : 300);

            var dirImg = page.Locator(".ob-rustig__kompas-dir-card img").First;
            var dirBox = await dirImg.BoundingBoxAsync();
            Assert.NotNull(dirBox);
            Assert.InRange(dirBox!.Height, 48, 185);

            await page.GetByTestId("onboarding-kompas-go-passport").ClickAsync();
            await page.WaitForURLAsync("**/candidate/paspoort**");
            await page.Locator(".passport-page").WaitForAsync(new() { Timeout = 120_000 });

            await page.GotoAsync(root + "/candidate/start/kompas");
            await page.GetByTestId("onboarding-kompas-complete").ClickAsync();
            await page.WaitForURLAsync("**/candidate/kompas**");

            await page.GotoAsync(root + "/candidate/start/kompas");
            await page.GetByTestId("onboarding-kompas-full-tests").ClickAsync();
            await page.WaitForURLAsync("**/candidate/kompas#kompas-full-tests**");
        }
    }
}
