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
    private const string EnvRunR4 = "JOBSY_E2E_REF_VERIF_SCREENSHOTS_R4";
    private const string Password = "Jobsy123!";
    private const string Email = ReferenceVerificationDemoIds.Email;

    private static readonly (int Width, int Height, string Tag)[] Viewports =
    [
        (1440, 900, "1440"),
        (390, 844, "390")
    ];

    private static string OutputDir => "/cursor/stores/self/references";

    private static string OutputDirR4 => "/opt/cursor/artifacts/references-r4";

    private static bool ShouldRun =>
        string.Equals(Environment.GetEnvironmentVariable(EnvRun), "1", StringComparison.Ordinal);

    private static bool ShouldRunR4 =>
        string.Equals(Environment.GetEnvironmentVariable(EnvRunR4), "1", StringComparison.Ordinal);

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
            await page.WaitForSelectorAsync(
                "[data-testid='ref-verification-tip'], [data-testid='ref-verification-tip-followup']",
                new() { Timeout = 15_000 });
            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(page, Path.Combine(OutputDir, $"01-tip-{tag}.png"));

            await CaptureRequestFormAsync(page, baseUrl, tag, OutputDir);

            await page.GotoAsync($"{baseUrl}/candidate/paspoort?tab=references", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForSelectorAsync("[data-testid='passport-references-tab']", new() { Timeout = 15_000 });
            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(page, Path.Combine(OutputDir, $"05-status-table-{tag}.png"));

            var detailId = ReferenceVerificationDemoIds.ConfirmedRequestId;
            await page.GotoAsync($"{baseUrl}/candidate/paspoort/referenties/{detailId:D}", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForSelectorAsync("[data-testid='ref-verif-detail']", new() { Timeout = 15_000 });
            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(page, Path.Combine(OutputDir, $"06-bekijken-detail-{tag}.png"));

            await page.GotoAsync($"{baseUrl}/candidate/paspoort?tab=dna", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForSelectorAsync(".passport-dna", new() { Timeout = 15_000 });
            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(page, Path.Combine(OutputDir, $"07-dna-verified-unverified-{tag}.png"));

            await page.GotoAsync($"{baseUrl}/candidate/paspoort", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForSelectorAsync("[data-testid='passport-employer-preview']", new() { Timeout = 15_000 });
            await page.Locator("[data-testid='passport-employer-preview']").ScrollIntoViewIfNeededAsync();
            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(page, Path.Combine(OutputDir, $"08-paspoort-endorsed-only-{tag}.png"));

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
            await page.WaitForSelectorAsync(".ref-verif-public__section", new() { Timeout = 15_000 });
            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(page, Path.Combine(OutputDir, $"03-referent-empty-{tag}.png"));

            var factRows = page.Locator(".ref-verif-public__row:not(.ref-verif-public__row--stack)");
            var factRowCount = await factRows.CountAsync();
            for (var r = 0; r < factRowCount; r++)
            {
                await factRows.Nth(r).Locator(".ref-verif-pills__btn").First.ClickAsync();
            }

            var comps = page.Locator(".ref-verif-public__comp");
            var compCount = await comps.CountAsync();
            for (var i = 0; i < Math.Min(3, compCount); i++)
            {
                await comps.Nth(i).ClickAsync();
            }

            await page.Locator("#ref-comment").FillAsync("Korte toelichting voor Sanne — alleen zij ziet dit.");
            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(page, Path.Combine(OutputDir, $"04-referent-filled-{tag}.png"));

            var expired = ReferenceVerificationDemoIds.ExpiredReferentToken;
            await page.GotoAsync($"{baseUrl}/werk-referentie/{expired}", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForSelectorAsync(".ref-verif-public", new() { Timeout = 15_000 });
            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(page, Path.Combine(OutputDir, $"09-referent-expired-{tag}.png"));

            await context.CloseAsync();
        }

        var pngs = Directory.GetFiles(OutputDir, "*.png");
        Assert.True(pngs.Length >= 18, $"Expected at least 18 screenshots, got {pngs.Length}.");
    }

    [Fact]
    public async Task Capture_reference_verification_screenshots_r4()
    {
        if (!ShouldRunR4)
        {
            return;
        }

        var baseUrl = await KbE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            Assert.Fail("JOBSY_E2E_BASE_URL unreachable — start API :5200 and Web :5201 with seeded DB.");
        }

        Directory.CreateDirectory(OutputDirR4);
        foreach (var stale in Directory.GetFiles(OutputDirR4, "*.png"))
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

            await CaptureRequestFormAsync(page, baseUrl, tag, OutputDirR4);

            await page.GotoAsync($"{baseUrl}/candidate/paspoort?tab=references", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForSelectorAsync("[data-testid='passport-references-tab']", new() { Timeout = 15_000 });
            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(page, Path.Combine(OutputDirR4, $"05-status-table-{tag}.png"));

            await page.GotoAsync($"{baseUrl}/candidate/paspoort?tab=dna", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForSelectorAsync(".passport-dna", new() { Timeout = 15_000 });
            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(page, Path.Combine(OutputDirR4, $"07-dna-verified-unverified-{tag}.png"));

            await page.GotoAsync($"{baseUrl}/candidate/paspoort", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForSelectorAsync("[data-testid='passport-employer-preview']", new() { Timeout = 15_000 });
            await page.Locator("[data-testid='passport-employer-preview']").ScrollIntoViewIfNeededAsync();
            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(page, Path.Combine(OutputDirR4, $"08-paspoort-endorsed-only-{tag}.png"));

            await context.CloseAsync();
        }

        foreach (var (width, height, tag) in Viewports)
        {
            await using var context = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IgnoreHTTPSErrors = true
            });
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();

            var expired = ReferenceVerificationDemoIds.ExpiredReferentToken;
            await page.GotoAsync($"{baseUrl}/werk-referentie/{expired}", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForSelectorAsync(".ref-verif-public", new() { Timeout = 15_000 });
            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(page, Path.Combine(OutputDirR4, $"09-referent-expired-{tag}.png"));

            await context.CloseAsync();
        }

        var pngs = Directory.GetFiles(OutputDirR4, "*.png");
        Assert.Equal(10, pngs.Length);
    }

    private static async Task CaptureRequestFormAsync(IPage page, string baseUrl, string tag, string outputDir)
    {
        var entry = ReferenceVerificationDemoIds.WorkEntryBakkerij;
        await page.GotoAsync($"{baseUrl}/candidate/paspoort/referentie/vraag/{entry:D}", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForSelectorAsync(".ref-verif-request__grid", new() { Timeout = 15_000 });
        Assert.Equal(0, await page.Locator("text=Deze werkervaring is niet gevonden.").CountAsync());
        await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(page, Path.Combine(outputDir, $"02-request-form-{tag}.png"));
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
