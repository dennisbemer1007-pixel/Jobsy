using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Optional probe against acceptatie: guest mobile banenkaart should not hard-reload when backgrounded briefly.
/// Skips when acceptatie is unreachable. Does not fail CI by default (short watch).
/// </summary>
[Collection("PlaywrightSmoke")]
public class AcceptatieMobileReconnectProbePlaywrightTests
{
    [Fact]
    public async Task Acceptatie_guest_banenkaart_background_probe_no_document_reload()
    {
        const string baseUrl = "https://acceptatie.lobsy.nl";
        if (!await IsReachableAsync(baseUrl))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        await using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 412, Height = 915 },
            UserAgent = "Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Mobile Safari/537.36",
            IsMobile = true,
            HasTouch = true
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.AddInitScriptAsync(
            """
            () => {
              window.__lobsyNavCount = 0;
              window.addEventListener('load', () => { window.__lobsyNavCount = (window.__lobsyNavCount || 0) + 1; });
            }
            """);

        await page.GotoAsync(baseUrl + "/banenkaart?weergave=kaart", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
        await page.WaitForSelectorAsync(".jobsy-discovery, .kb-filter-bar", new() { Timeout = 90_000 });
        var later = page.Locator(".kb-start-prompt__later");
        if (await later.CountAsync() > 0)
        {
            await later.First.ClickAsync();
        }

        await page.EvaluateAsync("() => { window.__lobsyNavCount = 0; }");
        await page.WaitForTimeoutAsync(5000);

        await context.SetOfflineAsync(true);
        await page.WaitForTimeoutAsync(4000);
        await context.SetOfflineAsync(false);
        await page.WaitForTimeoutAsync(8000);

        await page.EvaluateAsync("() => document.dispatchEvent(new Event('visibilitychange'))");
        await page.WaitForTimeoutAsync(5000);

        var navCount = await page.EvaluateAsync<int>("() => window.__lobsyNavCount || 0");
        Assert.True(navCount == 0, $"Acceptatie probe saw {navCount} unprompted load events (baseline before fix often >0 on circuit loss).");
    }

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
            using var res = await http.GetAsync(baseUrl + "/");
            return res.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
