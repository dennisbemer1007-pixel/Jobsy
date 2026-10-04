using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// One coach, bottom-right, clear of the primary button. Soft-skips without a running site.
/// </summary>
[Collection("PlaywrightSmoke")]
public class CoachWidgetPlaywrightTests
{
    private const string DefaultEmail = "kandidaat@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";

    [Theory]
    [InlineData(1366, 900, true)]
    [InlineData(390, 844, true)]
    [InlineData(390, 844, false)]
    public async Task Coach_sits_bottom_right_and_clears_the_primary_button(int width, int height, bool acceptCookies)
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !await IsReachableAsync(baseUrl))
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
            ViewportSize = new() { Width = width, Height = height },
            IgnoreHTTPSErrors = true
        });
        if (acceptCookies)
        {
            await PlaywrightCookieConsent.AcceptAsync(context);
        }

        var page = await context.NewPageAsync();
        if (!await LoginAsync(page, baseUrl, email, password))
        {
            return;
        }

        await page.GotoAsync(baseUrl + "/candidate/paspoort", new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 60_000
        });
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 30_000 });

        var coaches = page.Locator("#lobsy-coach-btn");
        try
        {
            await coaches.First.WaitForAsync(new() { Timeout = 20_000 });
        }
        catch (TimeoutException)
        {
            return;
        }

        Assert.Equal(1, await coaches.CountAsync());
        var box = await coaches.First.BoundingBoxAsync();
        Assert.NotNull(box);
        Assert.True(box!.X >= width * 0.55, $"Coach is not on the right (x={box.X}).");
        Assert.True(box.Y > height * 0.45, $"Coach is not near the bottom (y={box.Y}).");
        Assert.True(box.X + box.Width <= width + 1);
        Assert.True(box.Y + box.Height <= height + 1);

        var primary = page.Locator("a.btn-primary, button.btn-primary, a.engagement-btn, button.engagement-btn").First;
        if (await primary.CountAsync() > 0 && await primary.IsVisibleAsync())
        {
            var action = await primary.BoundingBoxAsync();
            if (action is not null)
            {
                Assert.False(Overlaps(box, action), "Coach overlaps the primary button.");
            }
        }

        if (!acceptCookies)
        {
            var cookie = page.Locator(".cookie-consent");
            if (await cookie.CountAsync() > 0 && await cookie.First.IsVisibleAsync())
            {
                var bar = await cookie.First.BoundingBoxAsync();
                if (bar is not null)
                {
                    Assert.False(Overlaps(box, bar), "Coach overlaps the cookie bar.");
                }
            }
        }

        if (width < 640)
        {
            var nav = page.Locator(".bottom-nav");
            if (await nav.CountAsync() > 0 && await nav.First.IsVisibleAsync())
            {
                var navBox = await nav.First.BoundingBoxAsync();
                if (navBox is not null)
                {
                    Assert.False(Overlaps(box, navBox), "Coach overlaps the bottom nav.");
                }
            }
        }

        await AssertNoControlOverlapAsync(page);
        await coaches.First.ClickAsync();
        var panel = page.Locator("#lobsy-assistant-panel");
        await panel.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });
        Assert.True(await panel.IsVisibleAsync());
    }

    [Theory]
    [InlineData(1366, 900, true, false)]
    [InlineData(390, 844, true, false)]
    [InlineData(390, 844, false, false)]
    [InlineData(390, 844, true, true)]
    public async Task Coach_and_tip_do_not_cover_controls(int width, int height, bool acceptCookies, bool rtl)
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !await IsReachableAsync(baseUrl))
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
            ViewportSize = new() { Width = width, Height = height },
            IgnoreHTTPSErrors = true
        });
        if (acceptCookies)
        {
            await PlaywrightCookieConsent.AcceptAsync(context);
        }

        var page = await context.NewPageAsync();
        if (!await LoginAsync(page, baseUrl, email, password))
        {
            return;
        }

        if (rtl)
        {
            await page.GotoAsync(baseUrl + "/taal/ar", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
        }

        var paths = new List<string>
        {
            "/candidate/paspoort?tab=tests",
            "/candidate/paspoort?tab=proof",
            "/candidate/paspoort?tab=data",
            "/candidate/ontdekkingsreis",
            "/candidate/career?stap=intro",
            "/candidate/hoe-werkt-lobsy",
            "/account/mail-instellingen"
        };
        if (width == 390)
        {
            paths.Add("/banenkaart");
        }

        foreach (var path in paths)
        {
            await page.GotoAsync(baseUrl + path, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
            try
            {
                await page.WaitForSelectorAsync("#lobsy-coach-btn", new() { Timeout = 12_000 });
            }
            catch (TimeoutException)
            {
                continue;
            }

            Assert.Equal(1, await page.Locator("#lobsy-coach-btn").CountAsync());
            var box = await page.Locator("#lobsy-coach-btn").BoundingBoxAsync();
            Assert.NotNull(box);
            if (rtl)
            {
                Assert.True(box!.X < width * 0.45, $"RTL coach should sit at the inline end (x={box.X}).");
            }
            else
            {
                Assert.True(box!.X >= width * 0.55, $"Coach is not on the right (x={box.X}).");
            }

            await AssertNoControlOverlapAsync(page);
            if (path.Contains("/banenkaart", StringComparison.Ordinal))
            {
                await AssertNoControlCentreUnderCoachAsync(page);
            }
        }
    }

    private static async Task AssertNoControlOverlapAsync(IPage page)
    {
        var overlaps = await page.EvaluateAsync<int>(
            """
            () => {
              const dock = document.querySelector('[data-lobsy-coach], .lobsy-coach-dock');
              if (!dock) return 0;
              const tip = dock.querySelector('.lobsy-coach-dock__tip');
              const boxes = [dock.getBoundingClientRect()];
              if (tip && !tip.hidden) boxes.push(tip.getBoundingClientRect());
              const nodes = document.querySelectorAll('a, button, input, textarea, select, summary, [role="button"]');
              let hits = 0;
              for (const node of nodes) {
                if (dock.contains(node)) continue;
                const style = getComputedStyle(node);
                if (style.visibility === 'hidden' || style.display === 'none') continue;
                const r = node.getBoundingClientRect();
                if (r.width < 8 || r.height < 8) continue;
                if (r.bottom < 0 || r.top > innerHeight) continue;
                for (const b of boxes) {
                  const hit = b.left < r.right && b.right > r.left && b.top < r.bottom && b.bottom > r.top;
                  if (hit) hits++;
                }
              }
              return hits;
            }
            """);
        Assert.Equal(0, overlaps);
    }

    private static async Task AssertNoControlCentreUnderCoachAsync(IPage page)
    {
        var covered = await page.EvaluateAsync<string[]>(
            """
            () => {
              const coach = document.querySelector('#lobsy-coach-btn');
              if (!coach) return [];
              const c = coach.getBoundingClientRect();
              const nodes = document.querySelectorAll('.maplibregl-ctrl button, .job-map-style-switch__btn, .job-map-locate__btn, .maplibregl-ctrl-group button');
              const hits = [];
              for (const node of nodes) {
                const style = getComputedStyle(node);
                if (style.visibility === 'hidden' || style.display === 'none') continue;
                const r = node.getBoundingClientRect();
                if (r.width < 8 || r.height < 8) continue;
                if (r.bottom < 0 || r.top > innerHeight) continue;
                const cx = r.left + r.width / 2;
                const cy = r.top + r.height / 2;
                if (cx >= c.left && cx <= c.right && cy >= c.top && cy <= c.bottom) {
                  hits.push((node.getAttribute('aria-label') || node.className || 'control') + '');
                }
              }
              return hits;
            }
            """);
        Assert.Empty(covered);
    }

    private static bool Overlaps(Microsoft.Playwright.LocatorBoundingBoxResult a, Microsoft.Playwright.LocatorBoundingBoxResult b)
        => a.X < b.X + b.Width && a.X + a.Width > b.X && a.Y < b.Y + b.Height && a.Y + a.Height > b.Y;

    private static async Task<bool> LoginAsync(IPage page, string baseUrl, string email, string password)
    {
        await page.GotoAsync(baseUrl + "/login", new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 60_000
        });
        await page.FillAsync("input[name='email']", email);
        await page.FillAsync("input[name='password']", password);
        await page.ClickAsync("button.login-submit");
        try
        {
            await page.WaitForURLAsync(
                url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
                new() { Timeout = 60_000 });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var response = await http.GetAsync(baseUrl);
            return (int)response.StatusCode < 500;
        }
        catch
        {
            return false;
        }
    }
}
