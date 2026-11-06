using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>Manual acc probe: JOBSY_ACC_LOGIN_PROBE=1 JOBSY_E2E_BASE_URL=https://acceptatie.lobsy.nl</summary>
[Collection("PlaywrightSmoke")]
public sealed class AccRoleLoginProbePlaywrightTests
{
    [Theory]
    [InlineData("kandidaat@jobsy.local")]
    [InlineData("leraar@jobsy.local")]
    [InlineData("schoolbeheer@jobsy.local")]
    public async Task Role_login_reaches_app(string email)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("JOBSY_ACC_LOGIN_PROBE"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "https://acceptatie.lobsy.nl").TrimEnd('/');
        if (!await KbE2e.IsReachableAsync(baseUrl))
        {
            return;
        }

        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(KbE2e.DesktopContext());
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);

        var ok = await KbE2e.TryLoginAsync(page, baseUrl, email);
        var bodySnippet = ok ? page.Url : (await page.ContentAsync())[..Math.Min(800, (await page.ContentAsync()).Length)];
        Assert.True(ok, $"Login failed for {email}. Url={page.Url} snippet={bodySnippet}");
    }
}
