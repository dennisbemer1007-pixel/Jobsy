using System.Net;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Auth E2E (file 08 flows 1–4, 9, 13). Soft-skips without reachable JOBSY_E2E_BASE_URL.
/// Never targets production (lobsy.nl).
/// </summary>
[Collection("PlaywrightSmoke")]
public class AuthE2EPlaywrightTests
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
    public async Task Login_wrong_password_pause_forgot_and_activate_redirect()
    {
        var baseUrl = await ResolveBaseUrlAsync();
        if (baseUrl is null)
        {
            return;
        }

        Assert.DoesNotContain("lobsy.nl", baseUrl, StringComparison.OrdinalIgnoreCase);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var artifactDir = Path.Combine(FindRepoRoot(), "artifacts", "auth-e2e");
        Directory.CreateDirectory(artifactDir);

        foreach (var (w, h, label) in new[] { (1440, 900, "desktop"), (390, 844, "mobile") })
        {
            await using var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = w, Height = h }
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

            await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            Assert.Empty(cspHits);
            await Assertions.Expect(page.Locator("h1").First).ToBeVisibleAsync();
            Assert.Equal(0, await page.Locator("input[name=rememberDevice]:checked").CountAsync());

            // Wrong password → alert, no email in URL
            await page.FillAsync("#login-email", "e2e-unknown@test.lobsy.local");
            await page.FillAsync("#login-password", "WrongPassword!1");
            await page.ClickAsync("button.au-submit, button[type=submit]");
            await page.WaitForURLAsync("**/login**", new() { Timeout = 30_000 });
            Assert.DoesNotContain("e2e-unknown", page.Url, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("@", page.Url, StringComparison.Ordinal);
            var alert = page.Locator("[role=alert], .au-alert--danger, .au-alert--sun");
            await Assertions.Expect(alert.First).ToBeVisibleAsync(new() { Timeout = 15_000 });

            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(artifactDir, $"login-error-{label}.png"),
                FullPage = true
            });

            // Forgot password uniform screen
            if (await page.Locator("a[href*='wachtwoord-vergeten']").CountAsync() > 0)
            {
                await page.GotoAsync(
                    baseUrl + "/wachtwoord-vergeten",
                    new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
                await Assertions.Expect(page.Locator("h1").First).ToBeVisibleAsync();
                Assert.Empty(cspHits);
            }

            // /register/activate → permanent redirect target /register
            using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
            {
                Timeout = TimeSpan.FromSeconds(15)
            };
            using var activate = await http.GetAsync(baseUrl + "/register/activate?token=abc");
            Assert.Equal(HttpStatusCode.MovedPermanently, activate.StatusCode);
            Assert.Equal("/register", activate.Headers.Location?.ToString());
        }
    }

    private static async Task<string?> ResolveBaseUrlAsync()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !await IsReachableAsync(baseUrl))
        {
            return null;
        }

        return baseUrl;
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
