using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public class LivegangFixesPlaywrightTests
{
    [Fact]
    public async Task Notifications_route_is_not_404_when_logged_in()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        var email = (Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? "").Trim();
        var password = (Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? DefaultPassword).Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        if (!await TryLoginAsync(page, baseUrl, email, password))
        {
            return;
        }

        var response = await page.GotoAsync($"{baseUrl}/notifications",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
        Assert.NotNull(response);
        Assert.NotEqual(404, response.Status);
        await page.Locator("h1").Filter(new() { HasText = "Meldingen" }).WaitForAsync(new() { Timeout = 15_000 });
    }

    [Fact]
    public async Task Notifications_unread_dot_aligns_to_card_right_edge_on_mobile()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        var email = (Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? "kandidaat@jobsy.local").Trim();
        var password = (Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? DefaultPassword).Trim();

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IsMobile = true,
            HasTouch = true,
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        if (!await KbE2e.TryLoginAsync(page, baseUrl, email, password))
        {
            return;
        }

        await page.GotoAsync($"{baseUrl}/notifications",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
        await KbE2e.WaitForStyledAppAsync(page, "app.min.css", "notifications.css");

        var dot = page.Locator(".notifications-page__unread-dot").First;
        if (await dot.CountAsync() == 0)
        {
            return;
        }

        await dot.WaitForAsync(new() { Timeout = 15_000 });
        var metrics = await page.EvaluateAsync<string>(
            """
            () => {
              const dot = document.querySelector('.notifications-page__unread-dot');
              const btn = dot?.closest('.notifications-page__item-btn');
              const item = dot?.closest('.notifications-page__item');
              if (!dot || !btn || !item) return null;
              const dotR = dot.getBoundingClientRect();
              const btnR = btn.getBoundingClientRect();
              const itemR = item.getBoundingClientRect();
              return JSON.stringify({
                gapItemRight: itemR.right - dotR.right,
                btnItemGap: Math.abs(itemR.right - btnR.right)
              });
            }
            """);
        Assert.False(string.IsNullOrEmpty(metrics), "Expected an unread notification row on /notifications.");
        using var doc = System.Text.Json.JsonDocument.Parse(metrics!);
        var gapPx = doc.RootElement.GetProperty("gapItemRight").GetDouble();
        var btnItemGap = doc.RootElement.GetProperty("btnItemGap").GetDouble();
        Assert.True(gapPx <= 20, $"Unread dot should sit within 20px of card right edge; gapItemRight={gapPx}px.");
        Assert.True(btnItemGap <= 2, $"Card button should fill card width; |itemRight-btnRight|={btnItemGap}px.");

        var position = await page.EvaluateAsync<string>(
            "() => getComputedStyle(document.querySelector('.notifications-page__unread-dot')).position");
        Assert.Equal("absolute", position);
    }

    private const string DefaultPassword = "Jobsy123!";

    private static async Task<bool> TryLoginAsync(IPage page, string baseUrl, string email, string password)
    {
        try
        {
            await page.GotoAsync(baseUrl + "/login",
                new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
            await page.FillAsync("input[type=email], input[name=email], #email", email);
            await page.FillAsync("input[type=password], input[name=password], #password", password);
            await page.ClickAsync("button[type=submit], button:has-text('Inloggen'), button:has-text('Log in')");
            await page.WaitForTimeoutAsync(1200);
            return !page.Url.Contains("/login", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
