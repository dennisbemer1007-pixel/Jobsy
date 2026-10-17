using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>Screenshots: apply rate limits in Platforminstellingen → Functies.</summary>
[Collection("PlaywrightSmoke")]
public sealed class AdminApplyRateSettingsPlaywrightTests
{
    private static bool ShouldRun =>
        string.Equals(Environment.GetEnvironmentVariable("JOBSY_E2E_ADMIN_APPLY_SETTINGS"), "1", StringComparison.Ordinal);

    [Fact]
    public async Task Admin_apply_rate_settings_visible_desktop_and_mobile()
    {
        if (!ShouldRun)
        {
            return;
        }

        var baseUrl = await KbE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            Assert.Fail("JOBSY_E2E_BASE_URL unreachable");
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        foreach (var (width, height, tag) in new[]
                 {
                     (KbE2e.Desktop.Width, KbE2e.Desktop.Height, "desktop"),
                     (KbE2e.Mobile.Width, KbE2e.Mobile.Height, "mobile")
                 })
        {
            await using var context = await browser.NewContextAsync(KbE2e.ContextFor(width, height));
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(120_000);

            Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, "admin@jobsy.local", KbE2e.DefaultPassword));
            await page.GotoAsync($"{baseUrl}/admin/instellingen", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.GetByText("Sollicitaties per uur", new() { Exact = false }).First.WaitForAsync(new() { Timeout = 60_000 });
            await page.GetByText("Anti-bot: sollicitaties per minuut", new() { Exact = false }).First.WaitForAsync();

            var dir = Path.Combine("/workspace/usability5/admin", tag);
            Directory.CreateDirectory(dir);
            await page.ScreenshotAsync(new() { Path = Path.Combine(dir, "platform-apply-rate-settings.png"), FullPage = true });
        }
    }
}
