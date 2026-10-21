using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Primary nav must keep URL and @Body in sync (no enhanced-nav / circuit races on desktop).
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class CandidatePrimaryNavSyncPlaywrightTests : IAsyncLifetime
{
    private static readonly (string Href, string Probe, string WrongProbe)[] Targets =
    [
        ("/carriere", ".career-page, .career-card", ".journey-overview"),
        ("/candidate/paspoort", ".passport-page", ".journey-overview"),
        ("/banenkaart", ".jobsy-discovery, .bk-page", ".journey-overview"),
        ("/candidate/ontdekkingsreis", ".journey-overview", ".career-page"),
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
    [InlineData(1280, 900, 10)]
    [InlineData(390, 844, 10)]
    public async Task Reload_then_fast_header_nav_content_matches_url(int width, int height, int cycles)
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
        var signIn = $"{baseUrl}/__test/sign-in?role=candidate&return={Uri.EscapeDataString("/candidate/ontdekkingsreis")}";
        await page.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.AddInitScriptAsync(
            """
            () => {
              window.__lobsyLoadCount = 0;
              window.addEventListener("load", () => { window.__lobsyLoadCount = (window.__lobsyLoadCount || 0) + 1; });
            }
            """);

        await page.GotoAsync($"{baseUrl}/candidate/ontdekkingsreis", new() { WaitUntil = WaitUntilState.Load, Timeout = 90_000 });
        await page.Locator(".journey-overview").WaitForAsync(new() { Timeout = 60_000 });
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 60_000 });

        for (var i = 0; i < cycles; i++)
        {
            await page.EvaluateAsync("() => { window.__lobsyLoadCount = 0; }");
            await page.ReloadAsync(new() { WaitUntil = WaitUntilState.Load });
            await page.WaitForFunctionAsync(
                "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
                null,
                new() { Timeout = 60_000 });
            await page.EvaluateAsync("() => { window.__lobsyLoadCount = 0; }");

            var (href, probe, wrongProbe) = Targets[i % Targets.Length];
            var link = width >= 500
                ? page.Locator($".candidate-header-nav a[href='{href}']").First
                : page.Locator($"nav.bottom-nav a[href='{href}']").First;
            await link.ClickAsync();

            await page.WaitForURLAsync(
                u => NormalizePath(u) == NormalizePath(href),
                new() { Timeout = 8_000 });

            var contentOk = await WaitForRouteContentAsync(page, probe, wrongProbe, timeoutMs: 1_000);
            Assert.True(contentOk, $"Cycle {i + 1}: content did not match {href} within 1s at {width}px.");
            var loads = await page.EvaluateAsync<int>("() => window.__lobsyLoadCount || 0");
            Assert.True(loads == 0, $"Cycle {i + 1}: unexpected full document reload after nav to {href} at {width}px.");
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

    private static async Task<bool> WaitForRouteContentAsync(
        IPage page,
        string probe,
        string wrongProbe,
        int timeoutMs)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            var state = await page.EvaluateAsync<bool>(
                """
                ({ probe, wrongProbe }) => {
                  const probes = probe.split(',').map(s => s.trim());
                  const wrong = wrongProbe.split(',').map(s => s.trim());
                  return probes.some(p => document.querySelector(p));
                }
                """,
                new { probe, wrongProbe });
            if (state)
            {
                return true;
            }

            await page.WaitForTimeoutAsync(50);
        }

        return false;
    }
}
