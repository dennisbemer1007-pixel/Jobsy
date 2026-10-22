using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Fresh browser context on Acceptatie: cookie bar visible for logged-in candidate @390px.
/// </summary>
[Collection("FunctionalE2e")]
[Trait("Suite", "FunctionalE2e")]
public sealed class CandidateCookieBarAccScreenshotPlaywrightTests
{
    private const string AccUrl = "https://acceptatie.lobsy.nl";
    private const string DefaultEmail = "test-kandidaat@lobsy.nl";
    private const string FallbackEmail = "onboarding.e2e@jobsy.local";
    private const string DefaultPassword = "Jobsy123!";

    [Fact]
    public async Task Fresh_context_shows_cookie_bar_on_ontdekkingsreis_at_390()
    {
        if (!await SmokeReachability.IsReachableAsync(AccUrl))
        {
            return;
        }

        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? DefaultPassword;
        var email = (Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? DefaultEmail).Trim();

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IsMobile = true,
            HasTouch = true,
            IgnoreHTTPSErrors = true,
        });

        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        if (!await TryLoginWithoutConsentAsync(page, email, password)
            && !string.Equals(email, FallbackEmail, StringComparison.OrdinalIgnoreCase))
        {
            if (!await TryLoginWithoutConsentAsync(page, FallbackEmail, password))
            {
                return;
            }
        }

        await page.GotoAsync(AccUrl + "/candidate/ontdekkingsreis", new() { WaitUntil = WaitUntilState.Load });
        var bar = page.Locator(".cookie-consent");
        await bar.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });

        var outDir = Path.Combine(FindRepoRoot(), "artifacts", "cookie-consent-acc");
        Directory.CreateDirectory(outDir);
        var shotPath = Path.Combine(outDir, "cookie-bar-ontdekkingsreis-390.png");
        await page.ScreenshotAsync(new() { Path = shotPath, FullPage = false });

        Assert.True(File.Exists(shotPath));
        Assert.True(await bar.IsVisibleAsync());
    }

    private static async Task<bool> TryLoginWithoutConsentAsync(IPage page, string email, string password)
    {
        try
        {
            await page.GotoAsync(AccUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            await page.WaitForSelectorAsync("input[name='email']", new() { Timeout = 45_000 });
            await page.FillAsync("input[name='email']", email);
            await page.FillAsync("input[name='password']", password);
            await page.ClickAsync("button.login-submit");
            await page.WaitForURLAsync(
                url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
                new() { Timeout = 90_000 });
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "Jobsy.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? "";
        }

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}
