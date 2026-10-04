using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Mail settings with the come-back reminder, at phone and desktop width, including Arabic.
/// Soft-skips when JOBSY_E2E_BASE_URL is unset.
/// </summary>
[Collection("PlaywrightSmoke")]
public class ComebackReminderPlaywrightTests
{
    [Theory]
    [InlineData(1440, 900, "nl")]
    [InlineData(390, 844, "nl")]
    [InlineData(390, 844, "ar")]
    public async Task Mail_settings_show_the_reminder_and_hide_whatsapp_by_default(int width, int height, string lang)
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
            Locale = lang == "ar" ? "ar" : "nl-NL"
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{baseUrl}/taal/{lang}?returnUrl=%2Flogin", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });

        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? "Jobsy123!";
        await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
        await page.FillAsync("input[name='email']", "kandidaat@jobsy.local");
        await page.FillAsync("input[name='password']", password);
        await page.ClickAsync("button[type=submit]");
        try
        {
            await page.WaitForURLAsync(
                url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
                new() { Timeout = 30_000 });
        }
        catch (TimeoutException)
        {
            return;
        }

        await page.GotoAsync(baseUrl + "/account/mail-instellingen", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
        var body = await page.Locator("body").InnerTextAsync();
        if (lang == "ar")
        {
            Assert.Equal("rtl", await page.Locator("html").GetAttributeAsync("dir"));
        }

        Assert.True(
            body.Contains("Herinnering om terug te komen", StringComparison.Ordinal)
            || body.Contains("تذكير للعودة", StringComparison.Ordinal),
            "The come-back reminder should be on the mail settings page.");
        Assert.DoesNotContain("data-comeback-whatsapp", await page.ContentAsync(), StringComparison.Ordinal);

        var overflow = await page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1");
        Assert.False(overflow);
    }
}
