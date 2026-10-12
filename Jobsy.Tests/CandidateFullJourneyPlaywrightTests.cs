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

        var signupCode = await SignUpAsync(page, baseUrl, email, firstName, viewportTag);
        await CompleteOnboardingWizardAsync(page, baseUrl, viewportTag);
        await CompleteDiscoveryJourneyDeepTestsAsync(page, baseUrl, viewportTag);

        await KbE2e.WaitForStyledAppAsync(page);
        await VisitPaspoortAndPdfAsync(page, baseUrl, viewportTag);
        await VisitExternalVacancyShareApplyAsync(page, baseUrl, viewportTag, email);
        await VisitCarriereAsync(page, baseUrl, viewportTag);
        await VisitBanenkaartAsync(page, baseUrl, viewportTag);
        await VisitApplicationsAndSettingsAsync(page, baseUrl, viewportTag, email, signupCode);
        if (fullJourney && viewportTag == "desktop")
        {
            await SwitchArabicOnPaspoortAsync(page, baseUrl, viewportTag);
        }

        await KbE2e.AssertNoCircuitErrorAsync(page);
        KbE2e.AssertHealthy(capture, requireNoIsochrone404: false, allowFont404: true);
    }

    private static async Task CompleteEmailCodeGateAsync(IPage page, string baseUrl, string email, string? staleSignupCode)
    {
        await page.Context.ClearCookiesAsync();
        await PlaywrightCookieConsent.AcceptAsync(page.Context);
        await page.GotoAsync(baseUrl + "/account-maken", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await KbE2e.WaitForStyledAppAsync(page);

        var sequenceBefore = await CandidateJourneyE2e.GetEmailCodeSequenceAsync(baseUrl, email) ?? 0;

        await page.Locator("input[name=email], input[type=email]").First.FillAsync(email);
        await page.Locator("button.login-submit, button[type=submit]").First.ClickAsync();
        await page.WaitForURLAsync(
            "**/account-maken/code**",
            new() { Timeout = 90_000, WaitUntil = WaitUntilState.DOMContentLoaded });

        var code = await CandidateJourneyE2e.FetchEmailCodeAsync(
            baseUrl,
            email,
            staleCode: staleSignupCode,
            afterSequence: sequenceBefore,
            maxAttempts: 80);
        Assert.False(string.IsNullOrWhiteSpace(code), "OTP — zet JOBSY_E2E_CAPTURE_EMAIL_CODES=1 en herstart de API");

        await page.Locator("input[name=code], input[inputmode=numeric]").First.FillAsync(code!);
        await page.Locator("button.login-submit, button[type=submit]").First.ClickAsync();
        await page.WaitForURLAsync(
            url => !url.Contains("/account-maken/code", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 90_000, WaitUntil = WaitUntilState.DOMContentLoaded });

        await page.GotoAsync(baseUrl + "/candidate/paspoort?tab=dna", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.WaitForURLAsync(
            url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 90_000, WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.Locator(".passport-overview, .passport-overview__title").First.WaitForAsync(new() { Timeout = 60_000 });
        await KbE2e.WaitForStyledAppAsync(page);
        await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
    }

    private static async Task<string?> SignUpAsync(IPage page, string baseUrl, string email, string firstName, string tag)
    {
        await page.GotoAsync(baseUrl + "/account-maken", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.WaitForSelectorAsync("#signup-title, .pub-signup__title", new() { Timeout = 60_000 });
        await KbE2e.WaitForStyledAppAsync(page);
        await CandidateJourneyE2e.ShotAsync(page, tag, "01-account-maken");

        await page.Locator("input[name=email], input[type=email]").First.FillAsync(email);
        await page.Locator("input[name=firstName]").First.FillAsync(firstName);
        await page.Locator("button.login-submit").First.ClickAsync();
        await page.WaitForURLAsync("**/account-maken/code**", new() { Timeout = 60_000 });

        var code = await CandidateJourneyE2e.FetchEmailCodeAsync(baseUrl, email);
        Assert.False(string.IsNullOrWhiteSpace(code), "E-mail code not captured — set JOBSY_E2E_CAPTURE_EMAIL_CODES=1 and restart API/Web");

        await page.Locator("input[name=code], input[inputmode=numeric]").First.FillAsync(code!);
        await page.Locator("button.login-submit, button[type=submit]").First.ClickAsync();
        await page.WaitForURLAsync(
            url => !url.Contains("/account-maken/code", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 90_000 });
        await KbE2e.WaitForStyledAppAsync(page);
        await CandidateJourneyE2e.ShotAsync(page, tag, "02-after-signup");
        return code;
    }

    private static async Task CompleteOnboardingWizardAsync(IPage page, string baseUrl, string tag)
    {
        await page.GotoAsync(baseUrl + "/candidate/start", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await KbE2e.WaitForStyledAppAsync(page);

        string? lastMiniStatement = null;
        var miniStagnant = 0;
        for (var step = 1; step <= 14; step++)
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

            if (await page.Locator(".test-flow, fieldset.q-likert, .q-likert__scale").CountAsync() > 0)
            {
                var statement = page.Locator(".test-flow__statement").First;
                var statementText = await statement.CountAsync() > 0 ? await statement.InnerTextAsync() : "";
                await CandidateJourneyE2e.AnswerLikertUntilDoneAsync(page, 8, 4);
                await CandidateJourneyE2e.TryAdvanceMiniTestWizardAsync(page);
                if (!string.IsNullOrWhiteSpace(statementText)
                    && string.Equals(statementText, lastMiniStatement, StringComparison.Ordinal))
                {
                    miniStagnant++;
                    if (miniStagnant >= 3)
                    {
                        break;
                    }
                }
                else
                {
                    miniStagnant = 0;
                    lastMiniStatement = statementText;
                }

                continue;
            }

            await FillVisibleWizardFieldsAsync(page);

            if (await page.Locator(".ob-wizard__presets button").CountAsync() > 0)
            {
                await CaptureOnboardingProgressHeaderWhileScrolledAsync(page, tag);
            }

            var skipDream = page.Locator(".ob-wizard__ghost").Filter(new() { HasText = "Weet ik nog niet" }).First;
            if (await skipDream.CountAsync() > 0)
            {
                await skipDream.ClickAsync(new() { Timeout = 5_000 });
                await page.WaitForTimeoutAsync(600);
                continue;
            }

            await CandidateJourneyE2e.ClickPrimaryAsync(page);
            await page.WaitForTimeoutAsync(600);
        }

        if (await page.Locator(".ob-wizard__finish").CountAsync() > 0)
        {
            await CandidateJourneyE2e.ClickPrimaryAsync(page);
            await page.WaitForTimeoutAsync(800);
        }

        await CandidateJourneyE2e.ShotAsync(page, tag, "04-onboarding-finish");
        var onboardingBody = await page.InnerTextAsync("body");
        Assert.False(
            onboardingBody.Contains("Niet bewaard", StringComparison.OrdinalIgnoreCase),
            "Onboarding should save each step before continuing");
    }

    private static async Task CaptureOnboardingProgressHeaderWhileScrolledAsync(IPage page, string tag)
    {
        var scroll = page.Locator(".ob-wizard__content").First;
        if (await scroll.CountAsync() == 0)
        {
            return;
        }

        await scroll.EvaluateAsync("el => { el.scrollTop = Math.min(el.scrollHeight, 420); }");
        await page.WaitForTimeoutAsync(350);
        await CandidateJourneyE2e.ShotAsync(page, tag, "03-onboarding-header-scrolled");
    }

    private static async Task FillVisibleWizardFieldsAsync(IPage page)
    {
        var fields = page.Locator(".ob-fields input:visible, .ob-wizard__content .ob-fields input:visible");
        if (await fields.CountAsync() >= 3)
        {
            var lastName = fields.Nth(1);
            if (string.IsNullOrWhiteSpace(await lastName.InputValueAsync()))
            {
                await lastName.FillAsync("Jansen");
            }

            var postcode = fields.Nth(2);
            if (string.IsNullOrWhiteSpace(await postcode.InputValueAsync()))
            {
                await postcode.FillAsync("2671GB");
            }

            await page.WaitForTimeoutAsync(400);
            var suggestion = page.Locator(".ob-wizard__suggestions button").First;
            if (await suggestion.CountAsync() > 0)
            {
                await suggestion.ClickAsync(new() { Timeout = 5_000 });
            }

            var city = page.Locator(".ob-fields label:has-text('Plaats') input, .ob-fields label:has-text('Woonplaats') input").First;
            if (await city.CountAsync() > 0 && string.IsNullOrWhiteSpace(await city.InputValueAsync()))
            {
                await city.FillAsync("Naaldwijk");
            }
        }

        var birthInput = page.Locator("[data-testid=onboarding-birth-date] input[type=date]").First;
        if (await birthInput.CountAsync() > 0 && string.IsNullOrWhiteSpace(await birthInput.InputValueAsync()))
        {
            var birth = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-22);
            await birthInput.FillAsync(birth.ToString("yyyy-MM-dd"));
            await birthInput.EvaluateAsync("el => el.dispatchEvent(new Event('change', { bubbles: true }))");
        }

        var availStart = page.Locator("[data-testid=availability-start-date] input[type=date]").First;
        if (await availStart.CountAsync() > 0 && string.IsNullOrWhiteSpace(await availStart.InputValueAsync()))
        {
            var start = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(14);
            await availStart.FillAsync(start.ToString("yyyy-MM-dd"));
            await availStart.EvaluateAsync("el => el.dispatchEvent(new Event('change', { bubbles: true }))");
        }

        var preset = page.Locator(".ob-wizard__presets button:not(.is-selected)").First;
        if (await preset.CountAsync() > 0)
        {
            await preset.ClickAsync(new() { Timeout = 5_000 });
        }

        var transport = page.Locator(".ob-wizard__transport button:not(.is-selected)").First;
        if (await transport.CountAsync() > 0)
        {
            await transport.ClickAsync(new() { Timeout = 5_000 });
        }

        foreach (var selector in new[]
                 {
                     ".ob-wizard__education-grid button:not(.is-selected)",
                     ".ob-wizard__mbo-levels button:not(.is-selected)",
                     ".ob-wizard__direction-chips button:not(.is-selected)",
                     ".ob-wizard__dream-grid button:not(.is-selected)"
                 })
        {
            var pick = page.Locator(selector).First;
            if (await pick.CountAsync() > 0)
            {
                await pick.ClickAsync(new() { Timeout = 5_000 });
            }
        }

        var dreamSearch = page.Locator(".ob-wizard__dream-search input").First;
        if (await dreamSearch.CountAsync() > 0 && string.IsNullOrWhiteSpace(await dreamSearch.InputValueAsync()))
        {
            await dreamSearch.FillAsync("Verpleegkundige");
            await page.WaitForTimeoutAsync(600);
            var dreamPick = page.Locator(".ob-wizard__dream-search button").First;
            if (await dreamPick.CountAsync() > 0)
            {
                await dreamPick.ClickAsync(new() { Timeout = 5_000 });
            }
        }

        var addJob = page.Locator("button.ob-wizard__add-job").First;
        if (await addJob.CountAsync() > 0)
        {
            await addJob.ClickAsync(new() { Timeout = 3_000 });
            await page.Locator(".ob-wizard__job input").First.FillAsync("Kassamedewerker");
        }

        var perDirect = page.Locator(".ob-wizard__start-option").Filter(new() { HasText = "Per direct" }).First;
        if (await perDirect.CountAsync() > 0 && await page.Locator(".ob-wizard__start-option.is-selected").CountAsync() == 0)
        {
            await perDirect.ClickAsync(new() { Timeout = 3_000 });
        }

        foreach (var input in await page.Locator("input[type=text]:visible, input[type=tel]:visible").AllAsync())
        {
            var name = await input.GetAttributeAsync("name") ?? "";
            if (name.Contains("email", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(await input.InputValueAsync()))
            {
                continue;
            }

            if (name.Contains("phone", StringComparison.OrdinalIgnoreCase))
            {
                await input.FillAsync("0612345678");
            }
        }

        foreach (var cb in await page.Locator(".ob-wizard__consent input[type=checkbox]:not(:checked), .ob-wizard__check input[type=checkbox]:not(:checked)").AllAsync())
        {
            try
            {
                await cb.CheckAsync(new() { Timeout = 2_000, Force = true });
            }
            catch (Exception)
            {
                // optional or off-screen consent
            }
        }
    }

    private static async Task CompleteDiscoveryJourneyDeepTestsAsync(IPage page, string baseUrl, string tag)
    {
        await page.GotoAsync(baseUrl + "/candidate/ontdekkingsreis", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await KbE2e.WaitForStyledAppAsync(page, "ontdekkingsreis.css");

        var stagnantRounds = 0;
        for (var round = 0; round < 240; round++)
        {
            if (await page.Locator(".journey-end, .journey-klaar, [data-screen=klaar]").CountAsync() > 0)
            {
                break;
            }

            if (await page.Locator(".journey-shed").CountAsync() > 0)
            {
                await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);
                await CandidateJourneyE2e.SelectMaxShedDepthAsync(page);
                await CandidateJourneyE2e.ShotAsync(page, tag, $"05-shed-{round:D2}");
                await CandidateJourneyE2e.ClickPrimaryAsync(page);
                await page.WaitForTimeoutAsync(600);
                continue;
            }

            if (await page.Locator(".test-flow, .test-likert, fieldset.q-likert, .questionnaire-shell").CountAsync() > 0)
            {
                var counter = page.Locator(".test-flow__counter").First;
                if (await counter.CountAsync() > 0)
                {
                    var counterText = await counter.InnerTextAsync();
                    if (counterText.Contains("van 25", StringComparison.Ordinal)
                        || counterText.Contains("van 18", StringComparison.Ordinal))
                    {
                        await CandidateJourneyE2e.ShotAsync(page, tag, "05-deep-test-counter");
                    }
                }

                await CandidateJourneyE2e.AnswerLikertUntilDoneAsync(page, 30, 4);
                _ = await CandidateJourneyE2e.TryAdvanceDiscoveryStepAsync(page);
                stagnantRounds = 0;
                continue;
            }

            if (await page.Locator(".ob-fields input").CountAsync() > 0)
            {
                await FillVisibleWizardFieldsAsync(page);
                await CandidateJourneyE2e.ClickPrimaryAsync(page);
                await page.WaitForTimeoutAsync(400);
                continue;
            }

            var consentBox = page.Locator(".ob-wizard__consent input[type=checkbox]").First;
            if (await consentBox.CountAsync() > 0 && !await consentBox.IsCheckedAsync())
            {
                await consentBox.CheckAsync(new() { Timeout = 5_000 });
            }

            var startTests = page.Locator("button.btn-primary").Filter(new() { HasText = "Start de eerste test" }).First;
            if (await startTests.CountAsync() > 0)
            {
                if (await startTests.IsEnabledAsync())
                {
                    await startTests.ClickAsync(new() { Timeout = 15_000 });
                    await page.WaitForTimeoutAsync(500);
                    continue;
                }

                await page.WaitForTimeoutAsync(400);
                continue;
            }

            if (await CandidateJourneyE2e.TryAdvanceDiscoveryStepAsync(page))
            {
                stagnantRounds = 0;
            }
            else
            {
                stagnantRounds++;
                if (stagnantRounds >= 20)
                {
                    break;
                }
            }

            await page.WaitForTimeoutAsync(300);
        }

        await CandidateJourneyE2e.ShotAsync(page, tag, "06-discovery-results");
        var body = await page.InnerTextAsync("body");
        var atResults = await page.Locator(".journey-end, .journey-klaar, [data-screen=klaar]").CountAsync() > 0
            || body.Contains("Dit ben jij", StringComparison.OrdinalIgnoreCase);
        Assert.True(atResults, "Discovery journey should reach results or completion");
        Assert.True(
            body.Contains("Dit ben jij", StringComparison.OrdinalIgnoreCase),
            "Discovery klaar screen should headline Dit ben jij");
        foreach (var dimension in new[] { "Wat je kunt", "Beroepen", "Cultuur", "Waarden" })
        {
            Assert.True(
                body.Contains(dimension, StringComparison.OrdinalIgnoreCase),
                $"Discovery results should show completed deep-test step: {dimension}");
        }
    }

    private static async Task VisitPaspoortAndPdfAsync(IPage page, string baseUrl, string tag)
    {
        await page.GotoAsync(baseUrl + "/candidate/paspoort?tab=dna", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
        await page.Locator(".passport-overview, .passport-overview__title").First.WaitForAsync(new() { Timeout = 60_000 });
        await CandidateJourneyE2e.ShotAsync(page, tag, "07-paspoort-dit-ben-jij");

        var downloadBtn = page.Locator("[data-testid=passport-download-pdf]").First;
        await downloadBtn.WaitForAsync(new() { Timeout = 60_000 });
        await Assertions.Expect(downloadBtn).ToBeEnabledAsync(new() { Timeout = 60_000 });

        var pdf = await CandidateJourneyE2e.FetchPaspoortPdfAsync(page, baseUrl);
        if (pdf is null || pdf.Length <= 5_000)
        {
            await downloadBtn.ClickAsync(new() { Timeout = 30_000 });
            await page.WaitForTimeoutAsync(800);
            pdf = await CandidateJourneyE2e.FetchPaspoortPdfAsync(page, baseUrl);
        }

        Assert.True(
            pdf is { Length: > 5_000 } && pdf[0] == (byte)'%' && pdf[1] == (byte)'P',
            "Paspoort PDF download should return a non-empty file");
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

    private static async Task VisitApplicationsAndSettingsAsync(IPage page, string baseUrl, string tag, string email, string? signupCode)
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
        await page.WaitForTimeoutAsync(800);

        if (string.Equals(email, KbE2e.DefaultCompleteEmail, StringComparison.OrdinalIgnoreCase))
        {
            Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, email, KbE2e.DefaultPassword));
            await CandidateJourneyE2e.ShotAsync(page, tag, "15-login-after-logout-otp");
            return;
        }

        await CompleteEmailCodeGateAsync(page, baseUrl, email, signupCode);
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
            await VisitApplicationsAndSettingsAsync(page, baseUrl, tag, KbE2e.DefaultCompleteEmail, signupCode: null);
            if (tag == "desktop")
            {
                await SwitchArabicOnPaspoortAsync(page, baseUrl, tag);
            }
        }
    }
}
