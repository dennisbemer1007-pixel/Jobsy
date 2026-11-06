using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
public sealed class PostReconnectNavPlaywrightTests : IAsyncLifetime
{
    private const string ArtifactsDir = "/opt/cursor/artifacts/screenshots";

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
    public async Task Reconnect_success_dismisses_failed_toast()
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IsMobile = true,
            IgnoreHTTPSErrors = true,
        });

        var page = await context.NewPageAsync();
        await page.GotoAsync($"{baseUrl}/__test/sign-in?role=candidate&return=/candidate/ontdekkingsreis");
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 60_000 });

        if (string.Equals(Environment.GetEnvironmentVariable("LOBSY_PR_EVIDENCE"), "1", StringComparison.Ordinal))
        {
            Directory.CreateDirectory(ArtifactsDir);
            await page.EvaluateAsync(
                """
                () => {
                  const m = document.getElementById('components-reconnect-modal');
                  if (!m) throw new Error('no modal');
                  m.classList.add('components-reconnect-failed');
                  m.classList.remove('components-reconnect-hide');
                  document.documentElement.setAttribute('data-lobsy-reconnect-active', '');
                }
                """);
            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(ArtifactsDir, "post-reconnect-toast-before-dismiss-390.png"),
                FullPage = false,
            });
        }

        var hidden = await page.EvaluateAsync<bool>(
            """
            async () => {
              const m = document.getElementById('components-reconnect-modal');
              if (!m) return false;
              m.classList.add('components-reconnect-failed');
              m.classList.remove('components-reconnect-hide');
              document.documentElement.setAttribute('data-lobsy-reconnect-active', '');
              window.Blazor = window.Blazor || {};
              window.Blazor.reconnect = () => Promise.resolve(true);
              const ok = await window.lobsyReconnect.tryReconnect('playwright-test');
              if (!ok) return false;
              const style = window.getComputedStyle(m);
              const reserve = !document.documentElement.hasAttribute('data-lobsy-reconnect-active');
              return m.classList.contains('components-reconnect-hide')
                && !m.classList.contains('components-reconnect-failed')
                && style.display === 'none'
                && reserve;
            }
            """);

        Assert.True(hidden);

        await page.WaitForFunctionAsync(
            """
            () => {
              const m = document.getElementById('components-reconnect-modal');
              return !!m && m.classList.contains('components-reconnect-hide');
            }
            """,
            null,
            new() { Timeout = 5_000 });

        if (string.Equals(Environment.GetEnvironmentVariable("LOBSY_PR_EVIDENCE"), "1", StringComparison.Ordinal))
        {
            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(ArtifactsDir, "post-reconnect-toast-after-dismiss-390.png"),
                FullPage = false,
            });
        }
    }

    [Fact]
    public async Task Desktop_header_active_follows_url_after_mobile_nav_and_resize()
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

        await page.Locator("nav.bottom-nav a[href='/candidate/ontdekkingsreis']").First.ClickAsync();
        await page.WaitForURLAsync(
            u => u.Contains("/candidate/ontdekkingsreis", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 30_000 });
        await page.Locator("nav.bottom-nav a[href='/candidate/paspoort']").First.ClickAsync();
        await page.WaitForURLAsync(
            u => u.Contains("/candidate/paspoort", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 30_000 });

        await page.SetViewportSizeAsync(1280, 900);

        var activeHref = await page.EvaluateAsync<string>(
            """
            () => {
              const a = document.querySelector('[data-testid=candidate-header-nav] .candidate-header-nav__link.is-active');
              return a ? (a.getAttribute('href') || '') : '';
            }
            """);

        Assert.Contains("/candidate/paspoort", activeHref, StringComparison.OrdinalIgnoreCase);

        if (string.Equals(Environment.GetEnvironmentVariable("LOBSY_PR_EVIDENCE"), "1", StringComparison.Ordinal))
        {
            Directory.CreateDirectory(ArtifactsDir);
            await page.Locator("[data-testid=candidate-header-nav]").ScreenshotAsync(new()
            {
                Path = Path.Combine(ArtifactsDir, "post-reconnect-header-active-paspoort-1280.png"),
            });
        }
    }

    [Fact]
    public async Task Post_offline_recovery_primary_nav_uses_spa_not_full_document_load()
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 900 },
            IgnoreHTTPSErrors = true,
        });

        var page = await context.NewPageAsync();
        var documentNavigations = 0;
        page.Response += (_, response) =>
        {
            if (response.Request.IsNavigationRequest && response.Request.ResourceType == "document")
            {
                documentNavigations++;
            }
        };

        await page.GotoAsync($"{baseUrl}/__test/sign-in?role=candidate&return=/candidate/ontdekkingsreis");
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 60_000 });

        await context.SetOfflineAsync(true);
        await page.WaitForTimeoutAsync(1500);
        await context.SetOfflineAsync(false);
        await page.WaitForFunctionAsync("() => navigator.onLine === true", null, new() { Timeout = 10_000 });
        await page.EvaluateAsync(
            """
            async () => {
              if (window.lobsyCircuit && typeof window.lobsyCircuit.recoverAfterSuspend === 'function') {
                await window.lobsyCircuit.recoverAfterSuspend('playwright-post-offline');
              }
            }
            """);

        var navBefore = documentNavigations;
        await page.Locator("[data-testid=candidate-header-nav] a[href='/carriere']").ClickAsync(new() { Timeout = 15_000 });
        await page.WaitForURLAsync(
            u => u.Contains("/carriere", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 30_000 });

        Assert.Equal(navBefore, documentNavigations);
    }
}
