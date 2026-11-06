using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Regression: candidate shells must scroll (single container, no horizontal overflow) after layout CSS changes.
/// </summary>
[Collection("PlaywrightSmoke")]
public class CandidateScrollRegressionPlaywrightTests
{
    public static TheoryData<int, int, string> ScrollRoutes => new()
    {
        { 390, 844, "/candidate/start" },
        { 1280, 800, "/candidate/start" },
        { 390, 844, "/candidate/ontdekkingsreis?stap=intro" },
        { 1280, 800, "/candidate/ontdekkingsreis?stap=intro" },
        { 390, 844, "/candidate/paspoort" },
        { 1280, 800, "/candidate/paspoort" },
        { 390, 844, "/candidate/vacancies" },
        { 1280, 800, "/candidate/vacancies" },
    };

    [Theory]
    [MemberData(nameof(ScrollRoutes))]
    public async Task Candidate_routes_scroll_vertically_without_horizontal_overflow(int width, int height, string path)
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IgnoreHTTPSErrors = true,
            IsMobile = width < 600,
            HasTouch = width < 600,
        });
        var page = await context.NewPageAsync();

        await page.GotoAsync(baseUrl + path, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        if (page.Url.Contains("/login", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await PlaywrightCookieConsent.AcceptOnPageAsync(page);

        var metrics = await page.EvaluateAsync<ScrollMetrics>(
            """
            () => {
              const doc = document.documentElement;
              const body = document.body;
              const pick = () => {
                const nodes = [
                  document.querySelector('.app-main'),
                  document.querySelector('.journey-stage'),
                  document.querySelector('.ob-wizard__content'),
                  document.scrollingElement,
                  doc,
                  body,
                ].filter(Boolean);
                for (const el of nodes) {
                  if (el.scrollHeight > el.clientHeight + 2) return el;
                }
                return document.scrollingElement || doc;
              };
              const scrollEl = pick();
              const before = scrollEl.scrollTop;
              scrollEl.scrollTop = before + Math.min(480, Math.max(0, scrollEl.scrollHeight - scrollEl.clientHeight));
              const after = scrollEl.scrollTop;
              const pageScroll = document.scrollingElement || doc;
              return {
                canScroll: scrollEl.scrollHeight > scrollEl.clientHeight + 2,
                scrolled: after > before + 1,
                horizontalOverflow: doc.scrollWidth > doc.clientWidth + 1,
                pageScrollable: pageScroll.scrollHeight > pageScroll.clientHeight + 2,
              };
            }
            """);

        Assert.False(metrics.HorizontalOverflow, $"{path}@{width}x{height}: horizontal overflow");
        if (metrics.CanScroll)
        {
            Assert.True(metrics.Scrolled, $"{path}@{width}x{height}: expected vertical scroll to move");
        }
    }

    [Fact]
    public async Task Candidate_start_scrolls_in_ios_pwa_standalone_viewport()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IgnoreHTTPSErrors = true,
            IsMobile = true,
            HasTouch = true,
            UserAgent =
                "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1",
        });
        await context.AddInitScriptAsync(
            """
            (() => {
              const orig = window.matchMedia.bind(window);
              window.matchMedia = (query) => {
                if (typeof query === 'string' && query.includes('display-mode') && query.includes('standalone')) {
                  return { matches: true, media: query, addListener: () => {}, removeListener: () => {}, addEventListener: () => {}, removeEventListener: () => {}, dispatchEvent: () => false };
                }
                return orig(query);
              };
              try { Object.defineProperty(navigator, 'standalone', { get: () => true, configurable: true }); } catch (_) {}
            })();
            """);

        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/candidate/start", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        if (page.Url.Contains("/login", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await PlaywrightCookieConsent.AcceptOnPageAsync(page);

        var scrolled = await page.EvaluateAsync<bool>(
            """
            () => {
              const scrollEl = document.querySelector('.app-main') || document.scrollingElement || document.documentElement;
              const before = scrollEl.scrollTop;
              scrollEl.scrollTop = before + 400;
              return scrollEl.scrollTop > before + 1 || scrollEl.scrollHeight <= scrollEl.clientHeight + 2;
            }
            """);

        Assert.True(scrolled, "iOS standalone /candidate/start should allow vertical scroll via app-main");
    }

    private sealed record ScrollMetrics(
        bool CanScroll,
        bool Scrolled,
        bool HorizontalOverflow,
        bool PageScrollable);
}
