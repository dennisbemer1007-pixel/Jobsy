using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Playwright helpers for multi-role journey captures (ROLES_AND_VIEWS.md).
/// Requires local stack, Postgres for feature flags, and <c>JOBSY_E2E_ROLES_JOURNEY=1</c>.
/// </summary>
public static class RoleJourneyE2e
{
    public const string ArtifactsRoot = "/opt/cursor/artifacts/screenshots/roles-journey";

    public static bool ShouldRun =>
        string.Equals(
            Environment.GetEnvironmentVariable("JOBSY_E2E_ROLES_JOURNEY"),
            "1",
            StringComparison.Ordinal);

    public static string ShotDir(string roleSlug, string viewportTag)
    {
        var dir = Path.Combine(ArtifactsRoot, roleSlug, viewportTag);
        Directory.CreateDirectory(dir);
        return dir;
    }

    public static async Task WaitForRoleChromeAsync(IPage page)
    {
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.WaitForSelectorAsync(
            "h1, main, body, .admin-shell, .metric-tile, .bottom-nav, nav[aria-label], .banenkaart, #map, .login-shell",
            new() { Timeout = 90_000, State = WaitForSelectorState.Attached });
        await page.WaitForTimeoutAsync(1_200);
        await KbE2e.AssertNoCircuitErrorAsync(page);
    }

    public static async Task ShotAsync(IPage page, string roleSlug, string viewportTag, string stepSlug)
    {
        var path = Path.Combine(ShotDir(roleSlug, viewportTag), $"{stepSlug}.png");
        await page.ScreenshotAsync(new() { Path = path, FullPage = true });
    }

    public static async Task VisitAsync(
        IPage page,
        string baseUrl,
        string roleSlug,
        string viewportTag,
        string path,
        string stepSlug)
    {
        await page.GotoAsync(
            baseUrl.TrimEnd('/') + path,
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await WaitForRoleChromeAsync(page);
        await page.EvaluateAsync("() => document.fonts.ready");
        await page.WaitForTimeoutAsync(600);
        await ShotAsync(page, roleSlug, viewportTag, stepSlug);
    }

    public static async Task AssertLoginAsync(IPage page, string baseUrl, string email)
    {
        var ok = await KbE2e.TryLoginAsync(page, baseUrl, email, KbE2e.DefaultPassword);
        Assert.True(ok, $"Login failed for {email}");
    }

    public static async Task TryTapBottomNavAsync(IPage page, string labelFragment)
    {
        var link = page.Locator(".bottom-nav a, .bottom-nav button").Filter(new() { HasText = labelFragment });
        if (await link.CountAsync() == 0)
        {
            return;
        }

        await link.First.ClickAsync(new() { Timeout = 15_000 });
        await page.WaitForTimeoutAsync(900);
    }
}
