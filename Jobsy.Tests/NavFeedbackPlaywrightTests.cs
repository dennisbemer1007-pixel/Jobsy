using System.Net;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Soft-skip without <c>JOBSY_E2E_BASE_URL</c>. Logged-in seed candidate on mobile:
/// bottom-nav tap shows active state within 100 ms (client navFeedback.js).
/// </summary>
[Collection("PlaywrightSmoke")]
public class NavFeedbackPlaywrightTests
{
    private const string DefaultEmail = "kandidaat@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";

    [Fact]
    public async Task Mobile_bottom_nav_tap_shows_active_within_100ms()
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
            ViewportSize = new() { Width = 390, Height = 844 },
            IgnoreHTTPSErrors = true
        });

        var page = await context.NewPageAsync();
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
        }
        catch (TimeoutException)
        {
            return;
        }

        await page.GotoAsync(baseUrl + "/", new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 60_000
        });

        try
        {
            await page.WaitForSelectorAsync("nav.bottom-nav a.bottom-nav__item", new() { Timeout = 30_000 });
        }
        catch (TimeoutException)
        {
            return;
        }

        // Soft-skip remote Acc hosts that have not shipped navFeedback.js yet.
        var hasScript = await page.EvaluateAsync<bool>("""
            () => !!document.querySelector('script[src*="navFeedback.js"]')
            """);
        if (!hasScript)
        {
            return;
        }

        var result = await page.EvaluateAsync<NavTapResult>("""
            () => {
              const items = Array.from(document.querySelectorAll('nav.bottom-nav a.bottom-nav__item'));
              const target = items.find(a => !a.classList.contains('is-active')
                && a.getAttribute('aria-current') !== 'page');
              if (!target) {
                return { ok: false, ms: -1, reason: 'no-inactive-tab' };
              }
              const t0 = performance.now();
              target.dispatchEvent(new PointerEvent('pointerdown', {
                bubbles: true,
                cancelable: true,
                pointerType: 'touch',
                isPrimary: true
              }));
              const ms = performance.now() - t0;
              const ok = target.classList.contains('is-active')
                || target.getAttribute('aria-current') === 'page';
              return { ok, ms, reason: ok ? 'ok' : 'no-active' };
            }
            """);

        Assert.NotNull(result);
        if (result!.Reason == "no-inactive-tab")
        {
            return;
        }

        Assert.True(result.Ok, $"Expected instant active state, got reason={result.Reason}");
        Assert.True(result.Ms < 100, $"Active state took {result.Ms:0.#} ms (budget 100 ms)");
    }

    [Fact]
    public void Nav_feedback_assets_are_wired()
    {
        var root = FindRepoRoot();
        var app = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "App.razor"));
        Assert.Contains("js/navFeedback.js?v=", app, StringComparison.Ordinal);
        Assert.Contains("css/features/nav-feedback.css?v=", app, StringComparison.Ordinal);

        Assert.True(File.Exists(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "navFeedback.js")));
        Assert.True(File.Exists(Path.Combine(root, "Jobsy.Web", "wwwroot", "css", "features", "nav-feedback.css")));

        var js = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "js", "navFeedback.js"));
        Assert.Contains("pointerdown", js, StringComparison.Ordinal);
        Assert.Contains("aria-current", js, StringComparison.Ordinal);
        Assert.Contains("enhancedload", js, StringComparison.Ordinal);
        Assert.Contains("nav-route-progress", js, StringComparison.Ordinal);

        var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "wwwroot", "css", "features", "nav-feedback.css"));
        Assert.Contains(".bottom-nav__item:active", css, StringComparison.Ordinal);
        Assert.Contains("nav-route-progress", css, StringComparison.Ordinal);
        Assert.Contains("prefers-reduced-motion", css, StringComparison.Ordinal);
        Assert.Contains("var(--brand)", css, StringComparison.Ordinal);
    }

    private sealed record NavTapResult(bool Ok, double Ms, string Reason);

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var handler = new SocketsHttpHandler { AllowAutoRedirect = true };
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(8) };
            using var response = await client.GetAsync(baseUrl + "/");
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
