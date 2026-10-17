using Jobsy.Tests.E2e;
using Microsoft.Playwright;
using Npgsql;

namespace Jobsy.Tests;

/// <summary>
/// Static SSR auth (/login, /account-maken, /account-maken/code): no Blazor circuit,
/// forms + auth.js + app-public.js only. Requires CI stack + JOBSY_E2E_BASE_URL.
/// Screenshots: set JOBSY_E2E_PERF2_SCREENSHOTS=1 → /cursor/stores/self/perf2/screenshots/
/// </summary>
[Collection("PlaywrightSmoke")]
public class NoBlazorStaticAuthPlaywrightTests
{
    private const string CandidateEmail = "kandidaat@jobsy.local";
    private const string Password = "Jobsy123!";
    private const string WrongPassword = "WrongPassword!";

    [Fact]
    public async Task Login_static_shell_full_flow_desktop_and_mobile()
    {
        var baseUrl = await RequireBaseUrlAsync();
        await RunLoginFlowAsync(baseUrl, 1440, 900, "login-flow-desktop");
        await RunLoginFlowAsync(baseUrl, 390, 844, "login-flow-mobile");
    }

    [Fact]
    public async Task Login_with_dummy_entra_config_shows_microsoft_and_divider()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("JOBSY_E2E_ENTRA_DUMMY"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var baseUrl = await RequireBaseUrlAsync();
        await using var browser = await KbE2e.LaunchChromiumAsync();
        await using var context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 390, Height = 844 } });
        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
        await AssertNoBlazorRuntimeAsync(page);
        var body = await page.InnerTextAsync("body");
        Assert.Contains("Microsoft", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("e-mail", body, StringComparison.OrdinalIgnoreCase);
        await MaybeShotAsync(page, "login-entra-dummy-mobile");
    }

    [Fact]
    public async Task Account_maken_and_code_flow_desktop_and_mobile()
    {
        var baseUrl = await RequireBaseUrlAsync();
        if (!string.Equals(Environment.GetEnvironmentVariable("JOBSY_E2E_CAPTURE_EMAIL_CODES"), "1", StringComparison.Ordinal))
        {
            Assert.Fail("Set JOBSY_E2E_CAPTURE_EMAIL_CODES=1 on API for signup code tests.");
        }

        await RunSignupCodeFlowAsync(baseUrl, 1440, 900, "signup-flow-desktop");
        await RunSignupCodeFlowAsync(baseUrl, 390, 844, "signup-flow-mobile");
    }

    private static async Task RunLoginFlowAsync(string baseUrl, int width, int height, string tag)
    {
        await ClearSeedCandidateLockoutAsync();
        await using var browser = await KbE2e.LaunchChromiumAsync();
        await using var context = await browser.NewContextAsync(KbE2e.ContextFor(width, height));
        var page = await context.NewPageAsync();

        await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
        await AssertNoBlazorRuntimeAsync(page);
        await AssertCookieBannerCompactAsync(page);
        await MaybeShotAsync(page, $"{tag}-01-banner-visible");

        await page.Locator(".cookie-consent [data-consent=necessary]").First.ClickAsync();
        await page.Locator(".cookie-consent").WaitForAsync(new() { State = WaitForSelectorState.Detached, Timeout = 5_000 });
        var known = await page.EvaluateAsync<bool>(
            "() => document.documentElement.classList.contains('cookie-consent-known')");
        Assert.True(known);
        await MaybeShotAsync(page, $"{tag}-02-banner-hidden");

        var forgot = page.Locator("a[href*='wachtwoord-vergeten'], a[href*='forgot']");
        Assert.True(await forgot.CountAsync() > 0, "Expected Wachtwoord vergeten link on login.");

        await VisibleLangSummary(page, width).ClickAsync();
        await VisibleLangMenu(page, width).Locator(".pub-lang__option[href*='/taal/en']").ClickAsync();
        await page.WaitForURLAsync("**/login**", new() { Timeout = 15_000 });
        await VisibleLangMenu(page, width).Locator(".pub-lang__code").First.WaitForAsync(new() { Timeout = 10_000 });

        await page.Locator("[data-au-password-toggle]").First.ClickAsync();
        var typeAfter = await page.Locator("#login-password").GetAttributeAsync("type");
        Assert.Equal("text", typeAfter);

        var decoyEmail = $"perf2-wrong.{Guid.NewGuid():N}@example.test";
        await page.Locator("#login-email").FillAsync(decoyEmail);
        await page.Locator("#login-password").FillAsync(WrongPassword);
        await page.Locator("form.au-form button[type=submit], button.login-submit").First.ClickAsync();
        await page.WaitForURLAsync("**/login**", new() { Timeout = 30_000 });
        var err = await page.InnerTextAsync("body");
        Assert.True(
            err.Contains("klopt niet", StringComparison.OrdinalIgnoreCase)
            || err.Contains("niet goed", StringComparison.OrdinalIgnoreCase)
            || err.Contains("wrong", StringComparison.OrdinalIgnoreCase)
            || err.Contains("not quite right", StringComparison.OrdinalIgnoreCase),
            "Expected wrong-password message on login.");

        await page.Locator("#login-email").FillAsync(CandidateEmail);
        await page.Locator("#login-password").FillAsync(Password);
        var remember = page.Locator("input[name=rememberDevice]");
        if (await remember.CountAsync() > 0)
        {
            await remember.CheckAsync();
        }

        await page.Locator("form.au-form button[type=submit], button.login-submit").First.ClickAsync();
        await page.WaitForURLAsync(
            url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 60_000 });
        await MaybeShotAsync(page, $"{tag}-03-after-login");
    }

    private static async Task RunSignupCodeFlowAsync(string baseUrl, int width, int height, string tag)
    {
        var email = $"perf2.{Guid.NewGuid():N}@example.test";
        await using var browser = await KbE2e.LaunchChromiumAsync();
        await using var context = await browser.NewContextAsync(KbE2e.ContextFor(width, height));
        var page = await context.NewPageAsync();

        await page.GotoAsync(baseUrl + "/account-maken", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
        await AssertNoBlazorRuntimeAsync(page);
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);

        await page.Locator("input[name=email]").First.FillAsync(email);
        var firstName = page.Locator("input[name=firstName]");
        if (await firstName.CountAsync() > 0)
        {
            await firstName.First.FillAsync("Perf");
        }
        await page.Locator("button.au-submit, button.login-submit").First.ClickAsync();
        await page.WaitForURLAsync("**/account-maken/code**", new() { Timeout = 60_000 });
        await AssertNoBlazorRuntimeAsync(page);
        await MaybeShotAsync(page, $"{tag}-01-code-step");

        var badCode = page.Locator("input[name=code]").First;
        await badCode.FillAsync("000000");
        await page.Locator("button.login-submit").First.ClickAsync();
        await page.WaitForURLAsync("**/account-maken/code**", new() { Timeout = 30_000 });
        var body = await page.InnerTextAsync("body");
        Assert.True(
            body.Contains("code", StringComparison.OrdinalIgnoreCase)
            || body.Contains("probeer", StringComparison.OrdinalIgnoreCase),
            "Expected code error state.");

        await page.Locator("form.pub-signup__resend button").First.ClickAsync();
        await page.WaitForURLAsync("**/account-maken/code**", new() { Timeout = 30_000 });

        var code = await CandidateJourneyE2e.FetchEmailCodeAsync(baseUrl, email);
        Assert.False(string.IsNullOrWhiteSpace(code));

        await page.Locator("input[name=code]").First.FillAsync(code!);
        await page.Locator("button.login-submit").First.ClickAsync();
        await page.WaitForURLAsync(
            url => !url.Contains("/account-maken/code", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 90_000 });
        await MaybeShotAsync(page, $"{tag}-02-after-signup");
    }

    private static async Task AssertNoBlazorRuntimeAsync(IPage page)
    {
        var hasBlazor = await page.EvaluateAsync<bool>(
            "() => !!document.querySelector('script[src*=\"blazor.web.js\"]')");
        Assert.False(hasBlazor, "Static auth page must not load blazor.web.js.");
    }

    private static ILocator VisibleLangMenu(IPage page, int viewportWidth) =>
        viewportWidth >= 900
            ? page.Locator(".pub-header__actions .pub-lang")
            : page.Locator(".pub-header__mobile--auth .pub-lang");

    private static ILocator VisibleLangSummary(IPage page, int viewportWidth) =>
        VisibleLangMenu(page, viewportWidth).Locator("summary.pub-lang__toggle");

    private static async Task AssertCookieBannerCompactAsync(IPage page)
    {
        var titleSize = await page.EvaluateAsync<double>(
            """
            () => {
              const t = document.querySelector('.cookie-consent__title');
              if (!t) return 999;
              return parseFloat(getComputedStyle(t).fontSize) || 999;
            }
            """);
        Assert.True(titleSize < 20, $"Cookie title font too large ({titleSize}px); missing cookie-consent.css?");
    }

    private static async Task ClearSeedCandidateLockoutAsync()
    {
        var cs = Environment.GetEnvironmentVariable("ConnectionStrings__JobsyDb")
            ?? Environment.GetEnvironmentVariable("JOBSY_E2E_JOBSY_DB");
        if (string.IsNullOrWhiteSpace(cs))
        {
            return;
        }

        await using var conn = new NpgsqlConnection(cs);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            """
            UPDATE "LocalAuthCredentials"
            SET "FailedLoginCount" = 0, "LockoutUntil" = NULL, "LockoutCount" = 0
            WHERE "Email" = @email
            """,
            conn);
        cmd.Parameters.AddWithValue("email", CandidateEmail);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<string> RequireBaseUrlAsync()
    {
        var baseUrl = await KbE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            Assert.Fail("JOBSY_E2E_BASE_URL unset or unreachable.");
        }

        return baseUrl;
    }

    private static async Task MaybeShotAsync(IPage page, string name)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("JOBSY_E2E_PERF2_SCREENSHOTS"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var dir = Environment.GetEnvironmentVariable("JOBSY_E2E_PERF2_SCREENSHOT_DIR")
            ?? "/cursor/stores/self/perf2/screenshots";
        Directory.CreateDirectory(dir);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(dir, name + ".jpg"),
            Type = ScreenshotType.Jpeg,
            Quality = 88,
            FullPage = false
        });
    }
}
