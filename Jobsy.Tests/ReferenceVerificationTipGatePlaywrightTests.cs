using Jobsy.Infrastructure.Data;
using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Tip gate on real Blazor UI. Run with stack up:
/// JOBSY_E2E_BASE_URL=http://127.0.0.1:5201 dotnet test --filter ReferenceVerificationTipGate
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class ReferenceVerificationTipGatePlaywrightTests
{
    private const string Password = "Jobsy123!";

    [Fact]
    public async Task Tip_hidden_when_dna_incomplete_on_profile()
    {
        var baseUrl = await KbE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            return;
        }

        await using var browser = await (await Playwright.CreateAsync()).Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await LoginAsync(page, baseUrl, ReferenceVerificationDemoIds.IncompleteEmail);

        await page.GotoAsync($"{baseUrl}/candidate/profile", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });
        await page.WaitForTimeoutAsync(1500);
        Assert.Equal(0, await page.Locator("[data-testid='ref-verification-tip']").CountAsync());
    }

    [Fact]
    public async Task Tip_visible_when_eligible_on_profile()
    {
        var baseUrl = await KbE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            return;
        }

        await using var browser = await (await Playwright.CreateAsync()).Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await LoginAsync(page, baseUrl, ReferenceVerificationDemoIds.Email);

        await page.GotoAsync($"{baseUrl}/candidate/profile", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });
        await page.WaitForSelectorAsync("[data-testid='ref-verification-tip']", new() { Timeout = 20_000 });
    }

    private static async Task LoginAsync(IPage page, string baseUrl, string email)
    {
        await page.GotoAsync($"{baseUrl}/login?returnUrl=%2Fcandidate%2Fprofile", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        var emailInput = page.Locator("input[type=email], input[name=email], #login-email").First;
        await emailInput.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await emailInput.FillAsync(email);
        await page.Locator("input[type=password], input[name=password]").First.FillAsync(Password);
        await page.Locator("button.login-submit, button.au-submit[type=submit], button[type=submit]").First.ClickAsync();
        await page.WaitForURLAsync(
            url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 60_000 });
    }
}
