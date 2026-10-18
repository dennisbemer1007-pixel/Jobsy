using Jobsy.Infrastructure.Data;
using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Reference verification UI screenshots (1440 + 390). Run:
/// JOBSY_E2E_REF_VERIF_SCREENSHOTS=1 JOBSY_E2E_BASE_URL=http://127.0.0.1:5201 dotnet test --filter ReferenceVerificationScreenshots
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class ReferenceVerificationScreenshotsPlaywrightTests
{
    private const string EnvRun = "JOBSY_E2E_REF_VERIF_SCREENSHOTS";
    private const string Password = "Jobsy123!";
    private const string Email = ReferenceVerificationDemoIds.Email;

    private static readonly (int Width, int Height, string Tag)[] Viewports =
    [
        (1440, 900, "1440"),
        (390, 844, "390")
    ];

    private static string OutputDir => "/cursor/stores/self/references";

    private static bool ShouldRun =>
        string.Equals(Environment.GetEnvironmentVariable(EnvRun), "1", StringComparison.Ordinal);

    [Fact]
    public async Task Capture_reference_verification_screenshots()
    {
        if (!ShouldRun)
        {
            return;
        }

        var baseUrl = await KbE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            Assert.Fail("JOBSY_E2E_BASE_URL unreachable — start API :5200 and Web :5201 with seeded DB.");
        }

        Directory.CreateDirectory(OutputDir);
        foreach (var stale in Directory.GetFiles(OutputDir, "*.png"))
        {
            File.Delete(stale);
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        foreach (var (width, height, tag) in Viewports)
        {
            await using var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IgnoreHTTPSErrors = true
            });
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();
            await LoginAsync(page, baseUrl, Email);

            await page.GotoAsync($"{baseUrl}/candidate/profile", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });
            await page.WaitForSelectorAsync("[data-testid='ref-verification-tip']", new() { Timeout = 15_000 });
            await page.ScreenshotAsync(new() { Path = Path.Combine(OutputDir, $"01-tip-{tag}.png"), FullPage = true });

            var entry = ReferenceVerificationDemoIds.WorkEntryBakkerij;
            await page.GotoAsync($"{baseUrl}/candidate/paspoort/referentie/vraag/{entry:D}", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForSelectorAsync(".ref-verif-request", new() { Timeout = 15_000 });
            await page.ScreenshotAsync(new() { Path = Path.Combine(OutputDir, $"02-request-form-{tag}.png"), FullPage = true });

            await page.GotoAsync($"{baseUrl}/candidate/paspoort?tab=references", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForSelectorAsync("[data-testid='passport-references-tab']", new() { Timeout = 15_000 });
            await page.ScreenshotAsync(new() { Path = Path.Combine(OutputDir, $"05-status-table-{tag}.png"), FullPage = true });

            var detailId = ReferenceVerificationDemoIds.ConfirmedRequestId;
            await page.GotoAsync($"{baseUrl}/candidate/paspoort/referenties/{detailId:D}", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForSelectorAsync("[data-testid='ref-verif-detail']", new() { Timeout = 15_000 });
            await page.ScreenshotAsync(new() { Path = Path.Combine(OutputDir, $"06-bekijken-detail-{tag}.png"), FullPage = true });

            await page.GotoAsync($"{baseUrl}/candidate/paspoort?tab=dna", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForSelectorAsync("[data-testid='dna-verified-competences']", new() { Timeout = 15_000 });
            await page.ScreenshotAsync(new() { Path = Path.Combine(OutputDir, $"07-dna-verified-unverified-{tag}.png"), FullPage = true });

            await page.GotoAsync($"{baseUrl}/candidate/paspoort", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForSelectorAsync("[data-testid='ref-verif-verified-competences']", new() { Timeout = 15_000 });
            await page.ScreenshotAsync(new() { Path = Path.Combine(OutputDir, $"08-paspoort-endorsed-only-{tag}.png"), FullPage = true });

            await context.CloseAsync();
        }

        // Public referent pages (anonymous).
        foreach (var (width, height, tag) in Viewports)
        {
            await using var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IgnoreHTTPSErrors = true
            });
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();

            var openToken = ReferenceVerificationDemoIds.OpenReferentToken;
            await page.GotoAsync($"{baseUrl}/werk-referentie/{openToken}", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForSelectorAsync(".ref-verif-public", new() { Timeout = 15_000 });
            await page.ScreenshotAsync(new() { Path = Path.Combine(OutputDir, $"03-referent-empty-{tag}.png"), FullPage = true });

            await page.Locator("input[name='emp']").First.ClickAsync();
            await page.Locator("input[name='period']").First.ClickAsync();
            await page.Locator("input[name='role']").First.ClickAsync();
            var checks = page.Locator(".ref-verif-public input[type='checkbox']");
            var count = await checks.CountAsync();
            for (var i = 0; i < Math.Min(3, count); i++)
            {
                await checks.Nth(i).CheckAsync();
            }

            await page.Locator("textarea").FillAsync("Korte toelichting voor Sanne — alleen zij ziet dit.");
            await page.ScreenshotAsync(new() { Path = Path.Combine(OutputDir, $"04-referent-filled-{tag}.png"), FullPage = true });

            var expired = ReferenceVerificationDemoIds.ExpiredReferentToken;
            await page.GotoAsync($"{baseUrl}/werk-referentie/{expired}", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForSelectorAsync(".ref-verif-public", new() { Timeout = 15_000 });
            await page.ScreenshotAsync(new() { Path = Path.Combine(OutputDir, $"09-referent-expired-{tag}.png"), FullPage = true });

            await context.CloseAsync();
        }

        var pngs = Directory.GetFiles(OutputDir, "*.png");
        Assert.True(pngs.Length >= 18, $"Expected at least 18 screenshots, got {pngs.Length}.");
    }

    private static async Task LoginAsync(IPage page, string baseUrl, string email)
    {
        await page.GotoAsync($"{baseUrl}/login?returnUrl=%2Fcandidate%2Fprofile", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        var emailInput = page.Locator("input[type=email], input[name=email], #login-email").First;
        await emailInput.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await emailInput.FillAsync(email);
        await page.Locator("input[type=password], input[name=password]").First.FillAsync(Password);
        await page.Locator("button.login-submit, button.au-submit[type=submit], button[type=submit]").First.ClickAsync();
        await page.WaitForURLAsync(
            url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 60_000 });
    }
}
