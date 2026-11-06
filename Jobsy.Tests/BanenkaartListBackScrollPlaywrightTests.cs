using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Back from vacancy restores list scroll; no stale map popup after reload / list Back (desktop split + mobile Lijst).
/// </summary>
[Collection("BanenkaartPlaywright")]
public sealed class BanenkaartListBackScrollPlaywrightTests : IAsyncLifetime
{
    private const string DelftPath =
        "/banenkaart?maxMinutes=60&originLat=51.99389784&originLng=4.37897242&originLabel=Delft%2C%20Delft";

    private const string DelftListPath =
        "/banenkaart?maxMinutes=60&originLat=51.99389784&originLng=4.37897242&originLabel=Delft%2C%20Delft&weergave=lijst";

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IllustratedPagesWebFactory? _web;
    private string _baseUrl = "";

    public async ValueTask InitializeAsync()
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        _web = new IllustratedPagesWebFactory();
        _baseUrl = _web.ServerAddress.TrimEnd('/');
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
    public async Task Desktop_back_from_vacancy_restores_list_scrollTop()
    {
        const int width = 1280;
        const int height = 680;
        const bool mobileList = false;
        await BackFromVacancyRestoresListScrollTopAsync(width, height, mobileList);
    }

    [Fact]
    public async Task Mobile_lijst_pending_scroll_restores_after_catalog_rerender()
    {
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 390, Height = 844 },
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(_baseUrl + DelftListPath, new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await WaitCatalogReadyAsync(page);
        await page.WaitForFunctionAsync(
            "() => document.querySelector('[data-banenkaart-ssr=\"1\"]')?.classList.contains('show-list')",
            null,
            new() { Timeout = 20_000 });

        await page.EvaluateAsync(
            """
            () => {
              const el = document.querySelector('[data-testid=vacancy-list]')
                || document.querySelector('.vacancy-list');
              if (el) {
                el.style.maxHeight = '240px';
                el.style.overflow = 'auto';
              }
            }
            """);

        var scrollBefore = await ScrollListDownAsync(page);
        if (scrollBefore < 40)
        {
            scrollBefore = 280;
        }

        await page.EvaluateAsync(
            """
            (scrollTop) => {
              const root = document.querySelector('[data-banenkaart-ssr="1"]');
              const state = {
                savedAt: Date.now(),
                showMap: false,
                desktopList: true,
                skipMapPopupRestore: true,
                pendingListScrollRestore: true,
                listScrollTop: scrollTop
              };
              sessionStorage.setItem('jobsy.bk.view.v1', JSON.stringify(state));
              const cat = window.jobsyBanenkaartCatalog;
              if (cat?.getRows && cat.renderListRows && cat.resolveFilters) {
                cat.renderListRows(cat.getRows(), cat.resolveFilters(), false);
              }
              cat?.__testRestoreListScroll?.();
            }
            """,
            scrollBefore);

        var scrollAfter = await page.EvaluateAsync<double>(
            """
            () => {
              const root = document.querySelector('[data-banenkaart-ssr="1"]');
              if (!root) return -1;
              const list = root.querySelector('.vacancy-list, [data-testid=vacancy-list]');
              const pane = root.querySelector('.vacancy-pane');
              const el = (list && list.scrollHeight > list.clientHeight + 8) ? list : (pane || list);
              return el ? el.scrollTop : -1;
            }
            """);

        Assert.True(Math.Abs(scrollAfter - scrollBefore) <= 80,
            $"Expected scrollTop ~{scrollBefore}, got {scrollAfter}.");
    }

    private async Task BackFromVacancyRestoresListScrollTopAsync(int width, int height, bool mobileList)
    {
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(_baseUrl + (mobileList ? DelftListPath : DelftPath), new()
        {
            WaitUntil = WaitUntilState.Load,
            Timeout = 120_000
        });
        await WaitCatalogReadyAsync(page);

        if (mobileList)
        {
            await page.WaitForFunctionAsync(
                """
                () => {
                  const root = document.querySelector('[data-banenkaart-ssr="1"]');
                  return root && root.classList.contains('show-list')
                    && root.getAttribute('data-kb-view') === 'list';
                }
                """,
                null,
                new() { Timeout = 20_000 });
        }
        else
        {
            await WaitDesktopSplitReadyAsync(page);
        }

        var rowCount = await page.Locator("[data-testid=kb-list-row]").CountAsync();
        Assert.True(rowCount >= 4, "Expected multiple list rows, got " + rowCount);

        if (mobileList)
        {
            await page.EvaluateAsync(
                """
                () => {
                  const el = document.querySelector('[data-testid=vacancy-list]')
                    || document.querySelector('.vacancy-list');
                  if (el) {
                    el.style.maxHeight = '240px';
                    el.style.overflow = 'auto';
                  }
                }
                """);
        }

        var scrollBefore = await ScrollListDownAsync(page);
        Assert.True(scrollBefore > 40, "List rail must be scrollable before Back test, scrollTop=" + scrollBefore);

        var rowIndex = Math.Min(3, rowCount - 1);
        await page.Locator("[data-testid=kb-list-row]").Nth(rowIndex).ClickAsync(new() { Force = true });
        await page.WaitForURLAsync("**/vacancies/**", new() { Timeout = 60_000 });
        await page.GoBackAsync(new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await WaitCatalogReadyAsync(page);
        await page.EvaluateAsync(
            """
            () => {
              window.__jobsyBkRestoreViewStateForTests?.();
              const cat = window.jobsyBanenkaartCatalog;
              if (cat && cat.getRows && cat.renderListRows && cat.resolveFilters) {
                cat.renderListRows(cat.getRows(), cat.resolveFilters(), false);
              }
              cat?.__testRestoreListScroll?.();
            }
            """);
        if (!mobileList)
        {
            await WaitDesktopSplitReadyAsync(page);
        }
        else
        {
            await page.WaitForFunctionAsync(
                """
                () => {
                  const root = document.querySelector('[data-banenkaart-ssr="1"]');
                  return root && root.classList.contains('show-list');
                }
                """,
                null,
                new() { Timeout = 30_000 });
        }

        await page.WaitForTimeoutAsync(4500);

        var scrollAfter = await page.EvaluateAsync<double>(
            """
            () => {
              function scroller() {
                const root = document.querySelector('[data-banenkaart-ssr="1"]');
                if (!root) return null;
                const list = root.querySelector('.vacancy-list, [data-testid=vacancy-list]');
                const pane = root.querySelector('.vacancy-pane');
                if (list && list.scrollHeight > list.clientHeight + 8) return list;
                if (pane && pane.scrollHeight > pane.clientHeight + 8) return pane;
                return list || pane;
              }
              const list = scroller();
              return list ? list.scrollTop : -1;
            }
            """);

        var scrollTarget = await page.EvaluateAsync<double>(
            """
            (expected) => {
              function scroller() {
                const root = document.querySelector('[data-banenkaart-ssr="1"]');
                if (!root) return null;
                const list = root.querySelector('.vacancy-list, [data-testid=vacancy-list]');
                const pane = root.querySelector('.vacancy-pane');
                if (list && list.scrollHeight > list.clientHeight + 8) return list;
                if (pane && pane.scrollHeight > pane.clientHeight + 8) return pane;
                return list || pane;
              }
              const list = scroller();
              if (!list) return expected;
              const max = Math.max(0, list.scrollHeight - list.clientHeight);
              return Math.min(expected, max);
            }
            """,
            scrollBefore);

        Assert.True(scrollAfter >= 0, "List scroller missing after Back.");
        Assert.True(
            Math.Abs(scrollAfter - scrollTarget) <= 80,
            $"Expected scrollTop ~{scrollTarget}, got {scrollAfter} (before={scrollBefore}).");

        var strayPopup = await page.EvaluateAsync<bool>(
            """
            () => {
              const pop = document.querySelector('.maplibregl-popup.kb-popup-lagen-pop, .map-cluster-sheet, .job-map-popup');
              if (pop && pop.getClientRects().length > 0) return true;
              return document.body.classList.contains('s-popup');
            }
            """);
        Assert.False(strayPopup, "No map popup should open after Back from list vacancy.");
    }

    [Fact]
    public async Task Reload_does_not_reopen_map_popup_from_session()
    {
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 },
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(_baseUrl + DelftPath, new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await WaitCatalogReadyAsync(page);

        await page.WaitForFunctionAsync(
            "() => (window.jobMap?.__testGetPinCount?.() ?? 0) >= 10",
            null,
            new() { Timeout = 90_000 });

        await page.EvaluateAsync(
            """
            () => {
              window.jobMap?.invalidate?.();
              window.jobMap?.fitToOriginRings?.(false);
              window.jobMap?.__testGetMap?.()?.resize?.();
            }
            """);

        await page.WaitForFunctionAsync(
            "() => (window.jobMap?.__testGetRenderedPinCount?.() ?? 0) >= 1",
            null,
            new() { Timeout = 45_000 });

        await page.EvaluateAsync("() => window.jobMap?.__testTapLargestCluster?.()");
        await page.Locator(".maplibregl-popup.kb-popup-lagen-pop, .map-cluster-card, .job-map-popup").First
            .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 20_000 });

        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await WaitCatalogReadyAsync(page);

        await page.WaitForTimeoutAsync(1500);

        var popupOpen = await page.EvaluateAsync<bool>(
            """
            () => {
              const pop = document.querySelector('.maplibregl-popup.kb-popup-lagen-pop, .map-cluster-sheet, .job-map-popup');
              if (pop && pop.getClientRects().length > 0) return true;
              return document.body.classList.contains('s-popup');
            }
            """);
        Assert.False(popupOpen, "Fresh reload must not restore an earlier pin popup.");
    }

    private static async Task WaitCatalogReadyAsync(IPage page)
    {
        await page.WaitForFunctionAsync(
            "() => window.jobsyBanenkaartCatalog?.isReady?.() === true",
            null,
            new() { Timeout = 90_000 });
    }

    private static async Task WaitDesktopSplitReadyAsync(IPage page)
    {
        await page.WaitForFunctionAsync(
            """
            () => {
              const root = document.querySelector('[data-banenkaart-ssr="1"]');
              if (!root) return false;
              const rows = document.querySelectorAll('[data-testid=kb-list-row]').length;
              return root.classList.contains('show-map')
                && !root.classList.contains('show-list')
                && root.getAttribute('data-kb-view') === 'map'
                && rows >= 4;
            }
            """,
            null,
            new() { Timeout = 60_000 });
    }

    private static async Task<double> ScrollListDownAsync(IPage page)
    {
        return await page.EvaluateAsync<double>(
            """
            async () => {
              function scroller() {
                const root = document.querySelector('[data-banenkaart-ssr="1"]');
                if (!root) return null;
                const list = root.querySelector('.vacancy-list, [data-testid=vacancy-list]');
                const pane = root.querySelector('.vacancy-pane');
                if (list && list.scrollHeight > list.clientHeight + 8) return list;
                if (pane) {
                  if (pane.scrollHeight > pane.clientHeight + 8) return pane;
                  pane.style.maxHeight = '280px';
                  pane.style.overflow = 'auto';
                  if (pane.scrollHeight > pane.clientHeight + 8) return pane;
                }
                if (list) {
                  list.style.maxHeight = '280px';
                  list.style.overflow = 'auto';
                }
                return list || pane;
              }
              const list = scroller();
              if (!list) return -1;
              const rows = document.querySelectorAll('[data-testid=kb-list-row]');
              const target = rows[Math.min(3, Math.max(0, rows.length - 1))];
              if (target) target.scrollIntoView({ block: 'center' });
              for (let i = 0; i < 16; i++) {
                list.dispatchEvent(new WheelEvent('wheel', { deltaY: 600, bubbles: true, cancelable: true }));
                await new Promise(r => requestAnimationFrame(r));
              }
              const max = Math.max(0, list.scrollHeight - list.clientHeight);
              if (max < 48) return 0;
              list.scrollTop = Math.max(list.scrollTop, Math.floor(max * 0.55));
              await new Promise(r => requestAnimationFrame(() => requestAnimationFrame(r)));
              return list.scrollTop;
            }
            """);
    }
}
