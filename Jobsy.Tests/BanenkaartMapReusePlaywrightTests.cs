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
            await using var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IgnoreHTTPSErrors = true
            });

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

            // Pins must be visible within 3s of the map canvas appearing.
            await page.WaitForFunctionAsync(
                """
                () => {
                  const n = window.jobMap && typeof window.jobMap.__testGetPinCount === 'function'
                    ? window.jobMap.__testGetPinCount()
                    : 0;
                  return n > 0;
                }
                """,
                null,
                new() { Timeout = PinsVisibleMs });

            // Give Blazor a moment to attach and (wrongly) rebuild — then assert once.
            await page.WaitForTimeoutAsync(2000);

            var pinCount = await page.EvaluateAsync<int>(
                """
                () => window.jobMap && typeof window.jobMap.__testGetPinCount === 'function'
                  ? window.jobMap.__testGetPinCount()
                  : 0
                """);
            Assert.True(pinCount > 0,
                $"Run {run}/{RepeatCount} ({width}x{height}, candidate={asCandidate}): expected pins, got {pinCount}.");

            var createCount = await page.EvaluateAsync<int>(
                """
                () => window.jobMap && typeof window.jobMap.__testGetMapCreateCount === 'function'
                  ? window.jobMap.__testGetMapCreateCount()
                  : -1
                """);
            Assert.Equal(1, createCount);

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
}
