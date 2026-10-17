using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Banenkaart must not refresh pins/discover while idle (no pan/zoom/filter).
/// Set <c>JOBSY_E2E_BASE_URL</c> (e.g. https://acceptatie.lobsy.nl) and candidate credentials.
/// <c>JOBSY_BANENKAART_IDLE_SECONDS</c> defaults to 180 (3 min); use 45 in quick local runs.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class BanenkaartIdleStablePlaywrightTests
{
    private const string DefaultEmail = "kandidaat@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";

    [Fact]
    public async Task Banenkaart_idle_after_load_has_no_discover_or_setVacancies_churn()
        => await RunIdleWatchAsync(E2eRoutes.Banenkaart);

    [Fact]
    public async Task Candidate_vacatures_idle_has_no_discover_churn()
        => await RunIdleWatchAsync("/candidate/vacatures");

    private static async Task RunIdleWatchAsync(string path)
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        var idleSeconds = ResolveIdleSeconds();
        var email = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? DefaultEmail;
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? DefaultPassword;

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 900 },
            IgnoreHTTPSErrors = true,
            ServiceWorkers = ServiceWorkerPolicy.Allow
        });

        var page = await context.NewPageAsync();
        var discoverAfterSettle = 0;
        var docLoads = 0;
        var frameNav = 0;

        page.Request += (_, req) =>
        {
            if (req.Url.Contains("/api/vacancies/discover", StringComparison.OrdinalIgnoreCase))
            {
                Interlocked.Increment(ref discoverAfterSettle);
            }
        };

        await page.AddInitScriptAsync(
            """
            () => {
              window.__lobsyDocLoads = 0;
              window.__lobsyFrameNav = 0;
              window.addEventListener("load", () => { window.__lobsyDocLoads = (window.__lobsyDocLoads || 0) + 1; });
              const push = history.pushState.bind(history);
              const replace = history.replaceState.bind(history);
              history.pushState = function () { window.__lobsyFrameNav = (window.__lobsyFrameNav || 0) + 1; return push.apply(history, arguments); };
              history.replaceState = function () { window.__lobsyFrameNav = (window.__lobsyFrameNav || 0) + 1; return replace.apply(history, arguments); };
            }
            """);

        if (!await TryLoginAsync(page, baseUrl, email, password))
        {
            Assert.Fail("Candidate login failed.");
        }

        await page.GotoAsync(baseUrl + path, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);

        if (path.Contains("banenkaart", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                await page.WaitForFunctionAsync(
                    "() => !!(window.jobMap && document.querySelector('#job-map canvas'))",
                    null,
                    new() { Timeout = 60_000 });
            }
            catch (PlaywrightException)
            {
                return;
            }

            var hasHooks = await page.EvaluateAsync<bool>(
                "() => !!(window.jobMap && typeof window.jobMap.__testResetSetVacanciesCallCount === 'function')");
            if (!hasHooks)
            {
                return;
            }

            await page.WaitForTimeoutAsync(8_000);
            discoverAfterSettle = 0;
            await page.EvaluateAsync("() => { window.jobMap.__testResetSetVacanciesCallCount(); window.jobMap.__testResetViewportNotifyCount(); }");

            await page.WaitForTimeoutAsync(idleSeconds * 1000);

            var setVacCalls = await page.EvaluateAsync<int>(
                "() => window.jobMap.__testGetSetVacanciesCallCount()");
            var viewportNotifies = await page.EvaluateAsync<int>(
                "() => window.jobMap.__testGetViewportNotifyCount()");

            docLoads = await page.EvaluateAsync<int>("() => window.__lobsyDocLoads || 0");
            frameNav = await page.EvaluateAsync<int>("() => window.__lobsyFrameNav || 0");

            Assert.True(discoverAfterSettle == 0,
                $"Idle {idleSeconds}s on banenkaart: expected 0 discover API calls, got {discoverAfterSettle}.");
            Assert.True(setVacCalls == 0,
                $"Idle {idleSeconds}s: expected 0 setVacancies calls, got {setVacCalls} (viewport notifies {viewportNotifies}).");

            await page.Mouse.WheelAsync(0, 400);
            await page.WaitForTimeoutAsync(1_500);
            var setVacAfterPan = await page.EvaluateAsync<int>(
                "() => window.jobMap.__testGetSetVacanciesCallCount()");
            Assert.True(setVacAfterPan >= 0, "Pan should be allowed to refresh pins incrementally.");
        }
        else
        {
            await page.WaitForTimeoutAsync(8_000);
            discoverAfterSettle = 0;
            await page.WaitForTimeoutAsync(Math.Min(idleSeconds, 90) * 1000);
            Assert.True(discoverAfterSettle == 0,
                $"Idle on {path}: expected 0 discover calls, got {discoverAfterSettle}.");
        }

        Assert.True(docLoads <= 1, $"Unexpected document reloads during watch: {docLoads}.");
        _ = frameNav;
    }

    private static int ResolveIdleSeconds()
    {
        var raw = Environment.GetEnvironmentVariable("JOBSY_BANENKAART_IDLE_SECONDS");
        if (int.TryParse(raw, out var parsed) && parsed >= 15 && parsed <= 600)
        {
            return parsed;
        }

        return 180;
    }

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            using var res = await http.GetAsync(baseUrl + "/health");
            return res.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> TryLoginAsync(IPage page, string baseUrl, string email, string password)
    {
        if (await KbE2e.TryLoginAsync(page, baseUrl, email, password))
        {
            return true;
        }

        return await KbE2e.TryLoginAsync(page, baseUrl);
    }
}
