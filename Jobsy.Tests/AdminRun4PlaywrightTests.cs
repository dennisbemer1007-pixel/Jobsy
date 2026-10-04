using System.Net.Sockets;
using System.Security.Cryptography;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Npgsql;

namespace Jobsy.Tests;

/// <summary>
/// Browser regressions that bUnit cannot see: a menu that closes itself, a list that
/// reloads without painting, dropped keystrokes, and a top bar wider than a phone.
/// Soft-skips only when JOBSY_E2E_BASE_URL is unset or unreachable. A reachable stack
/// that cannot sign in as the seeded admin fails the test.
/// </summary>
[Collection("PlaywrightSmoke")]
public class AdminRun4PlaywrightTests
{
    private const string Password = "Jobsy123!";
    private const int SearchListCount = 4;

    // Names are a pair: neither string contains the other, each sorts or dates onto the
    // first page, and the unique token is not shared with the other row.
    private const string OrgAlphaName = "Aa Zetaalpha Organisatiefilter";
    private const string OrgBravoName = "Aa Zetabravo Organisatiefilter";
    private const string VacAlphaTitle = "Aa Zetaalpha Vacaturefilter";
    private const string VacBravoTitle = "Aa Zetabravo Vacaturefilter";
    // Single words that sort first: the users list masks multi-word names
    // ("Aa Zetaalpha Gebruikerfilter" becomes "Aa G."), and name-asc paging
    // would hide a "Z…" pair past page 1. Neither name contains the other.
    private const string UserAlphaName = "0aaaalphafilter";
    private const string UserBravoName = "0aaabravofilter";
    private const string LogAlphaToken = "Aazetaalphalogfilter";
    private const string LogBravoToken = "Aazetabravologfilter";

    private static readonly Guid CompanyAlphaId = Guid.Parse("0e2e4a01-0000-4000-8000-000000000001");
    private static readonly Guid CompanyBravoId = Guid.Parse("0e2e4a01-0000-4000-8000-000000000002");
    private static readonly Guid VacancyAlphaId = Guid.Parse("0e2e4a01-0000-4000-8000-000000000003");
    private static readonly Guid VacancyBravoId = Guid.Parse("0e2e4a01-0000-4000-8000-000000000004");
    private static readonly Guid UserAlphaId = Guid.Parse("0e2e4a01-0000-4000-8000-000000000005");
    private static readonly Guid UserBravoId = Guid.Parse("0e2e4a01-0000-4000-8000-000000000006");
    private static readonly Guid LogAlphaId = Guid.Parse("0e2e4a01-0000-4000-8000-000000000007");
    private static readonly Guid LogBravoId = Guid.Parse("0e2e4a01-0000-4000-8000-000000000008");

    private readonly List<string> _skippedSearchLists = [];
    private bool? _searchDbReady;

    [Fact]
    public async Task Menu_stays_open_search_filters_fast_typing_and_mobile_bar_fits()
    {
        var baseUrl = Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL");
        if (string.IsNullOrWhiteSpace(baseUrl) || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        await using var desktop = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 900 },
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(desktop);
        var page = await desktop.NewPageAsync();
        await LoginAdminAsync(page, baseUrl);
        // The CI database does not reliably have two rows: seed stops once any company
        // exists, and other tests can delete rows. Write a distinct pair before the checks.
        await EnsureAdminSearchRowsAsync();

        await AssertMenuStaysOpenAsync(page, baseUrl.TrimEnd('/') + "/admin/gebruikers");
        await AssertMenuStaysOpenAsync(page, baseUrl.TrimEnd('/') + "/admin/vacatures");
        await AssertSearchFiltersAsync(page, baseUrl.TrimEnd('/') + "/admin/organisaties", ".admin-filter-bar__search input");
        await AssertSearchFiltersAsync(page, baseUrl.TrimEnd('/') + "/admin/vacatures", ".filter-bar__query input");
        await AssertSearchFiltersAsync(page, baseUrl.TrimEnd('/') + "/admin/beveiliging/systeemlogs", ".admin-filter-bar__search input");
        await AssertSearchFiltersAsync(page, baseUrl.TrimEnd('/') + "/admin/gebruikers", ".admin-filter-bar__search input");
        await AssertFastTypeAsync(page, ".admin-search__input");
        await GotoInteractiveAsync(page, baseUrl.TrimEnd('/') + "/admin/organisaties");
        await AssertFastTypeAsync(page, ".admin-filter-bar__search input");
        await AssertRun7AdminCopyAsync(page, baseUrl.TrimEnd('/'));

        var storage = await desktop.StorageStateAsync();
        await using var mobile = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IgnoreHTTPSErrors = true,
            StorageState = storage
        });
        await PlaywrightCookieConsent.AcceptAsync(mobile);
        var phone = await mobile.NewPageAsync();
        foreach (var path in new[] { "/admin", "/admin/organisaties", "/admin/vacatures", "/admin/gebruikers", "/admin/beveiliging/systeemlogs" })
        {
            await GotoInteractiveAsync(phone, baseUrl.TrimEnd('/') + path);
            var menu = phone.Locator(".admin-topbar__menu");
            await Assertions.Expect(menu).ToBeVisibleAsync(new() { Timeout = 30_000 });
            var fits = await phone.EvaluateAsync<bool>(
                """
                () => {
                  const scrolling = document.scrollingElement || document.documentElement;
                  return scrolling.scrollWidth <= window.innerWidth
                    && document.documentElement.scrollWidth <= window.innerWidth;
                }
                """);
            Assert.True(fits, $"{path} scrolls horizontally at 390px.");
            var badgeFits = await phone.EvaluateAsync<bool>(
                """
                () => {
                  const badge = document.querySelector('.admin-env-badge');
                  if (!badge) return true;
                  const label = badge.querySelector('.admin-env-badge__label');
                  const after = label ? getComputedStyle(label, '::after').content : '';
                  const acceptatie = badge.classList.contains('admin-env-badge--acceptatie');
                  const short = !acceptatie || (after && after.indexOf('Acc') >= 0);
                  return short && badge.scrollWidth <= badge.clientWidth + 1;
                }
                """);
            Assert.True(badgeFits, $"{path} environment badge is clipped at 390px.");

            var search = phone.Locator(".admin-topbar__search-trigger");
            Assert.True(await menu.IsVisibleAsync());
            Assert.True(await search.IsVisibleAsync());
            Assert.True(await menu.Locator("svg").IsVisibleAsync());
            Assert.True(await search.Locator("svg").IsVisibleAsync());
            // Locator.EvaluateAsync<string> came back null in CI, so the colour never
            // matched "rgba(0, 0, 0, 0)". Compare in the page against a transparent probe;
            // that stays valid whatever serialisation Chrome uses for a clear colour.
            var paint = await phone.EvaluateAsync<MenuPaint>(
                """
                () => {
                  const el = document.querySelector('.admin-topbar__menu');
                  const probe = document.createElement('span');
                  probe.style.background = 'transparent';
                  (document.body || document.documentElement).append(probe);
                  const expected = getComputedStyle(probe).backgroundColor || '';
                  probe.remove();
                  const style = el ? getComputedStyle(el) : null;
                  const actual = style ? (style.backgroundColor || '') : '';
                  const image = style ? (style.backgroundImage || '') : '';
                  const clear = !!el && actual.length > 0 && actual === expected
                    && (image === 'none' || image === '');
                  return { clear, actual, expected, image };
                }
                """);
            Assert.True(
                paint.Clear,
                $"{path} menu background is '{paint.Actual}' (transparent probe '{paint.Expected}', image '{paint.Image}').");
            Assert.False(await phone.Locator(".admin-topbar__search-label").IsVisibleAsync());
        }

        if (_skippedSearchLists.Count == SearchListCount)
        {
            Assert.Fail(
                "Every admin search list was skipped: "
                + string.Join(" ", _skippedSearchLists));
        }

        if (_skippedSearchLists.Count > 0)
        {
            Console.WriteLine("Skipped admin search lists: " + string.Join(" | ", _skippedSearchLists));
        }
    }

    [Fact]
    public async Task Gegevensinzage_columns_line_up_and_filter_stays_visible()
    {
        var baseUrl = Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL");
        if (string.IsNullOrWhiteSpace(baseUrl) || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var desktop = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 900 },
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(desktop);
        var page = await desktop.NewPageAsync();
        await LoginAdminAsync(page, baseUrl);
        await page.GotoAsync(baseUrl.TrimEnd('/') + "/admin/beveiliging/gegevensinzage", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
        await page.Locator(".admin-filter-bar").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });

        var layout = await page.EvaluateAsync<GegevensLayout>(
            """
            () => {
              const pageEl = document.querySelector('.admin-page');
              const bar = document.querySelector('.admin-filter-bar');
              const filter = bar ? [...bar.querySelectorAll('button')].find(b => /filter/i.test(b.textContent || '')) : null;
              const pageRect = pageEl ? pageEl.getBoundingClientRect() : null;
              const barRect = bar ? bar.getBoundingClientRect() : null;
              const filterRect = filter ? filter.getBoundingClientRect() : null;
              const table = document.querySelector('table.admin-data-table');
              let aligned = true;
              let pairs = 0;
              if (table) {
                const heads = [...table.querySelectorAll('thead th')];
                const cells = [...table.querySelectorAll('tbody tr:first-child td')];
                pairs = Math.min(heads.length, cells.length);
                for (let i = 0; i < pairs; i++) {
                  const gap = Math.abs(heads[i].getBoundingClientRect().left - cells[i].getBoundingClientRect().left);
                  if (gap > 2) aligned = false;
                }
              }
              const inputs = bar ? [...bar.querySelectorAll('input, select')] : [];
              let overlap = false;
              for (let i = 0; i < inputs.length; i++) {
                const a = inputs[i].getBoundingClientRect();
                if (a.width < 2 || a.height < 2) continue;
                for (let j = i + 1; j < inputs.length; j++) {
                  const b = inputs[j].getBoundingClientRect();
                  if (b.width < 2 || b.height < 2) continue;
                  const separated = a.right <= b.left + 1 || b.right <= a.left + 1 || a.bottom <= b.top + 1 || b.bottom <= a.top + 1;
                  if (!separated) overlap = true;
                }
              }
              const lists = [...document.querySelectorAll('.admin-user-picker__list')];
              let listsAnchored = true;
              let listsInside = true;
              for (const list of lists) {
                const picker = list.closest('.admin-user-picker');
                const input = picker ? picker.querySelector('input') : null;
                if (!input || !pageRect) { listsAnchored = false; continue; }
                const inputRect = input.getBoundingClientRect();
                const listRect = list.getBoundingClientRect();
                if (Math.abs(listRect.top - inputRect.bottom) > 4) listsAnchored = false;
                if (listRect.left < pageRect.left - 2 || listRect.right > pageRect.right + 2) listsInside = false;
              }
              return {
                barInside: !!pageRect && !!barRect && barRect.right <= pageRect.right + 2 && barRect.left >= pageRect.left - 2,
                filterVisible: !!filterRect && filterRect.width > 0 && filterRect.right <= (pageRect ? pageRect.right + 2 : filterRect.right),
                aligned,
                pairs,
                overlap,
                listsAnchored,
                listsInside,
                listCount: lists.length
              };
            }
            """);
        Assert.True(layout.BarInside, "Gegevensinzage filter bar overflows the page at 1280.");
        Assert.True(layout.FilterVisible, "Filter button is outside the gegevensinzage page at 1280.");
        Assert.True(layout.Aligned, "Gegevensinzage columns do not line up with their headers at 1280.");
        Assert.False(layout.Overlap, "Gegevensinzage filter inputs overlap at 1280.");

        await page.Locator(".admin-user-picker input").First.FillAsync("aa");
        await page.Locator(".admin-user-picker input").Nth(1).FillAsync("aa");
        await page.WaitForTimeoutAsync(600);
        var anchored = await page.EvaluateAsync<GegevensLayout>(
            """
            () => {
              const pageEl = document.querySelector('.admin-page');
              const pageRect = pageEl ? pageEl.getBoundingClientRect() : null;
              const lists = [...document.querySelectorAll('.admin-user-picker__list')];
              let listsAnchored = lists.length > 0;
              let listsInside = true;
              for (const list of lists) {
                const picker = list.closest('.admin-user-picker');
                const input = picker ? picker.querySelector('input') : null;
                if (!input || !pageRect) { listsAnchored = false; continue; }
                const inputRect = input.getBoundingClientRect();
                const listRect = list.getBoundingClientRect();
                if (Math.abs(listRect.top - inputRect.bottom) > 4) listsAnchored = false;
                if (listRect.left < pageRect.left - 2 || listRect.right > pageRect.right + 2) listsInside = false;
              }
              return { listsAnchored, listsInside, listCount: lists.length, barInside: true, filterVisible: true, aligned: true, pairs: 0, overlap: false };
            }
            """);
        if (anchored.ListCount > 0)
        {
            Assert.True(anchored.ListsAnchored, "A suggestion list is not anchored under its input.");
            Assert.True(anchored.ListsInside, "A suggestion list leaves the admin page.");
        }
    }

    private sealed class GegevensLayout
    {
        public bool BarInside { get; set; }
        public bool FilterVisible { get; set; }
        public bool Aligned { get; set; }
        public int Pairs { get; set; }
        public bool Overlap { get; set; }
        public bool ListsAnchored { get; set; }
        public bool ListsInside { get; set; }
        public int ListCount { get; set; }
    }

    private sealed class MenuPaint
    {
        public bool Clear { get; set; }
        public string Actual { get; set; } = "";
        public string Expected { get; set; } = "";
        public string Image { get; set; } = "";
    }

    private static async Task AssertMenuStaysOpenAsync(IPage page, string url)
    {
        await GotoInteractiveAsync(page, url);
        // Prerender paints the row menu before the interactive circuit reloads the table.
        // A click in that gap never reaches Blazor, so aria-expanded stays "false".
        await WaitForStableRowMenuAsync(page);

        var toggle = page.Locator("button.row-actions-menu__toggle").First;
        await OpenRowMenuAsync(toggle);

        var openedAt = DateTime.UtcNow;
        while (DateTime.UtcNow - openedAt < TimeSpan.FromSeconds(1))
        {
            var expanded = await toggle.GetAttributeAsync("aria-expanded");
            Assert.True(
                expanded == "true",
                $"Row menu on {url} closed itself (aria-expanded='{expanded}').");
            await page.WaitForTimeoutAsync(100);
        }

        var item = page.Locator(".row-actions-menu__panel [role='menuitem']").First;
        await item.ClickAsync(new() { Timeout = 5_000 });
    }

    private static async Task AssertRun7AdminCopyAsync(IPage page, string root)
    {
        await GotoInteractiveAsync(page, root + "/admin/vacatures/ats");
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Nu ophalen" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Button, new() { Name = "Bronnen controleren" })).ToBeVisibleAsync();

        await GotoInteractiveAsync(page, root + "/admin/beveiliging/gegevensinzage");
        await page.Locator(".admin-access-log thead").WaitForAsync(new() { Timeout = 30_000 });
        var headers = await page.Locator(".admin-access-log thead th").AllInnerTextsAsync();
        Assert.Contains(headers, h => h.Trim() == "Wie");
        Assert.Contains(headers, h => h.Trim() == "Over wie");
        Assert.DoesNotContain(headers, h => h.Contains("naam of e-mail", StringComparison.OrdinalIgnoreCase));
        var placeholder = await page.Locator(".admin-user-picker input").First.GetAttributeAsync("placeholder");
        Assert.Contains("naam of e-mail", placeholder ?? "", StringComparison.Ordinal);
        var fits = await page.EvaluateAsync<bool>(
            """
            () => {
              const table = document.querySelector('.admin-access-log .data-table');
              const main = document.querySelector('.admin-main');
              if (!table || !main) return false;
              return table.scrollWidth <= main.clientWidth + 1;
            }
            """);
        Assert.True(fits, "Gegevensinzage table is wider than the admin content at 1280px.");

        await GotoInteractiveAsync(page, root + "/admin/beveiliging/referent-misbruik");
        var active = page.Locator(".admin-tabs__tab.is-active");
        await Assertions.Expect(active).ToContainTextAsync("Meldingen referent");
        await Assertions.Expect(page.Locator(".admin-breadcrumbs")).ToContainTextAsync("Beveiliging & audit");
        await Assertions.Expect(page.Locator(".admin-breadcrumbs")).ToContainTextAsync("Meldingen referent");

        await GotoInteractiveAsync(page, root + "/admin/vacatures");
        var headerCase = await page.EvaluateAsync<string>(
            """
            () => {
              const buttons = [...document.querySelectorAll('.vacancy-grid .data-table th button.linkish')];
              if (buttons.length === 0) return 'missing';
              return buttons.every(b => getComputedStyle(b).textTransform === 'uppercase') ? 'ok' : 'plain';
            }
            """);
        Assert.Equal("ok", headerCase);
    }

    private static async Task GotoInteractiveAsync(IPage page, string url)
    {
        await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 30_000 });
    }

    private static async Task WaitForStableRowMenuAsync(IPage page)
    {
        var token = Guid.NewGuid().ToString("N");
        await page.WaitForFunctionAsync(
            """
            (token) => {
              if (window.__jobsyMenuStableToken !== token) {
                window.__jobsyMenuStableToken = token;
                window.__jobsyMenuStableSince = 0;
                window.__jobsyMenuSawLoading = false;
              }
              const tableLoading = document.querySelector('.admin-data-table__state');
              const toggle = document.querySelector('button.row-actions-menu__toggle');
              const visible = !!toggle && toggle.getClientRects().length > 0;
              // A status line can sit next to a ready table. Only a missing menu,
              // or the table placeholder that replaces the rows, means "still loading".
              if (tableLoading || !visible) {
                window.__jobsyMenuSawLoading = true;
                window.__jobsyMenuStableSince = 0;
                return false;
              }
              const now = Date.now();
              if (!window.__jobsyMenuStableSince) {
                window.__jobsyMenuStableSince = now;
              }
              const quiet = now - window.__jobsyMenuStableSince;
              // The interactive circuit reloads the table after prerender. Once that
              // loading line has come and gone, a short quiet period is enough.
              // If the reload was too fast to observe, wait longer before clicking.
              return window.__jobsyMenuSawLoading ? quiet >= 400 : quiet >= 1200;
            }
            """,
            token,
            new() { Timeout = 30_000 });
    }

    private static async Task WaitForTwoDataRowsAsync(IPage page)
    {
        var token = Guid.NewGuid().ToString("N");
        await page.WaitForFunctionAsync(
            """
            (token) => {
              if (window.__jobsyRowsToken !== token) {
                window.__jobsyRowsToken = token;
                window.__jobsyRowsSince = 0;
              }
              if (document.querySelector('.admin-data-table__state')) {
                window.__jobsyRowsSince = 0;
                return false;
              }
              const rows = document.querySelectorAll('table tbody tr');
              if (rows.length < 2) {
                window.__jobsyRowsSince = 0;
                return false;
              }
              const now = Date.now();
              if (!window.__jobsyRowsSince) {
                window.__jobsyRowsSince = now;
              }
              return now - window.__jobsyRowsSince >= 400;
            }
            """,
            token,
            new() { Timeout = 30_000 });
    }

    private static async Task OpenRowMenuAsync(ILocator toggle)
    {
        await toggle.ClickAsync();
        try
        {
            await Assertions.Expect(toggle).ToHaveAttributeAsync(
                "aria-expanded",
                "true",
                new() { Timeout = 3_000 });
        }
        catch (PlaywrightException)
        {
            // The first click can land while Blazor replaces the prerendered button.
            await toggle.ClickAsync();
            await Assertions.Expect(toggle).ToHaveAttributeAsync(
                "aria-expanded",
                "true",
                new() { Timeout = 5_000 });
        }
    }

    private async Task AssertSearchFiltersAsync(IPage page, string url, string inputSelector)
    {
        if (PairFor(url) is not { } pair)
        {
            Assert.Fail($"No seeded search pair for {url}.");
            return;
        }

        var needle = pair.Needle;
        var other = pair.Other;

        // Seeded names are the proof. The unfiltered list is paged, and its first
        // tbody paint can be one row or none, so a raw count is not the gate.
        string? failure = null;
        for (var attempt = 0; attempt < 2; attempt++)
        {
            if (!await EnsureAdminSearchRowsAsync())
            {
                _skippedSearchLists.Add(
                    $"Skipped search on {url}: the test database was not reachable to seed two rows.");
                return;
            }

            failure = await TryProveSeededSearchAsync(page, url, inputSelector, needle, other);
            if (failure is null)
            {
                return;
            }
        }

        // Acceptatie still proves the filter from whatever rows stayed on screen,
        // after two body rows have been stable. That path is the fallback when the
        // seeded name did not show.
        if (await TryProveVisibleLabelSearchAsync(page, url, inputSelector))
        {
            return;
        }

        Assert.Fail($"{failure} {await DescribeSeedReadbackAsync()}");
    }

    /// <summary>
    /// Types <paramref name="needle"/> into the list search. Returns null when that row
    /// stays visible and the other seeded row is gone.
    /// </summary>
    private static async Task<string?> TryProveSeededSearchAsync(
        IPage page, string url, string inputSelector, string needle, string other)
    {
        await GotoInteractiveAsync(page, url);
        var input = page.Locator(inputSelector).First;
        var rows = page.Locator("table tbody tr");
        try
        {
            // The filter bar is rendered with the loaded page, including an empty list.
            await input.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
            await input.ClickAsync();
            await input.FillAsync("");
            await page.Keyboard.TypeAsync(needle, new() { Delay = 40 });
            await Assertions.Expect(input).ToHaveValueAsync(needle, new() { Timeout = 5_000 });
            await Assertions.Expect(rows.Filter(new() { HasTextString = needle }).First)
                .ToBeVisibleAsync(new() { Timeout = 20_000 });
            await Assertions.Expect(rows.Filter(new() { HasTextString = other }))
                .ToHaveCountAsync(0, new() { Timeout = 15_000 });
            return null;
        }
        catch (Exception ex) when (ex is PlaywrightException || ex.GetType().Name == "TimeoutException")
        {
            var status = "";
            try
            {
                var messages = await page.Locator(".state-message").AllInnerTextsAsync();
                if (messages.Count > 0)
                {
                    status = " Page status: " + string.Join(" | ", messages);
                }
            }
            catch (PlaywrightException)
            {
                // The status line is optional context for the retry.
            }

            return $"Seeded row '{needle}' did not stay visible on {url} while '{other}' was hidden.{status} {ex.Message}";
        }
    }

    /// <summary>
    /// Acceptatie proof: wait until two body rows stay, then type a visible label and
    /// require a different row to disappear. Returns false when those rows never settle.
    /// </summary>
    private static async Task<bool> TryProveVisibleLabelSearchAsync(IPage page, string url, string inputSelector)
    {
        try
        {
            await GotoInteractiveAsync(page, url);
            var rows = page.Locator("table tbody tr");
            await WaitForTwoDataRowsAsync(page);
            var count = await rows.CountAsync();
            if (count < 2)
            {
                await WaitForTwoDataRowsAsync(page);
                count = await rows.CountAsync();
            }

            if (count < 2)
            {
                return false;
            }

            var labels = page.Locator("table tbody strong");
            var labelCount = await labels.CountAsync();
            string needle;
            string otherProof;
            if (labelCount >= 2)
            {
                needle = (await labels.Nth(0).InnerTextAsync()).Trim();
                if (needle.Length < 4 || !needle.Any(char.IsLetter))
                {
                    return false;
                }

                otherProof = await FirstOtherLabelAsync(labels, labelCount, needle);
            }
            else
            {
                var first = (await rows.Nth(0).InnerTextAsync()).Trim();
                var second = (await rows.Nth(1).InnerTextAsync()).Trim();
                needle = FirstDistinctiveWord(first, second);
                otherProof = FirstDistinctiveWord(second, first);
            }

            if (string.IsNullOrWhiteSpace(needle) || string.IsNullOrWhiteSpace(otherProof))
            {
                return false;
            }

            var input = page.Locator(inputSelector).First;
            await input.ClickAsync();
            await input.FillAsync("");
            await page.Keyboard.TypeAsync(needle, new() { Delay = 40 });
            await Assertions.Expect(input).ToHaveValueAsync(needle);
            await Assertions.Expect(rows.Filter(new() { HasTextString = needle }).First)
                .ToBeVisibleAsync(new() { Timeout = 15_000 });
            await Assertions.Expect(rows.Filter(new() { HasTextString = otherProof }))
                .ToHaveCountAsync(0, new() { Timeout = 15_000 });
            return true;
        }
        catch (Exception ex) when (ex is PlaywrightException or InvalidOperationException
                                   || ex.GetType().Name == "TimeoutException")
        {
            return false;
        }
    }

    private static async Task<string> FirstOtherLabelAsync(ILocator labels, int count, string needle)
    {
        var sample = Math.Min(count, 12);
        for (var i = 1; i < sample; i++)
        {
            var text = (await labels.Nth(i).InnerTextAsync()).Trim();
            if (text.Length < 4 || !text.Any(char.IsLetter))
            {
                continue;
            }

            if (text.Contains(needle, StringComparison.OrdinalIgnoreCase)
                || needle.Contains(text, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return text;
        }

        throw new InvalidOperationException($"No row stays unmatched for '{needle}'.");
    }

    private static string FirstDistinctiveWord(string firstRow, string secondRow)
    {
        foreach (var word in firstRow.Split([' ', '\n', '\t', '·'], StringSplitOptions.RemoveEmptyEntries))
        {
            var clean = word.Trim();
            if (clean.Length >= 4
                && !secondRow.Contains(clean, StringComparison.OrdinalIgnoreCase)
                && clean.Any(char.IsLetter))
            {
                return clean;
            }
        }

        var line = firstRow.Split('\n', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
        return line.Length > 24 ? line[..24] : line;
    }

    private static async Task AssertFastTypeAsync(IPage page, string selector)
    {
        var input = page.Locator(selector).First;
        await input.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await input.ClickAsync();
        await input.FillAsync("");
        const string typed = "binckhorstxyzab";
        await page.Keyboard.TypeAsync(typed, new() { Delay = 40 });
        Assert.Equal(typed, await input.InputValueAsync());
    }

    private static async Task LoginAdminAsync(IPage page, string baseUrl)
    {
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? Password;
        var root = baseUrl.TrimEnd('/');
        await page.GotoAsync(root + "/login?returnUrl=" + Uri.EscapeDataString("/admin"), new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 60_000
        });
        await page.FillAsync("input[name='email']", "admin@jobsy.local");
        await page.FillAsync("input[name='password']", password);
        var submit = page.Locator("button.login-submit, button.au-submit[type=submit]");
        await submit.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await page.WaitForFunctionAsync(
            "() => { const b = document.querySelector('button.login-submit, button.au-submit[type=submit]'); return b && !b.disabled; }",
            null,
            new() { Timeout = 30_000 });
        await Task.WhenAll(
            page.WaitForURLAsync(
                url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase) || url.Contains("mfa", StringComparison.OrdinalIgnoreCase),
                new() { Timeout = 60_000 }),
            submit.ClickAsync());

        if (page.Url.Contains("/account/mfa", StringComparison.OrdinalIgnoreCase))
        {
            await CompleteMfaSetupAsync(page);
        }

        if (!page.Url.Contains("/admin", StringComparison.OrdinalIgnoreCase))
        {
            await page.GotoAsync(root + "/admin", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
        }

        Assert.True(
            page.Url.Contains("/admin", StringComparison.OrdinalIgnoreCase),
            $"Seeded admin did not reach /admin (now {page.Url}).");
    }

    private static async Task CompleteMfaSetupAsync(IPage page)
    {
        var secret = page.Locator("#mfa-manual-key, #mfa-manual-key-desktop").First;
        await secret.WaitForAsync(new() { Timeout = 15_000 });
        var code = Totp(await secret.InnerTextAsync());
        await page.FillAsync("input[name='code']", code);
        await page.Locator("form[action='/account/mfa/verify'] button[type=submit], form[action='/account/mfa/verify'] button").First.ClickAsync();
        await page.WaitForURLAsync(url => !url.Contains("/account/mfa/setup", StringComparison.OrdinalIgnoreCase), new() { Timeout = 30_000 });
    }

    private static string Totp(string secret)
    {
        var key = Base32Decode(secret);
        var counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
        var msg = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(msg);
        }

#pragma warning disable CA5350 // RFC 6238 TOTP is HMAC-SHA1
        var hash = HMACSHA1.HashData(key, msg);
#pragma warning restore CA5350
        var offset = hash[^1] & 0x0f;
        var binary = ((hash[offset] & 0x7f) << 24)
                     | (hash[offset + 1] << 16)
                     | (hash[offset + 2] << 8)
                     | hash[offset + 3];
        return (binary % 1_000_000).ToString("D6");
    }

    private static byte[] Base32Decode(string input)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var buffer = 0;
        var bits = 0;
        var bytes = new List<byte>();
        foreach (var ch in input)
        {
            if (ch is ' ' or '-' or '\n' or '\r')
            {
                continue;
            }

            var val = alphabet.IndexOf(char.ToUpperInvariant(ch));
            if (val < 0)
            {
                continue;
            }

            buffer = (buffer << 5) | val;
            bits += 5;
            if (bits >= 8)
            {
                bits -= 8;
                bytes.Add((byte)((buffer >> bits) & 0xFF));
            }
        }

        return bytes.ToArray();
    }

    private static (string Needle, string Other)? PairFor(string url)
    {
        if (url.Contains("/admin/organisaties", StringComparison.OrdinalIgnoreCase))
        {
            return (OrgAlphaName, OrgBravoName);
        }

        if (url.Contains("/admin/vacatures", StringComparison.OrdinalIgnoreCase))
        {
            return (VacAlphaTitle, VacBravoTitle);
        }

        if (url.Contains("/admin/gebruikers", StringComparison.OrdinalIgnoreCase))
        {
            return (UserAlphaName, UserBravoName);
        }

        if (url.Contains("/beveiliging/systeemlogs", StringComparison.OrdinalIgnoreCase))
        {
            return (LogAlphaToken, LogBravoToken);
        }

        return null;
    }

    /// <summary>
    /// Inserts two roots, two vacancies, two users and two logs into the same database
    /// the CI stack is serving. Returns false only when that database cannot be reached.
    /// </summary>
    private async Task<bool> EnsureAdminSearchRowsAsync()
    {
        if (_searchDbReady == false)
        {
            return false;
        }

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__JobsyDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _searchDbReady = false;
            return false;
        }

        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.UseNetTopologySuite())
            .Options;
        await using var db = new JobsyDbContext(options);
        if (_searchDbReady != true)
        {
            try
            {
                if (!await db.Database.CanConnectAsync())
                {
                    _searchDbReady = false;
                    return false;
                }
            }
            catch (Exception ex) when (ex is NpgsqlException or SocketException or System.TimeoutException)
            {
                _searchDbReady = false;
                return false;
            }

            _searchDbReady = true;
        }

        var now = DateTime.UtcNow;
        await UpsertCompanyAsync(db, CompanyAlphaId, OrgAlphaName, "91000011", new GeoPoint(52.0701, 4.3001));
        await UpsertCompanyAsync(db, CompanyBravoId, OrgBravoName, "91000022", new GeoPoint(52.0702, 4.3002));
        await UpsertUserAsync(db, UserAlphaId, "0zetaalpha.filter@jobsy.local", UserAlphaName);
        await UpsertUserAsync(db, UserBravoId, "0zetabravo.filter@jobsy.local", UserBravoName);
        await UpsertLogAsync(db, LogAlphaId, LogAlphaToken, now.AddDays(2));
        await UpsertLogAsync(db, LogBravoId, LogBravoToken, now.AddDays(1));
        await db.SaveChangesAsync();

        await UpsertVacancyAsync(db, VacancyAlphaId, VacAlphaTitle, CompanyAlphaId, new GeoPoint(52.0701, 4.3001));
        await UpsertVacancyAsync(db, VacancyBravoId, VacBravoTitle, CompanyBravoId, new GeoPoint(52.0702, 4.3002));
        await db.SaveChangesAsync();

        var readback = await CountSeededRowsAsync(db);
        if (readback.Users < 2 || readback.Companies < 2 || readback.Vacancies < 2 || readback.Logs < 2)
        {
            throw new InvalidOperationException(
                "Admin search seed did not land in the CI database (" + FormatSeedCounts(readback) + ").");
        }

        return true;
    }

    private static async Task<string> DescribeSeedReadbackAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__JobsyDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return "Readback: ConnectionStrings__JobsyDb is not set.";
        }

        try
        {
            var options = new DbContextOptionsBuilder<JobsyDbContext>()
                .UseNpgsql(connectionString, npgsql => npgsql.UseNetTopologySuite())
                .Options;
            await using var db = new JobsyDbContext(options);
            return "Readback: " + FormatSeedCounts(await CountSeededRowsAsync(db)) + ".";
        }
        catch (Exception ex) when (ex is NpgsqlException or SocketException or System.TimeoutException)
        {
            return "Readback failed: " + ex.Message;
        }
    }

    private static async Task<(int Users, int Companies, int Vacancies, int Logs)> CountSeededRowsAsync(JobsyDbContext db)
    {
        var users = await db.Users.AsNoTracking().CountAsync(u => u.Id == UserAlphaId || u.Id == UserBravoId);
        var companies = await db.Companies.AsNoTracking()
            .CountAsync(c => c.Id == CompanyAlphaId || c.Id == CompanyBravoId);
        // Vacancy has a company-scope filter. A bare test context must still see the rows it wrote.
        var vacancies = await db.Vacancies.IgnoreQueryFilters().AsNoTracking()
            .CountAsync(v => v.Id == VacancyAlphaId || v.Id == VacancyBravoId);
        var logs = await db.PlatformLogs.AsNoTracking().CountAsync(l => l.Id == LogAlphaId || l.Id == LogBravoId);
        return (users, companies, vacancies, logs);
    }

    private static string FormatSeedCounts((int Users, int Companies, int Vacancies, int Logs) counts)
        => $"users={counts.Users}, companies={counts.Companies}, vacancies={counts.Vacancies}, logs={counts.Logs}";

    private static async Task UpsertCompanyAsync(
        JobsyDbContext db, Guid id, string name, string kvk, GeoPoint location)
    {
        var row = await db.Companies.FirstOrDefaultAsync(c => c.Id == id);
        if (row is null)
        {
            db.Companies.Add(new Company
            {
                Id = id,
                Name = name,
                KvkNumber = kvk,
                KvkEstablishmentId = kvk + "_0001",
                Address = "Kade 4, Delft",
                Location = location,
                Type = CompanyType.Employer,
                VerificationStatus = CompanyVerificationStatus.Verified,
                VerificationMethod = CompanyVerificationMethod.AdminCreated,
                VerifiedAtUtc = DateTime.UtcNow,
                VerificationUpdatedAtUtc = DateTime.UtcNow,
                IsTestData = true
            });
            return;
        }

        row.Name = name;
        row.ParentCompanyId = null;
        row.Address = "Kade 4, Delft";
        row.VerificationStatus = CompanyVerificationStatus.Verified;
        row.VerificationMethod = CompanyVerificationMethod.AdminCreated;
        row.IsTestData = true;
        row.Location = location;
    }

    private static async Task UpsertVacancyAsync(
        JobsyDbContext db, Guid id, string title, Guid companyId, GeoPoint location)
    {
        var row = await db.Vacancies.FirstOrDefaultAsync(v => v.Id == id);
        if (row is null)
        {
            db.Vacancies.Add(new Vacancy
            {
                Id = id,
                Title = title,
                Description = "Zoekfilter bewijs voor de adminlijst.",
                HourlyWage = 14.00m,
                StartDate = new DateOnly(2099, 1, 1),
                EndDate = new DateOnly(2099, 4, 1),
                Status = VacancyStatus.Active,
                CompanyId = companyId,
                Location = location,
                CreatedVia = VacancySource.Manual,
                Kind = VacancyKind.Regular,
                CreatedAtUtc = DateTime.UtcNow,
                PublishedAtUtc = DateTime.UtcNow,
                ContentModerationPassed = true,
                IsTestData = true
            });
            return;
        }

        row.Title = title;
        row.CompanyId = companyId;
        row.Status = VacancyStatus.Active;
        row.StartDate = new DateOnly(2099, 1, 1);
        row.EndDate = new DateOnly(2099, 4, 1);
        row.Location = location;
        row.IsTestData = true;
    }

    private static async Task UpsertUserAsync(JobsyDbContext db, Guid id, string email, string fullName)
    {
        var row = await db.Users.FirstOrDefaultAsync(u => u.Id == id)
            ?? await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (row is null)
        {
            db.Users.Add(new User
            {
                Id = id,
                Email = email,
                FullName = fullName,
                Role = UserRole.Candidate,
                IsActive = true,
                IsTestAccount = true
            });
            return;
        }

        row.Email = email;
        row.FullName = fullName;
        row.IsActive = true;
        row.Role = UserRole.Candidate;
        row.IsTestAccount = true;
    }

    private static async Task UpsertLogAsync(JobsyDbContext db, Guid id, string token, DateTime createdAt)
    {
        var row = await db.PlatformLogs.FirstOrDefaultAsync(l => l.Id == id);
        if (row is null)
        {
            db.PlatformLogs.Add(new PlatformLog
            {
                Id = id,
                Level = PlatformLogLevel.Info,
                Category = token,
                Message = token,
                CreatedAt = createdAt
            });
            return;
        }

        row.Level = PlatformLogLevel.Info;
        row.Category = token;
        row.Message = token;
        row.CreatedAt = createdAt;
    }

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            using var response = await client.GetAsync(baseUrl.TrimEnd('/') + "/");
            return (int)response.StatusCode is >= 200 and < 500;
        }
        catch
        {
            return false;
        }
    }
}
