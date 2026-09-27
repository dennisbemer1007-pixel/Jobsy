using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Banenkaart must meet the bottom nav with or without cookie consent.
/// Soft-skips without <c>JOBSY_E2E_BASE_URL</c>.
/// </summary>
[Collection("PlaywrightSmoke")]
public class BanenkaartCookiePaddingPlaywrightTests
{
    private const string DefaultEmail = "kandidaat@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";

    [Theory]
    [InlineData(360, 800)]
    [InlineData(390, 844)]
    [InlineData(412, 915)]
    [InlineData(430, 932)]
    public async Task Map_meets_bottom_nav_with_and_without_cookie_consent(int width, int height)
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !await IsReachableAsync(baseUrl))
        {
            return;
        }

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
        if (!await TryLoginAsync(page, baseUrl))
        {
            return;
        }

        await page.GotoAsync(baseUrl + "/", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.WaitForSelectorAsync("#job-map canvas", new() { Timeout = 60_000 });

        // Case A: cookies NOT accepted — hide banner visually but leave storage empty.
        await page.EvaluateAsync("""
            () => {
              try { localStorage.removeItem('Jobsy.CookieConsent'); } catch (e) {}
              document.documentElement.classList.remove('cookie-consent-known');
              const banner = document.querySelector('.cookie-consent');
              if (banner) banner.style.display = 'none';
            }
            """);
        await page.WaitForTimeoutAsync(200);
        await AssertMapMeetsNavAsync(page);

        var mapBeforeCluster = await MeasureMapRectAsync(page);
        await AssertClusterSheetHeightAsync(page);
        var mapAfterCluster = await MeasureMapRectAsync(page);
        Assert.InRange(Math.Abs(mapAfterCluster.Bottom - mapBeforeCluster.Bottom), 0, 2);
        Assert.InRange(Math.Abs(mapAfterCluster.Top - mapBeforeCluster.Top), 0, 2);

        // Close cluster if open.
        await page.EvaluateAsync("""
            () => {
              const close = document.querySelector('[data-cluster-close]');
              if (close) close.click();
            }
            """);
        await page.WaitForTimeoutAsync(200);

        // Case B: accept cookies via banner button (re-show banner first).
        await page.EvaluateAsync("""
            () => {
              try { localStorage.removeItem('Jobsy.CookieConsent'); } catch (e) {}
              document.documentElement.classList.remove('cookie-consent-known');
              const banner = document.querySelector('.cookie-consent');
              if (banner) banner.style.display = '';
            }
            """);
        await page.WaitForTimeoutAsync(150);
        var accept = page.Locator(".cookie-consent button.btn-compact").Last;
        if (await accept.CountAsync() > 0 && await accept.IsVisibleAsync())
        {
            await accept.ClickAsync();
            await page.WaitForTimeoutAsync(300);
        }
        else
        {
            // Banner already dismissed in this session — set consent via helper.
            await page.EvaluateAsync("""
                () => {
                  if (window.jobsyCookieConsent) window.jobsyCookieConsent.set('necessary');
                }
                """);
            await page.WaitForTimeoutAsync(150);
        }

        await AssertMapMeetsNavAsync(page);
    }

    private static async Task AssertMapMeetsNavAsync(IPage page)
    {
        var gap = await page.EvaluateAsync<double>("""
            () => {
              const map = document.querySelector('#job-map') || document.querySelector('.map-pane');
              const nav = document.querySelector('.bottom-nav');
              if (!map || !nav) return 999;
              const mr = map.getBoundingClientRect();
              const nr = nav.getBoundingClientRect();
              return Math.abs(nr.top - mr.bottom);
            }
            """);
        Assert.True(gap <= 2, $"Map bottom must meet bottom-nav top (gap was {gap:F1}px).");
    }

    private static async Task<(double Top, double Bottom)> MeasureMapRectAsync(IPage page)
    {
        var rect = await page.EvaluateAsync<double[]>("""
            () => {
              const map = document.querySelector('#job-map') || document.querySelector('.map-pane');
              if (!map) return [0, 0];
              const r = map.getBoundingClientRect();
              return [r.top, r.bottom];
            }
            """);
        return (rect[0], rect[1]);
    }

    private static async Task AssertClusterSheetHeightAsync(IPage page)
    {
        var opened = await page.EvaluateAsync<bool>("""
            async () => {
              const jm = window.jobMap;
              if (!jm || typeof jm.debugOpenLargestCluster !== 'function') return false;
              return !!(await jm.debugOpenLargestCluster());
            }
            """);
        if (!opened)
        {
            return;
        }

        await page.WaitForSelectorAsync(".map-cluster-sheet", new() { Timeout = 10_000 });
        var height = await page.EvaluateAsync<double>("""
            () => {
              const sheet = document.querySelector('.map-cluster-sheet');
              return sheet ? sheet.getBoundingClientRect().height : 0;
            }
            """);
        Assert.InRange(height, 198, 200);
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
            await page.ClickAsync("button.login-submit");
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
