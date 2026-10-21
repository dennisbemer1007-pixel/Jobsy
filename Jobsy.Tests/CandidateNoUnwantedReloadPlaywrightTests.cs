using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Ensures candidate flows do not full-page reload in the background (deploy bursts, SW updates, reconnect).
/// Set <c>JOBSY_RELOAD_WATCH_MINUTES</c> to override the default watch window (2 in CI, up to 10 manually).
/// </summary>
[Collection("PlaywrightSmoke")]
public class CandidateNoUnwantedReloadPlaywrightTests
{
    private const string DefaultEmail = "kandidaat@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";

    [Fact]
    public async Task Ontdekkingsreis_overview_idle_watch_has_no_unprompted_document_reload()
    {
        var baseUrl = ResolveBaseUrl();
        if (baseUrl is null)
        {
            return;
        }

        var email = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? DefaultEmail;
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? DefaultPassword;

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 800 },
            IgnoreHTTPSErrors = true,
            ServiceWorkers = ServiceWorkerPolicy.Allow
        });

        var page = await context.NewPageAsync();
        await page.AddInitScriptAsync(
            """
            () => {
              window.__lobsyNavCount = 0;
              window.addEventListener("load", () => { window.__lobsyNavCount = (window.__lobsyNavCount || 0) + 1; });
            }
            """);

        if (!await TryLoginAsync(page, baseUrl, email, password))
        {
            Assert.Fail("Candidate login failed.");
        }

        await page.GotoAsync(baseUrl + "/candidate/ontdekkingsreis", new() { WaitUntil = WaitUntilState.Load, Timeout = 90_000 });
        await page.WaitForSelectorAsync(".journey-overview h1", new() { Timeout = 45_000 });
        await page.EvaluateAsync("() => { window.__lobsyNavCount = 0; }");

        var end = DateTime.UtcNow.AddMinutes(Math.Min(ResolveWatchMinutes(), 4));
        while (DateTime.UtcNow < end)
        {
            await page.Mouse.WheelAsync(0, 320);
            await page.WaitForTimeoutAsync(8_000);
        }

        var navCount = await page.EvaluateAsync<int>("() => window.__lobsyNavCount || 0");
        Assert.True(navCount == 0, $"Unexpected reload on ontdekkingsreis overview: {navCount} load events.");
    }

    [Fact]
    public async Task Candidate_start_idle_watch_has_no_unprompted_document_reload()
    {
        var baseUrl = ResolveBaseUrl();
        if (baseUrl is null)
        {
            return;
        }

        await RunIdleWatchAsync(baseUrl, ResolveWatchMinutes());
    }

    private static async Task RunIdleWatchAsync(
        string baseUrl,
        int watchMinutes,
        string startPath = "/candidate/start",
        bool skipLogin = false)
    {
        var email = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? DefaultEmail;
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? DefaultPassword;

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
              window.__lobsyNavCount = 0;
              window.addEventListener("load", () => { window.__lobsyNavCount = (window.__lobsyNavCount || 0) + 1; });
            }
            """);

        if (!skipLogin)
        {
            if (!await TryLoginAsync(page, baseUrl, email, password))
            {
                Assert.Fail("Candidate login failed — cannot watch for unwanted reloads.");
            }
        }

        await page.GotoAsync(baseUrl + startPath, new() { WaitUntil = WaitUntilState.Load, Timeout = 90_000 });
        try
        {
            await page.WaitForFunctionAsync(
                "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
                null,
                new() { Timeout = 45_000 });
        }
        catch (TimeoutException)
        {
            Assert.Fail("Blazor circuit did not become ready — cannot measure idle reloads.");
        }
        await page.EvaluateAsync("() => { window.__lobsyNavCount = 0; }");

        var end = DateTime.UtcNow.AddMinutes(watchMinutes);
        while (DateTime.UtcNow < end)
        {
            await page.Mouse.WheelAsync(0, 280);
            await page.WaitForTimeoutAsync(8_000);
            await page.Keyboard.PressAsync("Tab");
            await page.WaitForTimeoutAsync(7_000);
        }

        var navCount = await page.EvaluateAsync<int>("() => window.__lobsyNavCount || 0");
        Assert.True(
            navCount == 0,
            $"Expected no unprompted document reload during {watchMinutes}m idle watch, got {navCount} load events.");
    }

    [Fact]
    public void Blazor_boot_and_service_worker_do_not_auto_reload_during_use()
    {
        var root = FindRepoRoot();
        var boot = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/js/blazor-boot.js"));
        Assert.DoesNotContain("location.reload()", boot, StringComparison.Ordinal);
        Assert.DoesNotContain("onRejectedAutoReload", boot, StringComparison.Ordinal);

        var core = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/js/app-core.js"));
        Assert.Contains("Nieuwe versie beschikbaar", core, StringComparison.Ordinal);
        Assert.Contains("userRequestedReload", core, StringComparison.Ordinal);

        var extras = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/js/app-extras.js"));
        Assert.Contains("confirmSessionExpiredThenLogout", extras, StringComparison.Ordinal);
        Assert.Contains("fetchSessionActivity(\"POST\").then(function (r) {\n                if (!sessionActivityOk(r)) {\n                    confirmSessionExpiredThenLogout();", extras, StringComparison.Ordinal);

        var sw = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/service-worker.published.js"));
        Assert.DoesNotContain("return self.skipWaiting();", sw, StringComparison.Ordinal);

        var legacySw = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/image-cache-sw.js"));
        Assert.Contains("unregister()", legacySw, StringComparison.Ordinal);
        Assert.DoesNotContain("clients.navigate", legacySw, StringComparison.Ordinal);
    }

    private static string? ResolveBaseUrl()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return null;
        }

        return IsReachableAsync(baseUrl).GetAwaiter().GetResult() ? baseUrl : null;
    }

    private static int ResolveWatchMinutes()
    {
        var raw = Environment.GetEnvironmentVariable("JOBSY_RELOAD_WATCH_MINUTES");
        if (int.TryParse(raw, out var minutes) && minutes is >= 1 and <= 15)
        {
            return minutes;
        }

        return string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL")) ? 2 : 3;
    }

    private static async Task<bool> TryLoginAsync(IPage page, string baseUrl, string email, string password)
    {
        try
        {
            await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
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
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            using var response = await http.GetAsync(baseUrl.TrimEnd('/') + "/");
            return (int)response.StatusCode is >= 200 and < 500;
        }
        catch
        {
            return false;
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}
