using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Mobile tab suspend: offline + visibility must recover (reconnect or throttled reload).
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class MobileSuspendRecoveryPlaywrightTests : IAsyncLifetime
{
    private IllustratedPagesWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
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

    [Fact]
    public async Task Offline_then_visible_recovers_circuit_at_390()
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IsMobile = true,
            HasTouch = true,
            IgnoreHTTPSErrors = true,
        });

        var page = await context.NewPageAsync();
        var signIn = $"{baseUrl}/__test/sign-in?role=candidate&return={Uri.EscapeDataString("/candidate/ontdekkingsreis")}";
        await page.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.Load });
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 60_000 });

        await context.SetOfflineAsync(true);
        await page.WaitForTimeoutAsync(1500);
        await context.SetOfflineAsync(false);

        var recovered = await page.EvaluateAsync<bool>(
            """
            async () => {
              if (!window.lobsyCircuit || typeof window.lobsyCircuit.recoverAfterSuspend !== 'function') {
                return false;
              }
              await window.lobsyCircuit.recoverAfterSuspend('playwright-test');
              return document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'
                && !document.documentElement.hasAttribute('data-lobsy-reconnect-active');
            }
            """);

        Assert.True(recovered);

        await page.Locator("nav.bottom-nav a[href='/candidate/paspoort']").First.ClickAsync(new() { Timeout = 15_000 });
        await page.WaitForURLAsync(
            u => u.Contains("/candidate/paspoort", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 30_000 });
    }

    [Fact]
    public async Task Offline_does_not_auto_reload_during_reconnect_exhaustion()
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IsMobile = true,
            HasTouch = true,
            IgnoreHTTPSErrors = true,
        });

        var page = await context.NewPageAsync();
        await page.GotoAsync($"{baseUrl}/__test/sign-in?role=candidate&return=/candidate/ontdekkingsreis");
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 60_000 });

        await context.SetOfflineAsync(true);
        await page.EvaluateAsync(
            """
            () => {
              window.lobsyCircuit.reloadPageOnce('playwright-offline-test');
            }
            """);

        await page.WaitForTimeoutAsync(45_000);
        var stillOnApp = await page.EvaluateAsync<bool>(
            "() => location.href.includes('/candidate/ontdekkingsreis') && !String(document.title).includes('INTERNET_DISCONNECTED')");
        Assert.True(stillOnApp, "Document reload ran while offline (ERR_INTERNET_DISCONNECTED).");
    }

    [Fact]
    public async Task Stuck_reconnect_trying_is_demoted_to_failed_with_visible_actions()
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IsMobile = true,
            HasTouch = true,
            IgnoreHTTPSErrors = true,
        });

        var page = await context.NewPageAsync();
        await page.GotoAsync($"{baseUrl}/__test/sign-in?role=candidate&return=/candidate/ontdekkingsreis");
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 60_000 });

        await page.EvaluateAsync(
            """
            () => {
              const m = document.getElementById('components-reconnect-modal');
              if (!m) throw new Error('no reconnect modal');
              m.classList.add('components-reconnect-show');
              m.classList.remove('components-reconnect-hide');
              document.documentElement.setAttribute('data-lobsy-reconnect-active', '');
            }
            """);

        await page.EvaluateAsync("() => window.lobsyCircuit.demoteStuckTrying()");

        var failedVisible = await page.EvaluateAsync<bool>(
            """
            () => {
              const m = document.getElementById('components-reconnect-modal');
              if (!m) return false;
              const failed = m.classList.contains('components-reconnect-failed');
              const btn = m.querySelector('[data-reconnect-reload]');
              return failed && btn && window.getComputedStyle(btn).display !== 'none';
            }
            """);
        Assert.True(failedVisible);
    }
}
