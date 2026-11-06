using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Volgende must work on early wizard steps without optional work status or custom availability grid.
/// </summary>
[Collection("PlaywrightSmoke")]
public class OnboardingWizardNextPlaywrightTests
{
    [Theory]
    [InlineData(390, 844)]
    [InlineData(1280, 800)]
    public async Task Volgende_advances_steps_1_through_3_without_optional_fields(int width, int height)
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IgnoreHTTPSErrors = true,
            IsMobile = width < 600,
            HasTouch = width < 600,
        });
        var page = await context.NewPageAsync();

        await page.GotoAsync(baseUrl + "/candidate/start", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        if (page.Url.Contains("/login", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);

        await FillStep1Async(page);
        await ClickVolgendeAsync(page);
        await Assertions.Expect(page.Locator(".ob-wizard__presets")).ToBeVisibleAsync(new() { Timeout = 15_000 });

        await SelectNonCustomPresetAsync(page);
        var fineTune = page.Locator(".ob-wizard__summary-action");
        if (await fineTune.CountAsync() > 0)
        {
            await Assertions.Expect(page.Locator("[role=grid]")).ToHaveCountAsync(0);
        }

        await ClickVolgendeAsync(page);
        await Assertions.Expect(page.Locator(".ob-wizard__transport")).ToBeVisibleAsync(new() { Timeout = 15_000 });

        var transport = page.Locator(".ob-wizard__transport button:not(.is-selected)").First;
        if (await transport.CountAsync() > 0)
        {
            await transport.ClickAsync(new() { Timeout = 5_000 });
        }

        await ClickVolgendeAsync(page);
        await page.WaitForTimeoutAsync(400);
        var stillOnTransport = await page.Locator(".ob-wizard__transport").IsVisibleAsync();
        Assert.False(stillOnTransport, "Volgende on step 3 should leave transport step");
    }

    private static async Task FillStep1Async(IPage page)
    {
        var first = page.Locator(".ob-fields label:has-text('Voornaam') input, .ob-fields label:has-text('First') input").First;
        if (await first.CountAsync() == 0)
        {
            first = page.Locator(".ob-fields input").First;
        }

        await first.FillAsync("Alex");
        await page.Locator(".ob-fields input").Nth(1).FillAsync("Jansen");
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
            var birthDate = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-22);
            await birth.FillAsync(birthDate.ToString("yyyy-MM-dd"));
            await birth.EvaluateAsync("el => el.dispatchEvent(new Event('change', { bubbles: true }))");
        }

        await page.WaitForTimeoutAsync(300);
    }

    private static async Task SelectNonCustomPresetAsync(IPage page)
    {
        var customCode = page.Locator(".ob-wizard__presets button").Filter(new() { HasText = "Zelf" });
        var pick = page.Locator(".ob-wizard__presets button:not(.is-selected)").First;
        if (await customCode.CountAsync() > 0 && await pick.CountAsync() > 0)
        {
            var text = await pick.InnerTextAsync();
            if (text.Contains("Zelf", StringComparison.OrdinalIgnoreCase))
            {
                pick = page.Locator(".ob-wizard__presets button:not(.is-selected)").Nth(1);
            }
        }

        if (await pick.CountAsync() > 0)
        {
            await pick.ClickAsync(new() { Timeout = 5_000 });
        }
    }

    private static async Task ClickVolgendeAsync(IPage page)
    {
        await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);
        var next = page.GetByRole(AriaRole.Button, new() { Name = "Volgende", Exact = true });
        if (await next.CountAsync() == 0)
        {
            next = page.Locator("button.ob-wizard__cta.btn-primary");
        }

        await Assertions.Expect(next.First).ToBeEnabledAsync(new() { Timeout = 20_000 });
        var force = (page.ViewportSize?.Width ?? 1280) < 600;
        await next.First.ClickAsync(new() { Timeout = 15_000, Force = force });
    }
}
