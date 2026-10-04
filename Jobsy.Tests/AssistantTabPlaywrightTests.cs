using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// The candidate coach is the round lobster, bottom-right. It stays inside the
/// viewport, opens the chat, and at phone width does not cover other controls.
/// Soft-skips only when no site is configured or the host is down.
/// </summary>
[Collection("PlaywrightSmoke")]
public class AssistantTabPlaywrightTests
{
    private const string DefaultEmail = "kandidaat@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";

    [Theory]
    [InlineData(1366, 900)]
    [InlineData(390, 844)]
    public async Task Assistant_tab_is_inside_the_viewport(int width, int height)
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
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/login", new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 60_000
        });
        await page.FillAsync("input[name='email']", email);
        await page.FillAsync("input[name='password']", password);
        await page.ClickAsync("button.login-submit");
        await page.WaitForURLAsync(
            url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 60_000 });

        await page.GotoAsync(baseUrl + "/candidate/paspoort", new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 60_000
        });
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 30_000 });

        var coach = page.Locator("#lobsy-coach-btn");
        await coach.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 20_000 });
        Assert.Equal(1, await coach.CountAsync());

        // Let the dock finish measuring the bottom nav before reading boxes.
        await page.WaitForFunctionAsync(
            """
            () => {
                const btn = document.querySelector('#lobsy-coach-btn');
                if (!btn) return false;
                const b = btn.getBoundingClientRect();
                if (b.width < 8 || b.height < 8) return false;
                if (b.left < 0 || b.top < 0 || b.right > window.innerWidth + 1 || b.bottom > window.innerHeight + 1) return false;
                const nav = document.querySelector('.bottom-nav');
                if (!nav || getComputedStyle(nav).display === 'none') return true;
                return b.bottom <= nav.getBoundingClientRect().top + 1;
            }
            """,
            null,
            new() { Timeout = 10_000 });

        var box = await coach.BoundingBoxAsync();
        Assert.NotNull(box);
        AssertInside(box!, width, height, "Coach");
        Assert.True(box!.X + box.Width / 2 >= width * 0.55, $"Coach is not on the right (x={box.X}).");
        Assert.True(box.Y + box.Height / 2 >= height * 0.45, $"Coach is not near the bottom (y={box.Y}).");

        var dock = await page.Locator(".lobsy-coach-dock").BoundingBoxAsync();
        Assert.NotNull(dock);
        AssertInside(dock!, width, height, "Coach dock");

        if (width <= 400)
        {
            var covered = await page.EvaluateAsync<string>(
                """
                () => {
                    const dock = document.querySelector('.lobsy-coach-dock');
                    if (!dock) return 'missing-dock';
                    const a = dock.getBoundingClientRect();
                    const hits = [];
                    for (const el of document.querySelectorAll('button, input, textarea, select')) {
                        if (dock.contains(el)) continue;
                        const s = getComputedStyle(el);
                        if (s.display === 'none' || s.visibility === 'hidden' || Number(s.opacity) === 0) continue;
                        const r = el.getBoundingClientRect();
                        if (r.width < 2 || r.height < 2) continue;
                        if (r.bottom <= 0 || r.top >= window.innerHeight || r.right <= 0 || r.left >= window.innerWidth) continue;
                        const overlap = a.left < r.right - 0.5 && a.right > r.left + 0.5 && a.top < r.bottom - 0.5 && a.bottom > r.top + 0.5;
                        if (!overlap) continue;
                        const label = (el.getAttribute('aria-label') || el.id || el.getAttribute('name') || el.className || el.tagName || '').toString().slice(0, 80);
                        hits.push(label);
                    }
                    return hits.join(' | ');
                }
                """);
            Assert.True(string.IsNullOrEmpty(covered), "Coach covers buttons or inputs: " + covered);
        }

        await coach.ClickAsync();
        var panel = page.Locator("#lobsy-assistant-panel");
        await panel.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });
        Assert.True(await panel.IsVisibleAsync());
        await Assertions.Expect(page.Locator("#lobsy-assistant-input")).ToBeVisibleAsync();
        await Assertions.Expect(coach).ToHaveAttributeAsync("aria-expanded", "true");
    }

    private static void AssertInside(LocatorBoundingBoxResult box, int width, int height, string name)
    {
        Assert.True(box.X >= -1, $"{name} starts left of the viewport (x={box.X}).");
        Assert.True(box.Y >= -1, $"{name} starts above the viewport (y={box.Y}).");
        Assert.True(box.X + box.Width <= width + 1, $"{name} ends right of the viewport (x={box.X}, w={box.Width}, viewport={width}).");
        Assert.True(box.Y + box.Height <= height + 1, $"{name} ends below the viewport (y={box.Y}, h={box.Height}, viewport={height}).");
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
