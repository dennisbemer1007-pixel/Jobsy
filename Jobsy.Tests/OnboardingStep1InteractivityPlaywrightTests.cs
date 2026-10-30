using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class OnboardingStep1InteractivityPlaywrightTests
{
    [Theory]
    [InlineData(1440, 900, false)]
    [InlineData(390, 844, true)]
    public async Task Step1_work_status_chip_and_volgende_respond_after_circuit_ready(int width, int height, bool mobile)
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        if (!await SmokeReachability.IsReachableAsync(baseUrl))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var ctx = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IsMobile = mobile,
            HasTouch = mobile,
            Locale = "nl-NL",
            IgnoreHTTPSErrors = true,
        });
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await RegisterAndOpenStep1Async(page, baseUrl);
        await WaitForCircuitReadyAsync(page);

        var chip = page.Locator(".work-status-chips .lobsy-choice-tile").First;
        await chip.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await chip.ScrollIntoViewIfNeededAsync();

        var hit = await page.EvaluateAsync<string>(
            """
            () => {
              const chip = document.querySelector('.work-status-chips .lobsy-choice-tile');
              if (!chip) return 'no-chip';
              const r = chip.getBoundingClientRect();
              const x = r.left + r.width / 2;
              const y = r.top + r.height / 2;
              const el = document.elementFromPoint(x, y);
              if (!el) return 'null-hit';
              if (el === chip || chip.contains(el)) return 'chip';
              const art = document.querySelector('[data-testid=onboarding-illustration-art], [data-testid=onboarding-mobile-hero]');
              if (art && (el === art || art.contains(el))) return 'hero-overlay';
              return el.tagName + (el.className ? '.' + String(el.className).split(' ')[0] : '');
            }
            """);

        Assert.Equal("chip", hit);

        await chip.ClickAsync(new() { Force = mobile });
        await Assertions.Expect(chip).ToHaveAttributeAsync("aria-pressed", "true");

        await page.Locator("#onboarding-field-firstname input").FillAsync("Alex");
        await page.Locator("#onboarding-field-lastname input").FillAsync("Jansen");
        await page.Locator("#onboarding-home-address").FillAsync("2671 GB Naaldwijk");
        await page.WaitForTimeoutAsync(400);
        var suggestion = page.Locator(".address-suggestions__item button").First;
        if (await suggestion.CountAsync() > 0)
        {
            await suggestion.ClickAsync();
        }

        var birthText = page.Locator("[data-testid=onboarding-birth-date] .jobsy-date-field__text");
        await birthText.FillAsync("15-06-2000");
        await birthText.BlurAsync();

        var next = page.GetByRole(AriaRole.Button, new() { Name = "Volgende", Exact = true });
        await Assertions.Expect(next).ToBeEnabledAsync(new() { Timeout = 20_000 });
    }

    private static async Task RegisterAndOpenStep1Async(IPage page, string baseUrl)
    {
        var email = $"ob-int.{Guid.NewGuid():N}@jobsy.local";
        await page.GotoAsync($"{baseUrl}/account-maken", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.Locator("input[type=email]").FillAsync(email);
        await page.Locator("input[name=firstName]").FillAsync("Ob");
        await page.Locator("button.login-submit, button.au-submit[type=submit]").First.ClickAsync();
        await page.WaitForURLAsync("**/code**", new() { Timeout = 60_000 });
        var code = await CandidateJourneyE2e.FetchEmailCodeAsync(baseUrl, email);
        Assert.False(string.IsNullOrWhiteSpace(code));
        await page.Locator("input[name=code]").FillAsync(code!);
        await page.Locator("button.login-submit").ClickAsync();
        await page.WaitForURLAsync(u => !u.ToString().Contains("/code", StringComparison.Ordinal), new() { Timeout = 90_000 });

        if (!page.Url.Contains("/candidate/start", StringComparison.OrdinalIgnoreCase))
        {
            await page.GotoAsync($"{baseUrl}/candidate/start", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        }

        await page.Locator(".ob-wizard__welcome, [data-testid=onboarding-step-1-illustrated]").First.WaitForAsync(new() { Timeout = 90_000 });
        if (await page.Locator(".ob-wizard__welcome").CountAsync() > 0)
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "Beginnen" }).ClickAsync(new() { Force = true });
        }

        await page.Locator("[data-testid=onboarding-step-1-illustrated]").WaitForAsync(new() { Timeout = 90_000 });
    }

    private static async Task WaitForCircuitReadyAsync(IPage page)
    {
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 90_000 });
    }
}
