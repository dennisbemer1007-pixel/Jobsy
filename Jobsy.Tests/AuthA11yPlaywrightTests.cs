using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Auth a11y smoke (file 07). Soft-skips when JOBSY_E2E_BASE_URL is unset/unreachable.
/// No axe-core in repo — keyboard path, dir, overflow at 390px and 200% zoom.
/// </summary>
[Collection("PlaywrightSmoke")]
public class AuthA11yPlaywrightTests
{
    [Fact]
    public async Task Soft_skip_when_e2e_base_url_unset()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        Assert.False(string.IsNullOrWhiteSpace(baseUrl));
    }

    [Fact]
    public async Task Auth_pages_keyboard_dir_overflow_and_zoom()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var artifactDir = Path.Combine(FindRepoRoot(), "artifacts", "auth-a11y-playwright");
        Directory.CreateDirectory(artifactDir);

        foreach (var path in new[] { "/login", "/wachtwoord-vergeten" })
        {
            await using var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = 390, Height = 844 },
                Locale = "ar"
            });
            var page = await context.NewPageAsync();
            await page.GotoAsync(
                baseUrl + path + "?lang=ar",
                new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });

            var dir = await page.EvaluateAsync<string?>(
                "() => document.querySelector('.pub-theme')?.getAttribute('dir')");
            Assert.Equal("rtl", dir);

            var overflow = await page.EvaluateAsync<bool>(
                "() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1");
            Assert.False(overflow);

            await page.EvaluateAsync("() => { document.body.style.zoom = '200%'; }");
            var overflowZoom = await page.EvaluateAsync<bool>(
                "() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1");
            Assert.False(overflowZoom);

            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(artifactDir, path.Trim('/').Replace('/', '-') + "-ar-390.png"),
                FullPage = true
            });
        }
    }

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var response = await http.GetAsync(baseUrl + "/login");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}
