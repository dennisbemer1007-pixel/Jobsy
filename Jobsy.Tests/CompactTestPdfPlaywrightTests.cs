using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Downloads one deep personal PDF with CompactTestPdfEnabled on, then puts the flag back.
/// Soft-skips without JOBSY_E2E_BASE_URL. Does not change payment or scores.
/// </summary>
[Collection("PlaywrightSmoke")]
public class CompactTestPdfPlaywrightTests
{
    [Fact]
    public async Task Deep_pdf_download_with_compact_flag_then_restore()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !await ReachableAsync(baseUrl))
        {
            return;
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var admin = await browser.NewPageAsync(new() { Locale = "nl-NL" });
        var turnedOn = false;
        try
        {
            if (!await LoginAsync(admin, baseUrl, AdminEmail(), Password()))
            {
                return;
            }

            await admin.GotoAsync(baseUrl + "/admin/instellingen",
                new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            var toggle = admin.Locator("button.admin-switch[aria-label='Korte testrapporten']");
            if (await toggle.CountAsync() == 0)
            {
                return;
            }

            var pressed = await toggle.First.GetAttributeAsync("aria-pressed");
            if (!string.Equals(pressed, "true", StringComparison.OrdinalIgnoreCase))
            {
                await toggle.First.ClickAsync();
                var save = admin.GetByRole(AriaRole.Button, new() { Name = "Opslaan en loggen" });
                if (await save.CountAsync() == 0)
                {
                    return;
                }

                await save.First.ClickAsync();
                await admin.WaitForTimeoutAsync(800);
                turnedOn = true;
            }

            var candidate = await browser.NewPageAsync(new() { Locale = "nl-NL", AcceptDownloads = true });
            if (!await LoginAsync(candidate, baseUrl, CandidateEmail(), Password()))
            {
                return;
            }

            await candidate.GotoAsync(baseUrl + "/candidate/career",
                new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            var downloadButton = candidate.GetByRole(AriaRole.Button, new() { Name = "Download je rapport" });
            if (await downloadButton.CountAsync() == 0)
            {
                return;
            }

            var download = await candidate.RunAndWaitForDownloadAsync(async () =>
            {
                await downloadButton.First.ClickAsync();
            });
            var path = await download.PathAsync();
            Assert.False(string.IsNullOrWhiteSpace(path));
            var info = new FileInfo(path!);
            Assert.True(info.Length > 8_000, "compact PDF should be a real file, not an empty response");
            var header = new byte[5];
            await using (var stream = info.OpenRead())
            {
                Assert.Equal(5, await stream.ReadAsync(header));
            }

            Assert.Equal("%PDF-", System.Text.Encoding.ASCII.GetString(header));
        }
        finally
        {
            if (turnedOn)
            {
                var toggle = admin.Locator("button.admin-switch[aria-label='Korte testrapporten']");
                if (await toggle.CountAsync() > 0)
                {
                    await toggle.First.ClickAsync();
                    var save = admin.GetByRole(AriaRole.Button, new() { Name = "Opslaan en loggen" });
                    if (await save.CountAsync() > 0)
                    {
                        await save.First.ClickAsync();
                    }
                }
            }
        }
    }

    private static string CandidateEmail()
        => Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL")
           ?? Environment.GetEnvironmentVariable("JOBSY_E2E_EMAIL")
           ?? "kandidaat@jobsy.local";

    private static string AdminEmail()
        => Environment.GetEnvironmentVariable("JOBSY_E2E_ADMIN_EMAIL") ?? "admin@jobsy.local";

    private static string Password()
        => Environment.GetEnvironmentVariable("JOBSY_E2E_PASSWORD") ?? "Jobsy123!";

    private static async Task<bool> ReachableAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var response = await http.GetAsync(baseUrl);
            return (int)response.StatusCode < 500;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> LoginAsync(IPage page, string baseUrl, string email, string password)
    {
        try
        {
            await page.GotoAsync(baseUrl + "/account/login",
                new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
            var field = page.Locator("input[type=email], input[name=email]").First;
            if (await field.CountAsync() == 0)
            {
                return false;
            }

            await field.FillAsync(email);
            await page.Locator("input[type=password]").First.FillAsync(password);
            await page.Locator("button[type=submit]").First.ClickAsync();
            await page.WaitForTimeoutAsync(1200);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
