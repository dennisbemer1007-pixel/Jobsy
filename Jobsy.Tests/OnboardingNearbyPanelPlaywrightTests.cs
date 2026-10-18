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

        if (capture)
        {
            await page.ScreenshotAsync(new() { Path = Path.Combine(artifacts, "1440-step1-before-address.png") });
        }

        Assert.Equal(0, await page.Locator("[data-testid=onboarding-nearby-panel-desktop]").CountAsync());

        await VroegResultaatPlaywrightTests.FillStepsThroughTransportAsync(page, maxTravelMinutes: 30);

        if (capture)
        {
            await page.Locator("[data-testid=onboarding-nearby-panel-desktop]").WaitForAsync(new() { Timeout = 60_000 });
            await page.ScreenshotAsync(new() { Path = Path.Combine(artifacts, "1440-step3-with-panel.png") });
        }

        await VroegResultaatPlaywrightTests.ClickNextAsync(page);
        await page.Locator("[data-testid=onboarding-nearby-panel-desktop]").WaitForAsync(new() { Timeout = 60_000 });
        Assert.Equal(0, await page.Locator("[data-testid=early-reward-nearby]").CountAsync());
        Assert.Equal(0, await page.Locator(".ob-nearby-panel a").CountAsync());

        if (capture)
        {
            await page.ScreenshotAsync(new() { Path = Path.Combine(artifacts, "1440-step4-panel-visible.png") });
        }

        await page.GotoAsync($"{baseUrl}/candidate/start/nearby", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForURLAsync(u => u.ToString().Contains("/candidate/start", StringComparison.OrdinalIgnoreCase)
                                        && !u.ToString().Contains("/nearby", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 30_000 });

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
        await mPage.Locator(".ob-nearby-panel__mobile-line").ClickAsync();
        await mPage.Locator(".ob-nearby-panel__mobile-sheet").WaitForAsync(new() { Timeout = 15_000 });
        Assert.Equal(0, await mPage.Locator(".ob-nearby-panel__mobile-sheet a").CountAsync());

        if (capture)
        {
            await mPage.ScreenshotAsync(new() { Path = Path.Combine(artifacts, "390-mobile-sheet-open.png") });
        }

        await CaptureWorkHistoryStepAsync(browser, baseUrl, artifacts, capture);
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

            await page.Locator("[data-testid=onboarding-work-job] input").Nth(1).FillAsync("Lobsy BV");
            await page.Locator("[data-testid=onboarding-work-job] input").First.FillAsync("Magazijnmedewerker");
            await page.Locator("[data-testid=onboarding-add-job]").WaitForAsync(new() { Timeout = 15_000 });

            if (capture)
            {
                await page.ScreenshotAsync(new() { Path = Path.Combine(artifacts, $"{tag}-work-history-filled.png") });
            }

            await page.GetByLabel("Ik heb nog geen werkervaring").CheckAsync();
            await page.WaitForTimeoutAsync(400);
            Assert.Equal(0, await page.Locator("[data-testid=onboarding-work-job]").CountAsync());

            if (capture)
            {
                await page.ScreenshotAsync(new() { Path = Path.Combine(artifacts, $"{tag}-work-history-no-experience.png") });
            }

            await page.GetByLabel("Ik heb nog geen werkervaring").UncheckAsync();
            await page.Locator("[data-testid=onboarding-work-job]").WaitForAsync(new() { Timeout = 30_000 });
            await page.Locator("[data-testid=onboarding-work-job] input").Nth(1).FillAsync("Lobsy BV");
            await page.Locator("[data-testid=onboarding-work-job] input").First.FillAsync("Magazijnmedewerker");
            await page.Locator("[data-testid=onboarding-work-job] textarea").FillAsync("Orders verzamelen en inpakken.");
            await page.Locator(".ob-job-dates__current input").CheckAsync();
            await page.Locator("[data-testid=onboarding-add-reference]").ClickAsync();
            await page.Locator("[data-testid=onboarding-reference-panel]").WaitForAsync(new() { Timeout = 15_000 });
            if (capture)
            {
                await page.ScreenshotAsync(new() { Path = Path.Combine(artifacts, $"{tag}-work-history-current-and-reference.png") });
            }
        }
    }
}
