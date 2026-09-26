using System.Collections.Concurrent;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Full mobile/desktop smoke against a running Web+API stack (CI or JOBSY_E2E_BASE_URL).
/// Soft-skips when the base URL is unset/unreachable so unit CI stays green offline.
/// </summary>
[Collection("PlaywrightSmoke")]
public class MobileSmokePlaywrightTests
{
    private const string DefaultPassword = "Jobsy123!";
    private static readonly string[] CandidateTabs =
    [
        "/",
        "/candidate/liked",
        "/candidate/applications",
        "/carriere",
        "/candidate/profile"
    ];

    [Fact]
    public async Task Smoke_anonymous_map_and_candidate_employer_admin()
    {
        var baseUrl = ResolveBaseUrl();
        if (string.IsNullOrWhiteSpace(baseUrl) || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        var artifactDir = Path.Combine(
            FindRepoRoot(),
            "artifacts",
            "playwright-smoke",
            DateTime.UtcNow.ToString("yyyyMMddHHmmss"));
        Directory.CreateDirectory(artifactDir);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        // --- Mobile anonymous map ---
        await using var mobile = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            Geolocation = new Geolocation { Latitude = 52.07f, Longitude = 4.3f },
            Permissions = ["geolocation"],
            IgnoreHTTPSErrors = true
        });
        var mobilePage = await mobile.NewPageAsync();
        var mobileGuard = AttachGuards(mobilePage);
        // Register before Goto — pins often complete during NetworkIdle and would
        // be missed by a post-navigation WaitForResponse.
        var mobilePinsWait = mobilePage.WaitForResponseAsync(
            r => r.Url.Contains("/api/vacancies/pins", StringComparison.OrdinalIgnoreCase)
                 && r.Status is >= 200 and < 400,
            new() { Timeout = 90_000 });
        await mobilePage.GotoAsync(baseUrl + "/", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await ExpectMapReadyAsync(mobilePage, mobilePinsWait);
        await mobilePage.ScreenshotAsync(new() { Path = Path.Combine(artifactDir, "01-anonymous-map-mobile.png"), FullPage = true });
        await TapPinOrClusterAsync(mobilePage);
        await mobilePage.ScreenshotAsync(new() { Path = Path.Combine(artifactDir, "02-anonymous-pin-card.png"), FullPage = true });

        await OpenFiltersAsync(mobilePage);
        await mobilePage.ScreenshotAsync(new() { Path = Path.Combine(artifactDir, "03-anonymous-filters.png"), FullPage = true });
        await CloseFiltersAsync(mobilePage);

        await OpenListAsync(mobilePage);
        await mobilePage.ScreenshotAsync(new() { Path = Path.Combine(artifactDir, "04-anonymous-list.png"), FullPage = true });

        await ClickLocateAsync(mobilePage);
        await TryAddressSearchAsync(mobilePage);
        await AssertNoFatalUiAsync(mobilePage);
        mobileGuard.AssertClean();

        // --- Desktop anonymous (1280×800) ---
        await using var desktop = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 800 },
            IgnoreHTTPSErrors = true
        });
        var desktopPage = await desktop.NewPageAsync();
        var desktopGuard = AttachGuards(desktopPage);
        var desktopPinsWait = desktopPage.WaitForResponseAsync(
            r => r.Url.Contains("/api/vacancies/pins", StringComparison.OrdinalIgnoreCase)
                 && r.Status is >= 200 and < 400,
            new() { Timeout = 90_000 });
        await desktopPage.GotoAsync(baseUrl + "/", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await ExpectMapReadyAsync(desktopPage, desktopPinsWait);
        await desktopPage.ScreenshotAsync(new() { Path = Path.Combine(artifactDir, "05-anonymous-map-desktop.png"), FullPage = true });
        await AssertNoFatalUiAsync(desktopPage);
        desktopGuard.AssertClean();

        // --- Demo candidate ---
        await using var candidateCtx = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IgnoreHTTPSErrors = true
        });
        var candidate = await candidateCtx.NewPageAsync();
        var candGuard = AttachGuards(candidate);
        await LoginAsync(candidate, baseUrl, "kandidaat@jobsy.local");
        await candidate.GotoAsync(baseUrl + "/", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
        await candidate.WaitForTimeoutAsync(600);
        await candidate.ScreenshotAsync(new() { Path = Path.Combine(artifactDir, "06-candidate-home.png"), FullPage = true });

        Assert.True(await candidate.Locator("nav.bottom-nav").CountAsync() > 0, "Bottom nav missing for candidate.");
        Assert.True(
            await candidate.Locator("a.jobsy-action--match, a[href='/candidate/match']").CountAsync() > 0,
            "Match button missing on Zoeken for candidate.");

        foreach (var (tab, idx) in CandidateTabs.Select((t, i) => (t, i)))
        {
            await candidate.GotoAsync(baseUrl + tab, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
            await candidate.WaitForTimeoutAsync(400);
            await AssertNoFatalUiAsync(candidate);
            await candidate.ScreenshotAsync(new()
            {
                Path = Path.Combine(artifactDir, $"07-candidate-tab-{idx}.png"),
                FullPage = true
            });
        }

        for (var i = 0; i < 10; i++)
        {
            await candidate.GotoAsync(baseUrl + "/carriere", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
            await candidate.WaitForTimeoutAsync(300);
            await AssertNoFatalUiAsync(candidate);
            await candidate.GotoAsync(baseUrl + "/candidate/profile", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
            await candidate.WaitForTimeoutAsync(300);
            await AssertNoFatalUiAsync(candidate);
        }

        candGuard.AssertClean();

        // --- Employer (filiaal) + Admin dashboards ---
        foreach (var (email, slug) in new[]
                 {
                     ("ondernemer@jobsy.local", "employer"),
                     ("admin@jobsy.local", "admin")
                 })
        {
            await using var ctx = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = 1280, Height = 800 },
                IgnoreHTTPSErrors = true
            });
            var page = await ctx.NewPageAsync();
            var guard = AttachGuards(page);
            await LoginAsync(page, baseUrl, email);
            await page.GotoAsync(baseUrl + "/home", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
            await page.WaitForTimeoutAsync(800);
            await AssertNoFatalUiAsync(page);
            await page.ScreenshotAsync(new() { Path = Path.Combine(artifactDir, $"08-{slug}-dashboard.png"), FullPage = true });
            guard.AssertClean();
        }
    }

    private static string? ResolveBaseUrl()
    {
        var fromEnv = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return fromEnv;
        }

        // Local CI stack default.
        var ci = (Environment.GetEnvironmentVariable("JOBSY_CI_WEB_URL") ?? "").Trim().TrimEnd('/');
        return string.IsNullOrWhiteSpace(ci) ? null : ci;
    }

    private static async Task LoginAsync(IPage page, string baseUrl, string email)
    {
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? DefaultPassword;
        await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });
        await page.FillAsync("input[name='email']", email);
        await page.FillAsync("input[name='password']", password);
        await page.ClickAsync("button.login-submit");
        await page.WaitForURLAsync(
            url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 60_000 });
    }

    private static async Task ExpectMapReadyAsync(IPage page, Task<IResponse>? pinsWait = null)
    {
        await page.WaitForSelectorAsync(
            "canvas.maplibregl-canvas, .maplibregl-canvas, #job-map, .job-map",
            new() { Timeout = 60_000 });

        if (pinsWait is not null)
        {
            try
            {
                var pins = await pinsWait;
                Assert.NotNull(pins);
                return;
            }
            catch (TimeoutException)
            {
                // Response may have fired before the waiter was attached on a warm cache
                // hit — fall through to an explicit same-origin probes.
            }
        }

        var pinsOk = await page.EvaluateAsync<bool>(
            """
            async () => {
              try {
                if (window.jobMap && typeof window.jobMap.__testGetPinCount === 'function'
                    && window.jobMap.__testGetPinCount() > 0) {
                  return true;
                }
                const res = await fetch('/api/vacancies/pins', { credentials: 'same-origin' });
                return res.ok || res.status === 304;
              } catch (e) {
                return false;
              }
            }
            """);
        Assert.True(pinsOk, "Map pins not ready (no pin count and pins HTTP failed).");
    }

    private static async Task TapPinOrClusterAsync(IPage page)
    {
        var canvas = page.Locator("canvas.maplibregl-canvas, .maplibregl-canvas").First;
        if (await canvas.CountAsync() == 0)
        {
            return;
        }

        var box = await canvas.BoundingBoxAsync();
        if (box is null)
        {
            return;
        }

        // Tap near centre; MapLibre hit-test may open popup, zoom a cluster, or miss.
        await page.Mouse.ClickAsync(box.X + box.Width / 2f, box.Y + box.Height / 2f);
        await page.WaitForTimeoutAsync(800);
        await page.Mouse.ClickAsync(box.X + box.Width * 0.55f, box.Y + box.Height * 0.45f);
        await page.WaitForTimeoutAsync(800);

        // Only assert on a real map popup / list card — never [class*='vacancy']
        // (matches empty shells like .vacancy-pane / .vacancy-list).
        var popupTitle = page.Locator(".map-popup__title, .maplibregl-popup .map-popup__title").First;
        if (await popupTitle.CountAsync() > 0)
        {
            var title = await popupTitle.InnerTextAsync();
            Assert.False(string.IsNullOrWhiteSpace(title));
            return;
        }

        var card = page.Locator(".vacancy-card, .discovery-card, .job-card").First;
        if (await card.CountAsync() > 0)
        {
            var text = (await card.InnerTextAsync()).Trim();
            if (text.Length > 0)
            {
                return;
            }
        }

        // Soft-ok: tap may have zoomed a cluster without opening a card.
    }

    private static async Task OpenFiltersAsync(IPage page)
    {
        var filter = page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Filter", RegexOptions.IgnoreCase) }).First;
        if (await filter.CountAsync() == 0)
        {
            filter = page.Locator("button:has-text('Filters'), .jobsy-action:has-text('Filters')").First;
        }

        if (await filter.CountAsync() > 0)
        {
            await filter.ClickAsync();
            await page.WaitForTimeoutAsync(400);
        }
    }

    /// <summary>
    /// Escape only clears address suggestions — close the filter sheet via its controls.
    /// </summary>
    private static async Task CloseFiltersAsync(IPage page)
    {
        var sheet = page.Locator("#discovery-filters");
        if (await sheet.CountAsync() == 0)
        {
            return;
        }

        var close = page.Locator(
            "#discovery-filters button.share-modal__close, #discovery-filters .filter-sheet__cancel, #discovery-filters [aria-label*='sluit' i]").First;
        if (await close.CountAsync() > 0)
        {
            await close.ClickAsync();
        }
        else
        {
            var backdrop = page.Locator(".filter-sheet-backdrop").First;
            if (await backdrop.CountAsync() > 0)
            {
                await backdrop.ClickAsync(new() { Force = true });
            }
        }

        try
        {
            await sheet.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 10_000 });
        }
        catch (TimeoutException)
        {
            // Sheet may already be gone / not modal on desktop.
        }
    }

    private static async Task OpenListAsync(IPage page)
    {
        var list = page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Lijst|List", RegexOptions.IgnoreCase) }).First;
        if (await list.CountAsync() == 0)
        {
            list = page.Locator("button:has-text('Lijst'), a:has-text('Lijst')").First;
        }

        if (await list.CountAsync() > 0)
        {
            await list.ClickAsync();
            await page.WaitForTimeoutAsync(500);
        }
    }

    private static async Task ClickLocateAsync(IPage page)
    {
        var locate = page.Locator(
            "button[title*='locatie' i], button[aria-label*='locatie' i], button[aria-label*='locate' i], .maplibregl-ctrl-geolocate").First;
        if (await locate.CountAsync() > 0)
        {
            await locate.ClickAsync(new() { Force = true });
            await page.WaitForTimeoutAsync(500);
        }
    }

    private static async Task TryAddressSearchAsync(IPage page)
    {
        var input = page.Locator("input[placeholder*='adres' i], input[aria-label*='adres' i], input[name*='address' i]").First;
        if (await input.CountAsync() == 0)
        {
            return;
        }

        await input.FillAsync("Den Haag");
        await page.WaitForTimeoutAsync(800);
        var suggestion = page.Locator(".address-suggest li, .suggestion, [role='option']").First;
        if (await suggestion.CountAsync() > 0)
        {
            await suggestion.ClickAsync();
        }
    }

    private static async Task AssertNoFatalUiAsync(IPage page)
    {
        var body = await page.Locator("body").InnerTextAsync();
        Assert.DoesNotContain("Even iets misgegaan", body, StringComparison.OrdinalIgnoreCase);
        var html = await page.ContentAsync();
        Assert.DoesNotContain("circuit-error", html, StringComparison.OrdinalIgnoreCase);
    }

    private static NetworkGuard AttachGuards(IPage page)
    {
        var guard = new NetworkGuard();
        page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                guard.ConsoleErrors.Add(msg.Text);
            }
        };
        page.Response += (_, response) =>
        {
            var status = response.Status;
            if (status == 429)
            {
                guard.Status429++;
            }
            else if (status >= 400 && status != 401)
            {
                // Blazor often hits 404 for optional chunks; ignore static 404s.
                var url = response.Url;
                if (url.Contains("/_framework/", StringComparison.OrdinalIgnoreCase)
                    || url.Contains(".js", StringComparison.OrdinalIgnoreCase)
                    || url.Contains(".css", StringComparison.OrdinalIgnoreCase)
                    || url.Contains("favicon", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (status >= 500 || status is 400 or 402 or 403 or 409 or 422)
                {
                    guard.FailedRequests.Add($"{status} {url}");
                }
            }
        };
        page.PageError += (_, err) => guard.ConsoleErrors.Add(err);
        return guard;
    }

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            using var response = await client.GetAsync(baseUrl.TrimEnd('/') + "/");
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

        throw new InvalidOperationException("Repo root not found.");
    }

    private sealed class NetworkGuard
    {
        public ConcurrentBag<string> ConsoleErrors { get; } = new();
        public ConcurrentBag<string> FailedRequests { get; } = new();
        public int Status429;

        public void AssertClean()
        {
            Assert.Equal(0, Status429);
            Assert.True(
                FailedRequests.IsEmpty,
                "Unexpected failed requests: " + string.Join(" | ", FailedRequests.Take(8)));
            // Filter known noisy browser messages.
            var fatal = ConsoleErrors
                .Where(e => !e.Contains("favicon", StringComparison.OrdinalIgnoreCase)
                            && !e.Contains("Download the React DevTools", StringComparison.OrdinalIgnoreCase))
                .ToList();
            Assert.True(fatal.Count == 0, "Console errors: " + string.Join(" | ", fatal.Take(8)));
        }
    }
}

[CollectionDefinition("PlaywrightSmoke", DisableParallelization = true)]
public sealed class PlaywrightSmokeCollection;
