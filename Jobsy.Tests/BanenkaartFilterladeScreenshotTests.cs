using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>Captures filterlade screenshots for PR evidence (CI stack on :5201).</summary>
public sealed class BanenkaartFilterladeScreenshotTests
{
    private const string OutDir = "/cursor/stores/self/banenkaart-b";

    [Fact]
    public async Task Capture_filterlade_screenshots_for_pr()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("JOBSY_CAPTURE_BANENKAART"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "http://127.0.0.1:5201").Trim().TrimEnd('/');
        Directory.CreateDirectory(OutDir);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        await using var browser = await (await Playwright.CreateAsync()).Chromium.LaunchAsync(new() { Headless = true });

        var errors = new List<string>();
        try { await CaptureDesktopLoggedOutAsync(browser, baseUrl); }
        catch (Exception ex) { errors.Add("desktop-logged-out: " + ex.Message); }
        try { await CaptureMobileLoggedOutAsync(browser, baseUrl); }
        catch (Exception ex) { errors.Add("mobile-logged-out: " + ex.Message); }
        try { await CaptureDesktopLoggedInAsync(browser, baseUrl); }
        catch (Exception ex) { errors.Add("desktop-logged-in: " + ex.Message); }

        var count = Directory.Exists(OutDir) ? Directory.GetFiles(OutDir, "*.png").Length : 0;
        Assert.True(count >= 6, $"Expected ≥6 screenshots, got {count}. {string.Join("; ", errors)}");
    }

    private static async Task CaptureDesktopLoggedOutAsync(IBrowser browser, string baseUrl)
    {
        await using var context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 1440, Height = 900 } });
        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
        await DismissStartAsync(page);
        await page.WaitForTimeoutAsync(1500);
        await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "desktop-1440-closed-logged-out.png") });
        await page.Locator("[data-testid=kb-panel-head] [data-testid=kb-filters-button]").ClickAsync(new() { Timeout = 30_000 });
        await page.WaitForSelectorAsync(".jobsy-discovery.filters-open [data-testid=kb-filter-sheet]", new() { State = WaitForSelectorState.Visible, Timeout = 90_000 });
        await page.WaitForTimeoutAsync(800);
        await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "desktop-1440-drawer-open-logged-out.png") });
    }

    private static async Task CaptureMobileLoggedOutAsync(IBrowser browser, string baseUrl)
    {
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IsMobile = true,
            HasTouch = true,
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
        await DismissStartAsync(page);
        await page.WaitForTimeoutAsync(1500);
        await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "mobile-390-closed-logged-out.png") });
        await page.Locator(".kb-filter-bar [data-testid=kb-filters-button]").ClickAsync();
        await page.WaitForSelectorAsync("[data-testid=kb-filter-sheet]", new() { Timeout = 15_000 });
        await page.WaitForTimeoutAsync(400);
        await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "mobile-390-sheet-open-logged-out.png") });
    }

    private static async Task CaptureDesktopLoggedInAsync(IBrowser browser, string baseUrl)
    {
        await using var context = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 1440, Height = 900 } });
        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
        await page.FillAsync("input[name='email']", "kandidaat@jobsy.local");
        await page.FillAsync("input[name='password']", "Jobsy123!");
        await page.Locator("button.login-submit, button.au-submit[type=submit]").ClickAsync();
        await page.GotoAsync(baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
        await DismissStartAsync(page);
        await page.WaitForTimeoutAsync(1500);
        await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "desktop-1440-closed-logged-in.png") });
        await page.Locator("[data-testid=kb-panel-head] [data-testid=kb-filters-button]").ClickAsync(new() { Timeout = 30_000 });
        await page.WaitForSelectorAsync(".jobsy-discovery.filters-open [data-testid=kb-filter-sheet]", new() { State = WaitForSelectorState.Visible, Timeout = 90_000 });
        await page.WaitForTimeoutAsync(800);
        await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "desktop-1440-drawer-open-logged-in.png") });
    }

    private static async Task DismissStartAsync(IPage page)
    {
        var later = page.Locator(".kb-start-prompt__later");
        if (await later.CountAsync() > 0)
        {
            await later.ClickAsync();
        }
    }
}
