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
    // Single words: the users list masks multi-word names ("Aa Zetaalpha Gebruikerfilter"
    // becomes "Aa G."), so both rows would look identical and the typed query would miss.
    private const string UserAlphaName = "Zetaalphagebruiker";
    private const string UserBravoName = "Zetabravogebruiker";
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

    private async Task AssertSearchFiltersAsync(IPage page, string url, string inputSelector)
    {
        await EnsureAdminSearchRowsAsync();
        await GotoInteractiveAsync(page, url);
        var rows = page.Locator("table tbody tr");
        var count = await CountDataRowsAsync(rows);
        if (count < 2 && await EnsureAdminSearchRowsAsync())
        {
            await GotoInteractiveAsync(page, url);
            count = await CountDataRowsAsync(rows);
        }

        if (count < 2)
        {
            if (_searchDbReady != true)
            {
                _skippedSearchLists.Add(
                    $"Skipped search on {url}: fewer than two rows, and the test database was not reachable to seed them.");
                return;
            }

            Assert.True(count >= 2, $"Need two rows on {url} to prove search hides a non-match.");
        }

        // Prefer the seeded pair. Those labels do not contain each other, so a hit on
        // one row is proof the filter hid the other. Fall back to whatever is visible
        // when the seeded rows are not on this page.
        var labels = page.Locator("table tbody strong");
        var labelCount = await labels.CountAsync();
        string needle;
        string otherProof;
        var seeded = await FindSeededPairAsync(rows, count, url);
        if (seeded is { } pair)
        {
            needle = pair.Needle;
            otherProof = pair.Other;
        }
        else if (labelCount >= 2)
        {
            // Snapshot the other row before typing. A short token such as "Binckhorst" also
            // matches an address the table does not show, so the query is the full primary
            // label (organisation, vacancy title, or user name) whenever the row has one.
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

    private static async Task<int> CountDataRowsAsync(ILocator rows)
    {
        try
        {
            await rows.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        }
        catch (Exception ex) when (ex.GetType().Name == "TimeoutException")
        {
            return 0;
        }

        return await rows.CountAsync();
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

    private static async Task<(string Needle, string Other)?> FindSeededPairAsync(ILocator rows, int count, string url)
    {
        var pair = PairFor(url);
        if (pair is null)
        {
            return null;
        }

        var seenNeedle = false;
        var seenOther = false;
        var sample = Math.Min(count, 50);
        for (var i = 0; i < sample; i++)
        {
            var text = await rows.Nth(i).InnerTextAsync();
            if (text.Contains(pair.Value.Needle, StringComparison.Ordinal))
            {
                seenNeedle = true;
            }

            if (text.Contains(pair.Value.Other, StringComparison.Ordinal))
            {
                seenOther = true;
            }
        }

        return seenNeedle && seenOther ? pair : null;
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
        return true;
    }

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
