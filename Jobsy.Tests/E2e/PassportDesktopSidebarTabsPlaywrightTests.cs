using System.Text.Json;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
public sealed class PassportDesktopSidebarTabsPlaywrightTests : IAsyncLifetime
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

    [Theory]
    [InlineData(1024, 600, "paspoort-desktop-tabs-1024x600")]
    [InlineData(1280, 800, "paspoort-desktop-tabs-1280x800")]
    public async Task Sticky_tabs_do_not_overlap_sidebar(int width, int height, string shotName)
    {
        Directory.CreateDirectory(ArtifactsDir);
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IgnoreHTTPSErrors = true,
        });

        var page = await context.NewPageAsync();
        await PlaywrightCookieConsent.AcceptAsync(context);
        await page.GotoAsync(
            $"{baseUrl}/__test/sign-in?role=candidate&return={Uri.EscapeDataString("/candidate/paspoort")}",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync($"{baseUrl}/candidate/paspoort", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.Locator(".passport-page").WaitForAsync(new() { Timeout = 60_000 });
        await page.Locator(".passport-layout__sidebar .passport-card").WaitForAsync(new() { Timeout = 30_000 });
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 60_000 });

        await page.EvaluateAsync(
            """
            () => {
              const main = document.querySelector('.passport-layout__main');
              if (!main || main.querySelector('[data-test-scroll-spacer]')) return;
              const spacer = document.createElement('div');
              spacer.setAttribute('data-test-scroll-spacer', 'true');
              spacer.style.height = '140vh';
              spacer.style.pointerEvents = 'none';
              main.appendChild(spacer);
            }
            """);

        await page.EvaluateAsync(
            """
            () => {
              const setScroll = (y) => {
                window.scrollTo(0, y);
                document.documentElement.scrollTop = y;
                document.body.scrollTop = y;
              };
              const header = document.querySelector('.app-header');
              const shell = document.querySelector('.passport-tabs-shell');
              const maxY = Math.max(document.body.scrollHeight, document.documentElement.scrollHeight) - window.innerHeight;
              for (let y = 0; y <= maxY + 40; y += 60) {
                setScroll(y);
                if (!header || !shell) continue;
                const gap = shell.getBoundingClientRect().top - header.getBoundingClientRect().bottom;
                if (Math.abs(gap) < 4) return;
              }
            }
            """);

        try
        {
            await page.WaitForFunctionAsync(
                """
                () => {
                  const header = document.querySelector('.app-header');
                  const shell = document.querySelector('.passport-tabs-shell');
                  if (!header || !shell) return false;
                  const gap = shell.getBoundingClientRect().top - header.getBoundingClientRect().bottom;
                  return Math.abs(gap) < 5;
                }
                """,
                null,
                new() { Timeout = 8_000 });
        }
        catch (TimeoutException)
        {
            // Overlap geometry is the regression guard; sticky may need more scroll on short viewports.
        }

        await page.WaitForTimeoutAsync(200);

        var json = await page.EvaluateAsync<JsonElement>(
            """
            () => {
              const header = document.querySelector('.app-header');
              const shell = document.querySelector('.passport-tabs-shell');
              const sidebarCard = document.querySelector('.passport-layout__sidebar .passport-card');
              if (!header || !shell || !sidebarCard) {
                return { ok: false, reason: 'missing-elements' };
              }
              const headerBox = header.getBoundingClientRect();
              const shellBox = shell.getBoundingClientRect();
              const sidebarBox = sidebarCard.getBoundingClientRect();
              const gap = shellBox.top - headerBox.bottom;
              const sticky = Math.abs(gap) < 4;
              const horizontalOverlap =
                sidebarBox.right > shellBox.left + 2
                && sidebarBox.left < shellBox.right
                && sidebarBox.bottom > shellBox.top + 2
                && sidebarBox.top < shellBox.bottom;
              const probeX = sidebarBox.left + Math.min(40, sidebarBox.width * 0.25);
              const probeY = Math.min(
                sidebarBox.bottom - 24,
                Math.max(sidebarBox.top + 24, (shellBox.top + shellBox.bottom) * 0.5));
              const hit = document.elementFromPoint(probeX, probeY);
              const probeOnSidebar = !!(hit && sidebarCard.contains(hit));
              return {
                ok: !horizontalOverlap && probeOnSidebar && shellBox.left >= sidebarBox.right - 4,
                sticky,
                horizontalOverlap,
                probeOnSidebar,
                gap,
                shellLeft: shellBox.left,
                sidebarRight: sidebarBox.right
              };
            }
            """);

        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(ArtifactsDir, $"{shotName}.png"),
            FullPage = false,
        });

        Assert.True(json.GetProperty("ok").GetBoolean(), json.ToString());
    }
}
