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
        await page.GotoAsync(baseUrl.TrimEnd('/') + "/admin/organisaties", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
        await AssertFastTypeAsync(page, ".admin-filter-bar__search input");

        await using var mobile = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(mobile);
        var phone = await mobile.NewPageAsync();
        foreach (var path in new[] { "/admin", "/admin/organisaties", "/admin/vacatures", "/admin/gebruikers", "/admin/beveiliging/systeemlogs" })
        {
            await phone.GotoAsync(baseUrl.TrimEnd('/') + path, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
            await phone.WaitForSelectorAsync(".admin-topbar__menu", new() { Timeout = 30_000 });
            var fits = await phone.EvaluateAsync<bool>(
                """
                () => {
                  const scrolling = document.scrollingElement || document.documentElement;
                  return scrolling.scrollWidth <= window.innerWidth
                    && document.documentElement.scrollWidth <= window.innerWidth;
                }
                """);
            Assert.True(fits, $"{path} scrolls horizontally at 390px.");

            var menu = phone.Locator(".admin-topbar__menu");
            var search = phone.Locator(".admin-topbar__search-trigger");
            Assert.True(await menu.IsVisibleAsync());
            Assert.True(await search.IsVisibleAsync());
            Assert.True(await menu.Locator("svg").IsVisibleAsync());
            Assert.True(await search.Locator("svg").IsVisibleAsync());
            var background = await menu.EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor");
            Assert.True(
                background is "rgba(0, 0, 0, 0)" or "transparent",
                $"Menu button background is {background}.");
            Assert.False(await phone.Locator(".admin-topbar__search-label").IsVisibleAsync());
        }
    }

    private static async Task AssertMenuStaysOpenAsync(IPage page, string url)
    {
        await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
        var toggle = page.Locator("button.row-actions-menu__toggle").First;
        await toggle.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await toggle.ClickAsync();
        await page.WaitForTimeoutAsync(1000);
        Assert.Equal("true", await toggle.GetAttributeAsync("aria-expanded"));
        var item = page.Locator(".row-actions-menu__panel [role='menuitem']").First;
        await item.ClickAsync(new() { Timeout = 5_000 });
    }

    private static async Task AssertSearchFiltersAsync(IPage page, string url, string inputSelector)
    {
        await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
        var rows = page.Locator("table tbody tr");
        await rows.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        var count = await rows.CountAsync();
        Assert.True(count >= 1, $"No rows on {url} to search.");
        var first = (await rows.Nth(0).InnerTextAsync()).Trim();
        var needle = FirstDistinctiveWord(first, count > 1 ? await rows.Nth(1).InnerTextAsync() : "");
        var input = page.Locator(inputSelector).First;
        await input.ClickAsync();
        await input.FillAsync("");
        await page.Keyboard.TypeAsync(needle, new() { Delay = 40 });
        Assert.Equal(needle, await input.InputValueAsync());
        await page.WaitForTimeoutAsync(900);
        var body = await page.Locator("table tbody").InnerTextAsync();
        Assert.Contains(needle, body, StringComparison.OrdinalIgnoreCase);
        if (count > 1)
        {
            var second = await rows.Nth(1).InnerTextAsync();
            if (!second.Contains(needle, StringComparison.OrdinalIgnoreCase))
            {
                Assert.DoesNotContain(second.Split('\n', StringSplitOptions.RemoveEmptyEntries)[0].Trim(), body, StringComparison.Ordinal);
            }
        }
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
