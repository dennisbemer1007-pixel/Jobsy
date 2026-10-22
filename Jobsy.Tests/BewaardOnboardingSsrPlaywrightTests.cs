using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class BewaardOnboardingSsrPlaywrightTests : IAsyncLifetime
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
    [InlineData(false, 1440, 900)]
    [InlineData(true, 390, 844)]
    public async Task Bewaard_and_onboarding_start_happy_flow(bool mobile, int width, int height)
    {
        await using var factory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory();
        factory.WelcomeDismissed = false;
        factory.SavedStep = 1;
        factory.StoredProfile.FirstName = "Sam";
        factory.StoredProfile.LastName = "Tester";
        var root = factory.EnsureServerAddress();
        await using var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new() { Width = width, Height = height },
            HasTouch = mobile,
            IsMobile = mobile,
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);

        var sampleId = IllustratedPagesApiStub.SampleVacancyId.ToString("D");
        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/liked"));
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.GetByTestId("saved-page-ssr").WaitForAsync(new() { Timeout = 60_000 });
        var unsaveTestId = mobile ? "saved-unsave-row-" + sampleId : "saved-unsave-card-" + sampleId;
        var unsaveForm = page.GetByTestId(unsaveTestId);
        await unsaveForm.Locator("button").ClickAsync();
        await page.WaitForURLAsync(u => u.Contains("saved=1", StringComparison.OrdinalIgnoreCase), new() { Timeout = 30_000 });
        await page.ReloadAsync();
        await page.GetByTestId("saved-page-ssr").WaitForAsync();
        await Assertions.Expect(page.GetByTestId(unsaveTestId)).ToHaveCountAsync(0);

        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start"));
        await page.GetByTestId("onboarding-start-screen").WaitForAsync(new() { Timeout = 60_000 });
        var shotDir = "/opt/cursor/artifacts/screenshots";
        Directory.CreateDirectory(shotDir);
        var suffix = mobile ? "390" : "1440";
        await page.ScreenshotAsync(new() { Path = $"{shotDir}/onboarding-start-{suffix}.png", FullPage = true });

        await ClickChromeLinkAsync(page, "/candidate/profile");
        await page.GoBackAsync();
        await page.GetByTestId("onboarding-start-screen").WaitForAsync();

        await ClickChromeLinkAsync(page, "/privacy/data");
        await page.GoBackAsync();
        await page.GetByTestId("onboarding-start-screen").WaitForAsync();

        await page.GetByTestId("onboarding-begin").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=1**");
        await WaitStep1MountedAsync(page);
        await page.ScreenshotAsync(new() { Path = $"{shotDir}/onboarding-step-01-{suffix}.png", FullPage = true });

        if (!mobile)
        {
            foreach (var href in new[] { "?step=1", "?step=4", "?step=6", "?step=7", "?step=10" })
            {
                var side = page.Locator($"a.ob-wizard__side-item[href*=\"{href}\"]").First;
                await side.ClickAsync();
                await page.WaitForURLAsync($"**{href}**", new() { Timeout = 30_000 });
                await page.GotoAsync(root + "/candidate/start?step=1");
            }

            await WaitStep1MountedAsync(page);
        }
        else
        {
            await WaitStep1MountedAsync(page);
        }

        await ClickChromeLinkAsync(page, "/candidate/profile");
        await page.GotoAsync(root + "/candidate/start?step=1");
        await WaitStep1MountedAsync(page);

        await ClickChromeLinkAsync(page, "/privacy/data");
        await page.GotoAsync(root + "/candidate/start?step=1");
        await WaitStep1MountedAsync(page);

        var statusTiles = page.Locator(".work-status-chips .lobsy-choice-tile");
        var tileCount = await statusTiles.CountAsync();
        Assert.True(tileCount >= 1, "Step 1 should expose work-status tiles.");
        for (var i = 0; i < tileCount; i++)
        {
            var tile = statusTiles.Nth(i);
            await tile.ClickAsync(new() { Force = true });
            await Assertions.Expect(tile).ToHaveAttributeAsync("aria-pressed", "true");
        }

        await page.Locator("[data-ob-client=firstName]").FillAsync("Sam");
        await page.Locator("[data-ob-client=lastName]").FillAsync("Tester");
        await page.Locator("#onboarding-home-address").FillAsync("2671GB");
        await page.Locator(".address-suggestions__item").First.ClickAsync(new() { Force = true, Timeout = 15_000 });
        await page.Locator("[data-ob-client=birthDate]").FillAsync("12-03-1990");
        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=2**", new() { Timeout = 60_000 });
    }

    private static async Task WaitStep1MountedAsync(IPage page)
    {
        await page.GetByTestId("onboarding-step-1-illustrated").First.WaitForAsync();
        await page.WaitForFunctionAsync(
            "() => document.querySelector('[data-ob-client-step=\"1\"]')?.getAttribute('data-ob-client-mounted') === '1'");
    }

    private static async Task ClickChromeLinkAsync(IPage page, string hrefSuffix)
    {
        var link = page.Locator($".ob-rustig__actions a[href=\"{hrefSuffix}\"]").First;
        await link.ClickAsync();
        await page.WaitForURLAsync($"**{hrefSuffix}**", new() { Timeout = 30_000 });
    }
}
