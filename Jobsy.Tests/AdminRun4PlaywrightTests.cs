using System.Security.Cryptography;
using Microsoft.Playwright;

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

        await AssertMenuStaysOpenAsync(page, baseUrl.TrimEnd('/') + "/admin/gebruikers");
        await AssertMenuStaysOpenAsync(page, baseUrl.TrimEnd('/') + "/admin/vacatures");
        await AssertSearchFiltersAsync(page, baseUrl.TrimEnd('/') + "/admin/organisaties", ".admin-filter-bar__search input");
        await AssertSearchFiltersAsync(page, baseUrl.TrimEnd('/') + "/admin/vacatures", ".filter-bar__query input");
        await AssertSearchFiltersAsync(page, baseUrl.TrimEnd('/') + "/admin/beveiliging/systeemlogs", ".admin-filter-bar__search input");
        await AssertSearchFiltersAsync(page, baseUrl.TrimEnd('/') + "/admin/gebruikers", ".admin-filter-bar__search input");
        await AssertFastTypeAsync(page, ".admin-search__input");
        await GotoInteractiveAsync(page, baseUrl.TrimEnd('/') + "/admin/organisaties");
        await AssertFastTypeAsync(page, ".admin-filter-bar__search input");

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
              return {
                barInside: !!pageRect && !!barRect && barRect.right <= pageRect.right + 2 && barRect.left >= pageRect.left - 2,
                filterVisible: !!filterRect && filterRect.width > 0 && filterRect.right <= (pageRect ? pageRect.right + 2 : filterRect.right),
                aligned,
                pairs
              };
            }
            """);
        Assert.True(layout.BarInside, "Gegevensinzage filter bar overflows the page at 1280.");
        Assert.True(layout.FilterVisible, "Filter button is outside the gegevensinzage page at 1280.");
        Assert.True(layout.Aligned, "Gegevensinzage columns do not line up with their headers at 1280.");
    }

    private sealed class GegevensLayout
    {
        public bool BarInside { get; set; }
        public bool FilterVisible { get; set; }
        public bool Aligned { get; set; }
        public int Pairs { get; set; }
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

    private static async Task AssertSearchFiltersAsync(IPage page, string url, string inputSelector)
    {
        await GotoInteractiveAsync(page, url);
        var rows = page.Locator("table.data-table tbody tr");
        // The circuit can paint the first row before the rest of the page arrives.
        try
        {
            await page.WaitForFunctionAsync(
                """
                () => document.querySelectorAll('table.data-table tbody tr').length >= 2
                """,
                null,
                new() { Timeout = 20_000 });
        }
        catch (TimeoutException)
        {
            var count = await rows.CountAsync();
            Assert.True(count >= 2, $"Need two rows on {url} to prove search hides a non-match (saw {count}).");
        }

        // Snapshot the other row before typing. A short token such as "Binckhorst" also
        // matches an address the table does not show, so the query is the full primary
        // label (organisation, vacancy title, or user name) whenever the row has one.
        var labels = page.Locator("table tbody strong");
        var labelCount = await labels.CountAsync();
        string needle;
        string otherProof;
        if (labelCount >= 2)
        {
            needle = (await labels.Nth(0).InnerTextAsync()).Trim();
            Assert.True(needle.Length >= 4 && needle.Any(char.IsLetter), $"Primary label on {url} is '{needle}'.");
            otherProof = await FirstOtherLabelAsync(labels, labelCount, needle);
        }
        else
        {
            var first = (await rows.Nth(0).InnerTextAsync()).Trim();
            var second = (await rows.Nth(1).InnerTextAsync()).Trim();
            needle = FirstDistinctiveWord(first, second);
            otherProof = FirstDistinctiveWord(second, first);
        }

        Assert.False(string.IsNullOrWhiteSpace(needle), $"No search needle on {url}.");
        Assert.False(string.IsNullOrWhiteSpace(otherProof), $"No unmatched row on {url}.");

        var input = page.Locator(inputSelector).First;
        await input.ClickAsync();
        await input.FillAsync("");
        await page.Keyboard.TypeAsync(needle, new() { Delay = 40 });
        await Assertions.Expect(input).ToHaveValueAsync(needle);

        await Assertions.Expect(rows.Filter(new() { HasTextString = needle }).First)
            .ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Assertions.Expect(rows.Filter(new() { HasTextString = otherProof }))
            .ToHaveCountAsync(0, new() { Timeout = 15_000 });
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
