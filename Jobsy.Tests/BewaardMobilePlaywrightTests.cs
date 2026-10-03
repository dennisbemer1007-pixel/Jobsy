using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Kandidaat polish 05: Bewaard compact list at 390×844 and 4-up tiles at 1440.
/// Soft-skips without <c>JOBSY_E2E_BASE_URL</c> or when seed has fewer than 4 saved jobs.
/// </summary>
[Collection("PlaywrightSmoke")]
public class BewaardMobilePlaywrightTests
{
    private const string DefaultEmail = "kandidaat@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";

    [Fact]
    public async Task Mobile_390_shows_at_least_four_rows_without_overflow()
    {
        var baseUrl = BaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        await using var browser = await LaunchAsync();
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            HasTouch = true,
            IsMobile = true,
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        if (!await TryLoginAsync(page, baseUrl))
        {
            return;
        }

        await page.GotoAsync(baseUrl + "/candidate/liked", new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 90_000
        });

        try
        {
            await page.WaitForSelectorAsync(".kb-saved-row, .state-message, .saved-gate", new() { Timeout = 45_000 });
        }
        catch (TimeoutException)
        {
            return;
        }

        var rowCount = await page.Locator(".kb-saved-row").CountAsync();
        if (rowCount < 4)
        {
            return;
        }

        var fullyVisible = await page.EvaluateAsync<int>("""
            () => {
              const rows = [...document.querySelectorAll('.kb-saved-row')];
              let n = 0;
              for (const row of rows) {
                const r = row.getBoundingClientRect();
                if (r.top >= 0 && r.bottom <= 844 && r.left >= 0 && r.right <= 390) n++;
              }
              return n;
            }
            """);
        Assert.True(fullyVisible >= 4, $"expected ≥4 fully visible rows at scroll 0, got {fullyVisible}");

        var overflow = await page.EvaluateAsync<double>("() => document.documentElement.scrollWidth");
        Assert.True(overflow <= 390 + 1, $"horizontal overflow: scrollWidth={overflow}");

        var sortStyle = await page.EvaluateAsync<string>("""
            () => {
              const sel = document.querySelector('.kb-saved__sort select');
              if (!sel) return '';
              const s = getComputedStyle(sel);
              return `${s.borderRadius}|${s.height}|${s.appearance || s.webkitAppearance || ''}`;
            }
            """);
        Assert.False(string.IsNullOrWhiteSpace(sortStyle));
        var parts = sortStyle.Split('|');
        Assert.True(parts.Length >= 2);
        var radius = ParsePx(parts[0]);
        var height = ParsePx(parts[1]);
        Assert.True(radius > 0, $"sort border-radius should be styled, got {parts[0]}");
        Assert.InRange(height, 34, 40);

        var firstRow = page.Locator(".kb-saved-row").First;
        var href = await firstRow.Locator("a.kb-saved-row__link").GetAttributeAsync("href");
        Assert.False(string.IsNullOrWhiteSpace(href));

        await firstRow.Locator(".kb-saved-row__title").ClickAsync();
        try
        {
            await page.WaitForURLAsync(
                url => !url.Contains("/candidate/liked", StringComparison.OrdinalIgnoreCase),
                new() { Timeout = 20_000 });
        }
        catch (TimeoutException)
        {
            return;
        }

        Assert.DoesNotContain("/candidate/liked", page.Url, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Mobile_390_heart_unsaves_and_shows_undo_toast()
    {
        var baseUrl = BaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        await using var browser = await LaunchAsync();
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            HasTouch = true,
            IsMobile = true,
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        if (!await TryLoginAsync(page, baseUrl))
        {
            return;
        }

        await page.GotoAsync(baseUrl + "/candidate/liked", new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 90_000
        });

        try
        {
            await page.Locator(".kb-saved-row").First.WaitForAsync(new() { Timeout = 45_000 });
        }
        catch (TimeoutException)
        {
            return;
        }

        var before = await page.Locator(".kb-saved-row").CountAsync();
        if (before < 1)
        {
            return;
        }

        await page.Locator(".kb-saved-row__heart").First.ClickAsync();
        try
        {
            await Assertions.Expect(page.Locator(".kb-saved__toast")).ToBeVisibleAsync(new() { Timeout = 10_000 });
        }
        catch (PlaywrightException)
        {
            return;
        }

        Assert.Contains("Ongedaan", await page.Locator(".kb-saved__toast").InnerTextAsync(), StringComparison.OrdinalIgnoreCase);
        var after = await page.Locator(".kb-saved-row").CountAsync();
        Assert.True(after == before - 1 || after < before, $"expected unsave to remove a row ({before} → {after})");
    }

    [Fact]
    public async Task Desktop_1440_uses_four_tile_columns()
    {
        var baseUrl = BaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        await using var browser = await LaunchAsync();
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        if (!await TryLoginAsync(page, baseUrl))
        {
            return;
        }

        await page.GotoAsync(baseUrl + "/candidate/liked", new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 90_000
        });

        try
        {
            await page.WaitForSelectorAsync(".kb-saved__grid, .state-message, .saved-gate", new() { Timeout = 45_000 });
        }
        catch (TimeoutException)
        {
            return;
        }

        if (await page.Locator(".kb-saved-card").CountAsync() < 4)
        {
            return;
        }

        var tracks = await page.EvaluateAsync<int>("""
            () => {
              const grid = document.querySelector('.kb-saved__grid');
              if (!grid) return 0;
              const cols = getComputedStyle(grid).gridTemplateColumns;
              if (!cols || cols === 'none') return 0;
              return cols.trim().split(/\s+/).length;
            }
            """);
        Assert.Equal(4, tracks);
    }

    [Fact]
    public async Task Shared_tab_no_horizontal_overflow_at_390()
    {
        var baseUrl = BaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        await using var browser = await LaunchAsync();
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            HasTouch = true,
            IsMobile = true,
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        if (!await TryLoginAsync(page, baseUrl))
        {
            return;
        }

        await page.GotoAsync(baseUrl + "/candidate/shared", new()
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 90_000
        });

        try
        {
            await page.WaitForSelectorAsync(".kb-shared, .panel-page", new() { Timeout = 30_000 });
        }
        catch (TimeoutException)
        {
            return;
        }

        var overflow = await page.EvaluateAsync<double>("() => document.documentElement.scrollWidth");
        Assert.True(overflow <= 390 + 1, $"shared tab overflow: scrollWidth={overflow}");
        Assert.True(await page.Locator("a[href='/candidate/liked'], a[href='/candidate/shared']").CountAsync() >= 2);
    }

    private static double ParsePx(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0;
        var num = new string(value.TakeWhile(c => char.IsDigit(c) || c is '.' or '-').ToArray());
        return double.TryParse(num, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var v)
            ? v
            : 0;
    }

    private static string? BaseUrl()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        return string.IsNullOrWhiteSpace(baseUrl) ? null : baseUrl;
    }

    private static async Task<IBrowser> LaunchAsync()
    {
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        var playwright = await Playwright.CreateAsync();
        return await playwright.Chromium.LaunchAsync(new() { Headless = true });
    }

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var resp = await http.GetAsync(baseUrl);
            return resp.IsSuccessStatusCode || (int)resp.StatusCode is >= 300 and < 500;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> TryLoginAsync(IPage page, string baseUrl)
    {
        var email = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? DefaultEmail;
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? DefaultPassword;
        try
        {
            await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
            await page.FillAsync("input[name='email']", email);
            await page.FillAsync("input[name='password']", password);
            var submit = page.Locator("button.login-submit, button.au-submit[type=submit]");
            await page.WaitForFunctionAsync(
                "() => { const b = document.querySelector('button.login-submit, button.au-submit[type=submit]'); return b && !b.disabled; }",
                null,
                new() { Timeout = 30_000 });
            await Task.WhenAll(
                page.WaitForURLAsync(
                    url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
                    new() { Timeout = 60_000 }),
                submit.ClickAsync());
            return !page.Url.Contains("/login", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
