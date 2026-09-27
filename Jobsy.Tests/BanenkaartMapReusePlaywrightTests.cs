using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// FIX 2 — Banenkaart must keep the early-boot MapLibre instance when Blazor
/// attaches (no dispose+rebuild) and must redraw pins from the cached payload
/// on HTTP 304. Soft-skips when <c>JOBSY_E2E_BASE_URL</c> is unset/unreachable.
/// </summary>
[Collection("PlaywrightSmoke")]
public class BanenkaartMapReusePlaywrightTests
{
    private const string DefaultEmail = "kandidaat@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";
    private const int RepeatCount = 5;
    private const int PinsVisibleMs = 3_000;

    [Fact]
    public void Job_map_reuses_boot_map_and_redraws_pins_on_304()
    {
        var root = FindRepoRoot();
        var js = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "jobMap.js"));
        Assert.Contains("pinsCachedPayload", js);
        Assert.Contains("res.status === 304", js);
        Assert.Contains("return pinsCachedPayload", js);
        Assert.Contains("function adoptMapContainer", js);
        Assert.Contains("mapCreateCount", js);
        Assert.Contains("__testGetMapCreateCount", js);

        var disposeStart = js.IndexOf("function dispose()", StringComparison.Ordinal);
        Assert.True(disposeStart > 0);
        var disposeEnd = js.IndexOf("function escapeHtml", disposeStart, StringComparison.Ordinal);
        Assert.True(disposeEnd > disposeStart);
        var disposeFn = js[disposeStart..disposeEnd];
        Assert.Contains("pinsEtag = null", disposeFn);
        Assert.Contains("pinsCachedPayload = null", disposeFn);

        var initStart = js.IndexOf("function init(elementId, vacancies, options)", StringComparison.Ordinal);
        var initEnd = js.IndexOf("function locateIconHtml", initStart, StringComparison.Ordinal);
        Assert.True(initStart > 0 && initEnd > initStart);
        var initFn = js[initStart..initEnd];
        Assert.Contains("adoptMapContainer(el)", initFn);
        Assert.DoesNotContain("if (map) {\n                dispose();", initFn);

        var maps = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "maps-loader.js"));
        Assert.Contains("jobMap.min.js?v=20260927-mapreuse", maps);

        var testFile = Path.Combine(root, "Jobsy.Tests", "BanenkaartMapReusePlaywrightTests.cs");
        Assert.True(File.Exists(testFile));
        var src = File.ReadAllText(testFile);
        Assert.Contains("390", src);
        Assert.Contains("844", src);
        Assert.Contains("1280", src);
        Assert.Contains("RepeatCount = 5", src);
        Assert.Contains("PinsVisibleMs = 3_000", src);
        Assert.Contains("__testGetMapCreateCount", src);
        Assert.Contains("kandidaat@jobsy.local", src);
    }

    [Theory]
    [InlineData(390, 844, false)]
    [InlineData(1280, 800, false)]
    [InlineData(390, 844, true)]
    [InlineData(1280, 800, true)]
    public async Task Home_map_pins_once_no_rebuild(int width, int height, bool asCandidate)
    {
        var baseUrl = ResolveBaseUrl();
        if (baseUrl is null)
        {
            return;
        }

        EnsureChromium();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        for (var run = 1; run <= RepeatCount; run++)
        {
            var injectLocal = string.Equals(
                Environment.GetEnvironmentVariable("JOBSY_E2E_INJECT_LOCAL_JOBMAP"),
                "1",
                StringComparison.Ordinal);

            await using var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IgnoreHTTPSErrors = true,
                // Acc PWA service worker bypasses page.route for jobMap — block it when injecting.
                ServiceWorkers = injectLocal ? ServiceWorkerPolicy.Block : ServiceWorkerPolicy.Allow
            });

            // Optional: verify this branch's jobMap against a remote Acc URL
            // that has not deployed the fix yet (JOBSY_E2E_INJECT_LOCAL_JOBMAP=1).
            // Route on the context before login so candidate sessions also get it.
            if (injectLocal)
            {
                var localMin = Path.Combine(FindRepoRoot(), "Jobsy.Web", "wwwroot", "js", "jobMap.min.js");
                var body = await File.ReadAllBytesAsync(localMin);
                var js = System.Text.Encoding.UTF8.GetString(body);
                await context.RouteAsync("**/jobMap.min.js**", async route =>
                {
                    await route.FulfillAsync(new()
                    {
                        Status = 200,
                        ContentType = "application/javascript; charset=utf-8",
                        Body = js
                    });
                });
            }

            if (asCandidate)
            {
                var loginPage = await context.NewPageAsync();
                if (!await TryLoginAsync(loginPage, baseUrl))
                {
                    return;
                }

                await loginPage.CloseAsync();
            }

            var page = await context.NewPageAsync();
            string? pageError = null;
            page.PageError += (_, error) => { pageError ??= error; };

            await page.GotoAsync(baseUrl + "/", new()
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 90_000
            });

            await page.WaitForSelectorAsync(
                "#job-map canvas, #job-map .maplibregl-canvas, canvas.maplibregl-canvas",
                new() { Timeout = 60_000 });

            // Pins within 3s, single MapLibre instance, still painted after Blazor attach.
            await page.WaitForFunctionAsync(
                """
                () => {
                  if (!window.jobMap || typeof window.jobMap.__testGetPinCount !== 'function') {
                    return false;
                  }
                  const pins = window.jobMap.__testGetPinCount();
                  const creates = typeof window.jobMap.__testGetMapCreateCount === 'function'
                    ? window.jobMap.__testGetMapCreateCount()
                    : -1;
                  const alive = typeof window.jobMap.isAlive === 'function'
                    ? window.jobMap.isAlive()
                    : true;
                  return pins > 0 && creates === 1 && alive;
                }
                """,
                null,
                new() { Timeout = PinsVisibleMs });

            // Blazor attach race window (~1.5s) — pins and createCount must hold.
            await page.WaitForTimeoutAsync(2000);

            var snapshot = await page.EvaluateAsync<MapSnapshot>(
                """
                () => ({
                  pins: window.jobMap && typeof window.jobMap.__testGetPinCount === 'function'
                    ? window.jobMap.__testGetPinCount() : 0,
                  creates: window.jobMap && typeof window.jobMap.__testGetMapCreateCount === 'function'
                    ? window.jobMap.__testGetMapCreateCount() : -1,
                  alive: window.jobMap && typeof window.jobMap.isAlive === 'function'
                    ? window.jobMap.isAlive() : false
                })
                """);
            Assert.True(snapshot.Pins > 0,
                $"Run {run}/{RepeatCount} ({width}x{height}, candidate={asCandidate}): expected pins, got {snapshot.Pins}.");
            Assert.Equal(1, snapshot.Creates);
            Assert.True(snapshot.Alive,
                $"Run {run}/{RepeatCount}: map not alive after Blazor settle.");

            Assert.True(string.IsNullOrEmpty(pageError),
                $"Run {run}/{RepeatCount}: page error: {pageError}");

            await page.CloseAsync();
        }
    }

    private static async Task<bool> TryLoginAsync(IPage page, string baseUrl)
    {
        var email = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? DefaultEmail;
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? DefaultPassword;
        try
        {
            await page.GotoAsync(baseUrl + "/login", new()
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 60_000
            });
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

    private static string? ResolveBaseUrl()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return null;
        }

        return IsReachableAsync(baseUrl).GetAwaiter().GetResult() ? baseUrl : null;
    }

    private static void EnsureChromium()
        => Microsoft.Playwright.Program.Main(["install", "chromium"]);

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

    private sealed class MapSnapshot
    {
        public int Pins { get; set; }
        public int Creates { get; set; }
        public bool Alive { get; set; }
    }
}
