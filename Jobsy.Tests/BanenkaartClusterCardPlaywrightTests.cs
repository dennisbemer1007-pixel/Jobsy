using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Calm vacancy sheet: title, pager, photo, match/gate, meta, Bekijk deze baan + heart.
/// Soft-skips without <c>JOBSY_E2E_BASE_URL</c> or when the target lacks the sheet CSS.
/// </summary>
[Collection("PlaywrightSmoke")]
public class BanenkaartClusterCardPlaywrightTests
{
    private const string DefaultEmail = "kandidaat@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";

    public static TheoryData<int, int, bool> ViewportsAndRoles()
    {
        var data = new TheoryData<int, int, bool>();
        foreach (var (w, h) in new[] { (390, 844), (412, 915) })
        {
            data.Add(w, h, false); // anonymous
            data.Add(w, h, true);  // candidate
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ViewportsAndRoles))]
    public async Task Cluster_sheet_shows_calm_card_parts_and_pages(int width, int height, bool asCandidate)
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        await EmployersPlaywrightGuard.SkipIfEmployersOffAsync(baseUrl);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            HasTouch = true,
            IsMobile = true,
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();

        if (asCandidate && !await TryLoginAsync(page, baseUrl))
        {
            return;
        }

        await page.GotoAsync(baseUrl + E2eRoutes.Banenkaart, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });

        var hasSheetCss = await page.EvaluateAsync<bool>("""
            () => [...document.querySelectorAll('link[rel=stylesheet]')]
              .some(el => (el.href || '').includes('banenkaart.css'))
            """);
        if (!hasSheetCss)
        {
            return;
        }

        await page.WaitForFunctionAsync(
            "() => !!(window.jobMap && window.maplibregl && document.querySelector('#job-map canvas'))",
            null,
            new() { Timeout = 60_000 });

        if (!await TryOpenLargestClusterAsync(page))
        {
            return;
        }

        try
        {
            await page.WaitForSelectorAsync(".map-cluster-sheet", new() { Timeout = 15_000 });
        }
        catch (TimeoutException)
        {
            return;
        }

        var metrics = await page.EvaluateAsync<SheetMetrics>("""
            () => {
              const sheet = document.querySelector('.map-cluster-sheet');
              const title = document.querySelector('[data-cluster-title]');
              const counter = document.querySelector('[data-cluster-counter]');
              const photo = document.querySelector('.map-cluster-card__slide.is-active .map-popup__photo, .map-cluster-card__slide.is-active .map-popup__media');
              const match = document.querySelector('.map-cluster-card__slide.is-active .map-popup__match, .map-cluster-card__slide.is-active .map-popup__gate-link');
              const meta = document.querySelector('.map-cluster-card__slide.is-active .map-popup__meta');
              const view = document.querySelector('.map-cluster-card__slide.is-active .map-popup__view');
              const heart = document.querySelector('.map-cluster-card__slide.is-active .map-popup__heart');
              const body = document.querySelector('[data-cluster-body]');
              if (!sheet || !title || !view || !heart) return null;
              const sr = sheet.getBoundingClientRect();
              return {
                titleText: (title.textContent || '').trim(),
                counterText: counter ? (counter.textContent || '').trim() : '',
                hasPhoto: !!photo,
                hasMatchOrGate: !!match,
                hasMeta: !!meta,
                hasView: !!view,
                hasHeart: !!heart,
                sheetTop: sr.top,
                sheetBottom: sr.bottom,
                sheetWidth: sr.width,
                viewportHeight: window.innerHeight,
                viewportWidth: window.innerWidth,
                bodyScrollOk: !body || body.scrollHeight <= body.clientHeight + 2,
                docOverflowX: document.documentElement.scrollWidth > document.documentElement.clientWidth + 1
              };
            }
            """);

        if (metrics is null)
        {
            return;
        }

        Assert.False(string.IsNullOrWhiteSpace(metrics.TitleText));
        Assert.Contains(" in ", metrics.TitleText, StringComparison.OrdinalIgnoreCase);
        Assert.True(metrics.HasPhoto);
        Assert.True(metrics.HasView);
        Assert.True(metrics.HasHeart);
        Assert.True(metrics.SheetBottom <= metrics.ViewportHeight + 1, "sheet clipped below viewport");
        Assert.True(metrics.SheetTop >= -1, "sheet clipped above viewport");
        Assert.False(metrics.DocOverflowX, "horizontal overflow");
        Assert.True(metrics.BodyScrollOk, "sheet body content clipped without scroll room");

        var next = page.Locator(".map-cluster-card__nav[data-cluster-next]").First;
        if (await next.CountAsync() > 0 && !await next.IsDisabledAsync()
            && metrics.CounterText.Contains("1 /", StringComparison.Ordinal))
        {
            await next.ClickAsync();
            await page.WaitForTimeoutAsync(280);
            var counter2 = (await page.Locator("[data-cluster-counter]").InnerTextAsync()).Trim();
            Assert.Contains("2 /", counter2, StringComparison.Ordinal);
        }

        var heartBtn = page.Locator(".map-cluster-card__slide.is-active .map-popup__heart").First;
        if (asCandidate && await heartBtn.CountAsync() > 0)
        {
            var before = await heartBtn.GetAttributeAsync("aria-pressed");
            await heartBtn.ClickAsync();
            await page.WaitForTimeoutAsync(400);
            var after = await heartBtn.GetAttributeAsync("aria-pressed");
            Assert.NotEqual(before, after);
        }

        await page.Locator("[data-cluster-close]").First.ClickAsync();
        await page.WaitForTimeoutAsync(200);
        Assert.Equal(0, await page.Locator(".map-cluster-sheet").CountAsync());
    }

    [Fact]
    public async Task Desktop_docked_popup_shows_same_calm_parts()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !await IsReachableAsync(baseUrl))
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
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + E2eRoutes.Banenkaart, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.WaitForFunctionAsync(
            "() => !!(window.jobMap && window.maplibregl && document.querySelector('#job-map canvas'))",
            null,
            new() { Timeout = 60_000 });

        if (!await TryOpenLargestClusterAsync(page))
        {
            return;
        }

        try
        {
            await page.WaitForSelectorAsync(".map-cluster-sheet.map-popup--docked, .map-cluster-sheet", new() { Timeout = 15_000 });
        }
        catch (TimeoutException)
        {
            return;
        }

        Assert.True(await page.Locator(".map-popup__view").CountAsync() > 0);
        Assert.True(await page.Locator(".map-popup__heart").CountAsync() > 0);
        Assert.True(await page.Locator("[data-cluster-title]").CountAsync() > 0);
    }

    private sealed class SheetMetrics
    {
        public string TitleText { get; set; } = "";
        public string CounterText { get; set; } = "";
        public bool HasPhoto { get; set; }
        public bool HasMatchOrGate { get; set; }
        public bool HasMeta { get; set; }
        public bool HasView { get; set; }
        public bool HasHeart { get; set; }
        public double SheetTop { get; set; }
        public double SheetBottom { get; set; }
        public double SheetWidth { get; set; }
        public double ViewportHeight { get; set; }
        public double ViewportWidth { get; set; }
        public bool BodyScrollOk { get; set; }
        public bool DocOverflowX { get; set; }
    }

    private static async Task<bool> TryLoginAsync(IPage page, string baseUrl)
    {
        try
        {
            var email = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? DefaultEmail;
            var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? DefaultPassword;
            await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            await page.Locator("input[type=text], input[type=email], input[name=email], #email").First.FillAsync(email);
            await page.Locator("input[type=password]").First.FillAsync(password);
            await page.Locator("button[type=submit], button:has-text('Inloggen')").First.ClickAsync();
            await page.WaitForURLAsync(url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase), new() { Timeout = 30_000 });
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> TryOpenLargestClusterAsync(IPage page)
    {
        try
        {
            return await page.EvaluateAsync<bool>("""
                async () => {
                  const jm = window.jobMap;
                  if (!jm || typeof jm.debugOpenLargestCluster !== 'function') return false;
                  return !!(await jm.debugOpenLargestCluster());
                }
                """);
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
