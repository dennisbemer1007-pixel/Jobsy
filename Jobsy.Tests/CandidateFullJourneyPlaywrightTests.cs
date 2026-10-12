using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Full candidate journey on a local live stack (1280 + 375). Requires
/// <c>JOBSY_E2E_FULL_JOURNEY=1</c>, <c>JOBSY_E2E_BASE_URL</c>,
/// <c>JOBSY_E2E_CAPTURE_EMAIL_CODES=1</c>, and Postgres for feature flags.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class CandidateFullJourneyPlaywrightTests
{
    [Fact]
    public async Task Full_journey_desktop_and_mobile_with_screenshots()
    {
        if (!CandidateJourneyE2e.ShouldRun)
        {
            return;
        }

        if (string.Equals(Environment.GetEnvironmentVariable("JOBSY_E2E_JOURNEY_SEED"), "1", StringComparison.Ordinal))
        {
            await RunSeededScreenshotJourneyAsync();
            return;
        }

        var baseUrl = await KbE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            Assert.Fail("JOBSY_E2E_BASE_URL unreachable");
        }

        await CandidateJourneyE2e.EnableLiveFeatureFlagsAsync(baseUrl);
        Assert.True(await CandidateJourneyE2e.WaitForFlagsAsync(baseUrl), "Feature flags did not turn on in time");

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        var desktopEmail = $"journey-d-{Guid.NewGuid():N}@jobsy.local";
        var mobileEmail = $"journey-m-{Guid.NewGuid():N}@jobsy.local";
        const string firstName = "Journey";

        await RunViewportAsync(browser, baseUrl, desktopEmail, firstName, 1280, 900, "desktop", fullJourney: true);
        await RunViewportAsync(browser, baseUrl, mobileEmail, firstName, 375, 812, "mobile", fullJourney: true);
    }

    private static async Task RunViewportAsync(
        IBrowser browser,
        string baseUrl,
        string email,
        string firstName,
        int width,
        int height,
        string viewportTag,
        bool fullJourney)
    {
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            HasTouch = width < 900,
            IsMobile = width < 900,
            Locale = "nl-NL",
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(90_000);
        var capture = KbE2e.AttachCapture(page);

        await SignUpAsync(page, baseUrl, email, firstName, viewportTag);
        await CompleteOnboardingWizardAsync(page, baseUrl, viewportTag);
        await CompleteDiscoveryJourneyDeepTestsAsync(page, baseUrl, viewportTag);

        await KbE2e.WaitForStyledAppAsync(page);
        await VisitPaspoortAndPdfAsync(page, baseUrl, viewportTag);
        await VisitExternalVacancyShareApplyAsync(page, baseUrl, viewportTag, email);
        await VisitCarriereAsync(page, baseUrl, viewportTag);
        await VisitBanenkaartAsync(page, baseUrl, viewportTag);
        await VisitApplicationsAndSettingsAsync(page, baseUrl, viewportTag, email);
        if (fullJourney && viewportTag == "desktop")
        {
            await SwitchArabicOnPaspoortAsync(page, baseUrl, viewportTag);
        }

        await KbE2e.AssertNoCircuitErrorAsync(page);
        KbE2e.AssertHealthy(capture, requireNoIsochrone404: false);
    }

    private static async Task SignUpAsync(IPage page, string baseUrl, string email, string firstName, string tag)
    {
        await page.GotoAsync(baseUrl + "/account-maken", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.WaitForSelectorAsync("#signup-title, .pub-signup__title", new() { Timeout = 60_000 });
        await page.WaitForSelectorAsync("link[rel=stylesheet]", new() { Timeout = 30_000 });
        await CandidateJourneyE2e.ShotAsync(page, tag, "01-account-maken");

        await page.Locator("input[name=email], input[type=email]").First.FillAsync(email);
        await page.Locator("input[name=firstName]").First.FillAsync(firstName);
        await page.Locator("button.login-submit").First.ClickAsync();
        await page.WaitForURLAsync("**/account-maken/code**", new() { Timeout = 60_000 });

        var code = await CandidateJourneyE2e.FetchEmailCodeAsync(baseUrl, email);
        Assert.False(string.IsNullOrWhiteSpace(code), "E-mail code not captured — set JOBSY_E2E_CAPTURE_EMAIL_CODES=1 and restart API/Web");

        await page.Locator("input[name=code], input[inputmode=numeric]").First.FillAsync(code!);
        await page.Locator("button.login-submit, button[type=submit]").First.ClickAsync();
        await page.WaitForURLAsync(url => !url.Contains("/account-maken/code", StringComparison.OrdinalIgnoreCase), new() { Timeout = 90_000 });
        await KbE2e.WaitForStyledAppAsync(page);
        await CandidateJourneyE2e.ShotAsync(page, tag, "02-after-signup");
    }

    private static async Task CompleteOnboardingWizardAsync(IPage page, string baseUrl, string tag)
    {
        await page.GotoAsync(baseUrl + "/candidate/start", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await KbE2e.WaitForStyledAppAsync(page);

        for (var step = 1; step <= 12; step++)
        {
            await page.WaitForTimeoutAsync(400);
            await CandidateJourneyE2e.ShotAsync(page, tag, $"03-onboarding-step-{step:D2}");

            if (await page.Locator(".ob-wizard__finish").CountAsync() > 0)
            {
                await CandidateJourneyE2e.ClickPrimaryAsync(page);
                break;
            }

            if (await page.Locator(".ob-wizard__install").CountAsync() > 0)
            {
                var later = page.Locator("button.ob-wizard__ghost").First;
                if (await later.CountAsync() > 0)
                {
                    await later.ClickAsync();
                    continue;
                }
            }

            await FillVisibleWizardFieldsAsync(page);
            await CandidateJourneyE2e.ClickPrimaryAsync(page);
        }

        await page.WaitForTimeoutAsync(800);
        await CandidateJourneyE2e.ShotAsync(page, tag, "04-onboarding-finish");
    }

    private static async Task FillVisibleWizardFieldsAsync(IPage page)
    {
        foreach (var input in await page.Locator("input[type=text]:visible, input[type=date]:visible, input[type=tel]:visible").AllAsync())
        {
            var name = await input.GetAttributeAsync("name") ?? "";
            if (name.Contains("email", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = await input.InputValueAsync();
            if (!string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (name.Contains("post", StringComparison.OrdinalIgnoreCase) || name.Contains("zip", StringComparison.OrdinalIgnoreCase))
            {
                await input.FillAsync("2671");
            }
            else if (name.Contains("phone", StringComparison.OrdinalIgnoreCase))
            {
                await input.FillAsync("0612345678");
            }
            else if (await input.GetAttributeAsync("type") == "date")
            {
                var min = await input.GetAttributeAsync("min") ?? DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd");
                await input.FillAsync(min);
            }
            else
            {
                await input.FillAsync("Test");
            }
        }

        var perDirect = page.Locator("input[type=radio][value=immediate], label:has-text('Per direct')").First;
        if (await perDirect.CountAsync() > 0)
        {
            await perDirect.ClickAsync(new() { Timeout = 3_000 });
        }

        foreach (var cb in await page.Locator("input[type=checkbox]:visible:not(:checked)").AllAsync())
        {
            try
            {
                await cb.CheckAsync(new() { Timeout = 2_000 });
            }
            catch (PlaywrightException)
            {
                // optional consent
            }
        }
    }

    private static async Task CompleteDiscoveryJourneyDeepTestsAsync(IPage page, string baseUrl, string tag)
    {
        await page.GotoAsync(baseUrl + "/candidate/ontdekkingsreis", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await KbE2e.WaitForStyledAppAsync(page, "ontdekkingsreis.css");

        for (var round = 0; round < 80; round++)
        {
            if (await page.Locator(".journey-end, .journey-klaar, [data-screen=klaar]").CountAsync() > 0)
            {
                break;
            }

            if (await page.Locator(".journey-shed").CountAsync() > 0)
            {
                await CandidateJourneyE2e.SelectMaxShedDepthAsync(page);
                await CandidateJourneyE2e.ShotAsync(page, tag, $"05-shed-{round:D2}");
                await CandidateJourneyE2e.ClickPrimaryAsync(page);
                continue;
            }

            if (await page.Locator(".test-flow, .questionnaire-shell, .ob-wizard").CountAsync() > 0)
            {
                await CandidateJourneyE2e.AnswerLikertUntilDoneAsync(page, 1, 4);
                continue;
            }

            if (await page.Locator("button.btn-primary:not([disabled])").CountAsync() > 0)
            {
                await CandidateJourneyE2e.ClickPrimaryAsync(page);
            }
            else
            {
                break;
            }

            await page.WaitForTimeoutAsync(300);
        }

        await CandidateJourneyE2e.ShotAsync(page, tag, "06-discovery-results");
        var body = await page.InnerTextAsync("body");
        Assert.True(
            body.Contains("Dit ben jij", StringComparison.OrdinalIgnoreCase)
            || body.Contains("klaar", StringComparison.OrdinalIgnoreCase)
            || body.Contains("Volledig", StringComparison.Ordinal),
            "Discovery journey should reach results or completion");
        Assert.True(
            body.Contains("Competentie", StringComparison.OrdinalIgnoreCase)
            && body.Contains("Carrière", StringComparison.OrdinalIgnoreCase)
            && body.Contains("Cultuur", StringComparison.OrdinalIgnoreCase)
            && body.Contains("Waarden", StringComparison.OrdinalIgnoreCase),
            "Discovery results should list all four deep-test dimensions");
    }

    private static async Task VisitPaspoortAndPdfAsync(IPage page, string baseUrl, string tag)
    {
        await page.GotoAsync(baseUrl + "/candidate/paspoort?tab=dna", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
        await page.Locator(".passport-overview, .passport-overview__title").First.WaitForAsync(new() { Timeout = 60_000 });
        await CandidateJourneyE2e.ShotAsync(page, tag, "07-paspoort-dit-ben-jij");

        var downloadBtn = page.Locator("[data-testid=passport-download-pdf]").First;
        await downloadBtn.WaitForAsync(new() { Timeout = 60_000 });
        var download = await page.RunAndWaitForDownloadAsync(async () =>
        {
            await downloadBtn.ClickAsync(new() { Timeout = 30_000 });
        }, new() { Timeout = 120_000 });
        var pdfPath = await download.PathAsync();
        var pdf = await File.ReadAllBytesAsync(pdfPath!);
        Assert.True(pdf.Length > 5_000, "Paspoort PDF download should return a non-empty file");
        var png = Path.Combine(CandidateJourneyE2e.ShotDir(tag), "08-paspoort-pdf-page1.png");
        await CandidateJourneyE2e.RenderPdfFirstPageAsync(pdf, png);
    }

    private static async Task VisitExternalVacancyShareApplyAsync(IPage page, string baseUrl, string tag, string email)
    {
        var extId = await CandidateJourneyE2e.SeedExternalVacancyFixtureAsync(baseUrl, email);
        if (extId is null)
        {
            return;
        }

        await page.GotoAsync($"{baseUrl}/candidate/external/{extId:D}", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
        await CandidateJourneyE2e.ShotAsync(page, tag, "11b-external-vacancy");

        var apply = page.Locator("a[href*='/apply'], button:has-text('Solliciteer')").First;
        if (await apply.CountAsync() > 0)
        {
            await apply.ClickAsync();
            await page.WaitForTimeoutAsync(1_500);
            await CandidateJourneyE2e.ShotAsync(page, tag, "11c-external-apply");
        }

        await page.GotoAsync(baseUrl + "/candidate/applications", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
        await CandidateJourneyE2e.ShotAsync(page, tag, "11d-notifications-applications");
    }

    private static async Task VisitCarriereAsync(IPage page, string baseUrl, string tag)
    {
        await page.GotoAsync(baseUrl + CareerE2e.CareerPath, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
        await CandidateJourneyE2e.ShotAsync(page, tag, "09-carriere");
    }

    private static async Task VisitBanenkaartAsync(IPage page, string baseUrl, string tag)
    {
        await page.GotoAsync(baseUrl + KbE2e.MapPath, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
        await page.WaitForSelectorAsync("#job-map, .map-pane", new() { Timeout = 60_000 });
        await page.WaitForTimeoutAsync(3_000);
        await CandidateJourneyE2e.ShotAsync(page, tag, "10-banenkaart");

        var vacancy = page.Locator("a[href*='/vacancies/']").First;
        if (await vacancy.CountAsync() > 0)
        {
            await vacancy.ClickAsync();
            await page.WaitForTimeoutAsync(1_500);
            await CandidateJourneyE2e.ShotAsync(page, tag, "11-vacancy-detail");
        }
    }

    private static async Task VisitApplicationsAndSettingsAsync(IPage page, string baseUrl, string tag, string email)
    {
        foreach (var (path, slug) in new[]
                 {
                     ("/candidate/settings", "13-instellingen"),
                     ("/candidate/privacy", "14-privacy")
                 })
        {
            await page.GotoAsync(baseUrl + path, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
            await CandidateJourneyE2e.ShotAsync(page, tag, slug);
        }

        await page.GotoAsync(baseUrl + "/logout", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync(baseUrl + "/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.Locator("input[type=email], #email").First.FillAsync(email);
        await page.Locator("button.login-submit, button[type=submit]").First.ClickAsync();
        await page.WaitForURLAsync("**/login/**", new() { Timeout = 60_000 });
        var code = await CandidateJourneyE2e.FetchEmailCodeAsync(baseUrl, email);
        Assert.False(string.IsNullOrWhiteSpace(code), "OTP na logout — zet JOBSY_E2E_CAPTURE_EMAIL_CODES=1");
        await page.Locator("input[name=code], input[inputmode=numeric]").First.FillAsync(code!);
        await page.Locator("button.login-submit, button[type=submit]").First.ClickAsync();
        await page.WaitForURLAsync(url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase), new() { Timeout = 90_000 });
        await CandidateJourneyE2e.ShotAsync(page, tag, "15-login-after-logout-otp");
    }

    private static async Task SwitchArabicOnPaspoortAsync(IPage page, string baseUrl, string tag)
    {
        await CareerE2e.TrySwitchLanguageAsync(page, baseUrl, "ar", "/candidate/paspoort?tab=dna");
        await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
        await CareerE2e.WaitForRtlAsync(page);
        await CandidateJourneyE2e.ShotAsync(page, tag, "16-paspoort-ar-rtl");
    }

    /// <summary>
    /// Seeded kandidaat walkthrough for styled screenshots when signup OTP timing is flaky.
    /// Set <c>JOBSY_E2E_JOURNEY_SEED=1</c> (still requires <c>JOBSY_E2E_FULL_JOURNEY=1</c>).
    /// </summary>
    private static async Task RunSeededScreenshotJourneyAsync()
    {
        var baseUrl = await KbE2e.TryReadyBaseUrlAsync();
        if (baseUrl is null)
        {
            Assert.Fail("JOBSY_E2E_BASE_URL unreachable");
        }

        await CandidateJourneyE2e.EnableLiveFeatureFlagsAsync(baseUrl);
        Assert.True(await CandidateJourneyE2e.WaitForFlagsAsync(baseUrl));

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        foreach (var (width, height, tag) in new[] { (1280, 900, "desktop"), (375, 812, "mobile") })
        {
            await using var context = await browser.NewContextAsync(KbE2e.ContextFor(width, height));
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();
            Assert.True(await KbE2e.TryLoginAsync(page, baseUrl));

            await page.GotoAsync(baseUrl + "/candidate/start", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
            await CandidateJourneyE2e.ShotAsync(page, tag, "03-onboarding-step-01");

            await page.GotoAsync(baseUrl + "/candidate/ontdekkingsreis", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
            await CandidateJourneyE2e.ShotAsync(page, tag, "06-discovery-results");

            await VisitPaspoortAndPdfAsync(page, baseUrl, tag);
            await VisitCarriereAsync(page, baseUrl, tag);
            await VisitBanenkaartAsync(page, baseUrl, tag);
            await VisitApplicationsAndSettingsAsync(page, baseUrl, tag, KbE2e.DefaultCompleteEmail);
            if (tag == "desktop")
            {
                await SwitchArabicOnPaspoortAsync(page, baseUrl, tag);
            }
        }
    }
}
