using Jobsy.Core.Contracts.WerkgeverApk;
using Jobsy.Web.Hosting;
using Microsoft.Playwright;

namespace Jobsy.Tests.WerkgeverApk;

[Collection("PlaywrightSmoke")]
public sealed class WerkgeverApkScreenshotPlaywrightTests : IAsyncLifetime
{
    private const string EnvRun = "JOBSY_WERKGEVER_APK_SCREENSHOTS";

    private static bool ShouldRun =>
        string.Equals(Environment.GetEnvironmentVariable(EnvRun), "1", StringComparison.Ordinal);

    private static string OutDir => "/opt/cursor/artifacts/werkgever-apk";

    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        if (!ShouldRun)
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
    }

    [Fact]
    public void Login_ssr_includes_coach_markup_in_employee_frame()
    {
        var html = WerkgeverApkEmployeeSsrHtml.RenderDocument(
            "Test",
            "<p>body</p>",
            new Dictionary<string, string> { ["company"] = "Demo", ["team"] = "Team", ["code"] = "ABC" });
        Assert.Contains("data-ssr-coach-open", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Capture_static_login_mock_html()
    {
        if (!ShouldRun || _browser is null)
        {
            return;
        }

        Directory.CreateDirectory(OutDir);
        var html = WerkgeverApkSsrHtml.RenderLoginPage(
            [
                new WerkgeverApkEmployeeCompanyOptionDto(
                    Guid.NewGuid(),
                    "Demo Logistiek B.V.",
                    [new WerkgeverApkEmployeeTeamOptionDto(Guid.NewGuid(), "Team Logistiek", 3)])
            ],
            null,
            null,
            error: null,
            antiforgery: "");
        var path = Path.Combine(OutDir, "login-preview.html");
        await File.WriteAllTextAsync(path, html);

        await using var context = await _browser.NewContextAsync();
        var page = await context.NewPageAsync();
        await page.GotoAsync(new Uri(new Uri("file://"), path).AbsoluteUri);
        await page.SetViewportSizeAsync(1440, 900);
        await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "01-login-1440x900.png"), FullPage = true });
        await page.SetViewportSizeAsync(390, 844);
        await page.ScreenshotAsync(new() { Path = Path.Combine(OutDir, "01-login-390x844.png"), FullPage = true });
    }
}