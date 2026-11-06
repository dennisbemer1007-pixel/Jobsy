using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
public sealed class CarrierePostReconnect502InvestigationPlaywrightTests : IAsyncLifetime
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
    public async Task After_offline_recovery_carriere_nav_and_cookie_header_sizes()
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 900 },
            IgnoreHTTPSErrors = true,
        });

        var page = await context.NewPageAsync();
        var documentLoads = new List<(string Url, int Status)>();
        page.Response += (_, r) =>
        {
            if (r.Request.IsNavigationRequest && r.Request.ResourceType == "document")
            {
                documentLoads.Add((r.Url, r.Status));
            }
        };

        await page.GotoAsync($"{baseUrl}/__test/sign-in?role=candidate&return=/candidate/ontdekkingsreis");
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 60_000 });

        await context.SetOfflineAsync(true);
        await page.WaitForTimeoutAsync(35_000);
        await context.SetOfflineAsync(false);
        await page.WaitForFunctionAsync("() => navigator.onLine === true", null, new() { Timeout = 10_000 });

        await page.EvaluateAsync(
            """
            async () => {
              if (window.lobsyCircuit && typeof window.lobsyCircuit.recoverAfterSuspend === 'function') {
                await window.lobsyCircuit.recoverAfterSuspend('investigation');
              }
              for (let i = 0; i < 8; i++) {
                try {
                  await fetch('/account/session-activity', { method: 'POST', credentials: 'same-origin', headers: { Accept: 'application/json' } });
                } catch (e) { }
              }
            }
            """);

        var metrics = await page.EvaluateAsync<string>(
            """
            async () => {
              const cookieLen = (document.cookie || '').length;
              let carriereStatus = 0;
              let paspoortStatus = 0;
              try {
                const c = await fetch('/carriere', { method: 'GET', credentials: 'same-origin', cache: 'no-store' });
                carriereStatus = c.status;
              } catch (e) { carriereStatus = -1; }
              try {
                const p = await fetch('/candidate/paspoort', { method: 'GET', credentials: 'same-origin', cache: 'no-store' });
                paspoortStatus = p.status;
              } catch (e) { paspoortStatus = -1; }
              return JSON.stringify({ cookieLen, carriereStatus, paspoortStatus });
            }
            """);

        documentLoads.Clear();
        await page.Locator("[data-testid=candidate-header-nav] a[href='/carriere']").ClickAsync(new() { Timeout = 15_000 });
        await page.WaitForURLAsync(
            u => u.Contains("/carriere", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 45_000 });

        var navInteractive = await page.EvaluateAsync<bool>(
            "() => document.documentElement.getAttribute('data-lobsy-candidate-nav-interactive') === 'true'");

        Assert.True(navInteractive, "Expected candidate nav interactive before carriere click.");
        Assert.Contains("carriere", page.Url, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("502", metrics, StringComparison.Ordinal);
        Assert.True(
            documentLoads.Count <= 1,
            $"Expected SPA nav (0-1 document loads), got {documentLoads.Count}: {string.Join(", ", documentLoads.Select(d => d.Status))}");
    }

    [Fact]
    public async Task SafeLocationAssign_prefetch_doubles_document_requests_when_nav_not_interactive()
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 900 },
            IgnoreHTTPSErrors = true,
        });

        var page = await context.NewPageAsync();
        await page.GotoAsync($"{baseUrl}/__test/sign-in?role=candidate&return=/candidate/ontdekkingsreis");
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 60_000 });

        var carrierePrefetches = 0;
        page.Request += (_, req) =>
        {
            if ((req.Method == "HEAD" || req.Method == "GET")
                && req.Url.Contains("/carriere", StringComparison.OrdinalIgnoreCase)
                && !req.Url.Contains("_blazor", StringComparison.OrdinalIgnoreCase))
            {
                carrierePrefetches++;
            }
        };

        await page.EvaluateAsync(
            """
            () => {
              document.documentElement.removeAttribute('data-lobsy-candidate-nav-interactive');
              window.lobsyAllowDocumentReload && window.lobsyAllowDocumentReload();
              window.__lobsyAllowDocumentReload = true;
              if (window.lobsyReconnect && window.lobsyReconnect.allowDocumentReload) {
                window.lobsyReconnect.allowDocumentReload();
              }
            }
            """);

        await page.EvaluateAsync(
            """
            () => {
              const a = document.querySelector('[data-testid=candidate-header-nav] a[href="/carriere"]');
              if (!a) throw new Error('no carriere link');
              a.dispatchEvent(new MouseEvent('click', { bubbles: true, cancelable: true, view: window }));
            }
            """);

        await page.WaitForTimeoutAsync(2500);
        Assert.True(
            carrierePrefetches >= 1,
            $"assignWithServerRetry should prefetch /carriere before assign; saw {carrierePrefetches} HEAD/GET(s).");
    }
}
