using System.Net;
using Jobsy.Web.Localization;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// /wie-zijn-wij five zones block at 1440×900 and 390×844 (NL + EN).
/// Run with a local app: JOBSY_E2E_BASE_URL=http://127.0.0.1:5201 dotnet test --filter About_zones_block
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class AboutZonesPlaywrightTests
{
    private static readonly (int Width, int Height)[] Viewports = [(1440, 900), (390, 844)];

    [Fact]
    public async Task About_zones_block_renders_on_desktop_and_mobile_in_nl_and_en()
    {
        var baseUrl = await ResolveBaseUrlAsync();
        if (baseUrl is null)
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        foreach (var (width, height) in Viewports)
        {
            await using var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
            });
            var page = await context.NewPageAsync();

            foreach (var lang in new[] { "nl", "en" })
            {
                await page.GotoAsync(
                    $"{baseUrl}/wie-zijn-wij?lang={lang}",
                    new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });

                var cookieAccept = page.GetByRole(AriaRole.Button, new() { Name = "Accepteer cookies" });
                if (await cookieAccept.CountAsync() > 0)
                {
                    await cookieAccept.ClickAsync();
                }

                var section = page.Locator("section.pp-about__zones");
                await Assertions.Expect(section).ToBeVisibleAsync();

                var items = section.Locator("li.pp-about__zones-item");
                await Assertions.Expect(items).ToHaveCountAsync(5);

                var firstHeading = UiStrings.Get("About.Zones.Strand.Heading", lang);
                await Assertions.Expect(items.First.Locator("h3")).ToHaveTextAsync(firstHeading);

                var lastHeading = UiStrings.Get("About.Zones.Licht.Heading", lang);
                await Assertions.Expect(items.Nth(4).Locator("h3")).ToHaveTextAsync(lastHeading);
            }
        }
    }

    private static async Task<string?> ResolveBaseUrlAsync()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return null;
        }

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var response = await http.GetAsync(baseUrl + "/privacy");
            return response.StatusCode is HttpStatusCode.OK ? baseUrl : null;
        }
        catch
        {
            return null;
        }
    }
}
