using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Acc 27-09 layout guards (§2 floating tabs, §3 Top 10 on mobile).
/// Soft-skips without <c>JOBSY_E2E_BASE_URL</c>.
/// </summary>
[Collection("PlaywrightSmoke")]
public class Acc2709PlaywrightTests
{
    private const string DefaultEmail = "kandidaat@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";

    [Fact]
    public async Task Mobile_kompas_tabs_are_static_and_scroll_away()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        if (!await TryLoginAsync(page, baseUrl))
        {
            return;
        }

        await page.GotoAsync(baseUrl + "/candidate/profile", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        var tabs = page.Locator(".kompas-tabs.admin-sublinks");
        if (await tabs.CountAsync() == 0)
        {
            return;
        }

        var position = await tabs.EvaluateAsync<string>("el => getComputedStyle(el).position");
        Assert.Equal("static", position);

        await page.EvaluateAsync("""
            () => {
              const scroller = document.querySelector('.panel-page.profile-page') || document.scrollingElement;
              if (scroller) scroller.scrollTop = 800;
            }
            """);
        await page.WaitForTimeoutAsync(200);
        var below = await tabs.EvaluateAsync<bool>("""
            el => {
              const scroller = document.querySelector('.panel-page.profile-page') || document.documentElement;
              const scrollerTop = scroller.getBoundingClientRect ? scroller.getBoundingClientRect().top : 0;
              return el.getBoundingClientRect().bottom < scrollerTop + 1;
            }
            """);
        Assert.True(below);
    }

    [Fact]
    public async Task Desktop_kompas_tabs_remain_sticky()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1366, Height = 900 },
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        if (!await TryLoginAsync(page, baseUrl))
        {
            return;
        }

        await page.GotoAsync(baseUrl + "/candidate/profile", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        var tabs = page.Locator(".kompas-tabs.admin-sublinks");
        if (await tabs.CountAsync() == 0)
        {
            return;
        }

        var position = await tabs.EvaluateAsync<string>("el => getComputedStyle(el).position");
        Assert.Equal("sticky", position);
    }

    [Theory]
    [InlineData("dna")]
    [InlineData("profiel")]
    [InlineData("tests")]
    [InlineData("fit")]
    public async Task Mobile_profile_hides_top10_competency_matches(string tab)
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        if (!await TryLoginAsync(page, baseUrl))
        {
            return;
        }

        await page.GotoAsync(
            $"{baseUrl}/candidate/profile?tab={tab}",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.WaitForTimeoutAsync(400);
        Assert.Equal(0, await page.Locator(".competency-matches").CountAsync());
    }

    private static async Task<bool> TryLoginAsync(IPage page, string baseUrl)
    {
        var email = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? DefaultEmail;
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? DefaultPassword;
        try
        {
            await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
            await page.FillAsync("input[name='email']", email);
            await page.FillAsync("input[name='password']", password);
            await page.ClickAsync("button.login-submit");
            await page.WaitForURLAsync(
                url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
                new() { Timeout = 60_000 });
            return true;
        }
        catch
        {
            return false;
        }
    }
}
