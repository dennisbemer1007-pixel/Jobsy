using System.Net;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Shared-preferences section on the passport Data tab. Soft-skips without JOBSY_E2E_BASE_URL,
/// and when PassportPdfV2Enabled is off (the section is not rendered).
/// </summary>
[Collection("PlaywrightSmoke")]
public class PassportSharedPlaywrightTests
{
    [Fact]
    public async Task Candidate_edits_shared_section_without_horizontal_overflow()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            Locale = "nl-NL",
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        if (!await TryLoginAsync(page, baseUrl))
        {
            return;
        }

        await page.GotoAsync(baseUrl + "/candidate/paspoort?tab=data",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        var title = page.GetByText("Dit deel ik met werkgevers en bureaus");
        if (await title.CountAsync() == 0)
        {
            return;
        }

        await title.First.ClickAsync();
        var region = page.Locator("[data-testid='work-region']");
        await region.FillAsync("Utrecht e.o.");
        var indoor = page.GetByRole(AriaRole.Button, new() { Name = "Liever binnen" });
        if (await indoor.CountAsync() > 0)
        {
            await indoor.First.ClickAsync();
        }

        var save = page.GetByRole(AriaRole.Button, new() { Name = "Opslaan" });
        if (await save.CountAsync() > 0)
        {
            await save.First.ClickAsync();
        }

        var overflow = await page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1");
        Assert.False(overflow);
    }

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var response = await http.GetAsync(baseUrl);
            return (int)response.StatusCode < 500;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> TryLoginAsync(IPage page, string baseUrl)
    {
        try
        {
            await page.GotoAsync(baseUrl + "/account/login",
                new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
            var email = page.Locator("input[type=email], input[name=email]").First;
            if (await email.CountAsync() == 0)
            {
                return false;
            }

            await email.FillAsync(Environment.GetEnvironmentVariable("JOBSY_E2E_EMAIL") ?? "kandidaat@jobsy.local");
            await page.Locator("input[type=password]").First.FillAsync(
                Environment.GetEnvironmentVariable("JOBSY_E2E_PASSWORD") ?? "Jobsy123!");
            await page.Locator("button[type=submit]").First.ClickAsync();
            await page.WaitForTimeoutAsync(1500);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
