using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class OnboardingSteps123MockupPlaywrightTests : IAsyncLifetime
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
    public async Task Steps_1_through_3_mockup_happy_flow(bool mobile, int width, int height)
    {
        await using var factory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory();
        factory.WelcomeDismissed = true;
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

        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start?step=1"));
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await WaitStepMountedAsync(page, 1);
        var shotDir = "/opt/cursor/artifacts/screenshots";
        Directory.CreateDirectory(shotDir);
        var suffix = mobile ? "390" : "1440";
        await page.ScreenshotAsync(new() { Path = $"{shotDir}/mockup-step-01-{suffix}.png", FullPage = true });

        await Assertions.Expect(page.GetByText("Optioneel. Je mag er meer dan één kiezen.")).ToBeVisibleAsync();

        await ClickChromeLinkAsync(page, root, "/candidate/profile");
        await ClickChromeLinkAsync(page, root, "/privacy/data");

        if (!mobile)
        {
            foreach (var href in new[] { "?step=4", "?step=6" })
            {
                await page.Locator($"a.ob-wizard__side-item[href*=\"{href}\"]").First.ClickAsync();
                await page.WaitForURLAsync($"**{href}**", new() { Timeout = 30_000 });
                await page.GotoAsync(root + "/candidate/start?step=1");
                await WaitStepMountedAsync(page, 1);
            }
        }

        var statusTiles = page.Locator(".work-status-chips .lobsy-choice-tile");
        var tileCount = await statusTiles.CountAsync();
        Assert.True(tileCount >= 1);
        for (var i = 0; i < tileCount; i++)
        {
            var tile = statusTiles.Nth(i);
            var statusPut = WaitProfilePutAsync(page);
            await tile.ClickAsync(new() { Force = true });
            await statusPut;
            await Assertions.Expect(tile).ToHaveAttributeAsync("aria-pressed", "true");
        }

        var studentTile = page.Locator(".work-status-chip[data-ob-code=student]");
        await Assertions.Expect(studentTile).ToHaveAttributeAsync("aria-pressed", "true");

        await page.Locator("[data-ob-client=firstName]").FillAsync("Sam");
        await page.Locator("[data-ob-client=lastName]").FillAsync("Mockup");
        await page.Locator("#onboarding-home-address").FillAsync("2671GB");
        await page.Locator(".address-suggestions__item").First.ClickAsync(new() { Force = true, Timeout = 15_000 });
        await page.Locator("[data-ob-client=birthDate]").FillAsync("12-03-1990");
        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=2**", new() { Timeout = 60_000 });
        await WaitStepMountedAsync(page, 2);
        await page.ScreenshotAsync(new() { Path = $"{shotDir}/mockup-step-02-{suffix}.png", FullPage = true });

        var presetTiles = page.Locator("[data-ob-client=presetGrid] .lobsy-choice-tile");
        var presetCount = await presetTiles.CountAsync();
        for (var i = 0; i < presetCount; i++)
        {
            var tile = presetTiles.Nth(i);
            var presetPut = WaitProfilePutAsync(page);
            await tile.ClickAsync(new() { Force = true });
            await presetPut;
        }

        var weekendTile = page.Locator("[data-ob-client=presetGrid] .lobsy-choice-tile[data-ob-code=weekend]");
        if (await weekendTile.GetAttributeAsync("aria-pressed") != "true")
        {
            var weekendOnPut = WaitProfilePutAsync(page);
            await weekendTile.ClickAsync(new() { Force = true });
            await weekendOnPut;
        }

        for (var i = 0; i < presetCount; i++)
        {
            var tile = presetTiles.Nth(i);
            var code = await tile.GetAttributeAsync("data-ob-code");
            if (string.Equals(code, "weekend", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (await tile.GetAttributeAsync("aria-pressed") != "true")
            {
                continue;
            }

            var offPut = WaitProfilePutAsync(page);
            await tile.ClickAsync(new() { Force = true });
            await offPut;
        }

        if (await weekendTile.GetAttributeAsync("aria-pressed") != "true")
        {
            var weekendPut = WaitProfilePutAsync(page);
            await weekendTile.ClickAsync(new() { Force = true });
            await weekendPut;
        }

        await Assertions.Expect(weekendTile).ToHaveAttributeAsync("aria-pressed", "true");

        await page.Locator("[data-ob-client=hoursMin]").EvaluateAsync("el => { el.value = '16'; el.dispatchEvent(new Event('input', { bubbles: true })); }");
        await page.Locator("[data-ob-client=hoursMax]").EvaluateAsync("el => { el.value = '32'; el.dispatchEvent(new Event('input', { bubbles: true })); }");

        var matrixCells = page.Locator(".availability-matrix__cell--btn");
        var matrixCount = await matrixCells.CountAsync();
        Assert.True(matrixCount >= 1);
        await matrixCells.First.ClickAsync(new() { Force = true });

        await page.Locator("[data-ob-client=startFromDate]").CheckAsync();
        var dateInput = page.Locator("[data-ob-client=availableFrom]");
        var datePut = WaitProfilePutAsync(page);
        await dateInput.FillAsync("01-06-2027");
        await dateInput.BlurAsync();
        await datePut;

        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=3**", new() { Timeout = 60_000 });
        await WaitStepMountedAsync(page, 3);

        await page.GetByTestId("onboarding-back").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=2**");
        await WaitStepMountedAsync(page, 2);
        await Assertions.Expect(dateInput).ToHaveValueAsync("01-06-2027");

        await page.ReloadAsync();
        await WaitStepMountedAsync(page, 2);
        await Assertions.Expect(dateInput).ToHaveValueAsync("01-06-2027");

        var customPut = WaitProfilePutAsync(page);
        await page.Locator("[data-ob-client=presetGrid] .lobsy-choice-tile[data-ob-code=custom]").ClickAsync(new() { Force = true });
        await customPut;

        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=3**", new() { Timeout = 60_000 });
        await WaitStepMountedAsync(page, 3);
        await page.ScreenshotAsync(new() { Path = $"{shotDir}/mockup-step-03-{suffix}.png", FullPage = true });

        var transportTiles = page.Locator("[data-ob-client=transportGrid] .lobsy-choice-tile");
        var transportCount = await transportTiles.CountAsync();
        for (var i = 0; i < transportCount; i++)
        {
            await transportTiles.Nth(i).ClickAsync(new() { Force = true });
        }

        var travelTiles = page.Locator("[data-ob-client=travelGrid] .lobsy-choice-tile");
        var travelCount = await travelTiles.CountAsync();
        for (var i = 0; i < travelCount; i++)
        {
            await travelTiles.Nth(i).ClickAsync(new() { Force = true });
        }

        await page.Locator("[data-ob-client=showCustomTravel]").ClickAsync();
        var travelPut = WaitProfilePutAsync(page);
        await page.Locator("[data-ob-client=customTravel]").FillAsync("55");
        await page.Locator("[data-ob-client=customTravel]").BlurAsync();
        await travelPut;

        await page.GetByTestId("onboarding-back").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=2**");
        await WaitStepMountedAsync(page, 2);
        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=3**");
        await WaitStepMountedAsync(page, 3);
        await Assertions.Expect(page.Locator("[data-ob-client=customTravel]")).ToHaveValueAsync("55");

        await page.ReloadAsync();
        await WaitStepMountedAsync(page, 3);
        await Assertions.Expect(page.Locator("[data-ob-client=customTravel]")).ToHaveValueAsync("55");

        await page.GetByTestId("onboarding-back").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=2**");
        await WaitStepMountedAsync(page, 2);
        await page.GetByTestId("onboarding-back").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=1**");
        await WaitStepMountedAsync(page, 1);
        await Assertions.Expect(page.Locator(".work-status-chip[data-ob-code=student]")).ToHaveAttributeAsync("aria-pressed", "true");
        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=2**");
    }

    private static async Task WaitStepMountedAsync(IPage page, int step)
    {
        await page.Locator($"[data-ob-client-step=\"{step}\"]").WaitForAsync(new() { Timeout = 90_000 });
        await page.WaitForFunctionAsync(
            $"(s) => document.querySelector('[data-ob-client-step=\"' + s + '\"]')?.getAttribute('data-ob-client-mounted') === '1'",
            step);
    }

    private static Task WaitProfilePutAsync(IPage page)
        => page.WaitForResponseAsync(
            r => r.Url.Contains("/api/onboarding-client/", StringComparison.OrdinalIgnoreCase)
                 && r.Request.Method is "PUT"
                 && r.Ok,
            new() { Timeout = 20_000 });

    private static async Task ClickChromeLinkAsync(IPage page, string root, string hrefSuffix)
    {
        var link = page.Locator($".ob-rustig__actions a[href=\"{hrefSuffix}\"]").First;
        await link.ClickAsync();
        await page.WaitForURLAsync($"**{hrefSuffix}**", new() { Timeout = 30_000 });
        await page.GotoAsync(root + "/candidate/start?step=1");
        await WaitStepMountedAsync(page, 1);
    }
}
