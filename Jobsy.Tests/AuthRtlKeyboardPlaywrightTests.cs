using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Auth RTL + keyboard E2E (file 08 flows 14–15). Soft-skips without host. Never production.
/// </summary>
[Collection("PlaywrightSmoke")]
public class AuthRtlKeyboardPlaywrightTests
{
    [Fact]
    public async Task Never_targets_production_host()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        Assert.DoesNotContain("lobsy.nl", baseUrl, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_ar_rtl_ltr_fields_and_keyboard_path()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        Assert.DoesNotContain("lobsy.nl", baseUrl, StringComparison.OrdinalIgnoreCase);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var artifactDir = Path.Combine(FindRepoRoot(), "artifacts", "auth-rtl");
        Directory.CreateDirectory(artifactDir);

        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
            Locale = "ar"
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(
            baseUrl + "/login?lang=ar",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });

        var dir = await page.EvaluateAsync<string?>(
            "() => document.querySelector('.pub-theme')?.getAttribute('dir')");
        Assert.Equal("rtl", dir);

        var emailDir = await page.Locator("#login-email").GetAttributeAsync("dir");
        var passwordDir = await page.Locator("#login-password").GetAttributeAsync("dir");
        Assert.Equal("ltr", emailDir);
        Assert.Equal("ltr", passwordDir);

        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(artifactDir, "login-ar-desktop.png"),
            FullPage = true
        });

        // Keyboard path: Tab into e-mail field
        for (var i = 0; i < 16; i++)
        {
            await page.Keyboard.PressAsync("Tab");
            var id = await page.EvaluateAsync<string?>(
                "() => document.activeElement && document.activeElement.id");
            if (string.Equals(id, "login-email", StringComparison.OrdinalIgnoreCase))
            {
                Assert.Equal("login-email", id);
                return;
            }
        }

        Assert.Fail("Keyboard path did not reach #login-email");
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
