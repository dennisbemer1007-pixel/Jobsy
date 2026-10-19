using System.Net;
using Jobsy.Web.Localization;
using Microsoft.Playwright;

namespace Jobsy.Tests.Errors;

/// <summary>
/// Blazor maps circuit loss to <c>components-reconnect-rejected</c>; that must not read as session expiry
/// until <c>/account/session-activity</c> confirms it.
/// </summary>
public class ReconnectSessionVerificationTests
{
    [Fact]
    public void Rejected_catalog_copy_is_neutral_and_session_expiry_is_a_separate_key()
    {
        var rejectedNl = UiStrings.Get("Status.Reconnect.Rejected", "nl");
        var rejectedEn = UiStrings.Get("Status.Reconnect.Rejected", "en");
        var expiredNl = UiStrings.Get("Status.Reconnect.SessionExpired", "nl");
        var expiredEn = UiStrings.Get("Status.Reconnect.SessionExpired", "en");

        Assert.Contains("verbinding", rejectedNl, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("connection", rejectedEn, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sessie is verlopen", rejectedNl, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("session has expired", rejectedEn, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sessie is verlopen", expiredNl, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("session has expired", expiredEn, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void App_shell_includes_session_expired_reconnect_copy_separate_from_rejected()
    {
        var app = File.ReadAllText(Path.Combine(RepoRoot(), "Jobsy.Web", "Components", "App.razor"));
        Assert.Contains("reconnect-toast__msg--session-expired", app, StringComparison.Ordinal);
        Assert.Contains("Status.Reconnect.SessionExpired", app, StringComparison.Ordinal);
        Assert.Contains("Status.Reconnect.Rejected", app, StringComparison.Ordinal);
    }

    [Fact]
    public void Blazor_boot_verifies_session_before_showing_expired_reconnect_copy()
    {
        var boot = File.ReadAllText(Path.Combine(RepoRoot(), "Jobsy.Web", "wwwroot", "js", "blazor-boot.js"));
        Assert.Contains("/account/session-activity", boot, StringComparison.Ordinal);
        Assert.Contains("handleRejectedState", boot, StringComparison.Ordinal);
        Assert.Contains("reconnect-toast--session-expired", boot, StringComparison.Ordinal);
        Assert.Contains("demoteRejectedToFailed", boot, StringComparison.Ordinal);
        Assert.Contains("reconnect-toast--auth-pending", boot, StringComparison.Ordinal);
        Assert.DoesNotContain("location.reload()", boot, StringComparison.Ordinal);
    }

    [Fact]
    public async Task App_shell_html_contains_both_reconnect_messages_in_dutch()
    {
        await using var factory = new ErrorPagesWebFactory { EmployersEnabled = true };
        using var client = factory.CreateHtmlClient();
        var html = WebUtility.HtmlDecode(
            await (await client.GetAsync("/status/404")).Content.ReadAsStringAsync());

        Assert.Contains(UiStrings.Get("Status.Reconnect.Rejected", "nl"), html, StringComparison.Ordinal);
        Assert.Contains(UiStrings.Get("Status.Reconnect.SessionExpired", "nl"), html, StringComparison.Ordinal);
    }

    /// <summary>
    /// Live Acceptatie/local: login, open paspoort, idle ~2 min, no session-expired reconnect toast.
    /// </summary>
    [Fact]
    public async Task Paspoort_after_login_idle_watch_shows_no_session_expired_reconnect_toast()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        const string email = "kandidaat@jobsy.local";
        const string password = "Jobsy123!";

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IgnoreHTTPSErrors = true
        });

        var page = await context.NewPageAsync();
        if (!await TryLoginAsync(page, baseUrl, email, password))
        {
            Assert.Fail("Candidate login failed.");
        }

        await page.GotoAsync(baseUrl + "/candidate/paspoort", new() { WaitUntil = WaitUntilState.Load, Timeout = 90_000 });
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 45_000 });

        var end = DateTime.UtcNow.AddMinutes(2);
        while (DateTime.UtcNow < end)
        {
            await page.Mouse.WheelAsync(0, 240);
            await page.WaitForTimeoutAsync(10_000);
        }

        var sessionExpiredVisible = await page.Locator(".reconnect-toast__msg--session-expired").IsVisibleAsync();
        Assert.False(sessionExpiredVisible, "Fresh session on paspoort must not show session-expired reconnect copy after idle watch.");

        var rejectedWithExpiredClass = await page.Locator("#components-reconnect-modal.reconnect-toast--session-expired").CountAsync();
        Assert.Equal(0, rejectedWithExpiredClass);
    }

    private static async Task<bool> TryLoginAsync(IPage page, string baseUrl, string email, string password)
    {
        try
        {
            await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            await page.WaitForSelectorAsync("input[name='email']", new() { Timeout = 30_000 });
            await page.FillAsync("input[name='email']", email);
            await page.FillAsync("input[name='password']", password);
            var submit = page.Locator("button.login-submit");
            await submit.ClickAsync();
            await page.WaitForURLAsync(
                url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
                new() { Timeout = 60_000 });
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            using var response = await http.GetAsync(baseUrl.TrimEnd('/') + "/");
            return (int)response.StatusCode is >= 200 and < 500;
        }
        catch
        {
            return false;
        }
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Jobsy.sln not found");
    }
}
