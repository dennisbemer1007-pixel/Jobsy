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
    [InlineData(false, 1024, 700)]
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
        await WaitStepReadyAsync(page, 2);

        var shotDir = "/opt/cursor/artifacts/screenshots";
        Directory.CreateDirectory(shotDir);
        var suffix = mobile ? "390" : width == 1024 ? "1024" : "1280";
        await page.ScreenshotAsync(new() { Path = $"{shotDir}/mockup-step-02-{suffix}.png", FullPage = true });

        if (!mobile)
        {
            await Assertions.Expect(page.Locator(".ob-rustig__rail")).ToHaveCountAsync(1);
            var firstPreset = page.Locator("[data-ob-client=presetGrid] .lobsy-choice-tile").First;
            await Assertions.Expect(firstPreset.Locator(".lobsy-choice-tile__stack strong")).Not.ToHaveTextAsync("");
        }

        var presetWeekend = page.Locator("[data-ob-client=presetGrid] button[name=\"obPreset\"][value=\"weekend\"]");
        await ClickTileAsync(presetWeekend, mobile);
        await WaitStepPostAsync(page, 2);
        await Assertions.Expect(presetWeekend).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("is-selected"));
        await ReloadStepAsync(page, 2);
        await Assertions.Expect(presetWeekend).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("is-selected"));
        Assert.NotEmpty(factory.StoredProfile.Preferences?.AvailabilityPresets ?? []);
        Assert.True((factory.StoredProfile.Preferences?.Availability?.Count ?? 0) > 0);

        var matrixCell = page.Locator("button[name=\"obMatrix\"]").First;
        await ClickTileAsync(matrixCell, mobile);
        await WaitStepPostAsync(page, 2);

        await page.Locator("input[name=\"obStartMode\"][value=\"now\"]").CheckAsync();
        await page.GetByTestId("onboarding-start-save").ClickAsync();
        await WaitStepPostAsync(page, 2);
        await Assertions.Expect(page.Locator("input[name=\"obStartMode\"][value=\"now\"]")).ToBeCheckedAsync();

        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=3**", new() { Timeout = 60_000 });
        await WaitStepReadyAsync(page, 3);
        await page.ScreenshotAsync(new() { Path = $"{shotDir}/mockup-step-03-{suffix}.png", FullPage = true });

        var ebike = page.Locator("button[name=\"obTransport\"][value=\"E-bike\"]");
        await ClickTileAsync(ebike, mobile);
        await WaitStepPostAsync(page, 3);
        await Assertions.Expect(ebike).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("is-selected"));
        Assert.Contains(
            "E-bike",
            factory.StoredProfile.Preferences?.PreferredTransport ?? "",
            StringComparison.OrdinalIgnoreCase);

        await ReloadStepAsync(page, 3);
        await Assertions.Expect(ebike).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("is-selected"));

        foreach (var code in new[] { "Fiets", "Auto", "OV", "Lopend" })
        {
            var tile = page.Locator("button[name=\"obTransport\"][value=\"" + code + "\"]");
            await ClickTileAsync(tile, mobile);
            await WaitStepPostAsync(page, 3);
            await Assertions.Expect(tile).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("is-selected"));
        }

        if (!mobile)
        {
            await ReloadStepAsync(page, 3);
            foreach (var code in new[] { "Fiets", "E-bike", "Auto", "OV", "Lopend" })
            {
                var tile = page.Locator("button[name=\"obTransport\"][value=\"" + code + "\"]");
                await Assertions.Expect(tile).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("is-selected"));
            }
        }

        var travel30 = page.Locator("button[name=\"obTravel\"][value=\"30\"]");
        await ClickTileAsync(travel30, mobile);
        await WaitStepPostAsync(page, 3);
        if (!mobile)
        {
            await ReloadStepAsync(page, 3);
            await Assertions.Expect(travel30).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("is-selected"));
        }

        await page.GetByTestId("onboarding-back").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=2**");
        await WaitStepReadyAsync(page, 2);
        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=3**");
        await WaitStepReadyAsync(page, 3);
        await Assertions.Expect(ebike).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("is-selected"));

        await page.GetByTestId("onboarding-next").ClickAsync();
        await page.WaitForURLAsync("**/candidate/start?step=4**", new() { Timeout = 60_000 });
        await WaitStepReadyAsync(page, 4);
        await page.ScreenshotAsync(new() { Path = $"{shotDir}/mockup-step-04-{suffix}.png", FullPage = true });

        if (!mobile)
        {
            var noWork = page.Locator(".ob-wizard__check input[type=\"checkbox\"]");
            await Assertions.Expect(noWork).ToBeVisibleAsync();
        }

        var noWorkBox = page.Locator("[data-ob-client=noWork]");
        if (await noWorkBox.CountAsync() > 0)
        {
            await noWorkBox.CheckAsync();
            await noWorkBox.UncheckAsync();
        }

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

    private static async Task ClickTileAsync(ILocator tile, bool mobile)
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

    private static Task WaitStepPostAsync(IPage page, int step)
        => page.WaitForURLAsync($"**/candidate/start?step={step}**", new() { Timeout = 30_000 });

    private static async Task ReloadStepAsync(IPage page, int step)
    {
        await page.WaitForLoadStateAsync(LoadState.Load);
        await page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.Load });
        await WaitStepReadyAsync(page, step);
    }

    private static async Task WaitStepReadyAsync(IPage page, int step)
    {
        await page.Locator($"[data-ob-client-step=\"{step}\"]").WaitForAsync(new() { Timeout = 90_000 });
        if (step is 1 or >= 7)
        {
            await page.WaitForFunctionAsync(
                $"(s) => document.querySelector('[data-ob-client-step=\"' + s + '\"]')?.getAttribute('data-ob-client-mounted') === '1'",
                step);
        }
    }

    [Theory]
    [InlineData(false, 1024, 700)]
    [InlineData(true, 390, 844)]
    public async Task Step2_start_save_and_enter_guard(bool mobile, int width, int height)
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

        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start?step=2"));
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await WaitStepReadyAsync(page, 2);

        var firstPreset = page.Locator("[data-ob-client=presetGrid] button[name=\"obPreset\"]").First;
        await Assertions.Expect(firstPreset).Not.ToHaveClassAsync(new System.Text.RegularExpressions.Regex("is-selected"));

        var dateInput = page.GetByTestId("onboarding-available-from");
        await page.Locator("input[name=\"obStartMode\"][value=\"from\"]").CheckAsync();
        await dateInput.FillAsync("2027-04-20");
        await page.GetByTestId("onboarding-start-save").ClickAsync();
        await WaitStepPostAsync(page, 2);
        await ReloadStepAsync(page, 2);
        await Assertions.Expect(page.Locator("input[name=\"obStartMode\"][value=\"from\"]")).ToBeCheckedAsync();
        await Assertions.Expect(dateInput).ToHaveValueAsync("2027-04-20");
        Assert.Equal(new DateOnly(2027, 4, 20), factory.StoredProfile.AvailableFromDate);

        await dateInput.FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await page.WaitForTimeoutAsync(500);
        await Assertions.Expect(firstPreset).Not.ToHaveClassAsync(new System.Text.RegularExpressions.Regex("is-selected"));
    }

    [Fact]
    public async Task Step4_checkbox_left_of_label_at_1024()
    {
        await using var factory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory();
        factory.WelcomeDismissed = true;
        var root = factory.EnsureServerAddress();
        await using var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new() { Width = 1024, Height = 700 },
            Locale = "nl-NL",
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString("/candidate/start?step=4"));
        await WaitStepReadyAsync(page, 4);

        var label = page.Locator("label.ob-wizard__check");
        var box = await label.BoundingBoxAsync();
        Assert.NotNull(box);
        Assert.True(box.X < 300, $"Expected checkbox row near left edge, got x={box.X}");
    }
}
