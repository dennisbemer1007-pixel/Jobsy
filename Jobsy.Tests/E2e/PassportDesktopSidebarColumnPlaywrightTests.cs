using System.Text.Json;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
public sealed class PassportDesktopSidebarColumnPlaywrightTests : IAsyncLifetime
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

    [Theory]
    [InlineData(1024, 600, true, "paspoort-sidebar-empty-1024x600-bottom")]
    [InlineData(1280, 800, true, "paspoort-sidebar-empty-1280x800-bottom")]
    [InlineData(1024, 600, false, "paspoort-sidebar-full-1024x600-bottom")]
    [InlineData(1280, 800, false, "paspoort-sidebar-full-1280x800-bottom")]
    public async Task Sidebar_passport_card_does_not_overlap_tip_at_page_bottom(
        int width,
        int height,
        bool minimalProfile,
        string shotName)
    {
        Directory.CreateDirectory(ArtifactsDir);
        using var web = new IllustratedPagesWebFactory(minimalPassportProfile: minimalProfile);
        _ = web.ServerAddress;
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IgnoreHTTPSErrors = true,
        });
        var page = await OpenPaspoortAsync(web, context, minimalProfile);
        await ScrollToPageBottomAsync(page);
        await page.WaitForTimeoutAsync(200);

        var json = await EvaluateSidebarStackAsync(page);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(ArtifactsDir, $"{shotName}.png"),
            FullPage = false,
        });

        Assert.True(json.GetProperty("ok").GetBoolean(), json.ToString());
    }

    private static async Task<IPage> OpenPaspoortAsync(
        IllustratedPagesWebFactory web,
        IBrowserContext context,
        bool minimalProfile)
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
        await page.Locator(".passport-layout__sidebar .kompas-tip").WaitForAsync(new() { Timeout = 30_000 });
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 60_000 });
        await WaitForSingleHeaderNavAsync(page);
        await page.EvaluateAsync(
            """
            () => {
              window.lobsyPaspoortLayout?.syncSidebarSticky?.();
              window.lobsyCandidateNav?.syncAppHeaderHeight?.();
            }
            """);

        if (!minimalProfile)
        {
            await page.Locator("#passport-panel-dna .passport-dna__grid").WaitForAsync(new() { Timeout = 30_000 });
        }

        return page;
    }

    private static async Task ScrollToPageBottomAsync(IPage page)
    {
        await page.EvaluateAsync(
            """
            () => {
              const root = document.querySelector('.passport-page--illustrated');
              if (root) {
                root.style.paddingBottom = `${Math.max(window.innerHeight * 0.15, 96)}px`;
              }
              const maxY = Math.max(
                document.documentElement.scrollHeight,
                document.body.scrollHeight) - window.innerHeight;
              window.scrollTo(0, Math.max(0, maxY));
            }
            """);
        await page.WaitForFunctionAsync(
            """
            () => {
              const maxY = Math.max(
                document.documentElement.scrollHeight,
                document.body.scrollHeight) - window.innerHeight;
              return Math.abs(window.scrollY - Math.max(0, maxY)) < 4;
            }
            """,
            null,
            new() { Timeout = 5_000 });
    }

    private static async Task<JsonElement> EvaluateSidebarStackAsync(IPage page)
    {
        return await page.EvaluateAsync<JsonElement>(
            """
            () => {
              const card = document.querySelector('.passport-layout__sidebar > .passport-card');
              const tip = document.querySelector('.passport-layout__sidebar > .kompas-tip');
              const sidebar = document.querySelector('.passport-layout__sidebar');
              const cta = tip?.querySelector('.kompas-tip__cta');
              if (!card || !tip || !sidebar) {
                return { ok: false, reason: 'missing-sidebar-elements' };
              }
              const cardBox = card.getBoundingClientRect();
              const tipBox = tip.getBoundingClientRect();
              const gap = tipBox.top - cardBox.bottom;
              const cardOverlapsTip = cardBox.bottom > tipBox.top + 3;
              const tipBelowCard = tipBox.top >= cardBox.bottom - 2;
              const ctaVisible = !!cta && cta.getBoundingClientRect().height > 0;
              let ctaHit = false;
              if (cta) {
                const r = cta.getBoundingClientRect();
                const x = r.left + r.width / 2;
                const y = r.top + r.height / 2;
                const hit = document.elementFromPoint(x, y);
                ctaHit = !!(hit && (cta === hit || cta.contains(hit)));
              }
              const sidebarStyle = getComputedStyle(sidebar);
              const innerScroll =
                sidebarStyle.overflowY === 'auto' || sidebarStyle.overflowY === 'scroll';
              const tipText = tip.querySelector('.kompas-tip__text');
              const tipTextClipped =
                !!tipText && tipText.scrollHeight > tipText.clientHeight + 2;
              const visibleNavRows = Array.from(
                document.querySelectorAll('.app-header .candidate-header-nav'))
                .filter((n) => {
                  const r = n.getBoundingClientRect();
                  return r.height > 0 && r.width > 0 && getComputedStyle(n).display !== 'none';
                }).length;
              return {
                ok: !cardOverlapsTip
                  && tipBelowCard
                  && ctaVisible
                  && ctaHit
                  && !innerScroll
                  && !tipTextClipped
                  && visibleNavRows === 1,
                cardOverlapsTip,
                tipBelowCard,
                gap,
                ctaHit,
                ctaVisible,
                innerScroll,
                tipTextClipped,
                visibleNavRows,
                scrollY: window.scrollY
              };
            }
            """);
    }

    private static async Task WaitForSingleHeaderNavAsync(IPage page)
    {
        await page.WaitForFunctionAsync(
            """
            () => {
              const reserve = document.querySelector('.candidate-header-nav--reserve');
              const live = document.querySelector('[data-testid=candidate-header-nav]');
              if (!live) return false;
              const reserveVisible = reserve
                && reserve.getBoundingClientRect().height > 0
                && getComputedStyle(reserve).display !== 'none';
              const navs = Array.from(document.querySelectorAll('.app-header .candidate-header-nav'))
                .filter((n) => {
                  const r = n.getBoundingClientRect();
                  return r.height > 0 && getComputedStyle(n).display !== 'none';
                });
              return !reserveVisible && navs.length === 1;
            }
            """,
            null,
            new() { Timeout = 30_000 });
    }
}
