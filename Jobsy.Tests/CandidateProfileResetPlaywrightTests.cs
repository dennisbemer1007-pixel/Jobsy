using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Desktop + mobile screenshots of profile danger zone and reset dialog.
/// Run: <c>JOBSY_E2E_PROFILE_RESET=1 JOBSY_E2E_BASE_URL=… dotnet test --filter ProfileResetPlaywright</c>
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class CandidateProfileResetPlaywrightTests
{
    private static bool ShouldRun =>
        string.Equals(Environment.GetEnvironmentVariable("JOBSY_E2E_PROFILE_RESET"), "1", StringComparison.Ordinal);

    [Fact]
    public async Task Profile_reset_ui_desktop_and_mobile()
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

        Environment.SetEnvironmentVariable(
            "JOBSY_E2E_ARTIFACTS_ROOT",
            "/opt/cursor/artifacts/profile-reset");
        Directory.CreateDirectory("/opt/cursor/artifacts/profile-reset");

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
            page.SetDefaultTimeout(90_000);

            Assert.True(await KbE2e.TryLoginAsync(page, baseUrl));
            await page.GotoAsync($"{baseUrl}/candidate/profile", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.Locator(".profile-danger-zone").ScrollIntoViewIfNeededAsync();
            await page.ScreenshotAsync(new()
            {
                Path = $"/opt/cursor/artifacts/profile-reset/{tag}-danger-zone.png"
            });

            await page.GetByRole(AriaRole.Button, new() { Name = "Begin opnieuw" }).ClickAsync();
            await page.Locator(".profile-reset-modal").WaitForAsync();
            await page.ScreenshotAsync(new()
            {
                Path = $"/opt/cursor/artifacts/profile-reset/{tag}-reset-dialog.png"
            });

            await KbE2e.AssertNoCircuitErrorAsync(page);
        }
    }
}
