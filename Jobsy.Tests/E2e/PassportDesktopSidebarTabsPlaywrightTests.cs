using System.Text.Json;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
public sealed class PassportDesktopSidebarTabsPlaywrightTests : IAsyncLifetime
{
    private const string ArtifactsDir = "/opt/cursor/artifacts/screenshots";

    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
    }

    [Fact]
    public async Task Capture_top_of_page_at_1280x800()
    {
        Directory.CreateDirectory(ArtifactsDir);
        using var web = new IllustratedPagesWebFactory();
        _ = web.ServerAddress;
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 800 },
            IgnoreHTTPSErrors = true,
        });
        var page = await OpenPaspoortDesktopAsync(web, context);
        await page.EvaluateAsync("() => window.scrollTo(0, 0)");
        await page.WaitForTimeoutAsync(300);

        var ready = await EvaluateLayoutAsync(page, requireSticky: false);
        Assert.True(ready.GetProperty("ok").GetBoolean(), ready.ToString());

        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(ArtifactsDir, "paspoort-desktop-top-1280x800.png"),
            FullPage = false,
        });
    }

    [Theory]
    [InlineData(1024, 600, "paspoort-desktop-tabs-1024x600")]
    [InlineData(1280, 800, "paspoort-desktop-tabs-1280x800")]
    public async Task Sticky_tabs_do_not_overlap_sidebar(int width, int height, string shotName)
    {
        Directory.CreateDirectory(ArtifactsDir);
        using var web = new IllustratedPagesWebFactory();
        _ = web.ServerAddress;
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IgnoreHTTPSErrors = true,
        });
        var page = await OpenPaspoortDesktopAsync(web, context);
        await ScrollUntilTabsStickyAsync(page);
        await page.WaitForTimeoutAsync(250);

        var json = await EvaluateLayoutAsync(page, requireSticky: true);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(ArtifactsDir, $"{shotName}.png"),
            FullPage = false,
        });

        Assert.True(json.GetProperty("ok").GetBoolean(), json.ToString());
    }

    private static async Task<IPage> OpenPaspoortDesktopAsync(IllustratedPagesWebFactory web, IBrowserContext context)
    {
        var baseUrl = web.ServerAddress.TrimEnd('/');
        var page = await context.NewPageAsync();
        await PlaywrightCookieConsent.AcceptAsync(context);
        await page.GotoAsync(
            $"{baseUrl}/__test/sign-in?role=candidate&return={Uri.EscapeDataString("/candidate/paspoort")}",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync($"{baseUrl}/candidate/paspoort", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.Locator(".passport-page").WaitForAsync(new() { Timeout = 60_000 });
        await page.Locator(".page-skeleton").WaitForAsync(new() { State = WaitForSelectorState.Detached, Timeout = 60_000 });
        await page.Locator(".passport-layout__sidebar .passport-card").WaitForAsync(new() { Timeout = 30_000 });
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 60_000 });

        await page.Locator("#passport-panel-dna .passport-dna__grid").WaitForAsync(new() { Timeout = 30_000 });
        await Assertions.Expect(page.Locator("#passport-panel-dna .panel-inline-error")).ToHaveCountAsync(0, new() { Timeout = 2_000 });
        await page.Locator("#passport-panel-dna .passport-dna__h").First.WaitForAsync(new() { Timeout = 10_000 });
        await page.EvaluateAsync(
            """
            () => {
              window.lobsyCandidateNav?.syncAppHeaderHeight?.();
              window.lobsyPaspoortLayout?.syncSidebarSticky?.();
            }
            """);

        return page;
    }

    private static async Task ScrollUntilTabsStickyAsync(IPage page)
    {
        await page.EvaluateAsync(
            """
            () => {
              const root = document.querySelector('.passport-page--illustrated');
              const shell = document.querySelector('.passport-tabs-shell');
              if (!root || !shell) return;
              const stickyTop = parseFloat(getComputedStyle(shell).top) || 68;
              const shellDocTop = shell.getBoundingClientRect().top + window.scrollY;
              const needScroll = shellDocTop - stickyTop + 48;
              const maxY = Math.max(
                document.documentElement.scrollHeight,
                document.body.scrollHeight) - window.innerHeight;
              if (maxY + 8 < needScroll) {
                root.style.paddingBottom = `${Math.ceil(needScroll - maxY + window.innerHeight * 0.35)}px`;
              }
            }
            """);

        for (var step = 0; step < 16; step++)
        {
            var stuck = await page.EvaluateAsync<bool>(
                """
                () => {
                  const shell = document.querySelector('.passport-tabs-shell');
                  const header = document.querySelector('.app-header');
                  if (!shell || !header) return false;
                  const stickyTop = parseFloat(getComputedStyle(shell).top);
                  const target = Number.isFinite(stickyTop) && stickyTop >= 0
                    ? stickyTop
                    : header.getBoundingClientRect().bottom;
                  const shellDocTop = shell.getBoundingClientRect().top + window.scrollY;
                  const targetScroll = Math.max(0, shellDocTop - target);
                  window.scrollTo(0, targetScroll);
                  const shellTop = shell.getBoundingClientRect().top;
                  return window.scrollY > 48 && Math.abs(shellTop - target) < 6;
                }
                """);
            if (stuck)
            {
                return;
            }

            await page.WaitForTimeoutAsync(120);
        }

        throw new InvalidOperationException("Passport tab band did not reach sticky position.");
    }

    private static async Task<JsonElement> EvaluateLayoutAsync(IPage page, bool requireSticky)
    {
        return await page.EvaluateAsync<JsonElement>(
            """
            (requireSticky) => {
              const header = document.querySelector('.app-header');
              const shell = document.querySelector('.passport-tabs-shell');
              const sidebar = document.querySelector('.passport-layout__sidebar');
              const sidebarCard = document.querySelector('.passport-layout__sidebar .passport-card');
              const dnaGrid = document.querySelector('#passport-panel-dna .passport-dna__grid');
              const dnaError = document.querySelector('#passport-panel-dna .panel-inline-error');
              if (!header || !shell || !sidebar || !sidebarCard || !dnaGrid) {
                return { ok: false, reason: 'missing-elements' };
              }
              if (dnaError) {
                return { ok: false, reason: 'dna-panel-error' };
              }
              const headerBox = header.getBoundingClientRect();
              const shellBox = shell.getBoundingClientRect();
              const sidebarBox = sidebar.getBoundingClientRect();
              const dnaBox = dnaGrid.getBoundingClientRect();
              const stickyTop = parseFloat(getComputedStyle(shell).top);
              const stickyTarget = Number.isFinite(stickyTop) && stickyTop >= 0
                ? stickyTop
                : headerBox.bottom;
              const gap = shellBox.top - stickyTarget;
              const sticky = window.scrollY > 64 && Math.abs(gap) < 6;
              const horizontalOverlap =
                sidebarBox.right > shellBox.left + 2
                && sidebarBox.left < shellBox.right
                && sidebarBox.bottom > shellBox.top + 2
                && sidebarBox.top < shellBox.bottom;
              const sidebarVisible =
                sidebarBox.height > 80
                && sidebarBox.bottom > headerBox.bottom + 8
                && sidebarBox.top < window.innerHeight - 24;
              const dnaLoaded = dnaBox.height > 40;
              const dnaInViewport = dnaLoaded && dnaBox.bottom > shellBox.bottom + 4 && dnaBox.top < window.innerHeight;
              const shellInMainColumn = shellBox.left >= sidebarBox.right - 6;
              const probeX = sidebarBox.left + Math.min(48, sidebarBox.width * 0.35);
              const probeY = Math.min(
                Math.max(headerBox.bottom + 36, sidebarBox.top + 36),
                sidebarBox.bottom - 24);
              const hit = document.elementFromPoint(probeX, probeY);
              const probeOnSidebar = !!(hit && sidebar.contains(hit));
              return {
                ok: (!requireSticky || sticky)
                  && !horizontalOverlap
                  && shellInMainColumn
                  && sidebarVisible
                  && dnaLoaded
                  && (requireSticky ? dnaInViewport : true)
                  && probeOnSidebar,
                sticky,
                horizontalOverlap,
                shellInMainColumn,
                sidebarVisible,
                dnaInViewport,
                dnaLoaded,
                probeOnSidebar,
                gap,
                shellLeft: shellBox.left,
                sidebarRight: sidebarBox.right
              };
            }
            """,
            requireSticky);
    }
}
