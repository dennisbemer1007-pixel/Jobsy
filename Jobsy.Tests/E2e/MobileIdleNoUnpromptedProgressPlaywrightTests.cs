using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Mobile idle ~45s: no unprompted top nav-route progress bar (Acceptatie audit).
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class MobileIdleNoUnpromptedProgressPlaywrightTests
{
    private const string DefaultEmail = "onboarding.e2e@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";

    [Fact]
    public async Task Mobile_banenkaart_idle_45s_has_no_nav_progress_bar()
    {
        var baseUrl = ResolveBaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        await EmployersPlaywrightGuard.SkipIfEmployersOffAsync(baseUrl);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IgnoreHTTPSErrors = true,
            ServiceWorkers = ServiceWorkerPolicy.Allow
        });

        var page = await context.NewPageAsync();
        await page.AddInitScriptAsync(
            """
            () => {
              window.__lobsyLoadCount = 0;
              window.addEventListener("load", () => { window.__lobsyLoadCount = (window.__lobsyLoadCount || 0) + 1; });
            }
            """);

        var email = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? DefaultEmail;
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? DefaultPassword;
        if (!await TryLoginAsync(page, baseUrl, email, password))
        {
            Assert.Fail("Candidate login failed.");
        }

        await page.GotoAsync(baseUrl.TrimEnd('/') + "/banenkaart?weergave=kaart",
            new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        try
        {
            await page.WaitForFunctionAsync(
                "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
                null,
                new() { Timeout = 90_000 });
        }
        catch (TimeoutException)
        {
            // continue
        }

        await page.EvaluateAsync("() => { window.__lobsyLoadCount = 0; }");
        await page.WaitForTimeoutAsync(45_000);

        var reloads = await page.EvaluateAsync<int>("() => window.__lobsyLoadCount || 0");
        var progressActive = await page.EvaluateAsync<bool>(
            "() => !!document.querySelector('.nav-route-progress.is-active')");

        Assert.True(reloads == 0, $"Unexpected document reload during idle: {reloads}.");
        Assert.False(progressActive, "nav-route-progress should not appear during unprompted idle.");
    }

    private static string? ResolveBaseUrl()
    {
        var raw = Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL");
        return string.IsNullOrWhiteSpace(raw) ? null : raw.Trim().TrimEnd('/');
    }

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            using var response = await http.GetAsync(baseUrl + "/");
            return response.IsSuccessStatusCode || (int)response.StatusCode is 302 or 301;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> TryLoginAsync(IPage page, string baseUrl, string email, string password)
    {
        await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
        await page.FillAsync("input[name='email']", email);
        await page.FillAsync("input[name='password']", password);
        await page.ClickAsync("button.login-submit");
        try
        {
            await page.WaitForURLAsync(
                url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
                new() { Timeout = 60_000 });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }
}
