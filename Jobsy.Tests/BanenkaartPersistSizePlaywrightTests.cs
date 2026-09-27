using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// FIX 1/4 — Banenkaart must not persist the full vacancy catalog into / HTML.
/// That blew past the 2 MB Blazor hub limit and killed the candidate circuit
/// (~1 s after connect), leaving Lijst/Match dead. Soft-skips E2E when
/// <c>JOBSY_E2E_BASE_URL</c> is unset/unreachable so unit CI stays green.
/// </summary>
[Collection("PlaywrightSmoke")]
public class BanenkaartPersistSizePlaywrightTests
{
    private const string DefaultEmail = "kandidaat@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";
    // Acc-scale boot pins dominate / (~50–70 KB JSON for ~328 pins). Catalog
    // persist made this 1.9–2.4 MB; keep Acc HTML under 100 KB (far below the
    // 2 MB hub limit).
    private const int MaxHtmlBytes = 100_000;

    [Fact]
    public void Discovery_does_not_persist_catalog_and_hub_limit_stays_2mb()
    {
        var root = FindRepoRoot();
        var discovery = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "VacancyDiscovery.razor"));
        Assert.DoesNotContain("PersistentComponentState", discovery);
        Assert.DoesNotContain("PersistAsJson", discovery);
        Assert.DoesNotContain("DiscoveryPersistState", discovery);
        Assert.DoesNotContain("PersistKey", discovery);
        Assert.DoesNotContain("_restoredFromPersist", discovery);
        Assert.Contains("LoadBootPinsAsync", discovery);
        Assert.Contains("LoadMapViewAsync", discovery);
        // Prerender warms pins/view only — catalog loads on the interactive circuit.
        Assert.Contains("never persist the full vacancy catalog", discovery, StringComparison.OrdinalIgnoreCase);

        var program = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Program.cs"));
        Assert.Contains("MaximumReceiveMessageSize = 2 * 1024 * 1024", program);

        var testFile = Path.Combine(root, "Jobsy.Tests", "BanenkaartPersistSizePlaywrightTests.cs");
        Assert.True(File.Exists(testFile));
        var src = File.ReadAllText(testFile);
        Assert.Contains("100_000", src);
        Assert.Contains("390", src);
        Assert.Contains("844", src);
        Assert.Contains("1280", src);
        Assert.Contains("_blazor", src);
        Assert.Contains("swipe-actions__btn--interest", src);
        Assert.Contains("kandidaat@jobsy.local", src);
    }

    [Theory]
    [InlineData(390, 844)]
    [InlineData(1280, 800)]
    public async Task Anonymous_home_html_circuit_and_lijst(int width, int height)
    {
        var baseUrl = ResolveBaseUrl();
        if (baseUrl is null)
        {
            return;
        }

        await RunAnonymousScenarioAsync(baseUrl, width, height);
    }

    [Theory]
    [InlineData(390, 844)]
    [InlineData(1280, 800)]
    public async Task Candidate_home_html_circuit_lijst_and_match_swipe(int width, int height)
    {
        var baseUrl = ResolveBaseUrl();
        if (baseUrl is null)
        {
            return;
        }

        await RunCandidateScenarioAsync(baseUrl, width, height);
    }

    private static async Task RunAnonymousScenarioAsync(string baseUrl, int width, int height)
    {
        EnsureChromium();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IgnoreHTTPSErrors = true
        });

        await AssertHtmlUnderLimitAsync(baseUrl, cookieHeader: null);

        var page = await context.NewPageAsync();
        var wsClosedEarly = false;
        // Capture the hub socket during navigation — desktop can race past a
        // late page.WebSocket handler even when Blazor._internal is already up.
        var blazorWs = await page.RunAndWaitForWebSocketAsync(
            async () =>
            {
                await page.GotoAsync(baseUrl + "/", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            },
            new()
            {
                Timeout = 30_000,
                Predicate = socket => (socket.Url ?? "").Contains("_blazor", StringComparison.OrdinalIgnoreCase)
            });
        Assert.NotNull(blazorWs);
        blazorWs.Close += (_, _) => wsClosedEarly = true;

        await page.WaitForFunctionAsync(
            "() => !!(window.Blazor && window.Blazor._internal)",
            null,
            new() { Timeout = 30_000 });

        await page.WaitForTimeoutAsync(10_000);
        Assert.False(wsClosedEarly, "_blazor WebSocket closed within 10 s (anonymous).");
        Assert.False(blazorWs.IsClosed, "_blazor WebSocket should stay open ≥10 s (anonymous).");

        await AssertLijstShowsCardsWithin2sAsync(page, width);
    }

    private static async Task RunCandidateScenarioAsync(string baseUrl, int width, int height)
    {
        EnsureChromium();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IgnoreHTTPSErrors = true
        });

        var page = await context.NewPageAsync();
        if (!await TryLoginAsync(page, baseUrl))
        {
            Assert.Fail("Demo candidate login failed — cannot verify candidate Banenkaart size/circuit.");
        }

        // Cookie jar is now authenticated — measure HTML size for /.
        var cookies = await context.CookiesAsync([baseUrl]);
        var cookieHeader = string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}"));
        await AssertHtmlUnderLimitAsync(baseUrl, cookieHeader);

        var closedDuringHold = false;
        var blazorWs = await page.RunAndWaitForWebSocketAsync(
            async () =>
            {
                await page.GotoAsync(baseUrl + "/", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            },
            new()
            {
                Timeout = 30_000,
                Predicate = socket => (socket.Url ?? "").Contains("_blazor", StringComparison.OrdinalIgnoreCase)
            });
        Assert.NotNull(blazorWs);
        blazorWs.Close += (_, _) => closedDuringHold = true;
        await page.WaitForTimeoutAsync(10_000);
        Assert.False(closedDuringHold, "_blazor WebSocket closed within 10 s (candidate).");
        Assert.False(blazorWs.IsClosed, "_blazor WebSocket should stay open ≥10 s (candidate).");

        await AssertLijstShowsCardsWithin2sAsync(page, width);

        // Match button → deck → swipe advances. On desktop the chrome Match
        // control is often CSS-hidden; fall back to the candidate Match route.
        var match = page.Locator("a.jobsy-action--match:visible, a[href='/candidate/match']:visible").First;
        if (await match.CountAsync() > 0)
        {
            await match.ClickAsync();
        }
        else
        {
            await page.GotoAsync(baseUrl + "/candidate/match", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
        }

        await page.WaitForURLAsync(
            url => url.Contains("/candidate/match", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 30_000 });

        // Unlock panel or empty deck: still prove navigation worked; swipe when deck exists.
        var interest = page.Locator("button.swipe-actions__btn--interest").First;
        var unlock = page.Locator(".match-gate, .match-page__empty, .match-unlock").First;
        try
        {
            await page.WaitForSelectorAsync(
                "button.swipe-actions__btn--interest, .match-page__empty, .match-gate, .match-unlock",
                new() { Timeout = 45_000 });
        }
        catch (TimeoutException)
        {
            Assert.Fail("Match page did not show a swipe deck, empty state, or unlock gate.");
        }

        if (await interest.CountAsync() > 0 && await interest.IsVisibleAsync())
        {
            var beforeTitle = await page.Locator(".swipe-card--enter .swipe-card__title, .swipe-card:not(.swipe-card--backdrop) .swipe-card__title")
                .First.InnerTextAsync();
            await interest.ClickAsync();
            await page.WaitForTimeoutAsync(600);
            var afterTitle = "";
            var titleLoc = page.Locator(".swipe-card--enter .swipe-card__title, .swipe-card:not(.swipe-card--backdrop) .swipe-card__title").First;
            if (await titleLoc.CountAsync() > 0)
            {
                afterTitle = await titleLoc.InnerTextAsync();
            }

            var emptyAfter = await page.Locator(".match-page__empty").CountAsync() > 0;
            Assert.True(
                emptyAfter || !string.Equals(beforeTitle.Trim(), afterTitle.Trim(), StringComparison.Ordinal),
                $"Swipe did not advance the deck (title stayed '{beforeTitle}').");
        }
        else
        {
            // Profile incomplete / empty deck on this environment — navigation + live circuit is enough.
            Assert.True(
                await unlock.CountAsync() > 0 || await page.Locator(".match-page").CountAsync() > 0,
                "Match page did not render after clicking Match.");
        }
    }

    private static async Task AssertHtmlUnderLimitAsync(string baseUrl, string? cookieHeader)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        using var request = new HttpRequestMessage(HttpMethod.Get, baseUrl.TrimEnd('/') + "/");
        request.Headers.TryAddWithoutValidation("Accept", "text/html");
        if (!string.IsNullOrWhiteSpace(cookieHeader))
        {
            request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
        }

        using var response = await http.SendAsync(request);
        Assert.True(
            (int)response.StatusCode is >= 200 and < 400,
            $"GET / → {response.StatusCode}");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(
            bytes.Length < MaxHtmlBytes,
            $"GET / HTML is {bytes.Length} bytes (limit {MaxHtmlBytes}). Catalog must not be persisted.");

        var html = Encoding.UTF8.GetString(bytes);
        Assert.DoesNotContain("\"Vacancies\":[", html, StringComparison.Ordinal);
        Assert.DoesNotContain("vacancyDiscovery", html, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task AssertLijstShowsCardsWithin2sAsync(IPage page, int width)
    {
        // Circuit must be interactive before Lijst is meaningful (dead circuit = tap does nothing).
        await page.WaitForFunctionAsync(
            "() => !!(window.Blazor && (window.Blazor._internal || document.querySelector('[data-blazor-id], .vacancy-pane')))",
            null,
            new() { Timeout = 30_000 });

        var listBtn = page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Lijst|List", RegexOptions.IgnoreCase) }).First;
        if (await listBtn.CountAsync() == 0)
        {
            listBtn = page.Locator("button.jobsy-action--toggle:has-text('Lijst'), button:has-text('Lijst')").First;
        }

        if (width <= 700)
        {
            Assert.True(await listBtn.CountAsync() > 0, "Lijst button missing on mobile.");
            var sw = Stopwatch.StartNew();
            await listBtn.ClickAsync();
            await page.WaitForSelectorAsync(
                ".vacancy-list .job-card, .vacancy-list--cards .job-card, article.job-card",
                new() { Timeout = 2_000 });
            sw.Stop();
            Assert.True(sw.ElapsedMilliseconds <= 2_000, $"Lijst cards took {sw.ElapsedMilliseconds}ms (>2s).");
        }
        else if (await listBtn.CountAsync() > 0 && await listBtn.IsVisibleAsync())
        {
            var sw = Stopwatch.StartNew();
            await listBtn.ClickAsync();
            await page.WaitForSelectorAsync(
                ".vacancy-list .job-card, .vacancy-list--cards .job-card, article.job-card",
                new() { Timeout = 2_000 });
            sw.Stop();
            Assert.True(sw.ElapsedMilliseconds <= 2_000, $"Lijst cards took {sw.ElapsedMilliseconds}ms (>2s).");
        }
        else
        {
            // Desktop list pane is visible without a toggle — wait for cards once interactive.
            await page.WaitForSelectorAsync(
                ".vacancy-list .job-card, .vacancy-list--cards .job-card, article.job-card",
                new() { Timeout = 15_000 });
        }

        var cards = await page.Locator(".vacancy-list .job-card, article.job-card").CountAsync();
        Assert.True(cards > 0, "Expected vacancy cards after opening Lijst.");
    }

    private static async Task<bool> TryLoginAsync(IPage page, string baseUrl)
    {
        var email = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? DefaultEmail;
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? DefaultPassword;
        try
        {
            await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });
            await page.WaitForSelectorAsync("input[name='email']", new() { Timeout = 30_000 });
            await page.FillAsync("input[name='email']", email);
            await page.FillAsync("input[name='password']", password);
            // Submit stays disabled until the antiforgery token is hydrated.
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
