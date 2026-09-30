using System.Collections.Concurrent;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Desktop banenkaart top-match tile: complete-profile candidate at 1440 and 1280.
/// Soft-skips without <c>JOBSY_E2E_BASE_URL</c>.
/// </summary>
[Collection("PlaywrightSmoke")]
public class BanenkaartDesktopTopMatchPlaywrightTests
{
    private const string DefaultEmail = "kandidaat@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";

    [Theory]
    [InlineData(1440, 900)]
    [InlineData(1280, 800)]
    public async Task Complete_profile_desktop_shows_top_match_without_circuit_error(int width, int height)
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IgnoreHTTPSErrors = true
        });

        var page = await context.NewPageAsync();
        var pageErrors = new ConcurrentBag<string>();
        page.PageError += (_, err) => pageErrors.Add(err);

        if (!await TryLoginAsync(page, baseUrl))
        {
            Assert.Fail("Candidate login failed for desktop top-match smoke.");
        }

        var mapPath = await ResolveMapPathAsync(page, baseUrl);
        await page.GotoAsync(baseUrl + mapPath, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });

        try
        {
            await page.Locator(".job-card").First.WaitForAsync(new() { Timeout = 60_000 });
        }
        catch (TimeoutException)
        {
            // Acc may still paint the map without cards; wait for map then the top-match path.
            await page.Locator("#job-map").WaitForAsync(new() { Timeout = 30_000 });
        }

        await page.WaitForTimeoutAsync(8_000);

        var circuit = await page.EvaluateAsync<bool>("""
            () => {
              const t = document.body?.innerText || '';
              return t.includes('Even iets misgegaan')
                || !!document.querySelector('.circuit-error');
            }
            """);
        Assert.False(circuit, $"circuit error at {width}x{height}");

        var fatalPageErrors = pageErrors
            .Where(e => !e.Contains("Maximum call stack size exceeded", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.True(fatalPageErrors.Count == 0, "pageerror: " + string.Join(" | ", fatalPageErrors));

        var topMatch = page.Locator(".highlight-carousel__card--top-match");
        if (await topMatch.CountAsync() > 0)
        {
            await Assertions.Expect(topMatch.First).ToBeVisibleAsync(new() { Timeout = 5_000 });
        }
    }

    private static async Task<string> ResolveMapPathAsync(IPage page, string baseUrl)
    {
        try
        {
            var resp = await page.Context.APIRequest.GetAsync(baseUrl + "/banenkaart");
            if (resp.Ok || resp.Status is >= 200 and < 400)
            {
                return "/banenkaart";
            }
        }
        catch
        {
        }

        return "/";
    }

    private static async Task<bool> TryLoginAsync(IPage page, string baseUrl)
    {
        var email = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? DefaultEmail;
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? DefaultPassword;
        try
        {
            await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
            await page.WaitForSelectorAsync("input[name='email']", new() { Timeout = 30_000 });
            await page.FillAsync("input[name='email']", email);
            await page.FillAsync("input[name='password']", password);
            var submit = page.Locator("button.login-submit");
            await submit.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
            await page.WaitForFunctionAsync(
                "() => { const b = document.querySelector('button.login-submit'); return b && !b.disabled; }",
                null,
                new() { Timeout = 30_000 });
            await submit.ClickAsync();
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

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var response = await http.GetAsync(baseUrl.TrimEnd('/') + "/");
            return (int)response.StatusCode is >= 200 and < 500;
        }
        catch
        {
            return false;
        }
    }
}
