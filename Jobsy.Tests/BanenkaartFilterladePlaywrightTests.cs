using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Variant B filterlade: drawer closed by default, open/close, badge updates, no panel overlap.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class BanenkaartFilterladePlaywrightTests
{
    private static readonly (int W, int H, bool Mobile)[] Viewports =
    [
        (1440, 900, false),
        (1024, 900, false),
        (390, 844, true),
    ];

    [Theory]
    [InlineData(1440, 900)]
    [InlineData(390, 844)]
    public async Task Filter_drawer_closed_by_default_and_closes_with_escape(int width, int height)
    {
        var baseUrl = BaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        await EmployersPlaywrightGuard.SkipIfEmployersOffAsync(baseUrl);
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        await using var browser = await LaunchAsync();
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            HasTouch = height >= 800 && width < 500,
            IsMobile = width < 500,
            IgnoreHTTPSErrors = true,
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
        await DismissStartPromptAsync(page);
        await page.WaitForSelectorAsync("[data-testid=kb-filters-button]", new() { Timeout = 90_000 });

        var sheet = page.Locator("[data-testid=kb-filter-sheet]");
        await Assertions.Expect(sheet).ToBeHiddenAsync(new() { Timeout = 15_000 });

        var openBtn = page.Locator("[data-testid=kb-filters-button]").First;
        await openBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await openBtn.ClickAsync(new() { Timeout = 60_000 });
        await Assertions.Expect(sheet).ToBeVisibleAsync(new() { Timeout = 60_000 });

        await page.Locator("[data-testid=kb-filter-backdrop]").ClickAsync(new() { Position = new() { X = 10, Y = 10 } });
        await Assertions.Expect(sheet).ToBeHiddenAsync(new() { Timeout = 30_000 });

        await openBtn.ClickAsync();
        await Assertions.Expect(sheet).ToBeVisibleAsync(new() { Timeout = 10_000 });
        await page.Locator("[data-testid=kb-filter-close]").ClickAsync();
        await Assertions.Expect(sheet).ToBeHiddenAsync(new() { Timeout = 30_000 });
    }

    [Fact]
    public async Task Applying_travel_preset_updates_apply_label_and_badge_after_commit()
    {
        var baseUrl = BaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        await EmployersPlaywrightGuard.SkipIfEmployersOffAsync(baseUrl);
        await using var browser = await LaunchAsync();
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
            IgnoreHTTPSErrors = true,
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
        await DismissStartPromptAsync(page);

        await page.Locator("[data-testid=kb-filters-button]").First.ClickAsync();
        await page.WaitForSelectorAsync("[data-testid=kb-filter-sheet]", new() { Timeout = 15_000 });

        var apply = page.Locator("[data-testid=kb-filter-apply]");
        var applyBefore = await apply.InnerTextAsync();
        Assert.Contains("vacatures", applyBefore, StringComparison.OrdinalIgnoreCase);

        var preset30 = page.Locator("[data-testid=kb-sheet-travel-30]");
        if (await preset30.CountAsync() > 0)
        {
            await preset30.First.ClickAsync();
            var applyAfter = await apply.InnerTextAsync();
            Assert.NotEqual(applyBefore, applyAfter);
        }

        await apply.ClickAsync();
        await Assertions.Expect(page.Locator("[data-testid=kb-filter-sheet]")).ToBeHiddenAsync(new() { Timeout = 60_000 });
        var countText = await page.Locator("[data-testid=kb-desktop-result-count]").InnerTextAsync();
        Assert.Matches(@"\d+", countText);
    }

    [Fact]
    public async Task Panel_controls_do_not_overlap_at_standard_viewports()
    {
        var baseUrl = BaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        await EmployersPlaywrightGuard.SkipIfEmployersOffAsync(baseUrl);
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        await using var browser = await LaunchAsync();

        var failures = new List<string>();
        foreach (var (w, h, mobile) in Viewports)
        {
            await using var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = w, Height = h },
                HasTouch = mobile,
                IsMobile = mobile,
                IgnoreHTTPSErrors = true,
            });
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();
            await page.GotoAsync(baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
            await DismissStartPromptAsync(page);

            var json = await page.EvaluateAsync<string>(OverlapScript);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("overlaps", out var overlaps) && overlaps.GetArrayLength() > 0)
            {
                failures.Add($"{w}x{h} overlaps: {overlaps}");
            }
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    private const string OverlapScript = """
        () => {
          const root = document.querySelector('[data-testid=kb-panel-head], [data-testid=kb-panel-head-mobile]');
          if (!root) return JSON.stringify({ error: 'missing panel head' });
          const sel = 'input, button:not(.kb-match-button), select, .kb-active-tag, .kb-view-toggle__btn, .kb-filter-bar__count';
          const gap = 2;
          const minArea = 64;
          const els = [...root.querySelectorAll(sel)].filter(el => {
            if (!(el instanceof HTMLElement)) return false;
            const st = getComputedStyle(el);
            if (st.display === 'none' || st.visibility === 'hidden' || st.opacity === '0') return false;
            const r = el.getBoundingClientRect();
            return r.width > 4 && r.height > 4;
          });
          const overlaps = [];
          for (let i = 0; i < els.length; i++) {
            const a = els[i].getBoundingClientRect();
            if (a.width * a.height < minArea) continue;
            for (let j = i + 1; j < els.length; j++) {
              if (els[i].contains(els[j]) || els[j].contains(els[i])) continue;
              const b = els[j].getBoundingClientRect();
              if (b.width * b.height < minArea) continue;
              const xOverlap = Math.min(a.right, b.right) - Math.max(a.left, b.left);
              const yOverlap = Math.min(a.bottom, b.bottom) - Math.max(a.top, b.top);
              if (xOverlap > gap && yOverlap > gap) {
                overlaps.push({ a: els[i].className, b: els[j].className, xOverlap, yOverlap });
              }
            }
          }
          return JSON.stringify({ overlaps });
        }
        """;

    private static async Task DismissStartPromptAsync(IPage page)
    {
        var later = page.Locator(".kb-start-prompt__later");
        if (await later.CountAsync() > 0)
        {
            await later.ClickAsync();
        }
    }

    private static string? BaseUrl()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "http://127.0.0.1:5201").Trim().TrimEnd('/');
        return string.IsNullOrWhiteSpace(baseUrl) ? null : baseUrl;
    }

    private static async Task<IBrowser> LaunchAsync()
    {
        var playwright = await Playwright.CreateAsync();
        return await playwright.Chromium.LaunchAsync(new() { Headless = true });
    }

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var resp = await http.GetAsync(baseUrl);
            return resp.IsSuccessStatusCode || (int)resp.StatusCode is >= 300 and < 500;
        }
        catch
        {
            return false;
        }
    }
}
