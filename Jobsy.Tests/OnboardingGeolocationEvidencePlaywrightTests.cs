using Jobsy.Core.Rules;
using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class OnboardingGeolocationEvidencePlaywrightTests
{
    private static string OutDir =>
        (Environment.GetEnvironmentVariable("JOBSY_GEO_ARTIFACTS") ?? "/cursor/stores/self/blockers").Trim();

    [Fact]
    public async Task Capture_onboarding_geolocation_hints_when_requested()
    {
        if ((Environment.GetEnvironmentVariable("JOBSY_CAPTURE_GEO") ?? "").Trim() != "1")
        {
            return;
        }

        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "http://127.0.0.1:5201").Trim().TrimEnd('/');
        if (!await SmokeReachability.IsReachableAsync(baseUrl))
        {
            return;
        }

        Directory.CreateDirectory(OutDir);
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        await CaptureAsync(browser, baseUrl, width: 1440, height: 900, accuracyMeters: 800,
            fileName: "onboarding-geolocation-low-accuracy-desktop-1440.png");
        await CaptureAsync(browser, baseUrl, width: 390, height: 844, accuracyMeters: 25,
            fileName: "onboarding-geolocation-precise-mobile-390.png", mobile: true);
    }

    private static async Task CaptureAsync(
        IBrowser browser,
        string baseUrl,
        int width,
        int height,
        float accuracyMeters,
        string fileName,
        bool mobile = false)
    {
        await using var ctx = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            DeviceScaleFactor = mobile ? 2 : 1,
            IsMobile = mobile,
            HasTouch = mobile,
            Locale = "nl-NL",
            Geolocation = new() { Latitude = 52.066f, Longitude = 4.235f, Accuracy = accuracyMeters },
            Permissions = ["geolocation"],
        });
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        var email = $"geo.{Guid.NewGuid():N}@jobsy.local";
        await page.GotoAsync($"{baseUrl}/account-maken", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 90_000 });
        await page.Locator("input[type=email]").FillAsync(email);
        await page.Locator("input[name=firstName]").FillAsync("Geo");
        await page.Locator("button.login-submit, button.au-submit[type=submit]").First.ClickAsync();
        await page.WaitForURLAsync("**/code**", new() { Timeout = 60_000 });
        var code = await CandidateJourneyE2e.FetchEmailCodeAsync(baseUrl, email);
        await page.Locator("input[name=code]").FillAsync(code!);
        await page.Locator("button.login-submit").ClickAsync();
        await page.WaitForURLAsync(u => !u.ToString().Contains("/code", StringComparison.Ordinal), new() { Timeout = 90_000 });

        await page.GotoAsync($"{baseUrl}/candidate/start", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);
        var welcome = page.Locator(".ob-wizard__welcome button").First;
        if (await welcome.CountAsync() > 0)
        {
            await welcome.ClickAsync(new() { Force = true });
        }

        await page.Locator("#onboarding-home-address, .ob-fields__address input[role=combobox]").First
            .WaitForAsync(new() { Timeout = 90_000 });
        try
        {
            await page.WaitForFunctionAsync(
                "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
                null,
                new() { Timeout = 90_000 });
        }
        catch (TimeoutException)
        {
            await page.WaitForTimeoutAsync(1500);
        }

        await page.Locator(".kb-address__geo").ClickAsync(new() { Force = true });
        var hint = page.Locator(".ob-wizard__hint--geo, [data-testid=geolocation-confirm-address]");
        await hint.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 90_000 });
        await page.WaitForTimeoutAsync(400);

        await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, fileName), FullPage = false });
    }
}
