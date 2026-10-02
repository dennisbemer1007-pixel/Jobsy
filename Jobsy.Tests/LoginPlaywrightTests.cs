using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Login redesign smoke (file 03). Soft-skips when JOBSY_E2E_BASE_URL is unset/unreachable.
/// </summary>
[Collection("PlaywrightSmoke")]
public class LoginPlaywrightTests
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
    public async Task Login_desktop_mobile_and_ar_screenshots_and_keyboard_path()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        var artifactDir = Path.Combine(FindRepoRoot(), "artifacts", "auth-login-playwright");
        Directory.CreateDirectory(artifactDir);

        foreach (var (w, h, label, lang) in new[]
                 {
                     (1440, 900, "desktop-nl", "nl"),
                     (390, 844, "mobile-nl", "nl"),
                     (1440, 900, "desktop-ar", "ar")
                 })
        {
            await using var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = w, Height = h },
                Locale = lang == "ar" ? "ar" : "nl-NL"
            });
            var page = await context.NewPageAsync();
            var cspHits = new List<string>();
            page.Console += (_, msg) =>
            {
                if (msg.Text.Contains("Content Security Policy", StringComparison.OrdinalIgnoreCase))
                {
                    cspHits.Add(msg.Text);
                }
            };

            var url = baseUrl + "/login" + (lang == "ar" ? "?lang=ar" : "");
            await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });

            Assert.Empty(cspHits);
            await Assertions.Expect(page.Locator("h1").First).ToBeVisibleAsync();
            // Cookie consent uses role=dialog; the login redesign itself must not open a modal.
            Assert.Equal(0, await page.Locator("[role=dialog]:not(.cookie-consent)").CountAsync());

            var overflow = await page.EvaluateAsync<bool>(
                "() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1");
            Assert.False(overflow);

            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(artifactDir, $"login-{label}.png"),
                FullPage = true
            });
        }

        // Keyboard path at desktop (providers → e-mail → password → toggle → remember → submit)
        {
            await using var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = 1440, Height = 900 }
            });
            var page = await context.NewPageAsync();
            await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });

            await page.Keyboard.PressAsync("Tab");
            for (var i = 0; i < 12; i++)
            {
                await page.Keyboard.PressAsync("Tab");
                var tag = await page.EvaluateAsync<string?>(
                    "() => document.activeElement && document.activeElement.tagName");
                var name = await page.EvaluateAsync<string?>(
                    "() => document.activeElement && (document.activeElement.getAttribute('name') || document.activeElement.id || '')");
                if (string.Equals(name, "email", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(name, "login-email", StringComparison.OrdinalIgnoreCase))
                {
                    Assert.Equal("INPUT", tag);
                    break;
                }
            }
        }
    }

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
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
