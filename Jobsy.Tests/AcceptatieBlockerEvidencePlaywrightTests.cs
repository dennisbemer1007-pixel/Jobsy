using System.Text.RegularExpressions;
using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>PNG evidence for acceptatie blockers when JOBSY_CAPTURE_BLOCKERS=1.</summary>
[Collection("PlaywrightSmoke")]
public sealed class AcceptatieBlockerEvidencePlaywrightTests
{
    private static string OutDir =>
        (Environment.GetEnvironmentVariable("JOBSY_BLOCKER_ARTIFACTS") ?? "/cursor/stores/self/blockers").Trim();

    [Fact]
    public async Task Debug_desktop_nav_dom_when_requested()
    {
        if ((Environment.GetEnvironmentVariable("JOBSY_DEBUG_NAV") ?? "").Trim() != "1")
        {
            return;
        }

        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "http://127.0.0.1:5201").Trim().TrimEnd('/');
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var ctx = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 1440, Height = 900 }, Locale = "nl-NL" });
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);
        await RegisterAndSkipToBanenkaartAsync(page, baseUrl);
        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await WaitForCircuitReadyAsync(page);
        await page.WaitForTimeoutAsync(3000);
        var dump = await page.EvaluateAsync<string>(
            """
            () => JSON.stringify({
              url: location.href,
              shell: document.querySelector('.app-shell')?.className ?? null,
              header: !!document.querySelector('.app-header'),
              nav: document.querySelectorAll('[data-testid=candidate-header-nav]').length,
              navClass: document.querySelectorAll('.candidate-header-nav').length,
              links: document.querySelectorAll('[data-testid=candidate-header-nav] a').length,
              bottomLinks: document.querySelectorAll('nav.bottom-nav a').length,
              headerSnippet: (document.querySelector('.app-header')?.innerHTML ?? '').slice(0, 800),
            }, null, 2)
            """);
        await File.WriteAllTextAsync("/tmp/nav-debug.json", dump);
        Assert.Fail(dump);
    }

    [Fact]
    public async Task Capture_merge_gate_header_and_mobile_banenkaart()
    {
        if ((Environment.GetEnvironmentVariable("JOBSY_CAPTURE_BLOCKERS") ?? "").Trim() != "1")
        {
            return;
        }

        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "http://127.0.0.1:5201").Trim().TrimEnd('/');
        if (!await SmokeReachability.IsReachableAsync(baseUrl))
        {
            return;
        }

        Directory.CreateDirectory(OutDir);
        await CandidateJourneyE2e.EnableLiveFeatureFlagsAsync(baseUrl);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        await CaptureBanenkaartHeaderAsync(browser, baseUrl, 1440, 900, mobile: false, "banenkaart-header-desktop-1440.png");
        await CaptureBanenkaartHeaderAsync(browser, baseUrl, 390, 844, mobile: true, "banenkaart-header-mobile-390.png");
        await CaptureSkipBanenkaartMobileOnlyAsync(browser, baseUrl);
        await CaptureBanenkaartLocationPromptMobileAsync(browser, baseUrl);
        await CaptureCandidateDesktopNavAsync(browser, baseUrl);
        await CaptureBanenkaartOpenForWorkDesktopAsync(browser, baseUrl);
    }

    [Fact]
    public async Task Capture_banenkaart_header_for_merge()
    {
        if ((Environment.GetEnvironmentVariable("JOBSY_CAPTURE_BLOCKERS") ?? "").Trim() != "1")
        {
            return;
        }

        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "http://127.0.0.1:5201").Trim().TrimEnd('/');
        if (!await SmokeReachability.IsReachableAsync(baseUrl))
        {
            return;
        }

        Directory.CreateDirectory(OutDir);
        await CandidateJourneyE2e.EnableLiveFeatureFlagsAsync(baseUrl);
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await CaptureBanenkaartHeaderAsync(browser, baseUrl, 1440, 900, mobile: false, "banenkaart-header-desktop-1440.png");
        await CaptureBanenkaartHeaderAsync(browser, baseUrl, 390, 844, mobile: true, "banenkaart-header-mobile-390.png");
    }

    [Fact]
    public async Task Capture_blocker_evidence_when_requested()
    {
        if ((Environment.GetEnvironmentVariable("JOBSY_CAPTURE_BLOCKERS") ?? "").Trim() != "1")
        {
            return;
        }

        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "http://127.0.0.1:5201").Trim().TrimEnd('/');
        if (!await SmokeReachability.IsReachableAsync(baseUrl))
        {
            return;
        }

        Directory.CreateDirectory(OutDir);
        await CandidateJourneyE2e.EnableLiveFeatureFlagsAsync(baseUrl);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        await CaptureSkipBanenkaartShellAsync(browser, baseUrl);
        await CaptureCandidateDesktopNavAsync(browser, baseUrl);
        await CaptureOnboardingFirstCompetencySaveAsync(browser, baseUrl);
    }

    private static async Task CaptureBanenkaartLocationPromptMobileAsync(IBrowser browser, string baseUrl)
    {
        await using var ctx = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            DeviceScaleFactor = 2,
            IsMobile = true,
            HasTouch = true,
            Locale = "nl-NL",
        });
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);
        await RegisterAndSkipToBanenkaartAsync(page, baseUrl);
        await WaitForCircuitReadyAsync(page);
        var clear = page.Locator(".kb-filter-bar .kb-address__clear").First;
        if (await clear.CountAsync() > 0)
        {
            await clear.ClickAsync(new() { Force = true });
        }
        else
        {
            await page.EvaluateAsync("() => window.jobsyGeo && window.jobsyGeo.clearSessionOrigin && window.jobsyGeo.clearSessionOrigin()");
        }

        await page.WaitForTimeoutAsync(600);
        await page.Locator(".kb-start-prompt").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        Assert.Equal(0, await page.Locator(".jobsy-chrome .kb-ext__promo:visible").CountAsync());
        await page.WaitForTimeoutAsync(400);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(OutDir, "skip-banenkaart-mobile-390-location-prompt.png"),
            FullPage = false,
        });
    }

    private static async Task CaptureBanenkaartOpenForWorkDesktopAsync(IBrowser browser, string baseUrl)
    {
        await using var ctx = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
            Locale = "nl-NL",
        });
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);
        await RegisterAndSkipToBanenkaartAsync(page, baseUrl);
        await page.GotoAsync($"{baseUrl}/candidate/paspoort?tab=data", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await WaitForCircuitReadyAsync(page);
        var openForWorkSwitch = page.Locator(".passport-data__switch-input").First;
        await openForWorkSwitch.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        if (await openForWorkSwitch.IsCheckedAsync())
        {
            await openForWorkSwitch.UncheckAsync(new() { Force = true });
            await page.WaitForTimeoutAsync(1500);
        }

        await page.GotoAsync($"{baseUrl}/banenkaart", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await WaitForCircuitReadyAsync(page);
        await page.Locator("[data-testid=open-for-work-prompt]").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await page.Locator(".vacancy-pane [data-testid=kb-view-lijst]").First.ClickAsync(new() { Force = true });
        await page.WaitForTimeoutAsync(800);
        var pane = page.Locator(".vacancy-pane");
        await pane.ScreenshotAsync(new()
        {
            Path = Path.Combine(OutDir, "banenkaart-open-for-work-desktop-1440.png"),
        });
    }

    private static async Task CaptureBanenkaartHeaderAsync(
        IBrowser browser,
        string baseUrl,
        int width,
        int height,
        bool mobile,
        string fileName)
    {
        await using var ctx = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            DeviceScaleFactor = mobile ? 2 : 1,
            IsMobile = mobile,
            HasTouch = mobile,
            Locale = "nl-NL",
        });
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);
        await RegisterAndSkipToBanenkaartAsync(page, baseUrl);
        if (!mobile)
        {
            await page.ReloadAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            await WaitForCircuitReadyAsync(page);
        }
        else
        {
            await page.WaitForTimeoutAsync(2000);
        }

        Assert.True(await page.Locator(".app-shell--banenkaart-shell").CountAsync() > 0, "Expected candidate banenkaart shell.");
        if (mobile)
        {
            await page.Locator(".app-header").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 90_000 });
            await page.Locator("nav.bottom-nav a").First
                .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        }
        else
        {
            await page.Locator("[data-testid=candidate-header-nav] a").First
                .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 90_000 });
        }

        await page.WaitForTimeoutAsync(800);
        var outPath = Path.Combine(OutDir, fileName);
        for (var attempt = 0; attempt < 4; attempt++)
        {
            var header = page.Locator(".app-header").Last;
            try
            {
                await header.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });
                await header.ScreenshotAsync(new() { Path = outPath });
                return;
            }
            catch (PlaywrightException) when (attempt < 3)
            {
                await page.WaitForTimeoutAsync(600);
            }
        }
    }

    private static async Task CaptureSkipBanenkaartMobileOnlyAsync(IBrowser browser, string baseUrl)
    {
        await using var ctx = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            DeviceScaleFactor = 2,
            IsMobile = true,
            HasTouch = true,
            Locale = "nl-NL",
        });
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);
        await RegisterAndSkipToBanenkaartAsync(page, baseUrl);
        await AssertBanenkaartCandidateShellAsync(page, mobile: true);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(OutDir, "skip-banenkaart-mobile-390.png"),
            FullPage = false,
        });
    }

    private static async Task CaptureCandidateDesktopNavAsync(IBrowser browser, string baseUrl)
    {
        await using var ctx = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
            Locale = "nl-NL",
        });
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await RegisterAndSkipToBanenkaartAsync(page, baseUrl);
        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await WaitForCircuitReadyAsync(page);
        await page.Locator("[data-testid=candidate-header-nav] a").First
            .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 90_000 });
        var activeCount = await page.Locator("[data-testid=candidate-header-nav] .is-active").CountAsync();
        Assert.True(activeCount > 0, "Expected an active candidate header nav link on banenkaart.");
        var accountToggle = page.Locator(".app-header .account-menu__toggle").First;
        await accountToggle.ClickAsync();
        await page.Locator(".app-header .account-menu__panel").First
            .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });
        await page.WaitForTimeoutAsync(600);
        await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "candidate-header-nav-banenkaart-desktop-1440.png"), FullPage = false });
        await page.Keyboard.PressAsync("Escape");

        await page.GotoAsync($"{baseUrl}/candidate/ontdekkingsreis", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await WaitForCircuitReadyAsync(page);
        await page.Locator("[data-testid=candidate-header-nav] a").First
            .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 90_000 });
        Assert.True(await page.Locator("[data-testid=header-nav-discovery].is-active").CountAsync() > 0,
            "Expected discovery link active on ontdekkingsreis.");
        await page.WaitForTimeoutAsync(800);
        await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "candidate-header-nav-discovery-desktop-1440.png"), FullPage = false });
    }

    private static async Task CaptureSkipBanenkaartShellAsync(IBrowser browser, string baseUrl)
    {
        foreach (var (width, height, mobile, fileName) in new[]
                 {
                     (390, 844, true, "skip-banenkaart-mobile-390.png"),
                     (1440, 900, false, "skip-banenkaart-desktop-1440.png"),
                 })
        {
            await using var ctx = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                DeviceScaleFactor = mobile ? 2 : 1,
                IsMobile = mobile,
                HasTouch = mobile,
                Locale = "nl-NL",
            });
            await PlaywrightCookieConsent.AcceptAsync(ctx);
            var page = await ctx.NewPageAsync();
            page.SetDefaultTimeout(120_000);

            await RegisterAndSkipToBanenkaartAsync(page, baseUrl);
            await AssertBanenkaartCandidateShellAsync(page, mobile);
            await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, fileName), FullPage = false });
        }
    }

    private static async Task CaptureOnboardingFirstCompetencySaveAsync(IBrowser browser, string baseUrl)
    {
        foreach (var (width, height, mobile, fileName) in new[]
                 {
                     (390, 844, true, "onboarding-competency-save-mobile-390.png"),
                     (1440, 900, false, "onboarding-competency-save-desktop-1440.png"),
                 })
        {
            await using var ctx = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IsMobile = mobile,
                HasTouch = mobile,
                Locale = "nl-NL",
            });
            await PlaywrightCookieConsent.AcceptAsync(ctx);
            var page = await ctx.NewPageAsync();
            page.SetDefaultTimeout(120_000);

            await RegisterFreshCandidateAsync(page, baseUrl);
            await ReachFirstCompetencyQuestionAsync(page);
            await page.Locator(".test-likert__opt").Nth(3).ClickAsync(new() { Force = true });
            await page.WaitForTimeoutAsync(800);
            Assert.DoesNotContain("Niet bewaard", await page.ContentAsync(), StringComparison.OrdinalIgnoreCase);
            await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, fileName), FullPage = false });
        }
    }

    private static async Task RegisterAndSkipToBanenkaartAsync(IPage page, string baseUrl)
    {
        await CandidateJourneyE2e.EnableLiveFeatureFlagsAsync(baseUrl);
        var email = $"blocker.{Guid.NewGuid():N}@jobsy.local";
        await page.GotoAsync($"{baseUrl}/account-maken", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.Locator("input[type=email]").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await page.Locator("input[type=email]").FillAsync(email);
        await page.Locator("input[name=firstName]").FillAsync("Blocker");
        await page.Locator("button.login-submit, button.au-submit[type=submit]").First.ClickAsync();
        await page.WaitForURLAsync("**/code**", new() { Timeout = 60_000 });
        var code = await CandidateJourneyE2e.FetchEmailCodeAsync(baseUrl, email);
        await page.Locator("input[name=code]").FillAsync(code!);
        await page.Locator("button.login-submit").ClickAsync();
        await page.WaitForURLAsync(u => !u.ToString().Contains("/code", StringComparison.Ordinal), new() { Timeout = 90_000 });

        await page.GotoAsync($"{baseUrl}/candidate/start", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await VroegResultaatPlaywrightTestsHelpers.EnsureWizardStartedAsync(page);
        await VroegResultaatPlaywrightTestsHelpers.FillStepsThroughTransportAsync(page, maxTravelMinutes: 30);
        await VroegResultaatPlaywrightTestsHelpers.ClickNextAsync(page);
        await page.Locator("[data-testid=early-reward-nearby]").WaitForAsync(new() { Timeout = 60_000 });
        await VroegResultaatPlaywrightTestsHelpers.ClickNextAsync(page);
        await VroegResultaatPlaywrightTestsHelpers.NavigateToTestConsentWithoutStartingAsync(page);
        await page.GetByRole(AriaRole.Heading, new() { Name = "Nu even over jou" }).WaitForAsync(new() { Timeout = 60_000 });
        await page.GetByTestId("onboarding-tests-later").First.ClickAsync(new() { Force = true });
        await page.WaitForURLAsync(u => u.ToString().Contains("/banenkaart", StringComparison.OrdinalIgnoreCase), new() { Timeout = 90_000 });
        await page.Locator(".jobsy-discovery").WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 90_000 });
        await page.WaitForFunctionAsync(
            "() => document.querySelector('.app-shell--banenkaart-shell') !== null",
            null,
            new() { Timeout = 60_000 });
        await page.WaitForTimeoutAsync(2500);
    }

    private static async Task AssertBanenkaartCandidateShellAsync(IPage page, bool mobile)
    {
        await page.Locator(".app-header").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        if (mobile)
        {
            await page.Locator("nav.bottom-nav").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
            Assert.Equal(0, await page.Locator(".jobsy-chrome__header:visible").CountAsync());
        }

        Assert.True(await page.Locator("[data-testid=kb-view-toggle]").CountAsync() > 0, "Expected Kaart/Lijst toggle in DOM.");
    }

    private static async Task WaitForCircuitReadyAsync(IPage page)
    {
        try
        {
            await page.WaitForFunctionAsync(
                "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
                null,
                new() { Timeout = 90_000 });
        }
        catch (TimeoutException)
        {
            await page.WaitForTimeoutAsync(1200);
        }
    }

    private static async Task RegisterFreshCandidateAsync(IPage page, string baseUrl, string? email = null)
    {
        email ??= $"mini1.{Guid.NewGuid():N}@jobsy.local";
        await page.GotoAsync($"{baseUrl}/account-maken", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 90_000 });
        await page.Locator("input[type=email]").FillAsync(email);
        await page.Locator("input[name=firstName]").FillAsync("Mini");
        await page.Locator("button.login-submit, button.au-submit[type=submit]").First.ClickAsync();
        await page.WaitForURLAsync("**/code**", new() { Timeout = 60_000 });
        var code = await CandidateJourneyE2e.FetchEmailCodeAsync(baseUrl, email);
        await page.Locator("input[name=code]").FillAsync(code!);
        await page.Locator("button.login-submit").ClickAsync();
        await page.WaitForURLAsync(u => !u.ToString().Contains("/code", StringComparison.Ordinal), new() { Timeout = 90_000 });
        await page.GotoAsync($"{baseUrl}/candidate/start", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
    }

    private static async Task ReachFirstCompetencyQuestionAsync(IPage page)
    {
        await VroegResultaatPlaywrightTestsHelpers.EnsureWizardStartedAsync(page);
        await VroegResultaatPlaywrightTestsHelpers.FillStepsThroughTransportAsync(page, maxTravelMinutes: 30);
        await VroegResultaatPlaywrightTestsHelpers.ClickNextAsync(page);
        await page.Locator("[data-testid=early-reward-nearby]").WaitForAsync(new() { Timeout = 60_000 });
        await VroegResultaatPlaywrightTestsHelpers.ClickNextAsync(page);
        await VroegResultaatPlaywrightTestsHelpers.NavigateToTestConsentWithoutStartingAsync(page);
        await page.GetByRole(AriaRole.Heading, new() { Name = "Nu even over jou" }).WaitForAsync(new() { Timeout = 60_000 });
        var consent = page.Locator(".ob-wizard__consent input[type=checkbox]");
        if (await consent.CountAsync() > 0)
        {
            await consent.First.CheckAsync(new() { Force = true });
            var accept = page.GetByRole(AriaRole.Button, new() { Name = "Akkoord" });
            if (await accept.CountAsync() > 0)
            {
                await accept.ClickAsync(new() { Force = true });
            }
        }

        await page.Locator(".test-likert").First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 90_000 });
    }
}

/// <summary>Shared helpers from VroegResultaatPlaywrightTests for evidence capture.</summary>
internal static class VroegResultaatPlaywrightTestsHelpers
{
    public static async Task EnsureWizardStartedAsync(IPage page)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            await page.WaitForFunctionAsync(
                """
                () => {
                  const loading = document.querySelector('.ob-wizard__loading');
                  if (loading) return false;
                  return document.querySelector('.ob-wizard__welcome')
                    || document.querySelector('.ob-fields')
                    || document.querySelector('.ob-wizard__finish');
                }
                """,
                null,
                new() { Timeout = 90_000 });

            if (await page.Locator(".ob-fields input").CountAsync() > 0
                || await page.Locator(".ob-wizard__welcome").CountAsync() > 0)
            {
                if (await page.Locator(".ob-wizard__welcome").CountAsync() > 0)
                {
                    await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);
                    await page.GetByRole(AriaRole.Button, new() { Name = "Beginnen" }).ClickAsync(new() { Force = true });
                    await page.WaitForTimeoutAsync(500);
                }

                await page.Locator(".ob-fields input").First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 90_000 });
                return;
            }

            await page.ReloadAsync(new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.WaitForTimeoutAsync(800);
        }
    }

    public static async Task FillStepsThroughTransportAsync(IPage page, int maxTravelMinutes, string postcode = "2671GB")
    {
        await page.Locator(".ob-fields input").First.FillAsync("Blocker");
        await page.Locator(".ob-fields input").Nth(1).FillAsync("Evidence");
        await page.Locator("#onboarding-home-address, .ob-fields input[inputmode=text]").First.FillAsync(postcode);
        await page.WaitForTimeoutAsync(500);
        var suggestion = page.Locator(".ob-wizard__suggestions button, .kb-address__suggest button").First;
        if (await suggestion.CountAsync() > 0)
        {
            await suggestion.ClickAsync(new() { Timeout = 8_000 });
        }

        var birth = page.Locator("[data-testid=onboarding-birth-date] input[type=date]").First;
        if (await birth.CountAsync() > 0)
        {
            await birth.FillAsync("2000-06-15");
        }

        await ClickNextAsync(page);
        await page.Locator(".ob-wizard__presets button").First.ClickAsync();
        await ClickNextAsync(page);
        await page.Locator("#onboarding-field-transport .lobsy-choice-tile, #onboarding-field-transport button").First.ClickAsync();
        var travel = page.Locator(".ob-wizard__transport .lobsy-choice-tile, .ob-wizard__transport button")
            .Filter(new() { HasText = maxTravelMinutes.ToString() });
        if (await travel.CountAsync() == 0)
        {
            travel = page.Locator(".ob-wizard__transport .lobsy-choice-tile, .ob-wizard__transport button").First;
        }

        await travel.First.ClickAsync();
    }

    public static async Task ClickNextAsync(IPage page)
    {
        await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);
        await page.Locator("[data-testid=onboarding-next]").ClickAsync(new() { Force = true, Timeout = 20_000 });
        await page.WaitForTimeoutAsync(500);
    }

    public static async Task NavigateToTestConsentWithoutStartingAsync(IPage page)
    {
        for (var step = 0; step < 6; step++)
        {
            if (await page.Locator(".ob-wizard__consent input[type=checkbox]").CountAsync() > 0)
            {
                return;
            }

            if (await page.Locator(".test-likert").CountAsync() > 0)
            {
                return;
            }

            var skip = page.Locator("button.ob-wizard__ghost").Filter(new() { HasTextRegex = new Regex("Overslaan", RegexOptions.IgnoreCase) });
            if (await skip.CountAsync() > 0)
            {
                await skip.First.ClickAsync(new() { Timeout = 8_000 });
                await page.WaitForTimeoutAsync(450);
                continue;
            }

            var unknown = page.Locator("button.ob-wizard__ghost").Filter(new() { HasTextRegex = new Regex("Weet ik nog niet", RegexOptions.IgnoreCase) });
            if (await unknown.CountAsync() > 0)
            {
                await unknown.First.ClickAsync(new() { Timeout = 8_000 });
                await page.WaitForTimeoutAsync(450);
                continue;
            }

            if (await page.Locator("[data-testid=onboarding-next]").CountAsync() > 0)
            {
                await ClickNextAsync(page);
                continue;
            }

            break;
        }
    }
}
