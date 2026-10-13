using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Wave-1 passport privacy line, data-download row, and talent-pool RIASEC hide.
/// Soft-skips when <c>JOBSY_E2E_BASE_URL</c> is unset or unreachable.
/// The old admin RIASEC toggle is not part of this switch (config key, default off).
/// </summary>
[Collection("PlaywrightSmoke")]
public class Wave1HonestClearPlaywrightTests
{
    private const string DefaultEmail = "kandidaat@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";

    [Theory]
    [InlineData(390, 844)]
    [InlineData(1440, 900)]
    public async Task Passport_promise_and_download_link_at_viewport(int width, int height)
    {
        var baseUrl = ResolveBaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        var email = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? DefaultEmail;
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? DefaultPassword;
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            IgnoreHTTPSErrors = true,
            AcceptDownloads = true
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await LoginAsync(page, baseUrl, email, password);

        await page.GotoAsync(baseUrl + "/candidate/paspoort", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 60_000
        });
        var promise = page.Locator("[data-testid=passport-privacy-promise]");
        await promise.First.WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
        Assert.True(await promise.First.IsVisibleAsync());

        await page.GotoAsync(baseUrl + "/candidate/paspoort?tab=data", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 60_000
        });
        var download = page.Locator("[data-testid=passport-download-data]");
        await download.WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
        await download.ClickAsync();
        await page.WaitForURLAsync(
            url => url.Contains("/privacy/data", StringComparison.OrdinalIgnoreCase),
            new PageWaitForURLOptions { Timeout = 30_000 });
        var exportLink = page.Locator("a[href='/privacy/data/export']");
        Assert.True(await exportLink.IsVisibleAsync());
        var exportFile = await page.RunAndWaitForDownloadAsync(async () =>
        {
            await exportLink.ClickAsync();
        });
        Assert.Matches(@"^lobsy-mijn-gegevens-\d{4}-\d{2}-\d{2}\.json$", exportFile.SuggestedFilename);
        var body = await page.Locator("body").InnerTextAsync();
        Assert.DoesNotContain("\"toelichting\"", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("{\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Passport_promise_is_visible_in_rtl()
    {
        var baseUrl = ResolveBaseUrl();
        if (baseUrl is null || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        var email = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? DefaultEmail;
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? DefaultPassword;
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 390, Height = 844 },
            Locale = "ar",
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await LoginAsync(page, baseUrl, email, password);
        await page.GotoAsync(baseUrl + "/candidate/paspoort?lang=ar", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 60_000
        });
        await page.Locator("[data-testid=passport-privacy-promise]").First.WaitForAsync(
            new LocatorWaitForOptions { Timeout = 30_000 });
        var dir = await page.Locator("[dir=rtl]").CountAsync();
        Assert.True(dir > 0);
    }

    [Fact]
    public async Task Talent_pool_hides_riasec_tag_field_when_switch_is_off()
    {
        var baseUrl = ResolveBaseUrl();
        var email = (Environment.GetEnvironmentVariable("JOBSY_E2E_EMPLOYER_EMAIL") ?? "").Trim();
        var password = (Environment.GetEnvironmentVariable("JOBSY_E2E_EMPLOYER_PASSWORD") ?? "").Trim();
        if (baseUrl is null || !await IsReachableAsync(baseUrl) || email.Length == 0 || password.Length == 0)
        {
            return;
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 },
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await LoginAsync(page, baseUrl, email, password);
        await page.GotoAsync(baseUrl + "/werkgever/talentpool", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 60_000
        });
        Assert.Equal(0, await page.GetByPlaceholder("Samenwerken, Social").CountAsync());
        Assert.Equal(0, await page.Locator("[data-testid=talent-holland]").CountAsync());
    }

    private static async Task LoginAsync(IPage page, string baseUrl, string email, string password)
    {
        await page.GotoAsync(baseUrl + "/login", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 60_000
        });
        await page.FillAsync("input[name='email']", email);
        await page.FillAsync("input[name='password']", password);
        await page.ClickAsync("button.login-submit");
        await page.WaitForURLAsync(
            url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
            new PageWaitForURLOptions { Timeout = 60_000 });
    }

    private static string? ResolveBaseUrl()
    {
        var fromEnv = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return fromEnv;
        }

        var ci = (Environment.GetEnvironmentVariable("JOBSY_CI_WEB_URL") ?? "").Trim().TrimEnd('/');
        return string.IsNullOrWhiteSpace(ci) ? null : ci;
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
