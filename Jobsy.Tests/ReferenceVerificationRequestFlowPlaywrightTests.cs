using Jobsy.Infrastructure.Data;
using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Tip → request form → send → status row. Requires Development stack.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class ReferenceVerificationRequestFlowPlaywrightTests
{
    private const string Password = "Jobsy123!";

    [Fact]
    public async Task Tip_to_form_to_send_shows_verstuurd_row()
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

        await page.GotoAsync($"{baseUrl}/candidate/paspoort", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });
        var askLink = page.Locator(
            "[data-testid='ref-verification-tip'] a.btn-primary, [data-testid='ref-verification-tip-followup'] a");
        await askLink.First.WaitForAsync(new() { Timeout = 20_000 });
        await askLink.First.ClickAsync();
        await page.WaitForURLAsync("**/referentie/vraag/**", new() { Timeout = 15_000 });
        await page.WaitForSelectorAsync(".ref-verif-request__grid", new() { Timeout = 15_000 });

        var entryAh = ReferenceVerificationDemoIds.WorkEntryAh;
        await page.GotoAsync($"{baseUrl}/candidate/paspoort/referentie/vraag/{entryAh:D}", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForSelectorAsync(".ref-verif-request__grid", new() { Timeout = 15_000 });

        await page.Locator("#ref-name").FillAsync("Flow Test Referent");
        await page.Locator("#ref-email").FillAsync("flow.referent@demo.local");
        await page.Locator("input[type=checkbox]").First.CheckAsync();
        await page.Locator(".ref-verif-request button.btn-primary").ClickAsync();
        await page.WaitForSelectorAsync("text=De mail is onderweg.", new() { Timeout = 20_000 });

        await page.GotoAsync($"{baseUrl}/candidate/paspoort?tab=references", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForSelectorAsync("[data-testid='passport-references-tab']", new() { Timeout = 15_000 });
        var table = page.Locator(".ref-verif-status-table");
        await table.WaitForAsync(new() { Timeout = 15_000 });
        Assert.True(await table.Locator("text=Verstuurd").CountAsync() > 0);
        Assert.True(await table.Locator("text=Flow Test Referent").CountAsync() > 0);
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
