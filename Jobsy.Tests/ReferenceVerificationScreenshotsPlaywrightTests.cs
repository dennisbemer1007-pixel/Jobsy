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
    private const string EnvRunR5 = "JOBSY_E2E_REF_VERIF_SCREENSHOTS_R5";
    private const string EnvRunR6 = "JOBSY_E2E_REF_VERIF_SCREENSHOTS_R6";
    private const string EnvRunR7 = "JOBSY_E2E_REF_VERIF_SCREENSHOTS_R7";
    private const string EnvRunR8 = "JOBSY_E2E_REF_VERIF_SCREENSHOTS_R8";
    private const string EnvRunR9 = "JOBSY_E2E_REF_VERIF_SCREENSHOTS_R9";
    private const string EnvRunR10 = "JOBSY_E2E_REF_VERIF_SCREENSHOTS_R10";
    private const string EnvRunR11 = "JOBSY_E2E_REF_VERIF_SCREENSHOTS_R11";
    private const string EnvRunR12 = "JOBSY_E2E_REF_VERIF_SCREENSHOTS_R12";
    private const string EnvRunR13 = "JOBSY_E2E_REF_VERIF_SCREENSHOTS_R13";
    private const string EnvRunMobhero = "JOBSY_E2E_REF_VERIF_SCREENSHOTS_MOBHERO";
    private const string EnvRunR14 = "JOBSY_E2E_REF_VERIF_SCREENSHOTS_R14";
    private const string EnvRunR15 = "JOBSY_E2E_REF_VERIF_SCREENSHOTS_R15";
    private const string EnvRunR16 = "JOBSY_E2E_REF_VERIF_SCREENSHOTS_R16";
    private const string EnvRunR17 = "JOBSY_E2E_REF_VERIF_SCREENSHOTS_R17";
    private const string EnvRunR18 = "JOBSY_E2E_REF_VERIF_SCREENSHOTS_R18";
    private const string EnvRunFinal = "JOBSY_E2E_REF_VERIF_SCREENSHOTS_FINAL";
    private const string EnvRunFinalR2 = "JOBSY_E2E_REF_VERIF_SCREENSHOTS_FINAL_R2";
    private const string Password = "Jobsy123!";
    private const string Email = ReferenceVerificationDemoIds.Email;

    private static readonly (int Width, int Height, string Tag)[] Viewports =
    [
        (1440, 900, "1440"),
        (390, 844, "390")
    ];

    private static string OutputDir => "/cursor/stores/self/references";

    private static string OutputDirR4 => "/opt/cursor/artifacts/references-r4";

    private static string OutputDirR5 => "/opt/cursor/artifacts/references-r5";

    private static string OutputDirR6 => "/opt/cursor/artifacts/references-r6";

    private static string OutputDirR7 => "/opt/cursor/artifacts/references-r7";

    private static string OutputDirR8 => "/opt/cursor/artifacts/references-r8";

    private static string OutputDirR9 => "/opt/cursor/artifacts/references-r9";

    private static string OutputDirR10 => "/opt/cursor/artifacts/references-r10";

    private static string OutputDirR11 => "/opt/cursor/artifacts/references-r11";

    private static string OutputDirR12 => "/opt/cursor/artifacts/references-r12";

    private static string OutputDirR13 => "/opt/cursor/artifacts/references-r13";

    private static string OutputDirMobhero => "/opt/cursor/artifacts/references-mobhero";

    private static string OutputDirR14 => "/opt/cursor/artifacts/references-r14";

    private static string OutputDirR15 => "/opt/cursor/artifacts/references-r15";

    private static string OutputDirR16 => "/opt/cursor/artifacts/references-r16";

    private static string OutputDirR17 => "/opt/cursor/artifacts/references-r17";

    private static string OutputDirR18 => "/opt/cursor/artifacts/references-r18";

    private static string OutputDirFinal => "/opt/cursor/artifacts/references-final";

    private static string OutputDirFinalR2 => "/opt/cursor/artifacts/references-final-r2";

    private static bool ShouldRun =>
        string.Equals(Environment.GetEnvironmentVariable(EnvRun), "1", StringComparison.Ordinal);

    private static bool ShouldRunR4 =>
        string.Equals(Environment.GetEnvironmentVariable(EnvRunR4), "1", StringComparison.Ordinal);

    private static bool ShouldRunR5 =>
        string.Equals(Environment.GetEnvironmentVariable(EnvRunR5), "1", StringComparison.Ordinal);

    private static bool ShouldRunR6 =>
        string.Equals(Environment.GetEnvironmentVariable(EnvRunR6), "1", StringComparison.Ordinal);

    private static bool ShouldRunR7 =>
        string.Equals(Environment.GetEnvironmentVariable(EnvRunR7), "1", StringComparison.Ordinal);

    private static bool ShouldRunR8 =>
        string.Equals(Environment.GetEnvironmentVariable(EnvRunR8), "1", StringComparison.Ordinal);

    private static bool ShouldRunR9 =>
        string.Equals(Environment.GetEnvironmentVariable(EnvRunR9), "1", StringComparison.Ordinal);

    private static bool ShouldRunR10 =>
        string.Equals(Environment.GetEnvironmentVariable(EnvRunR10), "1", StringComparison.Ordinal);

    private static bool ShouldRunR11 =>
        string.Equals(Environment.GetEnvironmentVariable(EnvRunR11), "1", StringComparison.Ordinal);

    private static bool ShouldRunR12 =>
        string.Equals(Environment.GetEnvironmentVariable(EnvRunR12), "1", StringComparison.Ordinal);

    private static bool ShouldRunR13 =>
        string.Equals(Environment.GetEnvironmentVariable(EnvRunR13), "1", StringComparison.Ordinal);

    private static bool ShouldRunMobhero =>
        string.Equals(Environment.GetEnvironmentVariable(EnvRunMobhero), "1", StringComparison.Ordinal);

    private static bool ShouldRunR14 =>
        string.Equals(Environment.GetEnvironmentVariable(EnvRunR14), "1", StringComparison.Ordinal);

    private static bool ShouldRunR15 =>
        string.Equals(Environment.GetEnvironmentVariable(EnvRunR15), "1", StringComparison.Ordinal);

    private static bool ShouldRunR16 =>
        string.Equals(Environment.GetEnvironmentVariable(EnvRunR16), "1", StringComparison.Ordinal);

    private static bool ShouldRunR17 =>
        string.Equals(Environment.GetEnvironmentVariable(EnvRunR17), "1", StringComparison.Ordinal);

    private static bool ShouldRunR18 =>
        string.Equals(Environment.GetEnvironmentVariable(EnvRunR18), "1", StringComparison.Ordinal);

    private static bool ShouldRunFinal =>
        string.Equals(Environment.GetEnvironmentVariable(EnvRunFinal), "1", StringComparison.Ordinal);

    private static bool ShouldRunFinalR2 =>
        string.Equals(Environment.GetEnvironmentVariable(EnvRunFinalR2), "1", StringComparison.Ordinal);

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

    [Fact]
    public async Task Capture_reference_verification_screenshots_r5()
    {
        if (!ShouldRunR5)
        {
            return;
        }

        var baseUrl = await KbE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            Assert.Fail("JOBSY_E2E_BASE_URL unreachable — start API :5200 and Web :5201 with seeded DB.");
        }

        Directory.CreateDirectory(OutputDirR5);
        foreach (var stale in Directory.GetFiles(OutputDirR5, "*.png"))
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

            await CaptureRequestFormAsync(page, baseUrl, tag, OutputDirR5);

            await page.GotoAsync($"{baseUrl}/candidate/paspoort", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForSelectorAsync("[data-testid='passport-employer-preview']", new() { Timeout = 15_000 });
            await page.Locator("[data-testid='passport-employer-preview']").ScrollIntoViewIfNeededAsync();
            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(page, Path.Combine(OutputDirR5, $"08-paspoort-endorsed-only-{tag}.png"));

            await context.CloseAsync();
        }

        var pngs = Directory.GetFiles(OutputDirR5, "*.png");
        Assert.Equal(4, pngs.Length);
    }

    [Fact]
    public async Task Capture_reference_verification_screenshots_r6()
    {
        if (!ShouldRunR6)
        {
            return;
        }

        var baseUrl = await KbE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            Assert.Fail("JOBSY_E2E_BASE_URL unreachable — start API :5200 and Web :5201 with seeded DB.");
        }

        Directory.CreateDirectory(OutputDirR6);
        foreach (var stale in Directory.GetFiles(OutputDirR6, "*.png"))
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
            await CaptureRequestFormAsync(page, baseUrl, tag, OutputDirR6);
            await context.CloseAsync();
        }

        var pngs = Directory.GetFiles(OutputDirR6, "*.png");
        Assert.Equal(2, pngs.Length);
    }

    [Fact]
    public async Task Capture_reference_verification_screenshots_r7()
    {
        if (!ShouldRunR7)
        {
            return;
        }

        var baseUrl = await KbE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            Assert.Fail("JOBSY_E2E_BASE_URL unreachable — start API :5200 and Web :5201 with seeded DB.");
        }

        Directory.CreateDirectory(OutputDirR7);
        foreach (var stale in Directory.GetFiles(OutputDirR7))
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

            await CaptureRequestFormAndConfirmAsync(page, baseUrl, tag, OutputDirR7);

            var open = ReferenceVerificationDemoIds.OpenReferentToken;
            await page.GotoAsync($"{baseUrl}/werk-referentie/{open}", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForSelectorAsync("[data-testid='ref-verif-public-open']", new() { Timeout = 20_000 });
            await page.WaitForSelectorAsync(".ref-verif-lift-card--visible", new() { Timeout = 5_000 });
            await Task.Delay(300);
            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(page, Path.Combine(OutputDirR7, $"04-referent-public-{tag}.png"));

            await page.GotoAsync($"{baseUrl}/candidate/paspoort", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForSelectorAsync("[data-testid='passport-employer-preview']", new() { Timeout = 15_000 });
            await page.Locator("[data-testid='passport-employer-preview']").ScrollIntoViewIfNeededAsync();
            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(page, Path.Combine(OutputDirR7, $"08-paspoort-endorsed-{tag}.png"));

            await context.CloseAsync();
        }

        await using (var videoContext = await browser.NewContextAsync(new()
                     {
                         ViewportSize = new() { Width = 1440, Height = 900 },
                         IgnoreHTTPSErrors = true,
                         RecordVideoDir = OutputDirR7,
                         RecordVideoSize = new() { Width = 1440, Height = 900 }
                     }))
        {
            await PlaywrightCookieConsent.AcceptAsync(videoContext);
            var page = await videoContext.NewPageAsync();
            await LoginAsync(page, baseUrl, Email);
            var entryAh = ReferenceVerificationDemoIds.WorkEntryAh;
            await page.GotoAsync($"{baseUrl}/candidate/paspoort/referentie/vraag/{entryAh:D}", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForSelectorAsync(".ref-verif-request__grid", new() { Timeout = 15_000 });
            await page.Locator("#ref-name").FillAsync("Animatie Demo");
            await page.Locator("#ref-email").FillAsync($"anim.{Guid.NewGuid():N}"[..18] + "@demo.local");
            await page.Locator("#ref-consent").CheckAsync();
            await page.Locator(".ref-verif-request__submit").ClickAsync();
            await page.WaitForSelectorAsync("[data-testid='ref-verif-request-sent']", new() { Timeout = 20_000 });
            await Task.Delay(700);
            await videoContext.CloseAsync();
        }

        var pngs = Directory.GetFiles(OutputDirR7, "*.png");
        Assert.Equal(8, pngs.Length);

        var webm = Directory.GetFiles(OutputDirR7, "*.webm").OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
        if (webm is not null)
        {
            var dest = Path.Combine(OutputDirR7, "03-request-confirm-animation.webm");
            if (!string.Equals(webm, dest, StringComparison.Ordinal))
            {
                File.Move(webm, dest, overwrite: true);
            }
        }

    }

    [Fact]
    public async Task Capture_reference_verification_screenshots_r8()
    {
        if (!ShouldRunR8)
        {
            return;
        }

        var baseUrl = await KbE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            Assert.Fail("JOBSY_E2E_BASE_URL unreachable — start API :5200 and Web :5201 with seeded DB.");
        }

        DeleteLegacyReferenceArtifactDirs();

        Directory.CreateDirectory(OutputDirR8);
        foreach (var stale in Directory.GetFiles(OutputDirR8))
        {
            File.Delete(stale);
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        foreach (var (width, height, tag) in Viewports)
        {
            await using var candidateContext = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IgnoreHTTPSErrors = true
            });
            await PlaywrightCookieConsent.AcceptAsync(candidateContext);
            var candidatePage = await candidateContext.NewPageAsync();
            await LoginAsync(candidatePage, baseUrl, Email);
            await CaptureRequestFormAndConfirmAsync(candidatePage, baseUrl, tag, OutputDirR8, useRealisticDemoName: true);

            await candidatePage.GotoAsync($"{baseUrl}/candidate/paspoort", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await candidatePage.WaitForSelectorAsync("[data-testid='passport-employer-preview']", new() { Timeout = 15_000 });
            await candidatePage.Locator("[data-testid='passport-employer-preview']").ScrollIntoViewIfNeededAsync();
            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(
                candidatePage,
                Path.Combine(OutputDirR8, $"08-paspoort-endorsed-{tag}.png"));
            await candidateContext.CloseAsync();

            await using var publicContext = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IgnoreHTTPSErrors = true
            });
            await PlaywrightCookieConsent.AcceptAsync(publicContext);
            var publicPage = await publicContext.NewPageAsync();
            var open = ReferenceVerificationDemoIds.OpenReferentToken;
            await publicPage.GotoAsync($"{baseUrl}/werk-referentie/{open}", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await publicPage.WaitForSelectorAsync("[data-testid='ref-verif-public-open']", new() { Timeout = 20_000 });
            await publicPage.WaitForSelectorAsync(".pub-header", new() { Timeout = 10_000 });
            Assert.Equal(0, await publicPage.Locator(".bottom-nav, .candidate-shell").CountAsync());
            await Task.Delay(350);
            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(
                publicPage,
                Path.Combine(OutputDirR8, $"04-referent-public-{tag}.png"));
            await publicContext.CloseAsync();
        }

        var pngs = Directory.GetFiles(OutputDirR8, "*.png");
        Assert.Equal(8, pngs.Length);
        Assert.InRange(pngs.Length, 1, 10);
    }

    [Fact]
    public async Task Capture_reference_verification_screenshots_r9()
    {
        if (!ShouldRunR9)
        {
            return;
        }

        var baseUrl = await KbE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            Assert.Fail("JOBSY_E2E_BASE_URL unreachable — start API :5200 and Web :5201 with seeded DB.");
        }

        DeleteLegacyReferenceArtifactDirs(includeR8: true);

        Directory.CreateDirectory(OutputDirR9);
        foreach (var stale in Directory.GetFiles(OutputDirR9))
        {
            File.Delete(stale);
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        foreach (var (width, height, tag) in Viewports)
        {
            await using var candidateContext = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IgnoreHTTPSErrors = true
            });
            await PlaywrightCookieConsent.AcceptAsync(candidateContext);
            var candidatePage = await candidateContext.NewPageAsync();
            await LoginAsync(candidatePage, baseUrl, Email);
            await CaptureRequestFormAndConfirmAsync(candidatePage, baseUrl, tag, OutputDirR9, useRealisticDemoName: true);

            await candidatePage.GotoAsync($"{baseUrl}/candidate/paspoort", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await candidatePage.WaitForSelectorAsync("[data-testid='passport-employer-preview']", new() { Timeout = 15_000 });
            await candidatePage.Locator("[data-testid='passport-employer-preview']").ScrollIntoViewIfNeededAsync();
            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(
                candidatePage,
                Path.Combine(OutputDirR9, $"08-paspoort-endorsed-{tag}.png"));
            await candidateContext.CloseAsync();

            await using var publicContext = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IgnoreHTTPSErrors = true
            });
            await PlaywrightCookieConsent.AcceptAsync(publicContext);
            var publicPage = await publicContext.NewPageAsync();
            var open = ReferenceVerificationDemoIds.OpenReferentToken;
            await publicPage.GotoAsync($"{baseUrl}/werk-referentie/{open}", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await publicPage.WaitForSelectorAsync("[data-testid='ref-verif-public-open']", new() { Timeout = 20_000 });
            await publicPage.WaitForSelectorAsync(".pub-header", new() { Timeout = 10_000 });
            await Task.Delay(350);
            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(
                publicPage,
                Path.Combine(OutputDirR9, $"04-referent-public-{tag}.png"));
            await publicContext.CloseAsync();
        }

        var pngs = Directory.GetFiles(OutputDirR9, "*.png");
        Assert.Equal(8, pngs.Length);
        Assert.InRange(pngs.Length, 1, 10);
    }

    [Fact]
    public async Task Capture_reference_verification_screenshots_r10()
    {
        if (!ShouldRunR10)
        {
            return;
        }

        var baseUrl = await KbE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            Assert.Fail("JOBSY_E2E_BASE_URL unreachable — start API :5200 and Web :5201 with seeded DB.");
        }

        DeleteLegacyReferenceArtifactDirs(includeR9: true);

        Directory.CreateDirectory(OutputDirR10);
        foreach (var stale in Directory.GetFiles(OutputDirR10))
        {
            File.Delete(stale);
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        foreach (var (width, height, tag) in Viewports)
        {
            await using var candidateContext = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IgnoreHTTPSErrors = true
            });
            await PlaywrightCookieConsent.AcceptAsync(candidateContext);
            var candidatePage = await candidateContext.NewPageAsync();
            await LoginAsync(candidatePage, baseUrl, Email);

            await candidatePage.GotoAsync($"{baseUrl}/candidate/paspoort", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await candidatePage.WaitForSelectorAsync("[data-testid='passport-employer-preview']", new() { Timeout = 15_000 });
            await candidatePage.Locator("[data-testid='passport-employer-preview']").ScrollIntoViewIfNeededAsync();
            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(
                candidatePage,
                Path.Combine(OutputDirR10, $"08-paspoort-endorsed-{tag}.png"));

            var requestPage = await candidateContext.NewPageAsync();
            await LoginAsync(requestPage, baseUrl, Email);
            await CaptureRequestFormAndConfirmAsync(requestPage, baseUrl, tag, OutputDirR10, useRealisticDemoName: true);
            await candidateContext.CloseAsync();

            await using var publicContext = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IgnoreHTTPSErrors = true
            });
            await PlaywrightCookieConsent.AcceptAsync(publicContext);
            var publicPage = await publicContext.NewPageAsync();
            var open = ReferenceVerificationDemoIds.OpenReferentToken;
            await publicPage.GotoAsync($"{baseUrl}/werk-referentie/{open}", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await publicPage.WaitForSelectorAsync("[data-testid='ref-verif-public-open']", new() { Timeout = 20_000 });
            await publicPage.WaitForSelectorAsync(".pub-header", new() { Timeout = 10_000 });
            await Task.Delay(350);
            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(
                publicPage,
                Path.Combine(OutputDirR10, $"04-referent-public-{tag}.png"));
            await publicContext.CloseAsync();
        }

        var pngs = Directory.GetFiles(OutputDirR10, "*.png");
        Assert.Equal(8, pngs.Length);
        Assert.InRange(pngs.Length, 1, 10);
    }

    [Fact]
    public async Task Capture_reference_verification_screenshots_r11()
    {
        if (!ShouldRunR11)
        {
            return;
        }

        var baseUrl = await KbE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            Assert.Fail("JOBSY_E2E_BASE_URL unreachable — start API :5200 and Web :5201 with seeded DB.");
        }

        DeleteLegacyReferenceArtifactDirs(includeR10: true);

        Directory.CreateDirectory(OutputDirR11);
        foreach (var stale in Directory.GetFiles(OutputDirR11))
        {
            File.Delete(stale);
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        foreach (var (width, height, tag) in Viewports)
        {
            await using var candidateContext = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IgnoreHTTPSErrors = true
            });
            await PlaywrightCookieConsent.AcceptAsync(candidateContext);
            var candidatePage = await candidateContext.NewPageAsync();
            await LoginAsync(candidatePage, baseUrl, Email);

            await candidatePage.GotoAsync($"{baseUrl}/candidate/paspoort", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await candidatePage.WaitForSelectorAsync("[data-testid='passport-employer-preview']", new() { Timeout = 15_000 });
            await candidatePage.WaitForSelectorAsync(".ref-verif-employer-preview__chip", new() { Timeout = 10_000 });
            await candidatePage.Locator("[data-testid='passport-employer-preview']").ScrollIntoViewIfNeededAsync();
            await Task.Delay(350);
            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(
                candidatePage,
                Path.Combine(OutputDirR11, $"08-paspoort-endorsed-{tag}.png"));

            await CaptureRequestFormAndConfirmAsync(candidatePage, baseUrl, tag, OutputDirR11, useRealisticDemoName: true);
            await candidateContext.CloseAsync();

            await using var publicContext = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IgnoreHTTPSErrors = true
            });
            await PlaywrightCookieConsent.AcceptAsync(publicContext);
            var publicPage = await publicContext.NewPageAsync();
            var open = ReferenceVerificationDemoIds.OpenReferentToken;
            await publicPage.GotoAsync($"{baseUrl}/werk-referentie/{open}", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await publicPage.WaitForSelectorAsync("[data-testid='ref-verif-public-open']", new() { Timeout = 20_000 });
            await publicPage.WaitForSelectorAsync(".ref-verif-gev-hero", new() { Timeout = 10_000 });
            await Task.Delay(350);
            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(
                publicPage,
                Path.Combine(OutputDirR11, $"04-referent-public-{tag}.png"));
            await publicContext.CloseAsync();
        }

        var pngs = Directory.GetFiles(OutputDirR11, "*.png");
        Assert.Equal(8, pngs.Length);
    }

    [Fact]
    public async Task Capture_reference_verification_screenshots_r12()
    {
        if (!ShouldRunR12)
        {
            return;
        }

        var baseUrl = await KbE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            Assert.Fail("JOBSY_E2E_BASE_URL unreachable — start API :5200 and Web :5201 with seeded DB.");
        }

        DeleteLegacyReferenceArtifactDirs(includeR11: true);

        Directory.CreateDirectory(OutputDirR12);
        foreach (var stale in Directory.GetFiles(OutputDirR12))
        {
            File.Delete(stale);
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        foreach (var (width, height, tag) in Viewports)
        {
            await using var candidateContext = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IgnoreHTTPSErrors = true
            });
            await PlaywrightCookieConsent.AcceptAsync(candidateContext);
            var candidatePage = await candidateContext.NewPageAsync();
            await LoginAsync(candidatePage, baseUrl, Email);

            await candidatePage.GotoAsync($"{baseUrl}/candidate/paspoort", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await candidatePage.WaitForSelectorAsync("[data-testid='passport-employer-preview']", new() { Timeout = 15_000 });
            await candidatePage.WaitForSelectorAsync(".ref-verif-employer-preview__chip", new() { Timeout = 10_000 });
            await candidatePage.Locator("[data-testid='passport-employer-preview']").ScrollIntoViewIfNeededAsync();
            await Task.Delay(350);
            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(
                candidatePage,
                Path.Combine(OutputDirR12, $"08-paspoort-endorsed-{tag}.png"));

            await CaptureRequestFormAndConfirmAsync(candidatePage, baseUrl, tag, OutputDirR12, useRealisticDemoName: true);
            await candidateContext.CloseAsync();

            await using var publicContext = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IgnoreHTTPSErrors = true
            });
            await PlaywrightCookieConsent.AcceptAsync(publicContext);
            var publicPage = await publicContext.NewPageAsync();
            var open = ReferenceVerificationDemoIds.OpenReferentToken;
            await publicPage.GotoAsync($"{baseUrl}/werk-referentie/{open}", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await publicPage.WaitForSelectorAsync("[data-testid='ref-verif-public-open']", new() { Timeout = 20_000 });
            await publicPage.WaitForSelectorAsync(".ref-verif-gev-hero", new() { Timeout = 10_000 });
            await Task.Delay(350);
            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(
                publicPage,
                Path.Combine(OutputDirR12, $"04-referent-public-{tag}.png"));
            await publicContext.CloseAsync();
        }

        var pngs = Directory.GetFiles(OutputDirR12, "*.png");
        Assert.Equal(8, pngs.Length);
    }

    [Fact]
    public async Task Capture_reference_verification_screenshots_r13()
    {
        if (!ShouldRunR13)
        {
            return;
        }

        var baseUrl = await KbE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            Assert.Fail("JOBSY_E2E_BASE_URL unreachable — start API :5200 and Web :5201 with seeded DB.");
        }

        DeleteLegacyReferenceArtifactDirs(includeR12: true);

        Directory.CreateDirectory(OutputDirR13);
        foreach (var stale in Directory.GetFiles(OutputDirR13))
        {
            File.Delete(stale);
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        foreach (var (width, height, tag) in Viewports)
        {
            await using var candidateContext = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IgnoreHTTPSErrors = true
            });
            await PlaywrightCookieConsent.AcceptAsync(candidateContext);
            var candidatePage = await candidateContext.NewPageAsync();
            await LoginAsync(candidatePage, baseUrl, Email);

            await candidatePage.GotoAsync($"{baseUrl}/candidate/paspoort", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await candidatePage.WaitForSelectorAsync("[data-testid='passport-employer-preview']", new() { Timeout = 15_000 });
            await candidatePage.WaitForSelectorAsync(".ref-verif-employer-preview__chip", new() { Timeout = 10_000 });
            await candidatePage.Locator("[data-testid='passport-employer-preview']").ScrollIntoViewIfNeededAsync();
            await Task.Delay(350);
            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(
                candidatePage,
                Path.Combine(OutputDirR13, $"08-paspoort-endorsed-{tag}.png"));

            await CaptureRequestFormAndConfirmAsync(candidatePage, baseUrl, tag, OutputDirR13, useRealisticDemoName: true);
            await candidateContext.CloseAsync();

            await using var publicContext = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IgnoreHTTPSErrors = true
            });
            await PlaywrightCookieConsent.AcceptAsync(publicContext);
            var publicPage = await publicContext.NewPageAsync();
            var open = ReferenceVerificationDemoIds.OpenReferentToken;
            await publicPage.GotoAsync($"{baseUrl}/werk-referentie/{open}", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await publicPage.WaitForSelectorAsync("[data-testid='ref-verif-public-open']", new() { Timeout = 20_000 });
            await publicPage.WaitForSelectorAsync(".ref-verif-gev-hero", new() { Timeout = 10_000 });
            await Task.Delay(350);
            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(
                publicPage,
                Path.Combine(OutputDirR13, $"04-referent-public-{tag}.png"));
            await publicContext.CloseAsync();
        }

        var pngs = Directory.GetFiles(OutputDirR13, "*.png");
        Assert.Equal(8, pngs.Length);
    }

    [Fact]
    public async Task Capture_reference_verification_screenshots_r14()
    {
        if (!ShouldRunR14)
        {
            return;
        }

        var baseUrl = await KbE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            Assert.Fail("JOBSY_E2E_BASE_URL unreachable — start API :5200 and Web :5201 with seeded DB.");
        }

        Directory.CreateDirectory(OutputDirR14);
        foreach (var stale in Directory.GetFiles(OutputDirR14))
        {
            File.Delete(stale);
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        foreach (var (width, height, tag) in Viewports)
        {
            await using var candidateContext = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IgnoreHTTPSErrors = true
            });
            await PlaywrightCookieConsent.AcceptAsync(candidateContext);
            var candidatePage = await candidateContext.NewPageAsync();
            await LoginAsync(candidatePage, baseUrl, Email);

            await candidatePage.GotoAsync($"{baseUrl}/candidate/paspoort", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await candidatePage.WaitForSelectorAsync("[data-testid='passport-employer-preview']", new() { Timeout = 15_000 });
            await candidatePage.Locator("[data-testid='passport-employer-preview']").ScrollIntoViewIfNeededAsync();
            await Task.Delay(350);
            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(
                candidatePage,
                Path.Combine(OutputDirR14, $"08-paspoort-endorsed-{tag}.png"));

            await CaptureRequestFormAndConfirmAsync(candidatePage, baseUrl, tag, OutputDirR14, useRealisticDemoName: true);
            if (tag == "390")
            {
                await AssertMobileInArtHeroAsync(candidatePage);
            }

            await candidateContext.CloseAsync();

            await using var publicContext = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IgnoreHTTPSErrors = true
            });
            await PlaywrightCookieConsent.AcceptAsync(publicContext);
            var publicPage = await publicContext.NewPageAsync();
            var open = ReferenceVerificationDemoIds.OpenReferentToken;
            await publicPage.GotoAsync($"{baseUrl}/werk-referentie/{open}", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await publicPage.WaitForSelectorAsync("[data-testid='ref-verif-public-open']", new() { Timeout = 20_000 });
            await publicPage.WaitForSelectorAsync(".ref-verif-gev-hero", new() { Timeout = 10_000 });
            if (tag == "390")
            {
                await AssertMobileInArtHeroAsync(publicPage);
                await publicPage.WaitForSelectorAsync(".ref-verif-public__hero-more", new() { Timeout = 5_000 });
            }

            await Task.Delay(350);
            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(
                publicPage,
                Path.Combine(OutputDirR14, $"04-referent-public-{tag}.png"));
            await publicContext.CloseAsync();
        }

        var pngs = Directory.GetFiles(OutputDirR14, "*.png");
        Assert.Equal(8, pngs.Length);
    }

    [Fact]
    public async Task Capture_reference_verification_screenshots_r15()
    {
        if (!ShouldRunR15)
        {
            return;
        }

        var baseUrl = await KbE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            Assert.Fail("JOBSY_E2E_BASE_URL unreachable — start API :5200 and Web :5201 with seeded DB.");
        }

        Directory.CreateDirectory(OutputDirR15);
        foreach (var stale in Directory.GetFiles(OutputDirR15))
        {
            File.Delete(stale);
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        foreach (var (width, height, tag) in Viewports)
        {
            await using var candidateContext = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IgnoreHTTPSErrors = true
            });
            await PlaywrightCookieConsent.AcceptAsync(candidateContext);
            var candidatePage = await candidateContext.NewPageAsync();
            await LoginAsync(candidatePage, baseUrl, Email);

            await candidatePage.GotoAsync($"{baseUrl}/candidate/paspoort", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await candidatePage.WaitForSelectorAsync("[data-testid='passport-employer-preview']", new() { Timeout = 15_000 });
            await candidatePage.Locator("[data-testid='passport-employer-preview']").ScrollIntoViewIfNeededAsync();
            await Task.Delay(350);
            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(
                candidatePage,
                Path.Combine(OutputDirR15, $"08-paspoort-endorsed-{tag}.png"));

            await CaptureRequestFormAndConfirmAsync(candidatePage, baseUrl, tag, OutputDirR15, useRealisticDemoName: true);
            if (tag == "390")
            {
                await AssertMobileInArtHeroAsync(candidatePage);
            }

            await candidateContext.CloseAsync();

            await using var publicContext = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IgnoreHTTPSErrors = true
            });
            await PlaywrightCookieConsent.AcceptAsync(publicContext);
            var publicPage = await publicContext.NewPageAsync();
            var open = ReferenceVerificationDemoIds.OpenReferentToken;
            await publicPage.GotoAsync($"{baseUrl}/werk-referentie/{open}", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await publicPage.WaitForSelectorAsync("[data-testid='ref-verif-public-open']", new() { Timeout = 20_000 });
            await publicPage.WaitForSelectorAsync(".ref-verif-gev-hero", new() { Timeout = 10_000 });
            if (tag == "390")
            {
                await AssertMobileInArtHeroAsync(publicPage);
                await publicPage.WaitForSelectorAsync(".ref-verif-public__hero-more", new() { Timeout = 5_000 });
            }

            await Task.Delay(350);
            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(
                publicPage,
                Path.Combine(OutputDirR15, $"04-referent-public-{tag}.png"));
            await publicContext.CloseAsync();
        }

        var pngs = Directory.GetFiles(OutputDirR15, "*.png");
        Assert.Equal(8, pngs.Length);
    }

    [Fact]
    public async Task Capture_reference_verification_screenshots_r16()
    {
        if (!ShouldRunR16)
        {
            return;
        }

        var baseUrl = await KbE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            Assert.Fail("JOBSY_E2E_BASE_URL unreachable — start API :5200 and Web :5201 with seeded DB.");
        }

        Directory.CreateDirectory(OutputDirR16);
        foreach (var stale in Directory.GetFiles(OutputDirR16))
        {
            File.Delete(stale);
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        foreach (var (width, height, tag) in Viewports)
        {
            await using var candidateContext = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IgnoreHTTPSErrors = true
            });
            await PlaywrightCookieConsent.AcceptAsync(candidateContext);
            var candidatePage = await candidateContext.NewPageAsync();
            await LoginAsync(candidatePage, baseUrl, Email);

            await candidatePage.GotoAsync($"{baseUrl}/candidate/paspoort", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await candidatePage.WaitForSelectorAsync("[data-testid='passport-employer-preview']", new() { Timeout = 15_000 });
            await candidatePage.Locator("[data-testid='passport-employer-preview']").ScrollIntoViewIfNeededAsync();
            await Task.Delay(350);
            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(
                candidatePage,
                Path.Combine(OutputDirR16, $"08-paspoort-endorsed-{tag}.png"));

            await CaptureRequestFormAndConfirmAsync(candidatePage, baseUrl, tag, OutputDirR16, useRealisticDemoName: true);
            if (tag == "390")
            {
                await AssertMobileInArtHeroAsync(candidatePage);
            }

            await candidateContext.CloseAsync();

            await using var publicContext = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IgnoreHTTPSErrors = true
            });
            await PlaywrightCookieConsent.AcceptAsync(publicContext);
            var publicPage = await publicContext.NewPageAsync();
            var open = ReferenceVerificationDemoIds.OpenReferentToken;
            await publicPage.GotoAsync($"{baseUrl}/werk-referentie/{open}", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await publicPage.WaitForSelectorAsync("[data-testid='ref-verif-public-open']", new() { Timeout = 20_000 });
            await publicPage.WaitForSelectorAsync(".ref-verif-gev-hero", new() { Timeout = 10_000 });
            if (tag == "390")
            {
                await AssertMobileInArtHeroAsync(publicPage);
                await publicPage.WaitForSelectorAsync(".ref-verif-public__hero-more", new() { Timeout = 5_000 });
            }

            await Task.Delay(350);
            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(
                publicPage,
                Path.Combine(OutputDirR16, $"04-referent-public-{tag}.png"));
            await publicContext.CloseAsync();
        }

        var pngs = Directory.GetFiles(OutputDirR16, "*.png");
        Assert.Equal(8, pngs.Length);
    }

    [Fact]
    public async Task Capture_reference_verification_screenshots_r17()
    {
        if (!ShouldRunR17)
        {
            return;
        }

        var baseUrl = await KbE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            Assert.Fail("JOBSY_E2E_BASE_URL unreachable — start API :5200 and Web :5201 with seeded DB.");
        }

        Directory.CreateDirectory(OutputDirR17);
        foreach (var stale in Directory.GetFiles(OutputDirR17))
        {
            File.Delete(stale);
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        foreach (var (width, height, tag) in Viewports)
        {
            await using var candidateContext = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IgnoreHTTPSErrors = true
            });
            await PlaywrightCookieConsent.AcceptAsync(candidateContext);
            var candidatePage = await candidateContext.NewPageAsync();
            await LoginAsync(candidatePage, baseUrl, Email);

            await candidatePage.GotoAsync($"{baseUrl}/candidate/paspoort", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await candidatePage.WaitForSelectorAsync("[data-testid='passport-employer-preview']", new() { Timeout = 15_000 });
            await candidatePage.Locator("[data-testid='passport-employer-preview']").ScrollIntoViewIfNeededAsync();
            await Task.Delay(350);
            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(
                candidatePage,
                Path.Combine(OutputDirR17, $"08-paspoort-endorsed-{tag}.png"));

            await CaptureRequestFormAndConfirmAsync(candidatePage, baseUrl, tag, OutputDirR17, useRealisticDemoName: true);
            if (tag == "390")
            {
                await AssertMobileGevoeligHeroAsync(candidatePage);
            }

            await candidateContext.CloseAsync();

            await using var publicContext = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IgnoreHTTPSErrors = true
            });
            await PlaywrightCookieConsent.AcceptAsync(publicContext);
            var publicPage = await publicContext.NewPageAsync();
            var open = ReferenceVerificationDemoIds.OpenReferentToken;
            await publicPage.GotoAsync($"{baseUrl}/werk-referentie/{open}", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await publicPage.WaitForSelectorAsync("[data-testid='ref-verif-public-open']", new() { Timeout = 20_000 });
            await publicPage.WaitForSelectorAsync(".ref-verif-gev-hero", new() { Timeout = 10_000 });
            if (tag == "390")
            {
                await AssertMobileGevoeligHeroAsync(publicPage);
                await publicPage.WaitForSelectorAsync(".ref-verif-public__hero-more", new() { Timeout = 5_000 });
            }

            await Task.Delay(350);
            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(
                publicPage,
                Path.Combine(OutputDirR17, $"04-referent-public-{tag}.png"));
            await publicContext.CloseAsync();
        }

        var pngs = Directory.GetFiles(OutputDirR17, "*.png");
        Assert.Equal(8, pngs.Length);
    }

    [Fact]
    public async Task Capture_reference_verification_screenshots_final()
    {
        if (!ShouldRunFinal)
        {
            return;
        }

        await Capture_reference_verification_screenshots_final_to_dir(OutputDirFinal);
    }

    [Fact]
    public async Task Capture_reference_verification_screenshots_final_r2()
    {
        if (!ShouldRunFinalR2)
        {
            return;
        }

        await Capture_reference_verification_screenshots_final_to_dir(OutputDirFinalR2);
    }

    private static async Task Capture_reference_verification_screenshots_final_to_dir(string outputDir)
    {
        var baseUrl = await KbE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            Assert.Fail("JOBSY_E2E_BASE_URL unreachable — start API :5200 and Web :5201 with seeded DB.");
        }

        Directory.CreateDirectory(outputDir);
        foreach (var stale in Directory.GetFiles(outputDir))
        {
            File.Delete(stale);
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        foreach (var (width, height, tag) in Viewports)
        {
            await using var formContext = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IgnoreHTTPSErrors = true
            });
            await PlaywrightCookieConsent.AcceptAsync(formContext);
            var formPage = await formContext.NewPageAsync();
            await LoginAsync(formPage, baseUrl, Email);
            await CaptureRequestFormScreenshotAsync(formPage, baseUrl, tag, outputDir);
            if (tag == "390")
            {
                await AssertMobileGevoeligHeroAsync(formPage);
            }

            await formContext.CloseAsync();
        }

        await using var submitContext = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(submitContext);
        var submitPage = await submitContext.NewPageAsync();
        await LoginAsync(submitPage, baseUrl, Email);
        await CaptureRequestSubmitAndConfirmScreenshotsAsync(
            submitPage, baseUrl, outputDir, useRealisticDemoName: true, alsoConfirmViewport: (390, 844));
        await submitContext.CloseAsync();

        foreach (var (width, height, tag) in Viewports)
        {
            await using var candidateContext = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IgnoreHTTPSErrors = true
            });
            await PlaywrightCookieConsent.AcceptAsync(candidateContext);
            var candidatePage = await candidateContext.NewPageAsync();
            await LoginAsync(candidatePage, baseUrl, Email);

            await candidatePage.GotoAsync($"{baseUrl}/candidate/paspoort", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await candidatePage.WaitForSelectorAsync("[data-testid='passport-employer-preview']", new() { Timeout = 15_000 });
            await candidatePage.Locator("[data-testid='passport-employer-preview']").ScrollIntoViewIfNeededAsync();
            await Task.Delay(350);
            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(
                candidatePage,
                Path.Combine(outputDir, $"08-paspoort-endorsed-{tag}.png"));

            await candidateContext.CloseAsync();

            await using var publicContext = await browser.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                IgnoreHTTPSErrors = true
            });
            await PlaywrightCookieConsent.AcceptAsync(publicContext);
            var publicPage = await publicContext.NewPageAsync();
            var open = ReferenceVerificationDemoIds.OpenReferentToken;
            await publicPage.GotoAsync($"{baseUrl}/werk-referentie/{open}", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await publicPage.WaitForSelectorAsync("[data-testid='ref-verif-public-open']", new() { Timeout = 20_000 });
            await publicPage.WaitForSelectorAsync(".ref-verif-gev-hero", new() { Timeout = 10_000 });
            if (tag == "390")
            {
                await AssertMobileGevoeligHeroAsync(publicPage);
                await publicPage.WaitForSelectorAsync(".ref-verif-public__hero-more", new() { Timeout = 5_000 });
            }

            await Task.Delay(350);
            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(
                publicPage,
                Path.Combine(outputDir, $"04-referent-public-{tag}.png"));
            await publicContext.CloseAsync();
        }

        var pngs = Directory.GetFiles(outputDir, "*.png");
        Assert.Equal(8, pngs.Length);
    }

    [Fact]
    public async Task Capture_reference_verification_public_mobile_r18()
    {
        if (!ShouldRunR18)
        {
            return;
        }

        var baseUrl = await KbE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            Assert.Fail("JOBSY_E2E_BASE_URL unreachable — start API :5200 and Web :5201 with seeded DB.");
        }

        Directory.CreateDirectory(OutputDirR18);
        foreach (var stale in Directory.GetFiles(OutputDirR18))
        {
            File.Delete(stale);
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var publicContext = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(publicContext);
        var publicPage = await publicContext.NewPageAsync();
        var open = ReferenceVerificationDemoIds.OpenReferentToken;
        await publicPage.GotoAsync($"{baseUrl}/werk-referentie/{open}", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await publicPage.WaitForSelectorAsync("[data-testid='ref-verif-public-open']", new() { Timeout = 20_000 });
        await publicPage.WaitForSelectorAsync(".ref-verif-public__intro-short", new() { Timeout = 10_000 });
        await AssertMobilePublicOpenHeroAsync(publicPage);
        await Task.Delay(350);
        await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(
            publicPage,
            Path.Combine(OutputDirR18, "04-referent-public-390.png"));
    }

    [Fact]
    public async Task Capture_reference_verification_screenshots_mobhero()
    {
        if (!ShouldRunMobhero)
        {
            return;
        }

        var baseUrl = await KbE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            Assert.Fail("JOBSY_E2E_BASE_URL unreachable — start API :5200 and Web :5201 with seeded DB.");
        }

        Directory.CreateDirectory(OutputDirMobhero);
        foreach (var stale in Directory.GetFiles(OutputDirMobhero))
        {
            File.Delete(stale);
        }

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        const int width = 390;
        const int height = 844;
        const string tag = "390";

        await using var candidateContext = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(candidateContext);
        var candidatePage = await candidateContext.NewPageAsync();
        await LoginAsync(candidatePage, baseUrl, Email);

        await candidatePage.GotoAsync($"{baseUrl}/candidate/paspoort", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await candidatePage.WaitForSelectorAsync("[data-testid='passport-employer-preview']", new() { Timeout = 15_000 });
        await candidatePage.Locator("[data-testid='passport-employer-preview']").ScrollIntoViewIfNeededAsync();
        await Task.Delay(350);
        await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(
            candidatePage,
            Path.Combine(OutputDirMobhero, $"08-paspoort-endorsed-{tag}.png"));

        await CaptureRequestFormAndConfirmAsync(candidatePage, baseUrl, tag, OutputDirMobhero, useRealisticDemoName: true);
        await AssertMobileInArtHeroAsync(candidatePage);
        await candidateContext.CloseAsync();

        await using var publicContext = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(publicContext);
        var publicPage = await publicContext.NewPageAsync();
        var open = ReferenceVerificationDemoIds.OpenReferentToken;
        await publicPage.GotoAsync($"{baseUrl}/werk-referentie/{open}", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await publicPage.WaitForSelectorAsync("[data-testid='ref-verif-public-open']", new() { Timeout = 20_000 });
        await publicPage.WaitForSelectorAsync(".ref-verif-gev-hero", new() { Timeout = 10_000 });
        await AssertMobileInArtHeroAsync(publicPage);
        await Task.Delay(350);
        await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(
            publicPage,
            Path.Combine(OutputDirMobhero, $"04-referent-public-{tag}.png"));
        await publicContext.CloseAsync();

        var pngs = Directory.GetFiles(OutputDirMobhero, "*.png");
        Assert.Equal(4, pngs.Length);
    }

    private static async Task AssertMobilePublicOpenHeroAsync(IPage page)
    {
        await AssertMobileGevoeligHeroAsync(page);
        Assert.Equal(0, await page.Locator(".ref-verif-gev-hero--public-open-split .ref-verif-gev-hero__lead:visible").CountAsync());
        var hero = page.Locator(".ref-verif-gev-hero").First;
        var heroBox = await hero.BoundingBoxAsync();
        Assert.NotNull(heroBox);
        var introShort = page.Locator(".ref-verif-public__intro-short");
        await introShort.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5_000 });
        var introBox = await introShort.BoundingBoxAsync();
        Assert.NotNull(introBox);
        Assert.True(introBox!.Y >= heroBox!.Y + heroBox.Height - 2, "Intro copy should sit below the hero band.");
    }

    private static async Task AssertMobileGevoeligHeroAsync(IPage page)
    {
        var hero = page.Locator(".ref-verif-gev-hero").First;
        await hero.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });
        var box = await hero.BoundingBoxAsync();
        Assert.NotNull(box);
        Assert.InRange(box!.Height, 170, 265);

        var media = page.Locator(".ref-verif-gev-hero__art");
        Assert.True(await media.IsVisibleAsync());
        var artBox = await media.BoundingBoxAsync();
        Assert.NotNull(artBox);
        Assert.True(artBox!.Width >= box.Width * 0.9, "Gevoelig mobhero: art layer spans hero width.");
        Assert.True(artBox.Height >= box.Height * 0.9, "Gevoelig mobhero: art layer spans hero height.");

        var copy = page.Locator(".ref-verif-gev-hero__copy");
        if (await copy.CountAsync() == 0 || !await copy.IsVisibleAsync())
        {
            return;
        }

        var copyBox = await copy.BoundingBoxAsync();
        Assert.NotNull(copyBox);
        Assert.True(copyBox!.Y >= box.Y - 1, "Hero copy should sit inside the band (top).");
        Assert.True(copyBox.Y + copyBox.Height <= box.Y + box.Height + 2, "Hero copy should not extend below the band.");
        Assert.True(
            copyBox.X + copyBox.Width <= box.X + box.Width * 0.72,
            "Gevoelig mobhero: copy stays in left ~70% lane.");
    }

    private static async Task AssertMobileInArtHeroAsync(IPage page)
    {
        var hero = page.Locator(".ref-verif-gev-hero").First;
        await hero.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10_000 });
        var box = await hero.BoundingBoxAsync();
        Assert.NotNull(box);
        Assert.InRange(box!.Height, 80, 265);

        var media = page.Locator(".ref-verif-gev-hero__art");
        if (await media.CountAsync() > 0)
        {
            Assert.True(await media.IsVisibleAsync());
            var artBox = await media.BoundingBoxAsync();
            Assert.NotNull(artBox);
            Assert.True(artBox!.Width <= box.Width * 0.48, "Figure lane should stay within ~45% width.");
            Assert.True(artBox.X >= box.Width * 0.52, "Figures should anchor on the right.");
        }

        var copy = page.Locator(".ref-verif-gev-hero__copy");
        if (await copy.CountAsync() == 0 || !await copy.IsVisibleAsync())
        {
            return;
        }

        var copyBox = await copy.BoundingBoxAsync();
        Assert.NotNull(copyBox);
        Assert.True(copyBox!.Y >= box.Y - 1, "Hero copy should sit inside the band (top).");
        Assert.True(copyBox.Y + copyBox.Height <= box.Y + box.Height + 2, "Hero copy should not extend below the band.");
        Assert.True(
            copyBox.X + copyBox.Width <= box.X + box.Width * 0.58,
            $"Copy should stay in the left text lane (copy right={copyBox.X + copyBox.Width:F0}, hero right={box.X + box.Width:F0}).");
    }

    private static void DeleteLegacyReferenceArtifactDirs(
        bool includeR8 = false,
        bool includeR9 = false,
        bool includeR10 = false,
        bool includeR11 = false,
        bool includeR12 = false)
    {
        var dirs = new List<string>
        {
            "/opt/cursor/artifacts/references-r7",
            "/opt/cursor/artifacts/references",
            OutputDir
        };
        if (includeR8)
        {
            dirs.Add(OutputDirR8);
        }

        if (includeR9)
        {
            dirs.Add(OutputDirR9);
        }

        if (includeR10)
        {
            dirs.Add(OutputDirR10);
        }

        if (includeR11)
        {
            dirs.Add(OutputDirR11);
        }

        if (includeR12)
        {
            dirs.Add(OutputDirR12);
        }

        foreach (var dir in dirs)
        {
            if (!Directory.Exists(dir))
            {
                continue;
            }

            foreach (var file in Directory.GetFiles(dir))
            {
                File.Delete(file);
            }

            try
            {
                Directory.Delete(dir);
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }

    private static async Task CaptureRequestFormAndConfirmAsync(
        IPage page,
        string baseUrl,
        string tag,
        string outputDir,
        bool useRealisticDemoName = false,
        Guid? workEntryId = null)
    {
        await CaptureRequestFormScreenshotAsync(page, baseUrl, tag, outputDir, workEntryId);
        await CaptureRequestSubmitAndConfirmScreenshotsAsync(
            page, baseUrl, outputDir, useRealisticDemoName, workEntryId, confirmTag: tag);
    }

    private static async Task CaptureRequestFormScreenshotAsync(
        IPage page,
        string baseUrl,
        string tag,
        string outputDir,
        Guid? workEntryId = null)
    {
        var entryId = workEntryId ?? ReferenceVerificationDemoIds.WorkEntryAh;
        await page.GotoAsync($"{baseUrl}/candidate/paspoort/referentie/vraag/{entryId:D}", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForSelectorAsync(".ref-verif-request-layout--ready", new() { Timeout = 20_000 });
        await page.WaitForSelectorAsync(".ref-verif-gev-hero", new() { Timeout = 15_000 });
        await page.WaitForSelectorAsync(".ref-verif-request__card .ref-verif-request__grid", new() { Timeout = 15_000 });
        await page.WaitForSelectorAsync(".ref-verif-request-aside", new() { Timeout = 10_000 });
        await Task.Delay(300);
        await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(page, Path.Combine(outputDir, $"02-request-form-{tag}.png"));
    }

    private static async Task CaptureRequestSubmitAndConfirmScreenshotsAsync(
        IPage page,
        string baseUrl,
        string outputDir,
        bool useRealisticDemoName = false,
        Guid? workEntryId = null,
        string confirmTag = "1440",
        (int Width, int Height)? alsoConfirmViewport = null)
    {
        var entryId = workEntryId ?? ReferenceVerificationDemoIds.WorkEntryAh;
        if (!page.Url.Contains(entryId.ToString("D"), StringComparison.OrdinalIgnoreCase))
        {
            await page.GotoAsync($"{baseUrl}/candidate/paspoort/referentie/vraag/{entryId:D}", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForSelectorAsync(".ref-verif-request-layout--ready", new() { Timeout = 20_000 });
        }

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var referentName = useRealisticDemoName ? "Mark de Boer" : "Screenshot Referent";
        await page.Locator("#ref-name").PressSequentiallyAsync(referentName, new() { Delay = 35 });
        await page.Locator("#ref-email").PressSequentiallyAsync(
            useRealisticDemoName ? $"mark.de.boer.{suffix}@demo.local" : $"shot.{suffix}@demo.local",
            new() { Delay = 25 });
        await page.Locator("label[for='ref-consent']").ClickAsync();
        await Task.Delay(500);
        var submit = page.Locator(".ref-verif-request__submit");
        var responseTask = page.WaitForResponseAsync(
            r => r.Url.Contains("reference-verification", StringComparison.OrdinalIgnoreCase)
                 && string.Equals(r.Request.Method, "POST", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 45_000 });
        await submit.ClickAsync();
        try
        {
            var response = await responseTask;
            if (!response.Ok)
            {
                Assert.Fail($"Reference request failed: HTTP {(int)response.Status}");
            }
        }
        catch (TimeoutException)
        {
            // Blazor may still render confirm without a captured response in some environments.
        }

        var sent = page.Locator("[data-testid='ref-verif-request-sent']");
        var error = page.Locator(".ref-verif-request__error");
        try
        {
            await sent.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 45_000 });
        }
        catch (TimeoutException)
        {
            if (await error.CountAsync() > 0)
            {
                Assert.Fail(await error.InnerTextAsync());
            }

            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(page, Path.Combine(outputDir, $"debug-after-submit-{confirmTag}.png"));
            throw;
        }

        await page.WaitForSelectorAsync(".ref-verif-request__confirm-title", new() { Timeout = 5_000 });
        await Task.Delay(650);
        await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(page, Path.Combine(outputDir, $"03-request-confirm-{confirmTag}.png"));

        if (alsoConfirmViewport is { } mobile)
        {
            await page.SetViewportSizeAsync(mobile.Width, mobile.Height);
            await Task.Delay(650);
            await PlaywrightScreenshotSanitizer.CaptureFullPageAsync(page, Path.Combine(outputDir, "03-request-confirm-390.png"));
        }
    }

    private static async Task CaptureRequestFormAsync(IPage page, string baseUrl, string tag, string outputDir)
    {
        var entry = ReferenceVerificationDemoIds.WorkEntryBakkerij;
        await page.GotoAsync($"{baseUrl}/candidate/paspoort/referentie/vraag/{entry:D}", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForSelectorAsync(".ref-verif-request__card .ref-verif-request__grid", new() { Timeout = 15_000 });
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
