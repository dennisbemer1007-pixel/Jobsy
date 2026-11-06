using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// UWV kansberoepen sample layer must show cards in the list, not only the count heading.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class BanenkaartUwvKansListPlaywrightTests
{
    private const string Email = "onboarding.e2e@jobsy.local";
    private const string Password = "Jobsy123!";

    [Fact]
    public async Task Nl_kansberoepen_layer_shows_sample_cards_in_list_column()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !await SmokeReachability.IsReachableAsync(baseUrl))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
            Locale = "nl-NL",
            IgnoreHTTPSErrors = true,
        });
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        if (!await Jobsy.Tests.E2e.KbE2e.TryLoginAsync(page, baseUrl, Email, Password))
        {
            return;
        }

        await page.GotoAsync(baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.WaitForSelectorAsync("[data-testid=kb-desktop-result-count]", new() { Timeout = 60_000 });
        var heading = await page.Locator("[data-testid=kb-desktop-result-count]").InnerTextAsync();
        if (!heading.Contains("kansberoepen", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await page.WaitForSelectorAsync(".vacancy-list--cards .vacancy-card, .kb-list-rows .vacancy-card", new() { Timeout = 45_000 });
        var count = await page.Locator(".vacancy-list--cards .vacancy-card, .kb-list-rows .vacancy-card").CountAsync();
        Assert.True(count >= 1, "Expected at least one kansberoep sample card in the list column.");
    }
}
