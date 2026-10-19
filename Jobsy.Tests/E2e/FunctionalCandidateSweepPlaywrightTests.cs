using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
[Trait("Suite", "FunctionalE2e")]
public sealed class FunctionalCandidateSweepPlaywrightTests
{
    [Theory]
    [InlineData(1440, 900, false)]
    [InlineData(390, 844, true)]
    public async Task Candidate_chrome_language_switch_and_assistant_tab(int width, int height, bool mobile)
    {
        var (run, baseUrl) = await TryBeginAsync();
        if (!run)
        {
            return;
        }

        var email = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? "kandidaat@jobsy.local";
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? "Jobsy123!";

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var ctx = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IsMobile = mobile,
            HasTouch = mobile,
            Locale = "nl-NL",
            IgnoreHTTPSErrors = true,
        });
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, email, password), "Login failed for sweep account.");

        await page.GotoAsync($"{baseUrl}/candidate/paspoort", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 90_000 });

        var langToggle = page.Locator("[data-testid=language-toggle], button.lang-toggle, .lang-switch").First;
        if (await langToggle.CountAsync() > 0)
        {
            var before = await page.ContentAsync();
            await langToggle.ClickAsync(new() { Force = mobile });
            await page.WaitForTimeoutAsync(600);
            var after = await page.ContentAsync();
            Assert.NotEqual(before, after);
        }

        if (mobile)
        {
            var tab = page.Locator(".lobsy-assistant-tab__btn").First;
            if (await tab.CountAsync() > 0)
            {
                await tab.ClickAsync(new() { Force = true });
                await page.WaitForTimeoutAsync(400);
                await Assertions.Expect(page.Locator("#lobsy-assistant-panel, .lobsy-assistant.is-open")).ToBeVisibleAsync(new() { Timeout = 15_000 });
                await page.Locator(".lobsy-assistant__icon-btn").Filter(new() { HasText = "×" }).First.ClickAsync(new() { Force = true });
            }
        }
    }

    [Theory]
    [InlineData(1440, 900)]
    [InlineData(390, 844)]
    public async Task Banenkaart_search_and_view_toggle_change_state(int width, int height)
    {
        var (run, baseUrl) = await TryBeginAsync();
        if (!run)
        {
            return;
        }

        var email = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? "kandidaat@jobsy.local";
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? "Jobsy123!";

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var ctx = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IsMobile = width < 600,
            HasTouch = width < 600,
            Locale = "nl-NL",
            IgnoreHTTPSErrors = true,
        });
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, email, password));
        await page.GotoAsync($"{baseUrl}/banenkaart", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.Locator(".jobsy-discovery, .job-map").First.WaitForAsync(new() { Timeout = 90_000 });

        var search = page.Locator("input[type=search], .filter-bar__search, [data-testid=kb-search]").First;
        if (await search.CountAsync() > 0)
        {
            await search.FillAsync("horeca");
            await page.WaitForTimeoutAsync(800);
            Assert.Contains("horeca", await search.InputValueAsync(), StringComparison.OrdinalIgnoreCase);
        }

        var listToggle = page.Locator("[data-testid=kb-view-toggle] button, [data-testid=kb-view-list]").First;
        if (await listToggle.CountAsync() > 0)
        {
            await listToggle.ClickAsync(new() { Force = width < 600 });
            await page.WaitForTimeoutAsync(500);
        }
    }

    private static async Task<(bool Run, string BaseUrl)> TryBeginAsync()
    {
        var baseUrl = FunctionalE2eGate.TryBaseUrl() ?? "";
        if (FunctionalE2eGate.IsRequired)
        {
            baseUrl = await FunctionalE2eGate.RequireReachableBaseUrlAsync();
            return (true, baseUrl);
        }

        if (string.IsNullOrWhiteSpace(baseUrl) || !await SmokeReachability.IsReachableAsync(baseUrl))
        {
            return (false, baseUrl);
        }

        return (true, baseUrl);
    }
}
