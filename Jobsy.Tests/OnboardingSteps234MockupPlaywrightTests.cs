using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class OnboardingSteps234MockupPlaywrightTests : IAsyncLifetime
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
    [InlineData(false, 1280, 900)]
    [InlineData(true, 390, 844)]
    public async Task Steps_2_through_4_mockup_happy_flow(bool mobile, int width, int height)
    {
        await using var factory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory();
        factory.WelcomeDismissed = true;
        factory.StubNearbyVacancyCount = mobile ? 0 : 12;
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
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await WaitStepMountedAsync(page, 2);

        var shotDir = "/opt/cursor/artifacts/screenshots";
        Directory.CreateDirectory(shotDir);
        var suffix = mobile ? "390" : "1280";
        await page.ScreenshotAsync(new() { Path = $"{shotDir}/mockup-step-02-{suffix}.png", FullPage = true });

        if (!mobile)
        {
            await Assertions.Expect(page.Locator(".ob-rustig__rail")).ToHaveCountAsync(1);
        }

        var presetTiles = page.Locator("[data-ob-client=presetGrid] .lobsy-choice-tile");
        var presetCount = await presetTiles.CountAsync();
        Assert.True(presetCount >= 1);
        for (var i = 0; i < presetCount; i++)
        {
            var put = WaitStepPutAsync(page, 2);
            await presetTiles.Nth(i).ClickAsync(new() { Force = true });
            await put;
        }

        var matrixCell = page.Locator(".availability-matrix__cell--btn").First;
        var matrixPut = WaitStepPutAsync(page, 2);
        await matrixCell.ClickAsync(new() { Force = true });
        await matrixPut;

        await page.Locator("[data-ob-client=startFromDate]").CheckAsync();
        var datePut = WaitStepPutAsync(page, 2);
        await page.Locator("[data-ob-client=availableFrom]").FillAsync("15-08-2027");
        await page.Locator("[data-ob-client=availableFrom]").BlurAsync();
        await datePut;

        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=3**", new() { Timeout = 60_000 });
        await WaitStepMountedAsync(page, 3);
        await page.ScreenshotAsync(new() { Path = $"{shotDir}/mockup-step-03-{suffix}.png", FullPage = true });

        var ebike = page.Locator("[data-ob-client=transportGrid] .lobsy-choice-tile[data-ob-code=\"E-bike\"]");
        var ebikePut = WaitStepPutAsync(page, 3);
        await ClickTransportTileAsync(ebike, mobile);
        await ebikePut;
        await Assertions.Expect(ebike).ToHaveAttributeAsync("aria-pressed", "true");
        Assert.Contains(
            "E-bike",
            factory.StoredProfile.Preferences?.PreferredTransport ?? "",
            StringComparison.OrdinalIgnoreCase);

        await page.ReloadAsync();
        await WaitStepMountedAsync(page, 3);
        await Assertions.Expect(ebike).ToHaveAttributeAsync("aria-pressed", "true");

        foreach (var code in new[] { "Fiets", "Auto", "OV", "Lopend" })
        {
            var tile = page.Locator("[data-ob-client=transportGrid] .lobsy-choice-tile[data-ob-code=\"" + code + "\"]");
            var tPut = WaitStepPutAsync(page, 3);
            await ClickTransportTileAsync(tile, mobile);
            await tPut;
            await Assertions.Expect(tile).ToHaveAttributeAsync("aria-pressed", "true");
        }

        await Assertions.Expect(ebike).ToHaveAttributeAsync("aria-pressed", "true");
        await page.ReloadAsync();
        await WaitStepMountedAsync(page, 3);
        foreach (var code in new[] { "Fiets", "E-bike", "Auto", "OV", "Lopend" })
        {
            var tile = page.Locator("[data-ob-client=transportGrid] .lobsy-choice-tile[data-ob-code=\"" + code + "\"]");
            await Assertions.Expect(tile).ToHaveAttributeAsync("aria-pressed", "true");
        }

        var travel30 = page.Locator("[data-ob-client=travelGrid] .lobsy-choice-tile[data-ob-travel=\"30\"]");
        var travelPut = WaitStepPutAsync(page, 3);
        await ClickTransportTileAsync(travel30, mobile);
        await travelPut;

        await page.GetByTestId("onboarding-back").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=2**");
        await WaitStepMountedAsync(page, 2);
        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=3**");
        await WaitStepMountedAsync(page, 3);
        foreach (var code in new[] { "Fiets", "E-bike", "Auto", "OV", "Lopend" })
        {
            var tile = page.Locator("[data-ob-client=transportGrid] .lobsy-choice-tile[data-ob-code=\"" + code + "\"]");
            await Assertions.Expect(tile).ToHaveAttributeAsync("aria-pressed", "true");
        }

        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=4**", new() { Timeout = 60_000 });
        await WaitStepMountedAsync(page, 4);
        await page.ScreenshotAsync(new() { Path = $"{shotDir}/mockup-step-04-{suffix}.png", FullPage = true });

        var noWork = page.Locator("[data-ob-client=noWork]");
        await noWork.CheckAsync();
        await noWork.UncheckAsync();

        var addJob = page.GetByTestId("onboarding-add-job");
        if (await addJob.CountAsync() > 0)
        {
            await addJob.ClickAsync();
        }

        await page.GetByTestId("onboarding-back").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=3**");
        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=4**");
    }

    private static async Task ClickTransportTileAsync(ILocator tile, bool mobile)
    {
        if (mobile)
        {
            await tile.TapAsync();
        }
        else
        {
            await tile.ClickAsync(new() { Force = true });
        }
    }

    private static Task WaitStepPutAsync(IPage page, int step)
        => page.WaitForResponseAsync(
            r => r.Url.Contains($"/api/onboarding-client/steps/{step}", StringComparison.OrdinalIgnoreCase)
                 && r.Request.Method is "PUT"
                 && r.Ok,
            new() { Timeout = 20_000 });

    private static async Task WaitStepMountedAsync(IPage page, int step)
    {
        await page.Locator($"[data-ob-client-step=\"{step}\"]").WaitForAsync(new() { Timeout = 90_000 });
        await page.WaitForFunctionAsync(
            $"(s) => document.querySelector('[data-ob-client-step=\"' + s + '\"]')?.getAttribute('data-ob-client-mounted') === '1'",
            step);
    }
}
