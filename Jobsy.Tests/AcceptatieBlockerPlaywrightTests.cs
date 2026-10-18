using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class AcceptatieBlockerPlaywrightTests
{
    [Fact]
    public async Task Onboarding_first_competency_mini_answer_saves_without_niet_bewaard()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "http://127.0.0.1:5201").Trim().TrimEnd('/');
        if (!await SmokeReachability.IsReachableAsync(baseUrl))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IsMobile = true,
            HasTouch = true,
            Locale = "nl-NL",
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        var email = $"mini.{Guid.NewGuid():N}@jobsy.local";
        await page.GotoAsync($"{baseUrl}/account-maken", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator("input[type=email]").FillAsync(email);
        await page.Locator("input[name=firstName]").FillAsync("Mini");
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
        var consentBox = page.Locator(".ob-wizard__consent input[type=checkbox]");
        if (await consentBox.CountAsync() > 0)
        {
            await consentBox.First.CheckAsync(new() { Force = true });
            var akkoord = page.GetByRole(AriaRole.Button, new() { Name = "Akkoord" });
            if (await akkoord.CountAsync() > 0)
            {
                await akkoord.ClickAsync(new() { Force = true });
            }
        }

        await page.Locator(".test-likert").First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 90_000 });
        await page.Locator(".test-likert__opt").Nth(3).ClickAsync(new() { Force = true });
        await page.WaitForTimeoutAsync(800);
        Assert.DoesNotContain("Niet bewaard", await page.ContentAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Discovery_career_step8_save_shows_saved_rail_and_capture_when_requested()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "http://127.0.0.1:5201").Trim().TrimEnd('/');
        if (!await SmokeReachability.IsReachableAsync(baseUrl))
        {
            return;
        }

        var capture = (Environment.GetEnvironmentVariable("JOBSY_CAPTURE_BLOCKERS") ?? "").Trim() == "1";
        var artifactDir = (Environment.GetEnvironmentVariable("JOBSY_BLOCKER_ARTIFACTS") ?? "/opt/cursor/artifacts/blockers").Trim();
        if (capture)
        {
            Directory.CreateDirectory(artifactDir);
        }

        await CandidateJourneyE2e.PrepareDiscoveryQuestionScreenForE2eAsync(KbE2e.DefaultCompleteEmail, step: 8);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        foreach (var (width, height, mobile, fileName) in new[]
                 {
                     (390, 844, true, "discovery-career-save-mobile-390.png"),
                     (1440, 900, false, "discovery-career-save-desktop-1440.png"),
                 })
        {
            await using var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IsMobile = mobile,
                HasTouch = mobile,
                Locale = "nl-NL",
                IgnoreHTTPSErrors = true,
            });
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(120_000);

            await LoginAndOpenDiscoveryStepAsync(page, baseUrl, stap: 8);
            await page.Locator(".journey-step .test-likert").First
                .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
            await page.Locator(".test-likert__opt").Nth(3).ClickAsync(new() { Force = true });
            await page.Locator(".journey-rail__saved.is-saved").WaitForAsync(new() { Timeout = 30_000 });
            Assert.Equal(0, await page.Locator(".journey-rail__saved.is-error").CountAsync());

            if (capture)
            {
                await page.WaitForTimeoutAsync(600);
                await page.ScreenshotAsync(new() { Path = Path.Combine(artifactDir, fileName), FullPage = false });
            }
        }
    }

    private static async Task LoginAndOpenDiscoveryStepAsync(IPage page, string baseUrl, int stap)
    {
        await page.GotoAsync($"{baseUrl}/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        if (await page.Locator("input[name=\"email\"]").CountAsync() > 0)
        {
            await page.FillAsync("input[name=\"email\"]", KbE2e.DefaultCompleteEmail);
            await page.FillAsync("input[name=\"password\"]", KbE2e.DefaultPassword);
            await page.ClickAsync("button.login-submit");
            try
            {
                await page.WaitForURLAsync(
                    u => !u.ToString().Contains("/login", StringComparison.OrdinalIgnoreCase),
                    new() { Timeout = 60_000 });
            }
            catch (TimeoutException)
            {
                await page.GotoAsync($"{baseUrl}/candidate/start", new()
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 90_000,
                });
            }
        }

        await page.GotoAsync($"{baseUrl}/candidate/ontdekkingsreis?stap={stap}", new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 90_000,
        });
        await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 90_000 });
    }
}
