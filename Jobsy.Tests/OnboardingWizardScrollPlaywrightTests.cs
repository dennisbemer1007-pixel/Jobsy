using System.Text.RegularExpressions;
using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Profile wizard steps 1–6 must start at scroll top on mobile (scroll container is <c>.app-main</c>).
/// Mini-test steps 7–10 use <c>test-flow</c> chrome; see <see cref="OnboardingMiniCoachDockPlaywrightTests"/>.
/// Run with <c>JOBSY_E2E_BASE_URL=http://127.0.0.1:5201</c>.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class OnboardingWizardScrollPlaywrightTests
{
    /// <summary>JOBSY_CAPTURE_ONBOARDING_SCROLL=before|after → saves steps 1,3,5,8 to agent store.</summary>
    [Fact]
    public async Task Capture_onboarding_scroll_screenshots_when_requested()
    {
        var mode = (Environment.GetEnvironmentVariable("JOBSY_CAPTURE_ONBOARDING_SCROLL") ?? "").Trim().ToLowerInvariant();
        if (mode is not ("before" or "after"))
        {
            return;
        }

        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "http://127.0.0.1:5201").Trim().TrimEnd('/');
        var outDir = $"/cursor/stores/self/onboarding-scroll/{mode}";
        Directory.CreateDirectory(outDir);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IgnoreHTTPSErrors = true,
            IsMobile = true,
            HasTouch = true,
            Locale = "nl-NL",
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await RegisterAndBeginWizardAsync(page, baseUrl);

        var captureSteps = new[] { 1, 3, 5, 8 };
        var current = 1;
        while (current <= 8)
        {
            await WaitWizardStepAsync(page, current);

            if (captureSteps.Contains(current))
            {
                if (mode == "after")
                {
                    await page.EvaluateAsync("() => { document.querySelector('.app-main')?.scrollTo(0,0); window.scrollTo(0,0); }");
                    await page.WaitForTimeoutAsync(250);
                }

                await page.ScreenshotAsync(new()
                {
                    Path = Path.Combine(outDir, $"step-{current:00}.png"),
                    FullPage = false,
                });
            }

            if (current >= 8)
            {
                break;
            }

            if (mode == "before")
            {
                await ScrollMainDownAsync(page);
            }

            await AdvanceWizardStepAsync(page, current);
            current++;
        }
    }

    private static async Task WaitWizardStepAsync(IPage page, int step)
    {
        await page.WaitForFunctionAsync(
            """
            expected => parseInt(document.querySelector('.ob-wizard__segments')?.getAttribute('aria-valuenow') || '0', 10) === expected
            """,
            step,
            new() { Timeout = 30_000 });
    }

    [Theory]
    [InlineData(390, 844)]
    [InlineData(360, 740)]
    public async Task Each_step_starts_with_heading_visible_at_scroll_top(int width, int height)
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IgnoreHTTPSErrors = true,
            IsMobile = true,
            HasTouch = true,
            Locale = "nl-NL",
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await RegisterAndBeginWizardAsync(page, baseUrl);

        const int lastStep = 6;
        for (var step = 1; step <= lastStep; step++)
        {
            await WaitWizardStepAsync(page, step);
            await AssertStepScrollTopAsync(page, step);

            if (step >= lastStep)
            {
                break;
            }

            await ScrollMainDownAsync(page);
            await AdvanceWizardStepAsync(page, step);
        }
    }

    private static async Task AssertStepScrollTopAsync(IPage page, int step)
    {
        var audit = await page.EvaluateAsync<ScrollAudit>(
            """
            () => {
              const main = document.querySelector('.app-main');
              const mainScroll = main ? main.scrollTop : 0;
              const winScroll = window.scrollY || 0;
              const h1 = document.querySelector('.ob-wizard__content h1, .ob-fields h1');
              if (!h1) {
                return { ok: false, reason: 'no-h1', mainScroll, winScroll, headingTop: null };
              }
              const rect = h1.getBoundingClientRect();
              const mainRect = main ? main.getBoundingClientRect() : { top: 0, bottom: window.innerHeight };
              const inView = rect.top >= mainRect.top - 2
                && rect.top < mainRect.bottom - 40
                && rect.height > 0;
              const ok = mainScroll <= 1 && winScroll <= 1 && inView;
              let reason = 'ok';
              if (mainScroll > 1) reason = 'main-scroll-' + mainScroll;
              else if (winScroll > 1) reason = 'window-scroll-' + winScroll;
              else if (!inView) reason = 'heading-offscreen top=' + rect.top;
              return { ok, reason, mainScroll, winScroll, headingTop: rect.top };
            }
            """);

        Assert.True(audit.Ok, $"Step {step}: {audit.Reason} main={audit.MainScroll} win={audit.WinScroll} h1Top={audit.HeadingTop}");
    }

    private static async Task ScrollMainDownAsync(IPage page)
    {
        await page.EvaluateAsync(
            """
            () => {
              const main = document.querySelector('.app-main');
              const amount = Math.min(480, (main?.scrollHeight || 800) - (main?.clientHeight || 400));
              if (main) main.scrollTop = amount;
              window.scrollTo(0, amount);
            }
            """);
        await page.WaitForTimeoutAsync(200);
    }

    private static async Task AdvanceWizardStepAsync(IPage page, int step)
    {
        if (step == 1)
        {
            var first = page.Locator(".ob-fields label:has-text('Voornaam') input, .ob-fields label:has-text('First') input").First;
            if (await first.CountAsync() == 0)
            {
                first = page.Locator(".ob-fields input").First;
            }

            await first.FillAsync("Scroll");
            await page.Locator(".ob-fields input").Nth(1).FillAsync("Test");
            await page.Locator("#onboarding-home-address, .ob-fields input[inputmode=text], .ob-fields input[autocomplete=postal-code]").First.FillAsync("2671GB");
            await page.WaitForTimeoutAsync(500);
            var suggestion = page.Locator(".ob-wizard__suggestions button").First;
            if (await suggestion.CountAsync() > 0)
            {
                await suggestion.ClickAsync(new() { Timeout = 5_000 });
            }

            var birth = page.Locator("[data-testid=onboarding-birth-date] input[type=date]").First;
            if (await birth.CountAsync() > 0)
            {
                await birth.FillAsync("2000-06-15");
                await birth.EvaluateAsync("el => el.dispatchEvent(new Event('change', { bubbles: true }))");
            }

            await page.WaitForTimeoutAsync(300);
        }
        else if (step == 2)
        {
            await page.Locator(".ob-wizard__presets button").First.ClickAsync();
        }
        else if (step == 3)
        {
            await page.Locator("#onboarding-field-transport .lobsy-choice-tile, #onboarding-field-transport button").First.ClickAsync();
            await page.Locator(".ob-wizard__transport .lobsy-choice-tile, .ob-wizard__transport button").Filter(new() { HasText = "30" }).First.ClickAsync();
        }
        else if (step == 4)
        {
            var skipWork = page.Locator("button.ob-wizard__ghost").Filter(new() { HasTextRegex = new Regex("Overslaan", RegexOptions.IgnoreCase) }).First;
            if (await skipWork.CountAsync() > 0)
            {
                await skipWork.ClickAsync(new() { Timeout = 8_000 });
                return;
            }
        }
        else if (step == 5)
        {
            await page.Locator("button").Filter(new() { HasTextRegex = new Regex("^MBO$", RegexOptions.IgnoreCase) }).First.ClickAsync(new() { Timeout = 8_000 });
            await ClickOnboardingNextAsync(page);
            return;
        }
        else if (step == 6)
        {
            var unknown = page.Locator("button.ob-wizard__ghost").Filter(new() { HasTextRegex = new Regex("Weet ik nog niet", RegexOptions.IgnoreCase) }).First;
            if (await unknown.CountAsync() > 0)
            {
                await unknown.ClickAsync(new() { Timeout = 8_000 });
                return;
            }
        }
        else if (step == 7)
        {
            foreach (var cb in await page.Locator(".ob-wizard__consent input:not(:checked)").AllAsync())
            {
                await cb.CheckAsync(new() { Force = true });
            }
        }
        else if (step is >= 7 and <= 9)
        {
            for (var q = 0; q < 6; q++)
            {
                var likert = page.Locator(".test-likert input[value=\"4\"]").First;
                if (await likert.CountAsync() == 0)
                {
                    break;
                }

                await likert.ClickAsync(new() { Force = true });
                await page.WaitForTimeoutAsync(350);
                var miniNext = page.Locator("button.test-flow__next:not([disabled])").First;
                if (await miniNext.CountAsync() > 0)
                {
                    await miniNext.ClickAsync(new() { Force = true });
                    await page.WaitForTimeoutAsync(400);
                }
            }

            return;
        }

        await ClickOnboardingNextAsync(page);
    }

    private static async Task RegisterAndBeginWizardAsync(IPage page, string baseUrl)
    {
        var email = $"scroll.ob.{Guid.NewGuid():N}@jobsy.local";
        await page.GotoAsync($"{baseUrl}/account-maken", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 90_000 });
        await page.Locator("input[type=email]").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 90_000 });
        await page.Locator("input[type=email]").FillAsync(email);
        await page.Locator("input[name=firstName]").FillAsync("Scroll");
        var submit = page.Locator("button.login-submit, button.au-submit[type=submit]");
        await submit.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await submit.First.ClickAsync();
        await page.WaitForURLAsync("**/code**", new() { Timeout = 60_000 });
        var code = await CandidateJourneyE2e.FetchEmailCodeAsync(baseUrl, email);
        Assert.False(string.IsNullOrWhiteSpace(code));
        await page.Locator("input[name=code]").FillAsync(code!);
        await page.Locator("button.login-submit").ClickAsync();
        await page.WaitForURLAsync(u => !u.ToString().Contains("/code", StringComparison.Ordinal), new() { Timeout = 90_000 });

        await page.GotoAsync($"{baseUrl}/candidate/start", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.WaitForTimeoutAsync(600);
        if (await page.Locator(".ob-wizard__welcome").CountAsync() > 0)
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "Beginnen" }).ClickAsync();
            await page.WaitForTimeoutAsync(400);
        }
    }

    private static async Task ClickOnboardingNextAsync(IPage page)
    {
        await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);
        var next = page.Locator("[data-testid=onboarding-next]");
        await next.ClickAsync(new() { Force = true, Timeout = 15_000 });
        await page.WaitForTimeoutAsync(450);
    }

    private sealed class ScrollAudit
    {
        public bool Ok { get; set; }
        public string? Reason { get; set; }
        public double MainScroll { get; set; }
        public double WinScroll { get; set; }
        public double? HeadingTop { get; set; }
    }
}
