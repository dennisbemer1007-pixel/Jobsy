using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Captures research UI at 1280×900 (desktop) and 390×844 (mobile) into /opt/cursor/artifacts.
/// Requires <c>JOBSY_E2E_BASE_URL</c> (e.g. http://localhost:5201).
/// </summary>
[Collection("PlaywrightSmoke")]
public class ResearchViewportScreenshotTests
{
    private const string CandidateEmail = "kandidaat@jobsy.local";
    private const string CandidatePassword = "Jobsy123!";
    private const string AdminEmail = "admin@jobsy.local";
    private const string AdminPassword = "Jobsy123!";

    public static IEnumerable<object[]> Viewports =>
    [
        [1280, 900, "desktop"],
        [390, 844, "mobile"]
    ];

    [Theory]
    [MemberData(nameof(Viewports))]
    public async Task Capture_research_surfaces(int width, int height, string label)
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        Directory.CreateDirectory("/opt/cursor/artifacts");
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        await using var adminCtx = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IgnoreHTTPSErrors = true
        });
        var adminPage = await adminCtx.NewPageAsync();
        if (await LoginAsync(adminPage, baseUrl, AdminEmail, AdminPassword))
        {
            await adminPage.GotoAsync(baseUrl + "/admin/research", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await adminPage.Locator("[data-testid='research-admin-dashboard']").WaitForAsync(new() { Timeout = 60_000 });
            var enableToggle = adminPage.Locator(".platform-features-card input[type='checkbox']").First;
            if (await enableToggle.CountAsync() > 0 && !await enableToggle.IsCheckedAsync())
            {
                await enableToggle.CheckAsync();
                await adminPage.WaitForTimeoutAsync(800);
            }
            await adminPage.ScreenshotAsync(new()
            {
                Path = $"/opt/cursor/artifacts/research-admin-dashboard-{label}.png",
                FullPage = true
            });
            var exportBtn = adminPage.Locator("[data-testid='research-export-btn']");
            if (await exportBtn.CountAsync() > 0)
            {
                await exportBtn.ScrollIntoViewIfNeededAsync();
                await adminPage.ScreenshotAsync(new()
                {
                    Path = $"/opt/cursor/artifacts/research-admin-export-{label}.png",
                    FullPage = true
                });
            }

            try
            {
                var pilotCode = await EnsurePilotCampaignAsync(adminPage, baseUrl);
                if (!string.IsNullOrWhiteSpace(pilotCode))
                {
                    await using var pilotCtx = await browser.NewContextAsync(new()
                    {
                        ViewportSize = new() { Width = width, Height = height },
                        IgnoreHTTPSErrors = true
                    });
                    var pilotPage = await pilotCtx.NewPageAsync();
                    await pilotPage.GotoAsync($"{baseUrl}/pilot/{pilotCode}", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
                    await pilotPage.WaitForTimeoutAsync(1200);
                    await pilotPage.ScreenshotAsync(new()
                    {
                        Path = $"/opt/cursor/artifacts/research-pilot-{label}.png",
                        FullPage = true
                    });
                }
            }
            catch (PlaywrightException)
            {
                // Pilot UI is optional when admin create fails in headless CI.
            }
        }

        await using var candidateCtx = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IgnoreHTTPSErrors = true
        });
        var page = await candidateCtx.NewPageAsync();

        await page.GotoAsync(baseUrl + "/info/onderzoek", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.Locator("[data-testid='research-info-page']").WaitForAsync(new() { Timeout = 30_000 });
        await page.ScreenshotAsync(new()
        {
            Path = $"/opt/cursor/artifacts/research-info-onderzoek-{label}.png",
            FullPage = true
        });

        if (!await LoginAsync(page, baseUrl, CandidateEmail, CandidatePassword))
        {
            return;
        }

        await page.GotoAsync(baseUrl + "/candidate/research-consent?return=/candidate/profile&test=competence");
        var panel = page.Locator("[data-testid='research-consent-panel']");
        if (await panel.CountAsync() > 0)
        {
            await page.Locator("[data-testid='research-consent-checkbox']").CheckAsync();
            await FillDemographicsAsync(page);
            await page.ScreenshotAsync(new()
            {
                Path = $"/opt/cursor/artifacts/research-consent-after-scan-{label}.png",
                FullPage = true
            });
        }

        try
        {
            await page.GotoAsync(baseUrl + "/candidate/profile", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.EvaluateAsync("() => document.getElementById('consent')?.scrollIntoView({block:'start'})");
            await page.WaitForTimeoutAsync(600);
            var privacy = page.Locator("[data-testid='profile-research-privacy']");
            if (await privacy.CountAsync() > 0)
            {
                var checkbox = page.Locator("[data-testid='research-consent-checkbox']");
                if (await checkbox.CountAsync() > 0)
                {
                    await checkbox.CheckAsync();
                    await FillDemographicsAsync(page);
                }

                await page.ScreenshotAsync(new()
                {
                    Path = $"/opt/cursor/artifacts/research-privacy-demographics-{label}.png",
                    FullPage = true
                });
            }
        }
        catch (TimeoutException)
        {
            // Profile layout can differ when passport redirect is active; info + consent pages are the primary captures.
        }
    }

    private static async Task FillDemographicsAsync(IPage page)
    {
        foreach (var id in new[] { "_age", "_gender", "_education", "_native", "_testLang" })
        {
            var select = page.Locator($"select#{id}");
            if (await select.CountAsync() == 0)
            {
                continue;
            }

            var options = await select.Locator("option").AllAsync();
            if (options.Count > 1)
            {
                var value = await options[1].GetAttributeAsync("value");
                if (!string.IsNullOrWhiteSpace(value))
                {
                    await select.SelectOptionAsync(new[] { value });
                }
            }
        }
    }

    private static async Task<string?> EnsurePilotCampaignAsync(IPage adminPage, string baseUrl)
    {
        await adminPage.GotoAsync(baseUrl + "/admin/research", new() { WaitUntil = WaitUntilState.NetworkIdle });
        var titleInput = adminPage.Locator("input[type='text']").First;
        if (await titleInput.CountAsync() == 0)
        {
            return null;
        }

        await titleInput.FillAsync($"UAT pilot {DateTime.UtcNow:yyyyMMddHHmm}");
        await adminPage.GetByRole(AriaRole.Button, new() { Name = "Nieuwe pilotlink" }).ClickAsync();
        await adminPage.Locator("ul li").First.WaitForAsync(new() { Timeout = 30_000 });
        var itemText = await adminPage.Locator("ul li").First.InnerTextAsync();
        var marker = "code ";
        var idx = itemText.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
        {
            var tail = itemText[(idx + marker.Length)..];
            var code = tail.Split([' ', '—', '-', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(code))
            {
                return code.Trim();
            }
        }

        var link = adminPage.Locator("a[href*='/pilot/']").First;
        if (await link.CountAsync() == 0)
        {
            return null;
        }

        var href = await link.GetAttributeAsync("href") ?? "";
        return href.Split('/').LastOrDefault(s => !string.IsNullOrWhiteSpace(s));
    }

    private static async Task<bool> LoginAsync(IPage page, string baseUrl, string email, string password)
    {
        await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60_000 });
        if (await page.Locator("input[name='email']").CountAsync() == 0)
        {
            return false;
        }

        await page.FillAsync("input[name='email']", email);
        await page.FillAsync("input[name='password']", password);
        var submit = page.Locator("button.login-submit, button.au-submit[type=submit]");
        await submit.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await Task.WhenAll(
            page.WaitForURLAsync(
                url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
                new() { Timeout = 60_000 }),
            submit.ClickAsync());
        return !page.Url.Contains("/login", StringComparison.OrdinalIgnoreCase);
    }
}
