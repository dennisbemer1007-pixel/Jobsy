using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Desktop (1440): no unprompted full document reload while idle (Acceptatie audit signal).
/// Uses load-event counting after circuit ready — initial navigation is not counted.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class DesktopIdleNoReloadPlaywrightTests
{
    private const string DefaultEmail = "onboarding.e2e@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";

    [Theory]
    [InlineData("/", false)]
    [InlineData("/candidate/paspoort?tab=dna", true)]
    [InlineData("/banenkaart?weergave=kaart", true)]
    public async Task Idle_45s_has_no_unprompted_document_reload(string path, bool login)
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
            ViewportSize = new() { Width = 1440, Height = 900 },
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

        if (login)
        {
            var email = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? DefaultEmail;
            var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? DefaultPassword;
            if (!await TryLoginAsync(page, baseUrl, email, password))
            {
                Assert.Fail("Candidate login failed.");
            }
        }

        await page.GotoAsync(baseUrl.TrimEnd('/') + path, new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        try
        {
            await page.WaitForFunctionAsync(
                "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
                null,
                new() { Timeout = 90_000 });
        }
        catch (TimeoutException)
        {
            // Guest home may not set circuit; still watch load count.
        }

        await page.EvaluateAsync("() => { window.__lobsyLoadCount = 0; }");
        await page.WaitForTimeoutAsync(45_000);

        var reloads = await page.EvaluateAsync<int>("() => window.__lobsyLoadCount || 0");
        Assert.True(reloads == 0, $"Unexpected document reload on {path} at 1440×900: {reloads} load event(s).");
    }

    private static string? ResolveBaseUrl()
    {
        var raw = Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL");
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return raw.Trim().TrimEnd('/');
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
        await page.GotoAsync(baseUrl + "/login?returnUrl=%2Fbanenkaart", new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 90_000
        });
        await page.FillAsync("input[name='email']", email);
        await page.FillAsync("input[name='password']", password);
        var submit = page.Locator("button.login-submit, button.au-submit[type=submit]");
        await submit.ClickAsync();
        try
        {
            await page.WaitForURLAsync(
                url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
                new() { Timeout = 90_000 });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }
}
