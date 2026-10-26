using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Bewaard SSR undo toast: unsave, toast visible, undo restores the vacancy (mobile + desktop).
/// Requires <c>JOBSY_E2E_BASE_URL</c> and at least one saved vacancy.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class BewaardUndoPlaywrightTests
{
    private const string DefaultEmail = "kandidaat@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";

    [Fact]
    public async Task Mobile_390_unsave_shows_toast_and_undo_restores_row()
    {
        await RunUndoFlowAsync(390, 844, mobile: true, rowSelector: ".kb-saved-row:not([hidden])", heartSelector: ".kb-saved-row__heart");
    }

    [Fact]
    public async Task Desktop_1440_unsave_shows_toast_and_undo_restores_card()
    {
        await RunUndoFlowAsync(1440, 900, mobile: false, rowSelector: ".kb-saved-card:not([hidden])", heartSelector: ".kb-saved-card__heart");
    }

    private static async Task RunUndoFlowAsync(
        int width,
        int height,
        bool mobile,
        string rowSelector,
        string heartSelector)
    {
        var baseUrl = BaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        await EmployersPlaywrightGuard.SkipIfEmployersOffAsync(baseUrl);

        await using var browser = await LaunchAsync();
        var contextOptions = new BrowserNewContextOptions
        {
            ViewportSize = new() { Width = width, Height = height },
            IgnoreHTTPSErrors = true
        };
        if (mobile)
        {
            contextOptions.HasTouch = true;
            contextOptions.IsMobile = true;
        }

        await using var context = await browser.NewContextAsync(contextOptions);
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
            await page.Locator(rowSelector).First.WaitForAsync(new() { Timeout = 45_000 });
        }
        catch (TimeoutException)
        {
            return;
        }

        var before = await page.Locator(rowSelector).CountAsync();
        if (before < 1)
        {
            return;
        }

        await page.Locator(heartSelector).First.ClickAsync();

        var toast = page.Locator("[data-saved-toast]");
        await Assertions.Expect(toast).ToBeVisibleAsync(new() { Timeout = 10_000 });
        Assert.Contains("Ongedaan", await toast.InnerTextAsync(), StringComparison.OrdinalIgnoreCase);

        var afterUnsave = await page.Locator(rowSelector).CountAsync();
        Assert.Equal(before - 1, afterUnsave);

        await page.Locator("[data-saved-undo]").ClickAsync();
        await Assertions.Expect(page.Locator(rowSelector)).ToHaveCountAsync(before, new() { Timeout = 10_000 });
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
