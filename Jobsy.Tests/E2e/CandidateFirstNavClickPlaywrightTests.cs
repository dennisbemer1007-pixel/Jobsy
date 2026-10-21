using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// First primary-nav click after a fresh load must never leave URL, body, title, and active nav out of sync.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class CandidateFirstNavClickPlaywrightTests : IAsyncLifetime
{
    private const string StartPath = "/candidate/ontdekkingsreis";

    private static readonly (string Href, string Probe)[] FirstClickTargets =
    [
        ("/candidate/paspoort", ".passport-page"),
        ("/banenkaart", ".jobsy-discovery, .bk-page"),
        ("/candidate/applications", ".apps-page, .kb-apps"),
        ("/carriere", ".career-page, .career-card"),
        ("/candidate/ontdekkingsreis", ".journey-overview"),
    ];

    private IllustratedPagesWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        Environment.SetEnvironmentVariable("JOBSY_UWV_FEATURES", "1");
        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        _web = new IllustratedPagesWebFactory();
        _ = _web.ServerAddress;
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        _web?.Dispose();
    }

    [Theory]
    [InlineData(1280, 900, 20)]
    [InlineData(390, 844, 20)]
    public async Task Fresh_load_first_primary_nav_click_stays_in_sync(int width, int height, int cycles)
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IsMobile = width < 500,
            HasTouch = width < 500,
            IgnoreHTTPSErrors = true,
        });

        var page = await context.NewPageAsync();
        await PlaywrightCookieConsent.AcceptAsync(context);
        var signIn = $"{baseUrl}/__test/sign-in?role=candidate&return={Uri.EscapeDataString(StartPath)}";
        await page.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync($"{baseUrl}{StartPath}", new() { WaitUntil = WaitUntilState.Load, Timeout = 90_000 });

        for (var cycle = 0; cycle < cycles; cycle++)
        {
            await page.ReloadAsync(new() { WaitUntil = WaitUntilState.Load });
            await page.WaitForFunctionAsync(
                "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
                null,
                new() { Timeout = 60_000 });
            await page.WaitForFunctionAsync(
                "() => document.documentElement.getAttribute('data-lobsy-candidate-nav-interactive') === 'true'",
                null,
                new() { Timeout = 15_000 });

            var (href, probe) = FirstClickTargets[cycle % FirstClickTargets.Length];
            if (NormalizePath(href) == NormalizePath(StartPath))
            {
                (href, probe) = ("/candidate/paspoort", ".passport-page");
            }

            var link = width >= 500
                ? page.Locator($".candidate-header-nav a[href='{href}']").First
                : page.Locator($"nav.bottom-nav a[href='{href}']").First;
            await link.ClickAsync(new() { Timeout = 8_000 });

            await page.WaitForURLAsync(
                u => NormalizePath(u) == NormalizePath(href),
                new() { Timeout = 8_000 });

            var ok = await WaitForInSyncAsync(page, href, probe, timeoutMs: 1_000);
            Assert.True(ok, $"Cycle {cycle + 1}: first click to {href} out of sync within 1s at {width}px.");
        }
    }

    private static string NormalizePath(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return uri.AbsolutePath.TrimEnd('/');
        }

        return url.TrimEnd('/');
    }

    private static async Task<bool> WaitForInSyncAsync(
        IPage page,
        string href,
        string probe,
        int timeoutMs)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            var state = await page.EvaluateAsync<bool>(
                """
                ({ href, probe }) => {
                  const norm = (u) => {
                    try {
                      const p = new URL(u, location.href).pathname.replace(/\/$/, '') || '/';
                      return p;
                    } catch (e) { return u; }
                  };
                  const target = norm(href);
                  if (norm(location.pathname) !== target) return false;
                  const probes = probe.split(',').map(s => s.trim());
                  if (!probes.some(p => document.querySelector(p))) return false;
                  const visible = [...document.querySelectorAll('[data-lobsy-candidate-nav="true"]')]
                    .filter(el => el.offsetParent !== null || el.getClientRects().length > 0);
                  const actives = visible.filter(el =>
                    el.classList.contains('is-active') || el.getAttribute('aria-current') === 'page');
                  if (actives.length !== 1) return false;
                  return norm(actives[0].getAttribute('href') || '') === target;
                }
                """,
                new { href, probe });
            if (state)
            {
                return true;
            }

            await page.WaitForTimeoutAsync(40);
        }

        return false;
    }
}
