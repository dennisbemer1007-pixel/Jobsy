using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class OnboardingNearbyPanelPlaywrightTests
{
    [Fact]
    public async Task Nearby_panel_and_legacy_redirect_when_smoke_reachable()
    {
        var capture = (Environment.GetEnvironmentVariable("JOBSY_CAPTURE_ONBOARDING_PANEL") ?? "").Trim() == "1";
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "http://127.0.0.1:5201").Trim().TrimEnd('/');
        if (!await SmokeReachability.IsReachableAsync(baseUrl))
        {
            return;
        }

        var artifacts = "/cursor/stores/self/onboarding-panel";
        if (capture)
        {
            Directory.CreateDirectory(artifacts);
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        await CaptureDesktopNearbyAsync(browser, baseUrl, artifacts, capture);
        await CaptureMobileNearbyAsync(browser, baseUrl, artifacts, capture);
        await CaptureWorkHistoryStepAsync(browser, baseUrl, artifacts, capture);
        await CaptureEducationStepAsync(browser, baseUrl, artifacts, capture);
    }

    private static async Task CaptureDesktopNearbyAsync(
        IBrowser browser,
        string baseUrl,
        string artifacts,
        bool capture)
    {
        await using var desktop = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
            Locale = "nl-NL",
            IgnoreHTTPSErrors = true,
        });
        await PlaywrightCookieConsent.AcceptAsync(desktop);
        var page = await desktop.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await VroegResultaatPlaywrightTests.RegisterFreshCandidateAsync(page, baseUrl);
        await VroegResultaatPlaywrightTests.EnsureWizardStartedAsync(page);

        Assert.Equal(0, await page.Locator("[data-testid=onboarding-nearby-panel-desktop]").CountAsync());
        if (capture)
        {
            await page.ScreenshotAsync(new() { Path = Path.Combine(artifacts, "1440-nearby-before-address.png") });
        }

        await FillPersonalAddressOnlyAsync(page);
        await page.Locator("[data-testid=onboarding-nearby-panel-desktop]").WaitForAsync(new() { Timeout = 90_000 });
        if (capture)
        {
            await page.ScreenshotAsync(new() { Path = Path.Combine(artifacts, "1440-nearby-after-address.png") });
        }

        await VroegResultaatPlaywrightTests.ClickNextAsync(page);
        await page.Locator(".ob-wizard__presets button").First.ClickAsync();
        await VroegResultaatPlaywrightTests.ClickNextAsync(page);
        await page.Locator("#onboarding-field-transport .lobsy-choice-tile, #onboarding-field-transport button").First.ClickAsync();
        await page.Locator(".ob-wizard__transport .lobsy-choice-tile, .ob-wizard__transport button").Filter(new() { HasText = "30" }).First.ClickAsync();

        var countBefore = await ReadNearbyCountAsync(page, mobile: false);
        await VroegResultaatPlaywrightTests.ClickNextAsync(page);
        await page.Locator("[data-testid=onboarding-work-job]").WaitForAsync(new() { Timeout = 60_000 });
        await VroegResultaatPlaywrightTests.ClickNextAsync(page);
        await page.Locator(".ob-wizard__education-grid .lobsy-choice-tile, .ob-wizard__education-grid button").Filter(new() { HasText = "MBO" }).First.ClickAsync();
        await page.WaitForTimeoutAsync(1200);
        var countAfter = await ReadNearbyCountAsync(page, mobile: false);
        if (capture)
        {
            await page.ScreenshotAsync(new() { Path = Path.Combine(artifacts, "1440-nearby-narrowed-count.png") });
        }

        Assert.True(countAfter <= countBefore || countBefore == 0);

        await page.GotoAsync($"{baseUrl}/candidate/start/nearby", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForURLAsync(u => u.ToString().Contains("/candidate/start", StringComparison.OrdinalIgnoreCase)
                                        && !u.ToString().Contains("/nearby", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 30_000 });
        Assert.Equal(0, await page.Locator("[data-testid=early-reward-nearby]").CountAsync());
        Assert.Equal(0, await page.Locator(".ob-nearby-panel a").CountAsync());
    }

    private static async Task CaptureMobileNearbyAsync(
        IBrowser browser,
        string baseUrl,
        string artifacts,
        bool capture)
    {
        await using var mobile = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IsMobile = true,
            HasTouch = true,
            Locale = "nl-NL",
            IgnoreHTTPSErrors = true,
        });
        await PlaywrightCookieConsent.AcceptAsync(mobile);
        var mPage = await mobile.NewPageAsync();
        mPage.SetDefaultTimeout(120_000);
        await VroegResultaatPlaywrightTests.RegisterFreshCandidateAsync(mPage, baseUrl);
        await VroegResultaatPlaywrightTests.EnsureWizardStartedAsync(mPage);
        await VroegResultaatPlaywrightTests.FillStepsThroughTransportAsync(mPage, maxTravelMinutes: 30);
        await VroegResultaatPlaywrightTests.ClickNextAsync(mPage);
        await mPage.Locator("[data-testid=onboarding-nearby-panel-mobile]").WaitForAsync(new() { Timeout = 60_000 });

        if (capture)
        {
            await mPage.ScreenshotAsync(new() { Path = Path.Combine(artifacts, "390-nearby-line-collapsed.png") });
        }

        await mPage.Locator(".ob-nearby-panel__mobile-line").ClickAsync();
        await mPage.Locator(".ob-nearby-panel__mobile-sheet").WaitForAsync(new() { Timeout = 15_000 });
        Assert.Equal(0, await mPage.Locator(".ob-nearby-panel__mobile-sheet a").CountAsync());

        if (capture)
        {
            await mPage.ScreenshotAsync(new() { Path = Path.Combine(artifacts, "390-nearby-sheet-open.png") });
        }
    }

    private static async Task CaptureWorkHistoryStepAsync(
        IBrowser browser,
        string baseUrl,
        string artifacts,
        bool capture)
    {
        foreach (var (tag, width, height, mobile) in new[]
                 {
                     ("1440", 1440, 900, false),
                     ("390", 390, 844, true),
                 })
        {
            await using var ctx = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IsMobile = mobile,
                HasTouch = mobile,
                Locale = "nl-NL",
                IgnoreHTTPSErrors = true,
            });
            await PlaywrightCookieConsent.AcceptAsync(ctx);
            var page = await ctx.NewPageAsync();
            page.SetDefaultTimeout(120_000);
            await VroegResultaatPlaywrightTests.RegisterFreshCandidateAsync(page, baseUrl);
            await VroegResultaatPlaywrightTests.EnsureWizardStartedAsync(page);
            await VroegResultaatPlaywrightTests.FillStepsThroughTransportAsync(page, maxTravelMinutes: 30);
            await VroegResultaatPlaywrightTests.ClickNextAsync(page);

            await page.Locator("[data-testid=onboarding-work-job]").WaitForAsync(new() { Timeout = 60_000 });
            Assert.Equal(0, await page.Locator("[data-testid=onboarding-add-job]").CountAsync());

            if (capture)
            {
                await page.ScreenshotAsync(new() { Path = Path.Combine(artifacts, $"{tag}-work-history-default-open.png") });
            }

            await page.Locator("[data-testid=onboarding-work-job] input").First.FillAsync("Magazijnmedewerker");
            await page.Locator("[data-testid=onboarding-work-job] input").Nth(1).FillAsync("Lobsy BV");
            await page.WaitForTimeoutAsync(600);
            var chip = page.Locator(".ob-job-duty-chips .lobsy-choice-tile, .ob-job-duty-chips button").First;
            if (await chip.CountAsync() > 0)
            {
                await chip.ClickAsync();
            }

            await page.Locator("[data-testid=onboarding-work-job] textarea").FillAsync("Orders verzamelen en klanten helpen.");
            if (capture)
            {
                await page.ScreenshotAsync(new() { Path = Path.Combine(artifacts, $"{tag}-work-history-duties-and-chips.png") });
            }

            await page.Locator(".ob-job-dates__current input").CheckAsync();
            await page.WaitForTimeoutAsync(400);
            if (capture)
            {
                await page.ScreenshotAsync(new() { Path = Path.Combine(artifacts, $"{tag}-work-history-current-job.png") });
            }
        }
    }

    private static async Task CaptureEducationStepAsync(
        IBrowser browser,
        string baseUrl,
        string artifacts,
        bool capture)
    {
        foreach (var (tag, width, height, mobile) in new[]
                 {
                     ("1440", 1440, 900, false),
                     ("390", 390, 844, true),
                 })
        {
            await using var ctx = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IsMobile = mobile,
                HasTouch = mobile,
                Locale = "nl-NL",
                IgnoreHTTPSErrors = true,
            });
            await PlaywrightCookieConsent.AcceptAsync(ctx);
            var page = await ctx.NewPageAsync();
            page.SetDefaultTimeout(120_000);
            await VroegResultaatPlaywrightTests.RegisterFreshCandidateAsync(page, baseUrl);
            await VroegResultaatPlaywrightTests.EnsureWizardStartedAsync(page);
            await VroegResultaatPlaywrightTests.FillStepsThroughTransportAsync(page, maxTravelMinutes: 30);
            await VroegResultaatPlaywrightTests.ClickNextAsync(page);
            await page.Locator("[data-testid=onboarding-work-job]").WaitForAsync(new() { Timeout = 60_000 });
            await VroegResultaatPlaywrightTests.ClickNextAsync(page);
            await page.Locator(".ob-wizard__education-grid").WaitForAsync(new() { Timeout = 60_000 });
            await page.Locator(".ob-wizard__education-grid .lobsy-choice-tile, .ob-wizard__education-grid button").Filter(new() { HasText = "HAVO" }).First.ClickAsync();
            var direction = page.Locator("#onboarding-education-direction");
            await direction.WaitForAsync(new() { Timeout = 30_000 });
            await direction.FillAsync("Techn");
            await page.WaitForTimeoutAsync(400);
            var suggestion = page.Locator(".education-direction-input__option").First;
            if (await suggestion.CountAsync() > 0)
            {
                await suggestion.ClickAsync();
            }

            Assert.Equal(0, await page.Locator(".ob-wizard__direction-chips").CountAsync());
            if (capture)
            {
                await page.ScreenshotAsync(new() { Path = Path.Combine(artifacts, $"{tag}-education-havo-direction.png") });
            }

            await page.Locator(".ob-wizard__education-grid .lobsy-choice-tile, .ob-wizard__education-grid button").Filter(new() { HasText = "Basisschool" }).First.ClickAsync();
            await page.WaitForTimeoutAsync(500);
            Assert.Equal(0, await direction.CountAsync());
            if (capture)
            {
                await page.ScreenshotAsync(new() { Path = Path.Combine(artifacts, $"{tag}-education-basisschool.png") });
            }
        }
    }

    private static async Task FillPersonalAddressOnlyAsync(IPage page)
    {
        await page.Locator(".ob-fields input").First.FillAsync("Vroeg");
        await page.Locator(".ob-fields input").Nth(1).FillAsync("Test");
        await VroegResultaatPlaywrightTests.ResolveOnboardingHomeAddressAsync(page);
        await VroegResultaatPlaywrightTests.FillOnboardingBirthDateIfPresentAsync(page);
    }

    private static async Task<int> ReadNearbyCountAsync(IPage page, bool mobile)
    {
        var locator = mobile
            ? page.Locator(".ob-nearby-panel__mobile-text")
            : page.Locator(".ob-nearby-panel--desktop .ob-nearby-panel__count");
        if (await locator.CountAsync() == 0)
        {
            return 0;
        }

        var text = await locator.First.InnerTextAsync();
        var digits = new string(text.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var n) ? n : 0;
    }
}
