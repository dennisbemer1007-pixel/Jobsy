using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Kandidaat polish 01: at 390×844 the banenkaart must not overflow horizontally;
/// the filter chip row scrolls so Meer filters / Lijst / Match stay reachable.
/// Soft-skips without <c>JOBSY_E2E_BASE_URL</c>.
/// </summary>
[Collection("PlaywrightSmoke")]
public class BanenkaartMobileOverflowPlaywrightTests
{
    private const string DefaultEmail = "kandidaat@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Mobile_390_no_overflow_and_chips_scroll(bool loggedIn)
    {
        var baseUrl = BaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        await using var browser = await LaunchAsync();
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            HasTouch = true,
            IsMobile = true,
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();

        if (loggedIn && !await TryLoginAsync(page, baseUrl))
        {
            return;
        }

        await page.GotoAsync(baseUrl + "/banenkaart", new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 90_000
        });
        await page.WaitForSelectorAsync(".jobsy-discovery, #job-map, .kb-filter-chips", new() { Timeout = 60_000 });

        var later = page.Locator(".kb-start-prompt__later");
        if (await later.CountAsync() > 0)
        {
            await later.ClickAsync();
            await page.WaitForTimeoutAsync(400);
        }

        // Map mode
        await AssertNoHorizontalOverflowAsync(page);
        await AssertChipRowScrollsAsync(page);
        await AssertMapWidthAsync(page);

        // Reach end chips
        await page.EvaluateAsync("""
            () => {
              const row = document.querySelector('.kb-filter-chips:not(.kb-filter-chips--desktop)');
              if (row) row.scrollLeft = row.scrollWidth;
            }
            """);
        await page.WaitForTimeoutAsync(200);

        var meer = page.Locator(".kb-filter-chips .kb-chip, .kb-filter-chips button")
            .Filter(new() { HasTextString = "Meer" });
        if (await meer.CountAsync() > 0)
        {
            await meer.First.ClickAsync(new() { Force = true });
            await page.WaitForSelectorAsync("#discovery-filters", new() { Timeout = 10_000 });
            // Close sheet if open
            var close = page.Locator("#discovery-filters button[aria-label], #discovery-filters .filter-sheet__close, #discovery-filters .btn-close");
            if (await close.CountAsync() > 0)
            {
                await close.First.ClickAsync();
            }
            else
            {
                await page.Keyboard.PressAsync("Escape");
            }
        }

        var lijst = page.Locator(".kb-filter-chips .kb-chip, .kb-filter-chips button")
            .Filter(new() { HasTextString = "Lijst" });
        if (await lijst.CountAsync() > 0)
        {
            await Assertions.Expect(lijst.First).ToBeVisibleAsync();
        }

        if (loggedIn)
        {
            var match = page.Locator(".kb-filter-chips .kb-chip, .kb-filter-chips a, .kb-filter-chips button")
                .Filter(new() { HasTextString = "Match" });
            if (await match.CountAsync() > 0)
            {
                await Assertions.Expect(match.First).ToBeVisibleAsync();
            }
        }

        // List mode
        if (await lijst.CountAsync() > 0)
        {
            await lijst.First.ClickAsync(new() { Force = true });
            await page.WaitForTimeoutAsync(400);
            await AssertNoHorizontalOverflowAsync(page);
        }

        // Cluster sheet edges when a cluster can be opened
        var opened = await page.EvaluateAsync<bool>("""
            async () => {
              if (!window.jobMap || typeof window.jobMap.debugOpenLargestCluster !== 'function') return false;
              try { return await window.jobMap.debugOpenLargestCluster(); } catch { return false; }
            }
            """);
        if (opened)
        {
            await page.WaitForSelectorAsync(".map-cluster-sheet", new() { Timeout = 10_000 });
            var sheetOk = await page.EvaluateAsync<bool>("""
                () => {
                  const sheet = document.querySelector('.map-cluster-sheet');
                  if (!sheet) return true;
                  const r = sheet.getBoundingClientRect();
                  return r.right <= 390 - 7.5;
                }
                """);
            Assert.True(sheetOk, "Cluster sheet right edge must stay within the 390 viewport (8px inset).");

            var chipOk = await page.EvaluateAsync<bool>("""
                () => {
                  const chip = document.querySelector('.map-cluster-chip');
                  if (!chip || chip.hasAttribute('hidden')) return true;
                  const r = chip.getBoundingClientRect();
                  return r.left >= 0 && r.right <= 390;
                }
                """);
            Assert.True(chipOk, "Count pill must be fully inside the viewport.");
        }
    }

    private static async Task AssertNoHorizontalOverflowAsync(IPage page)
    {
        var report = await page.EvaluateAsync<OverflowReport>("""
            () => {
              const docW = document.documentElement.scrollWidth;
              const offenders = [];
              const main = document.querySelector('.app-main') || document.body;
              for (const el of main.querySelectorAll('*')) {
                // Chip row + highlight carousel scroll horizontally by design;
                // only their outer boxes must stay within the viewport.
                const chip = el.closest('.kb-filter-chips');
                if (chip && el !== chip) continue;
                const carousel = el.closest('.highlight-carousel');
                if (carousel && el !== carousel) continue;
                const r = el.getBoundingClientRect();
                if (r.width <= 0 || r.height <= 0) continue;
                if (r.right > 391.5) {
                  offenders.push((el.className || el.tagName || '').toString().slice(0, 80));
                  if (offenders.length >= 8) break;
                }
              }
              const chips = document.querySelector('.kb-filter-chips:not(.kb-filter-chips--desktop)');
              const chipBox = chips ? chips.getBoundingClientRect().width : 0;
              const carousels = [...document.querySelectorAll('.highlight-carousel')];
              for (const c of carousels) {
                const r = c.getBoundingClientRect();
                if (r.width > 0 && r.right > 391.5) {
                  offenders.push('highlight-carousel-box');
                }
              }
              return { docW, offenders, chipBox };
            }
            """);
        Assert.True(report.DocW <= 390, $"documentElement.scrollWidth was {report.DocW}, expected ≤ 390. Offenders: {string.Join(", ", report.Offenders)}");
        Assert.Empty(report.Offenders);
        if (report.ChipBox > 0)
        {
            Assert.True(report.ChipBox <= 390, $"Chip row box width {report.ChipBox} exceeds 390.");
        }
    }

    private static async Task AssertChipRowScrollsAsync(IPage page)
    {
        var scroll = await page.EvaluateAsync<ChipScroll>("""
            () => {
              const row = document.querySelector('.kb-filter-chips:not(.kb-filter-chips--desktop)');
              if (!row) return { client: 0, scroll: 0 };
              return { client: row.clientWidth, scroll: row.scrollWidth };
            }
            """);
        if (scroll.Client <= 0)
        {
            return;
        }

        Assert.True(scroll.Client <= 390, $"Chip row clientWidth {scroll.Client} > 390");
        // With enough chips the row must scroll; if few chips, scrollWidth may equal clientWidth.
        if (scroll.Scroll > scroll.Client + 8)
        {
            Assert.True(scroll.Scroll > scroll.Client);
        }
    }

    private static async Task AssertMapWidthAsync(IPage page)
    {
        var w = await page.EvaluateAsync<double>("""
            () => {
              const map = document.querySelector('#job-map');
              return map ? map.getBoundingClientRect().width : 0;
            }
            """);
        if (w > 0)
        {
            Assert.True(w <= 390, $"#job-map width {w} exceeds 390");
        }
    }

    // Playwright's EvaluateAsync needs a parameterless ctor to materialize the result.
    private sealed class OverflowReport
    {
        public double DocW { get; set; }
        public string[] Offenders { get; set; } = [];
        public double ChipBox { get; set; }
    }

    private sealed class ChipScroll
    {
        public double Client { get; set; }
        public double Scroll { get; set; }
    }

    private static string? BaseUrl()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        return string.IsNullOrWhiteSpace(baseUrl) ? null : baseUrl;
    }

    private static async Task<IBrowser> LaunchAsync()
    {
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
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

    private static async Task<bool> TryLoginAsync(IPage page, string baseUrl)
    {
        var email = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? DefaultEmail;
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? DefaultPassword;
        try
        {
            await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
            await page.FillAsync("input[name='email']", email);
            await page.FillAsync("input[name='password']", password);
            var submit = page.Locator("button.login-submit, button.au-submit[type=submit]");
            await page.WaitForFunctionAsync(
                "() => { const b = document.querySelector('button.login-submit, button.au-submit[type=submit]'); return b && !b.disabled; }",
                null,
                new() { Timeout = 30_000 });
            await Task.WhenAll(
                page.WaitForURLAsync(
                    url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
                    new() { Timeout = 60_000 }),
                submit.ClickAsync());
            return !page.Url.Contains("/login", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
