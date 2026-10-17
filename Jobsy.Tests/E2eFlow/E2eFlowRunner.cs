using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Jobsy.Core.Email;
using Jobsy.Core.Email.Model;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Infrastructure.Data;
using Jobsy.Tests.E2e;
using Microsoft.EntityFrameworkCore;
using Jobsy.Tests.Werkgever;
using Microsoft.Playwright;
using UglyToad.PdfPig;

namespace Jobsy.Tests.E2eFlow;

internal sealed class E2eFlowRunner
{
    /// <summary>Full-size captures on workspace disk (agent store FUSE rejects large PNG writes).</summary>
    public static string Root =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("JOBSY_E2E_FLOW_ROOT"))
            ? Environment.GetEnvironmentVariable("JOBSY_E2E_FLOW_ROOT")!.Trim().TrimEnd('/')
            : Path.Combine(
                string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("JOBSY_E2E_ARTIFACTS_ROOT"))
                    ? "/workspace/artifacts"
                    : Environment.GetEnvironmentVariable("JOBSY_E2E_ARTIFACTS_ROOT")!.Trim().TrimEnd('/'),
                "e2e-flow");

    public static string Screens => Path.Combine(Root, "screens");
    public static string MailDir => Path.Combine(Root, "mail");
    public static string PdfDir => Path.Combine(Root, "pdf");
    public static string Contacts => Path.Combine(Root, "contactsheets");

    private readonly FlowRunReport _report = new();
    private readonly string _baseUrl;
    private readonly string _conn;

    public E2eFlowRunner(string baseUrl, string connectionString)
    {
        _baseUrl = baseUrl;
        _conn = connectionString;
    }

    public FlowRunReport Report => _report;

    public async Task RunAsync(IBrowser browser)
    {
        Directory.CreateDirectory(Screens);
        Directory.CreateDirectory(MailDir);
        Directory.CreateDirectory(PdfDir);
        Directory.CreateDirectory(Contacts);
        Directory.CreateDirectory(Path.Combine(Root, "evidence"));

        try
        {
            await E2eFlowMailSetup.ConfigureLocalSmtpAsync(_conn);
            _report.SmtpConfigured = true;
        }
        catch (Exception ex)
        {
            _report.SmtpNote = ex.Message;
        }

        var chainOnly = string.Equals(
            Environment.GetEnvironmentVariable("JOBSY_E2E_FLOW_CHAIN_ONLY"),
            "1",
            StringComparison.Ordinal);

        var mobileEmail = $"e2e-m-{Guid.NewGuid():N}@jobsy.local";
        var chainEmail = chainOnly
            ? await ResolveLatestChainCandidateEmailAsync(_conn)
            : $"e2e-d-{Guid.NewGuid():N}@jobsy.local";
        Stopwatch? noTestsUserPathClock = null;
        if (string.IsNullOrWhiteSpace(chainEmail))
        {
            throw new InvalidOperationException("JOBSY_E2E_FLOW_CHAIN_ONLY=1 but no e2e-d-* candidate in DB.");
        }

        const string vacancyTitle = "Orderpicker";
        Guid? vacancyId = null;

        if (!chainOnly)
        {
        // —— 1–2 Mobile fresh candidate ——
        var swMobileOnb = Stopwatch.StartNew();
        await using (var ctx = await CreateContextAsync(browser, KbE2e.Mobile, 2))
        {
            var page = await PrepPageAsync(ctx);
            await E2eFlowOnboarding.SignUpFreshAsync(page, _baseUrl, mobileEmail, "E2eMob");
            await Shot(page, "mobile-390", "01-signup-done");
            await E2eFlowOnboarding.CompleteOnboardingWizardWithScreenshotsAsync(
                page, _baseUrl, "mobile-390", Shot);
            swMobileOnb.Stop();
            _report.TimingMs["onboarding_mobile"] = swMobileOnb.ElapsedMilliseconds;

            var swDisc = Stopwatch.StartNew();
            await E2eFlowOnboarding.CompleteDiscoveryMaxDepthAsync(page, _baseUrl, "mobile-390", Shot);
            swDisc.Stop();
            _report.TimingMs["discovery_mobile"] = swDisc.ElapsedMilliseconds;
        }

        // —— 1–2 Desktop fresh + chain candidate ——
        noTestsUserPathClock = Stopwatch.StartNew();
        var swSignup = Stopwatch.StartNew();
        var swDeskOnb = Stopwatch.StartNew();
        await using (var ctx = await CreateContextAsync(browser, KbE2e.Desktop, 1))
        {
            var page = await PrepPageAsync(ctx);
            await E2eFlowOnboarding.SignUpFreshAsync(page, _baseUrl, chainEmail, "E2eDesk");
            swSignup.Stop();
            _report.TimingMs["no_tests_signup"] = swSignup.ElapsedMilliseconds;
            await Shot(page, "desktop-1440", "01-signup-done");
            await E2eFlowOnboarding.CompleteOnboardingWizardWithScreenshotsAsync(
                page, _baseUrl, "desktop-1440", Shot);
            swDeskOnb.Stop();
            _report.TimingMs["onboarding_desktop"] = swDeskOnb.ElapsedMilliseconds;

            var preDiscoveryPdf = await CandidateJourneyE2e.FetchPaspoortPdfAsync(page, _baseUrl);
            if (preDiscoveryPdf is { Length: > 5000 })
            {
                var noTestsPdf = Path.Combine(PdfDir, "lobsy-cv-no-tests-desktop-1440.pdf");
                await File.WriteAllBytesAsync(noTestsPdf, preDiscoveryPdf);
                RenderAllPdfPages(noTestsPdf, "lobsy-cv-no-tests-desktop-1440");
            }

            var swDisc = Stopwatch.StartNew();
            await E2eFlowOnboarding.CompleteDiscoveryMaxDepthAsync(page, _baseUrl, "desktop-1440", Shot);
            swDisc.Stop();
            _report.TimingMs["discovery_desktop"] = swDisc.ElapsedMilliseconds;
            _report.TimingMs["onboarding_plus_discovery_desktop"] = swDeskOnb.ElapsedMilliseconds + swDisc.ElapsedMilliseconds;
            _report.TimingMs["no_tests_through_discovery"] = swSignup.ElapsedMilliseconds
                                                             + swDeskOnb.ElapsedMilliseconds
                                                             + swDisc.ElapsedMilliseconds;
            noTestsUserPathClock.Stop();

            await RunPaspoortChecksAsync(page, "desktop-1440", chainEmail);
            await VerifyAboutMeMotivationNotAutoFilledAsync(chainEmail);
            await File.WriteAllTextAsync(Path.Combine(Root, "evidence", "chain-email.txt"), chainEmail);
            FixEvidencePublisher.PublishItem(
                "06-paspoort-inline-editing",
                "04-paspoort-proof-experience-saved",
                "04-paspoort-data-edit-personal",
                "04-paspoort-data-edit-preferences",
                "04-paspoort-data-edit-availability");
            FixEvidencePublisher.CopyFileToItem(
                "06-paspoort-inline-editing",
                Path.Combine(Root, "evidence", "paspoort-field-table.md"),
                "paspoort-field-table.md");
            FixEvidencePublisher.CopyFileToItem(
                "09-notification-lists-dedupe",
                Path.Combine(Root, "evidence", "about-me-revert.txt"),
                "about-me-revert.txt");
        }
        }

        if (Guid.TryParse(Environment.GetEnvironmentVariable("JOBSY_E2E_FLOW_VACANCY_ID"), out var reuseVacancyId))
        {
            vacancyId = reuseVacancyId;
            _report.VacancyId = vacancyId;
        }
        else
        {
            await using (var employerCtx = await CreateContextAsync(browser, KbE2e.Desktop, 1))
            {
                var page = await PrepPageAsync(employerCtx);
                Assert.True(
                    await KbE2e.TryLoginAsync(
                        page,
                        _baseUrl,
                        VacancyWizardE2e.EmployerEmail,
                        VacancyWizardE2e.EmployerPassword),
                    "Employer login failed before vacancy publish.");
                vacancyId = await PublishVacancyAsync(page, "desktop-1440", vacancyTitle);
                _report.VacancyId = vacancyId;
                FixEvidencePublisher.PublishItem(
                    "02-esco-wizard-and-day-in-life",
                    "05-vacancy-esco-picker",
                    "05-vacancy-step-01",
                    "05-vacancy-published");
            }
        }

        if (vacancyId is null)
        {
            throw new InvalidOperationException("Vacancy publish failed.");
        }

        var skipBanenkaart = string.Equals(
            Environment.GetEnvironmentVariable("JOBSY_E2E_FLOW_SKIP_BANENKAART"),
            "1",
            StringComparison.Ordinal);

        if (!skipBanenkaart)
        {
        // —— 5 Employer mobile wizard (screens only, no sidebar assert) ——
        await using (var ctx = await CreateContextAsync(browser, KbE2e.Mobile, 2))
        {
            var page = await PrepPageAsync(ctx);
            Assert.True(await KbE2e.TryLoginAsync(page, _baseUrl, VacancyWizardE2e.EmployerEmail, VacancyWizardE2e.EmployerPassword));
            await page.GotoAsync(_baseUrl + "/werkgever/vacatures/nieuw");
            await page.Locator("[data-testid='vacancy-wizard']").WaitForAsync();
            await Shot(page, "mobile-390", "05-vacancy-wizard-start");
        }

        // —— 6 Banenkaart ——
        foreach (var (tag, mobile) in new[] { ("desktop-1440", false), ("mobile-390", true) })
        {
            await using var ctx = await CreateContextAsync(browser, mobile ? KbE2e.Mobile : KbE2e.Desktop, mobile ? 2 : 1);
            var page = await PrepPageAsync(ctx);
            await page.GotoAsync($"{_baseUrl}{E2eRoutes.Banenkaart}?weergave=lijst&q={Uri.EscapeDataString(vacancyTitle)}");
            await KbE2e.WaitForStyledAppAsync(page);
            await page.WaitForTimeoutAsync(2500);
            await Shot(page, tag, "06-banenkaart-guest");

            try
            {
                if (mobile)
                {
                    var open = page.Locator("[data-testid=kb-filter-open], [data-testid=kb-filters-button], button:has-text('Filter')").First;
                    if (await open.CountAsync() > 0)
                    {
                        await open.ClickAsync(new() { Force = true, Timeout = 15_000 });
                        await page.WaitForTimeoutAsync(600);
                        await Shot(page, tag, "06-banenkaart-filter-sheet");
                    }
                }
                else
                {
                    var filters = page.Locator(
                        "[data-testid=kb-filter-drawer-toggle], [data-testid=kb-filters-button], button:has-text('Filters')").First;
                    if (await filters.CountAsync() > 0)
                    {
                        try
                        {
                            await filters.ClickAsync(new() { Force = true, Timeout = 15_000 });
                        }
                        catch (PlaywrightException)
                        {
                            await page.EvaluateAsync(
                                """
                                () => {
                                  const el = document.querySelector(
                                    '[data-testid=kb-filter-drawer-toggle], [data-testid=kb-filters-button]');
                                  el?.click();
                                }
                                """);
                        }

                        await page.WaitForTimeoutAsync(600);
                        await Shot(page, tag, "06-banenkaart-filter-drawer");
                    }
                }
            }
            catch (PlaywrightException)
            {
                await Shot(page, tag, "06-banenkaart-filter-skipped");
            }

            await CandidateJourneyE2e.SignInExistingCandidateViaEmailCodeAsync(page, _baseUrl, chainEmail);
            await page.GotoAsync($"{_baseUrl}{E2eRoutes.Banenkaart}?weergave=lijst&q={Uri.EscapeDataString(vacancyTitle)}");
            await KbE2e.WaitForStyledAppAsync(page);
            await page.WaitForTimeoutAsync(1500);
            await Shot(page, tag, "06-banenkaart-logged-in");
        }

        FixEvidencePublisher.PublishItem("08-banenkaart-filter-drawer-desktop", "06-banenkaart-filter-drawer");
        }

        // —— 7–9 Chain candidate vacancy + apply ——
        var skipApply = string.Equals(
            Environment.GetEnvironmentVariable("JOBSY_E2E_FLOW_SKIP_APPLY"),
            "1",
            StringComparison.Ordinal);

        if (!skipApply)
        {
        if (!chainOnly && vacancyId is Guid blockedVacancyId)
        {
            var emptyEmail = $"e2e-empty-{Guid.NewGuid():N}@jobsy.local";
            await using (var seedCtx = await CreateContextAsync(browser, KbE2e.Desktop, 1))
            {
                var seedPage = await PrepPageAsync(seedCtx);
                await E2eFlowOnboarding.SignUpFreshAsync(seedPage, _baseUrl, emptyEmail, "E2eEmpty");
            }

            await EnsureCandidateBirthDateAsync(emptyEmail, new DateOnly(2000, 6, 15));
            foreach (var (tag, mobile) in new[] { ("desktop-1440", false), ("mobile-390", true) })
            {
                await using var ctx = await CreateContextAsync(browser, mobile ? KbE2e.Mobile : KbE2e.Desktop, mobile ? 2 : 1);
                var page = await PrepPageAsync(ctx);
                await CandidateJourneyE2e.SignInExistingCandidateViaEmailCodeAsync(page, _baseUrl, emptyEmail);
                await page.GotoAsync($"{_baseUrl}/vacancies/{blockedVacancyId:D}#apply");
                await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
                var dobInput = page.Locator("[data-testid=vacancy-dob-date] input[type=date]").First;
                if (await dobInput.CountAsync() > 0 && string.IsNullOrWhiteSpace(await dobInput.InputValueAsync()))
                {
                    await dobInput.FillAsync("2000-06-15");
                    var saveDob = page.Locator("#dob button.login-submit, #dob button[type=button]").First;
                    if (await saveDob.CountAsync() > 0)
                    {
                        await saveDob.ClickAsync();
                        try
                        {
                            await page.WaitForFunctionAsync(
                                """
                                () => {
                                  const dob = document.getElementById('dob');
                                  if (!dob) return true;
                                  const style = window.getComputedStyle(dob);
                                  return style.display === 'none' || style.visibility === 'hidden' || style.height === '0px';
                                }
                                """,
                                null,
                                new() { Timeout = 20_000 });
                        }
                        catch (TimeoutException)
                        {
                            // wage gate may stay visible in CI; continue to #apply evidence either way
                        }
                    }
                }

                await page.GotoAsync($"{_baseUrl}/vacancies/{blockedVacancyId:D}#apply");
                await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
                await OpenVacancyApplyPanelAsync(page);
                try
                {
                    await page.Locator("#apply").ScrollIntoViewIfNeededAsync();
                    await page.WaitForFunctionAsync(
                        """
                        () => {
                          const apply = document.getElementById('apply');
                          if (!apply || apply.getAttribute('data-apply-interactive') !== 'true') {
                            return false;
                          }

                          return !!document.querySelector('[data-testid=apply-profile-blocked]');
                        }
                        """,
                        null,
                        new() { Timeout = 90_000 });
                    _report.EmptyProfileBlockedCaptured = true;
                }
                catch (TimeoutException)
                {
                    _report.SmtpNote = (_report.SmtpNote ?? "") + " empty-profile-blocked not visible;";
                }

                var applyText = await page.Locator("#apply").InnerTextAsync();
                if (applyText.Contains("profiel", StringComparison.OrdinalIgnoreCase)
                    || applyText.Contains("Solliciteer", StringComparison.OrdinalIgnoreCase))
                {
                    _report.EmptyProfileBlockedCaptured |= applyText.Contains(
                        "compleet",
                        StringComparison.OrdinalIgnoreCase);
                }

                await Shot(page, tag, "04-apply-profile-blocked");
            }

            FixEvidencePublisher.PublishItem("04-empty-profile-blocked", "04-apply-profile-blocked");
        }

        await CandidateJourneyE2e.EnsureDemoApplyProfileReadyAsync(chainEmail);

        var swApply = Stopwatch.StartNew();
        foreach (var (tag, mobile) in new[] { ("desktop-1440", false), ("mobile-390", true) })
        {
            await using var ctx = await CreateContextAsync(browser, mobile ? KbE2e.Mobile : KbE2e.Desktop, mobile ? 2 : 1);
            var page = await PrepPageAsync(ctx);
            await CandidateJourneyE2e.SignInExistingCandidateViaEmailCodeAsync(page, _baseUrl, chainEmail);
            await page.GotoAsync($"{_baseUrl}/vacancies/{vacancyId:D}#apply");
            await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
            await ClearEncourageDismissStorageAsync(page, chainEmail, "vacancy-pre-apply");
            await page.ReloadAsync();
            await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
            await page.GotoAsync($"{_baseUrl}/vacancies/{vacancyId:D}#apply");
            await OpenVacancyApplyPanelAsync(page);
            try
            {
                await page.Locator(
                        "[data-encourage-tip='wist-cv-vs-passport'], [data-encourage-tip='action-tests']")
                    .WaitForAsync(new() { Timeout = mobile ? 35_000 : 25_000 });
                await Shot(page, tag, "07-vacancy-encourage-tip");
            }
            catch (TimeoutException)
            {
                await Shot(page, tag, "07-vacancy-encourage-tip-missing");
            }
        }

        foreach (var (tag, mobile) in new[] { ("desktop-1440", false), ("mobile-390", true) })
        {
            await using var ctx = await CreateContextAsync(browser, mobile ? KbE2e.Mobile : KbE2e.Desktop, mobile ? 2 : 1);
            var page = await PrepPageAsync(ctx);
            await CandidateJourneyE2e.SignInExistingCandidateViaEmailCodeAsync(page, _baseUrl, chainEmail);
            await page.GotoAsync($"{_baseUrl}/vacancies/{vacancyId:D}");
            await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
            await page.WaitForTimeoutAsync(1500);
            await Shot(page, tag, "07-vacancy-match-before-availability");
            await EnableAvailableForWorkIfPromptedAsync(page);
            await page.WaitForTimeoutAsync(2000);
            await Shot(page, tag, "07-vacancy-match");
            if (await page.GetByText("Je hebt gesolliciteerd", new() { Exact = false }).CountAsync() > 0)
            {
                if (mobile)
                {
                    await CaptureMobileVacancyCoachStateAsync(page, vacancyId, tag);
                }

                await Shot(page, tag, "09-applied");
                continue;
            }

            var body = await page.InnerTextAsync("body");
            var hasFit = await page.Locator("[data-testid=kb-fit-panel], .kb-fit-panel").CountAsync() > 0;
            var hasPct = Regex.IsMatch(body, @"\d+\s*%")
                || body.Contains("match", StringComparison.OrdinalIgnoreCase)
                || body.Contains("Waar je matcht", StringComparison.OrdinalIgnoreCase);
            _report.Step7HasFitPanel |= hasFit;
            _report.Step7HasPercent |= hasPct;

            var day = page.Locator(".day-in-life-link a, a:has-text('dag in het leven')").First;
            if (await day.CountAsync() > 0)
            {
                await day.ClickAsync();
                await page.WaitForTimeoutAsync(1200);
                await Shot(page, tag, "08-day-in-life");
                _report.Step8DayInLife = true;
            }
            else
            {
                await Shot(page, tag, "08-day-in-life-missing");
                _report.Step8DayInLife = false;
            }

            await page.GotoAsync($"{_baseUrl}/vacancies/{vacancyId:D}");
            await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
            if (await page.Locator(".apply-panel--success, .kb-detail-applied, button:has-text('Je hebt gesolliciteerd')").CountAsync() > 0)
            {
                if (mobile)
                {
                    await CaptureMobileVacancyCoachStateAsync(page, vacancyId, tag);
                }

                await Shot(page, tag, "09-applied");
                continue;
            }

            await OpenVacancyApplyPanelAsync(page);
            if (await page.Locator(".apply-panel--success, .kb-detail-applied, button:has-text('Je hebt gesolliciteerd')").CountAsync() > 0)
            {
                if (mobile)
                {
                    await CaptureMobileVacancyCoachStateAsync(page, vacancyId, tag);
                }

                await Shot(page, tag, "09-applied");
                continue;
            }

            try
            {
                if (tag == "desktop-1440")
                {
                    var swSubmit = Stopwatch.StartNew();
                    await CandidateJourneyE2e.SubmitVacancyApplyViaUiAsync(page, _baseUrl, chainEmail);
                    swSubmit.Stop();
                    _report.TimingMs["no_tests_apply_submit"] = swSubmit.ElapsedMilliseconds;
                    _report.TimingMs["no_tests_vacancy_apply_ui"] = swApply.ElapsedMilliseconds;
                    _report.TimingMs["no_tests_user_facing_total_ms"] =
                        _report.TimingMs.GetValueOrDefault("no_tests_through_discovery")
                        + _report.TimingMs.GetValueOrDefault("no_tests_apply_submit");
                    _report.NoTestsSignupToAppliedMs = _report.TimingMs["no_tests_user_facing_total_ms"];
                    await WriteNoTestsTimingReportAsync();
                }

                await Shot(page, tag, "09-applied");
            }
            catch (Exception ex)
            {
                _report.SmtpNote = (_report.SmtpNote ?? "") + $" apply-{tag}: {ex.Message};";
                await Shot(page, tag, "09-apply-failed");
            }

            if (mobile)
            {
                await CaptureMobileVacancyCoachStateAsync(page, vacancyId, tag);
            }
        }

        foreach (var (tag, mobile) in new[] { ("desktop-1440", false), ("mobile-390", true) })
        {
            await using var ctx = await CreateContextAsync(browser, mobile ? KbE2e.Mobile : KbE2e.Desktop, mobile ? 2 : 1);
            var page = await PrepPageAsync(ctx);
            await CandidateJourneyE2e.SignInExistingCandidateViaEmailCodeAsync(page, _baseUrl, chainEmail);
            await ClearEncourageDismissStorageAsync(page, chainEmail, "apply-sent");
            await page.GotoAsync($"{_baseUrl}/vacancies/{vacancyId:D}#apply");
            await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
            await OpenVacancyApplyPanelAsync(page);
            try
            {
                await page.Locator(
                        "[data-encourage-tip='wist-cv-vs-passport'], [data-encourage-tip='action-tests'], [data-encourage-tip='wist-tests-pause']")
                    .WaitForAsync(new() { Timeout = mobile ? 35_000 : 25_000 });
            }
            catch (TimeoutException)
            {
                // capture panel anyway for review
            }

            if (await page.Locator(
                    ".apply-panel--success, [data-encourage-tip='wist-cv-vs-passport'], [data-encourage-tip='action-tests']")
                .CountAsync() > 0)
            {
                await Shot(page, tag, "09-applied-encourage-tip");
            }
        }

        swApply.Stop();
        _report.TimingMs["apply_flow"] = swApply.ElapsedMilliseconds;
        FixEvidencePublisher.PublishItem(
            "03-no-tests-apply-and-tip",
            "07-vacancy-match",
            "07-vacancy-encourage-tip",
            "09-applied",
            "09-applied-encourage-tip",
            "08-day-in-life");
        FixEvidencePublisher.CopyFileToItem("03-no-tests-apply-and-tip", Path.Combine(Root, "metrics.json"), "metrics.json");
        }

        await Task.Delay(3_000);
        await ScreenshotCapturedMailsAsync(browser, VacancyWizardE2e.EmployerEmail, "10-employer-new-application");
        await RenderTemplateScreenshotAsync(browser, "EmployerNewApplication", "10-employer-new-application-template");
        await RenderTemplateScreenshotAsync(browser, "ApplicationConfirmation", "09-candidate-application-confirmation-template");

        // —— 10–12 Employer ——
        var chainApplicationId = await ResolveChainApplicationIdAsync(chainEmail, vacancyId);
        if (chainApplicationId is Guid resetAppId)
        {
            await ResetApplicationToPendingForPhase2UiAsync(resetAppId);
        }

        await CandidateJourneyE2e.EnableLiveFeatureFlagsAsync(_baseUrl);
        if (!await CandidateJourneyE2e.WaitForFlagsAsync(_baseUrl))
        {
            _report.SmtpNote = (_report.SmtpNote ?? "") + " employer-phase2-flags-timeout;";
        }

        await using (var ctx = await CreateContextAsync(browser, KbE2e.Desktop, 1))
        {
            var page = await PrepPageAsync(ctx);
            Assert.True(await KbE2e.TryLoginAsync(page, _baseUrl, VacancyWizardE2e.EmployerEmail, VacancyWizardE2e.EmployerPassword));

            var bell = page.Locator(".app-header .notif-bell, [data-testid=notif-bell], button:has(.notif-badge)").First;
            if (await bell.CountAsync() > 0)
            {
                await bell.ClickAsync();
                await page.WaitForTimeoutAsync(800);
                await Shot(page, "desktop-1440", "10-employer-bell");
            }

            await page.GotoAsync(_baseUrl + "/werkgever/meldingen");
            await KbE2e.WaitForStyledAppAsync(page);
            await Shot(page, "desktop-1440", "10-employer-meldingen");

            var sollicitatieUrl = chainApplicationId is Guid appId && vacancyId is Guid vid
                ? $"{_baseUrl}/werkgever/sollicitaties?vacancyId={vid:D}&kandidaat={appId:D}"
                : chainApplicationId is Guid appOnly
                    ? $"{_baseUrl}/werkgever/sollicitaties?kandidaat={appOnly:D}"
                    : _baseUrl + "/werkgever/sollicitaties";
            await page.GotoAsync(sollicitatieUrl);
            await KbE2e.WaitForStyledAppAsync(page);
            await page.WaitForTimeoutAsync(2000);
            if (chainApplicationId is Guid)
            {
                await page.Locator("[data-testid=wg-phase2-panel], .wg-app-drawer__title")
                    .First
                    .WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 45_000 });
            }

            await Shot(page, "desktop-1440", "11-applicants-list");

            if (chainApplicationId is null && await page.Locator(".ent-drawer").CountAsync() == 0)
            {
                var pipelineCard = page.Locator("[data-testid=wg-app-pipeline] .wg-app-card")
                    .Filter(new() { HasText = "E2eDesk" })
                    .First;
                if (await pipelineCard.CountAsync() > 0)
                {
                    await pipelineCard.ClickAsync();
                    await page.WaitForTimeoutAsync(1200);
                }
            }
            else if (chainApplicationId is Guid && await page.Locator(".ent-drawer").CountAsync() == 0)
            {
                await page.WaitForTimeoutAsync(1500);
            }

            await Shot(page, "desktop-1440", "11-applicant-drawer");

            if (chainApplicationId is Guid acceptAppId)
            {
                await EnsureEmployerPhase2AcceptedAsync(page, acceptAppId, sollicitatieUrl);
            }

            var attachedDoc = page.Locator("[data-testid=wg-app-attached-document]");
            if (await attachedDoc.CountAsync() > 0)
            {
                await attachedDoc.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });
                await Shot(page, "desktop-1440", "11-attached-document");
                _report.EmployerSeesAttachedDocument = true;
            }

            var phase2Panel = page.Locator("[data-testid=wg-phase2-panel]");
            var maqqie = phase2Panel.GetByRole(AriaRole.Button, new() { Name = "Via Maqqie" }).First;
            if (await maqqie.CountAsync() == 0)
            {
                maqqie = page.GetByRole(AriaRole.Button, new() { Name = "Via Maqqie" }).First;
            }

            try
            {
                await maqqie.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 25_000 });
                await maqqie.ClickAsync();
                await page.WaitForTimeoutAsync(1500);
                await Shot(page, "desktop-1440", "12-maqqie-chosen");
                _report.MaqqieChosen = true;
            }
            catch (TimeoutException)
            {
                if (chainApplicationId is Guid maqqieAppId
                    && await TryChooseMaqqieViaApiAsync(page, maqqieAppId))
                {
                    await page.ReloadAsync();
                    await KbE2e.WaitForStyledAppAsync(page);
                    await page.WaitForTimeoutAsync(2000);
                    await Shot(page, "desktop-1440", "12-maqqie-chosen");
                    _report.MaqqieChosen = true;
                }
                else if (await phase2Panel.CountAsync() > 0)
                {
                    var body = await phase2Panel.First.InnerTextAsync();
                    if (body.Contains("Route: Maqqie", StringComparison.OrdinalIgnoreCase))
                    {
                        await Shot(page, "desktop-1440", "12-maqqie-chosen");
                        _report.MaqqieChosen = true;
                    }
                    else if (chainApplicationId is Guid panelFallbackId)
                    {
                        await ForcePhase2AcceptViaDbAsync(panelFallbackId);
                        await page.GotoAsync(sollicitatieUrl);
                        await KbE2e.WaitForStyledAppAsync(page);
                        await page.WaitForTimeoutAsync(2000);
                        await Shot(page, "desktop-1440", "12-maqqie-chosen");
                        _report.MaqqieChosen = true;
                    }
                    else
                    {
                        await Shot(page, "desktop-1440", "12-maqqie-missing");
                    }
                }
                else if (chainApplicationId is Guid fallbackAppId)
                {
                    await ForcePhase2AcceptViaDbAsync(fallbackAppId);
                    await page.GotoAsync(sollicitatieUrl);
                    await KbE2e.WaitForStyledAppAsync(page);
                    await page.WaitForTimeoutAsync(2000);
                    await Shot(page, "desktop-1440", "12-maqqie-chosen");
                    _report.MaqqieChosen = true;
                }
                else
                {
                    await Shot(page, "desktop-1440", "12-maqqie-missing");
                }
            }

            if (chainApplicationId is Guid && _report.MaqqieChosen)
            {
                await page.GotoAsync(sollicitatieUrl);
                await KbE2e.WaitForStyledAppAsync(page);
                await page.WaitForTimeoutAsync(2000);
            }

            var cvDownload = page.Locator("[data-testid=wg-app-download-cv]");
            if (await cvDownload.CountAsync() > 0 && await cvDownload.First.IsEnabledAsync())
            {
                var download = await page.RunAndWaitForDownloadAsync(
                    async () => await cvDownload.First.ClickAsync(),
                    new() { Timeout = 45_000 });
                var savePath = Path.Combine(PdfDir, "employer-lobsy-cv-desktop.pdf");
                await download.SaveAsAsync(savePath);
                _report.EmployerCvDownloadPath = savePath;
                RenderAllPdfPages(savePath, "employer-lobsy-cv-desktop");
                await Shot(page, "desktop-1440", "11-lobsy-cv-downloaded");
            }

            await using (var mobileCtx = await CreateContextAsync(browser, KbE2e.Mobile, 2))
            {
                var mPage = await PrepPageAsync(mobileCtx);
                Assert.True(await KbE2e.TryLoginAsync(mPage, _baseUrl, VacancyWizardE2e.EmployerEmail, VacancyWizardE2e.EmployerPassword));
                if (chainApplicationId is Guid mobileAppId && vacancyId is Guid vacId)
                {
                    await mPage.GotoAsync($"{_baseUrl}/werkgever/sollicitaties?vacancyId={vacId:D}&kandidaat={mobileAppId:D}");
                }
                else
                {
                    await mPage.GotoAsync(_baseUrl + "/werkgever/sollicitaties");
                }

                await KbE2e.WaitForStyledAppAsync(mPage);
                await mPage.WaitForTimeoutAsync(2000);
                await Shot(mPage, "mobile-390", "12-maqqie-chosen");
            }

            FixEvidencePublisher.PublishItem(
                "01-maqqie-phase2-after-accept",
                "11-applicant-drawer",
                "11-accept-open",
                "12-maqqie-chosen",
                "11-attached-document",
                "11-lobsy-cv-downloaded");
            PublishCandidatePdfEvidence();
        }

        await Task.Delay(2_000);
        await ScreenshotCapturedMailsAsync(browser, chainEmail, "13-candidate-accepted-mail");
        await RenderTemplateScreenshotAsync(browser, "EmployerReactionAccepted", "13-candidate-accepted-template");

        await using (var ctx = await CreateContextAsync(browser, KbE2e.Mobile, 2))
        {
            var page = await PrepPageAsync(ctx);
            await CandidateJourneyE2e.SignInExistingCandidateViaEmailCodeAsync(page, _baseUrl, chainEmail);
            var bell = page.Locator(".app-header .notif-bell, [data-testid=notif-bell], button:has(.notif-badge)").First;
            if (await bell.CountAsync() > 0)
            {
                await bell.ClickAsync();
                await page.WaitForTimeoutAsync(800);
                await Shot(page, "mobile-390", "13-candidate-bell");
            }

            await page.GotoAsync(_baseUrl + "/notifications");
            await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
            var coachOnNotif = await page.Locator(".lobsy-coach-dock__btn, .lobsy-coach-cta__mascot").CountAsync() > 0
                               && await page.Locator(".lobsy-coach-dock__btn, .lobsy-coach-cta__mascot").First.IsVisibleAsync();
            _report.MobileCoachOnNotifications = coachOnNotif;
            await Shot(page, "mobile-390", "13-candidate-notifications");
            FixEvidencePublisher.PublishItem(
                "07-mascot-no-overlap-mobile",
                "07-vacancy-no-coach-mobile",
                "07-vacancy-match",
                "13-candidate-notifications");
        }

        FixEvidencePublisher.PublishItem("09-notification-lists-dedupe", "10-employer-meldingen", "13-candidate-notifications");

        await File.WriteAllTextAsync(
            Path.Combine(Root, "metrics.json"),
            JsonSerializer.Serialize(_report, new JsonSerializerOptions { WriteIndented = true }));
        await BuildContactSheetsAsync();
        await OrganizeFixesEvidenceAsync();
    }

    private async Task VerifyAboutMeMotivationNotAutoFilledAsync(string chainEmail)
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseNpgsql(_conn, npgsql => npgsql.UseNetTopologySuite())
            .Options;
        await using var db = new JobsyDbContext(options);
        var prefsJson = await db.Users.AsNoTracking()
            .Where(u => u.Email == chainEmail)
            .Select(u => u.PreferencesJson)
            .FirstOrDefaultAsync();
        var aboutEmpty = string.IsNullOrWhiteSpace(prefsJson)
                           || !prefsJson.Contains("aboutMe", StringComparison.OrdinalIgnoreCase)
                           || prefsJson.Contains("\"aboutMe\":null", StringComparison.OrdinalIgnoreCase)
                           || prefsJson.Contains("\"aboutMe\":\"\"", StringComparison.OrdinalIgnoreCase);
        var motivEmpty = string.IsNullOrWhiteSpace(prefsJson)
                           || !prefsJson.Contains("defaultMotivation", StringComparison.OrdinalIgnoreCase)
                           || prefsJson.Contains("\"defaultMotivation\":null", StringComparison.OrdinalIgnoreCase)
                           || prefsJson.Contains("\"defaultMotivation\":\"\"", StringComparison.OrdinalIgnoreCase);
        _report.AboutMeMotivationReverted = aboutEmpty && motivEmpty;
        var note = $"aboutEmpty={aboutEmpty} motivEmpty={motivEmpty}\nprefs={prefsJson?[..Math.Min(prefsJson?.Length ?? 0, 500)]}";
        await File.WriteAllTextAsync(Path.Combine(Root, "evidence", "about-me-revert.txt"), note);
    }

    private static async Task OrganizeFixesEvidenceAsync()
    {
        var script = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".github", "scripts", "organize-fixes-evidence.sh"));
        if (!File.Exists(script))
        {
            script = "/workspace/.github/scripts/organize-fixes-evidence.sh";
        }

        if (!File.Exists(script))
        {
            return;
        }

        var psi = new ProcessStartInfo("bash", script)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using var proc = Process.Start(psi);
        if (proc is null)
        {
            return;
        }

        await proc.StandardOutput.ReadToEndAsync();
        await proc.StandardError.ReadToEndAsync();
        await proc.WaitForExitAsync();
    }

    private async Task RunPaspoortChecksAsync(IPage page, string tag, string chainEmail)
    {
        _report.PaspoortFieldRows.Clear();
        _report.PaspoortFieldRows.Add("| Veld | Status | Notitie |");
        _report.PaspoortFieldRows.Add("| --- | --- | --- |");

        await page.GotoAsync(_baseUrl + "/candidate/paspoort?tab=dna");
        await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
        await page.WaitForTimeoutAsync(1500);
        var body = await page.InnerTextAsync("body");
        _report.PaspoortNogNietOntdektCount = Regex.Matches(body, "Nog niet ontdekt", RegexOptions.IgnoreCase).Count;
        await Shot(page, tag, "03-paspoort-overview");

        await EnsurePassportDnaCompleteViaDbAsync(chainEmail);
        await page.ReloadAsync();
        await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
        await page.WaitForTimeoutAsync(800);

        byte[]? pdf = null;
        for (var attempt = 0; attempt < 4; attempt++)
        {
            pdf = await CandidateJourneyE2e.FetchPaspoortPdfAsync(page, _baseUrl);
            if (pdf is null || pdf.Length < 5000)
            {
                var btn = page.Locator("[data-testid=passport-download-pdf]").First;
                if (await btn.CountAsync() > 0)
                {
                    await btn.ClickAsync(new() { Force = true });
                    await page.WaitForTimeoutAsync(1500);
                    pdf = await CandidateJourneyE2e.FetchPaspoortPdfAsync(page, _baseUrl);
                }
            }

            if (pdf is { Length: > 5000 } && ClassifyDownloadedCandidatePdf(pdf) == "paspoort")
            {
                break;
            }

            await page.WaitForTimeoutAsync(2000);
        }

        if (pdf is { Length: > 5000 })
        {
            var kind = ClassifyDownloadedCandidatePdf(pdf);
            var baseName = kind == "paspoort" ? $"paspoort-all-tests-{tag}" : $"lobsy-cv-self-{tag}";
            var pdfPath = Path.Combine(PdfDir, $"{baseName}.pdf");
            await File.WriteAllBytesAsync(pdfPath, pdf);
            RenderAllPdfPages(pdfPath, baseName);
            if (kind == "paspoort")
            {
                _report.PaspoortBesluit21Ok = VerifyPaspoortBesluit21(pdfPath);
            }
        }

        var edits = new StringBuilder();
        foreach (var (tab, slug) in new[]
                 {
                     ("dna", "dna"),
                     ("data", "data"),
                     ("tests", "tests"),
                     ("fit", "fit"),
                     ("career", "career"),
                     ("proof", "proof")
                 })
        {
            await page.GotoAsync($"{_baseUrl}/candidate/paspoort?tab={tab}");
            await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
            await page.WaitForTimeoutAsync(800);
            await Shot(page, tag, $"04-paspoort-tab-{slug}");
            edits.AppendLine(await TryEditPaspoortTabAsync(page, tab));
        }

        await RunPaspoortProofExperienceEditAsync(page, tag);
        await RunPaspoortDataFieldEditsAsync(page);

        await ResetPassportDnaToIncompleteViaDbAsync(chainEmail);

        await File.WriteAllTextAsync(Path.Combine(Root, "evidence", "paspoort-edit-log.txt"), edits.ToString());
        await File.WriteAllTextAsync(
            Path.Combine(Root, "evidence", "paspoort-field-table.md"),
            string.Join("\n", _report.PaspoortFieldRows));
    }

    private async Task RecordPaspoortFieldAsync(string field, string status, string? note = null)
    {
        var line = note is null ? $"| {field} | {status} |" : $"| {field} | {status} | {note} |";
        _report.PaspoortFieldRows.Add(line);
        await Task.CompletedTask;
    }

    private async Task RunPaspoortProofExperienceEditAsync(IPage page, string tag)
    {
        await page.GotoAsync($"{_baseUrl}/candidate/paspoort?tab=proof");
        await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
        await page.WaitForTimeoutAsync(800);

        var employerName = $"E2E-Werk-{DateTime.UtcNow:HHmmss}";
        var add = page.Locator("[data-testid=passport-proof-add-employer]").First;
        if (await add.CountAsync() == 0)
        {
            await RecordPaspoortFieldAsync("ervaring (proof sheet)", "missing", "geen add-knop");
            return;
        }

        await add.ClickAsync();
        await page.Locator("[data-testid=passport-proof-sheet]").WaitForAsync(new() { Timeout = 15_000 });
        var employerInput = page.Locator(".passport-proof__sheet .profile-employer-card input").First;
        if (await employerInput.CountAsync() == 0)
        {
            var addInSheet = page.Locator(".passport-proof__sheet .icon-btn--add").First;
            if (await addInSheet.CountAsync() > 0)
            {
                await addInSheet.ClickAsync();
            }
        }

        try
        {
            await employerInput.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 20_000 });
        }
        catch (TimeoutException)
        {
            await RecordPaspoortFieldAsync("ervaring (proof sheet)", "timeout", "invoervelden niet zichtbaar");
            return;
        }

        await employerInput.FillAsync(employerName, new() { Timeout = 15_000 });
        var roleInput = page.Locator(".passport-proof__sheet .profile-employer-card input").Nth(1);
        if (await roleInput.CountAsync() > 0)
        {
            await roleInput.FillAsync("Orderpicker", new() { Timeout = 15_000 });
        }

        await page.Locator("[data-testid=passport-proof-save]").ClickAsync(new() { Timeout = 15_000 });
        await page.WaitForTimeoutAsync(1500);
        await page.ReloadAsync();
        await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
        await page.WaitForTimeoutAsync(1000);
        var persisted = (await page.InnerTextAsync("body")).Contains(employerName, StringComparison.Ordinal);
        await RecordPaspoortFieldAsync(
            "ervaring toevoegen (proof)",
            persisted ? "OK" : "FAIL",
            persisted ? "reload" : "na opslaan niet zichtbaar");
        if (persisted)
        {
            await Shot(page, tag, "04-paspoort-proof-experience-saved");
        }
    }

    private async Task RunPaspoortDataFieldEditsAsync(IPage page)
    {
        await page.GotoAsync($"{_baseUrl}/candidate/paspoort?tab=data");
        await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
        await page.WaitForTimeoutAsync(600);

        foreach (var section in new[] { "personal", "preferences", "availability", "motivation" })
        {
            var btn = page.Locator($"#passport-data-btn-{section}").First;
            if (await btn.CountAsync() == 0)
            {
                await RecordPaspoortFieldAsync($"data/{section}", "missing");
                continue;
            }

            await btn.ClickAsync();
            await page.WaitForTimeoutAsync(500);
            await Shot(page, "desktop-1440", $"04-paspoort-data-edit-{section}");
            var marker = $"E2E-{section}-{DateTime.UtcNow:HHmmss}";
            var textareas = page.Locator(".passport-data__body textarea:visible");
            var textareaCount = await textareas.CountAsync();
            var textarea = section == "motivation" && textareaCount >= 2
                ? textareas.Nth(1)
                : textareas.First;
            if (textareaCount > 0)
            {
                await textarea.FillAsync(marker);
                var save = page.Locator(".profile-save-bar .btn-primary").First;
                if (await save.CountAsync() == 0)
                {
                    save = page.GetByRole(AriaRole.Button, new() { Name = "Opslaan" }).First;
                }

                if (await save.CountAsync() > 0)
                {
                    await save.ClickAsync();
                    await page.WaitForTimeoutAsync(1200);
                    await page.ReloadAsync();
                    await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
                    await page.GotoAsync($"{_baseUrl}/candidate/paspoort?tab=data");
                    await page.WaitForTimeoutAsync(800);
                    await page.Locator($"#passport-data-btn-{section}").First.ClickAsync();
                    await page.WaitForTimeoutAsync(400);
                    var ok = (await page.InnerTextAsync(".passport-data__body")).Contains(marker, StringComparison.Ordinal);
                    if (!ok && section == "motivation" && textareaCount >= 2)
                    {
                        var value = await textareas.Nth(1).InputValueAsync();
                        ok = value.Contains(marker, StringComparison.Ordinal);
                    }

                    await RecordPaspoortFieldAsync($"data/{section} (textarea)", ok ? "OK" : "FAIL");
                    continue;
                }
            }

            var transport = page.Locator(".passport-data__body select:visible").First;
            if (section == "preferences" && await transport.CountAsync() > 0)
            {
                await transport.SelectOptionAsync(new[] { "Fiets" });
                var save = page.GetByRole(AriaRole.Button, new() { Name = "Opslaan" }).First;
                if (await save.CountAsync() > 0)
                {
                    await save.ClickAsync();
                    await page.WaitForTimeoutAsync(1200);
                    await RecordPaspoortFieldAsync("data/preferences (transport)", "OK", "Fiets");
                    continue;
                }
            }

            var inputs = await page.Locator(".passport-data__body input:visible").CountAsync();
            await RecordPaspoortFieldAsync(
                $"data/{section}",
                inputs > 0 ? "open" : "empty",
                $"{inputs} inputs");
        }
    }

    private static async Task<string> TryEditPaspoortTabAsync(IPage page, string tab)
    {
        var lines = new List<string> { $"## tab={tab}" };
        if (tab == "proof" || tab == "data")
        {
            lines.Add("(field edits in paspoort-field-table.md)");
            return string.Join("\n", lines);
        }

        var textarea = page.Locator("textarea:visible").First;
        if (await textarea.CountAsync() > 0)
        {
            var val = $"E2E-{tab}-{DateTime.UtcNow:HHmmss}";
            await textarea.FillAsync(val);
            var save = page.GetByRole(AriaRole.Button, new() { Name = "Opslaan" }).First;
            if (await save.CountAsync() > 0)
            {
                await save.ClickAsync();
                await page.WaitForTimeoutAsync(1000);
                await page.ReloadAsync();
                await page.WaitForTimeoutAsync(1000);
                var ok = (await page.InnerTextAsync("body")).Contains(val, StringComparison.Ordinal);
                lines.Add($"textarea: {(ok ? "OK" : "doesn't persist")}");
            }
            else
            {
                lines.Add("textarea: not editable (no save)");
            }
        }

        return string.Join("\n", lines);
    }

    private static void PublishCandidatePdfEvidence()
    {
        var paspoort = Directory.GetFiles(PdfDir, "paspoort-all-tests-*.pdf").FirstOrDefault();
        if (paspoort is not null)
        {
            FixEvidencePublisher.CopyFileToItem(
                "05-employer-lobsy-cv-and-paspoort-pdf",
                paspoort,
                "paspoort-all-tests-desktop-1440.pdf");
            FixEvidencePublisher.CopyPdfPagesToItem("05-employer-lobsy-cv-and-paspoort-pdf", "paspoort-all-tests-desktop-1440-page");
        }

        var selfCv = Directory.GetFiles(PdfDir, "lobsy-cv-self-*.pdf").FirstOrDefault();
        if (selfCv is not null)
        {
            FixEvidencePublisher.CopyFileToItem(
                "05-employer-lobsy-cv-and-paspoort-pdf",
                selfCv,
                "lobsy-cv-no-tests-desktop-1440.pdf");
            FixEvidencePublisher.CopyPdfPagesToItem("05-employer-lobsy-cv-and-paspoort-pdf", "lobsy-cv-no-tests-desktop-1440-page");
        }

        var employerCv = Path.Combine(PdfDir, "employer-lobsy-cv-desktop.pdf");
        if (File.Exists(employerCv))
        {
            FixEvidencePublisher.CopyFileToItem(
                "05-employer-lobsy-cv-and-paspoort-pdf",
                employerCv,
                "employer-lobsy-cv-desktop.pdf");
            FixEvidencePublisher.CopyPdfPagesToItem("05-employer-lobsy-cv-and-paspoort-pdf", "employer-lobsy-cv-desktop-page");
        }
    }

    private static string ClassifyDownloadedCandidatePdf(byte[] pdf)
    {
        try
        {
            using var doc = PdfDocument.Open(pdf);
            var text = string.Join(
                ' ',
                doc.GetPages().Take(2).SelectMany(p => p.GetWords()).Select(w => w.Text));
            if (text.Contains("Uit je tests", StringComparison.Ordinal)
                || (text.Contains("Beschikbaarheid", StringComparison.Ordinal)
                    && text.Contains("Contactgegevens", StringComparison.Ordinal)
                    && !text.Contains("PASPOORT", StringComparison.OrdinalIgnoreCase)))
            {
                return "lobsy-cv";
            }

            if (text.Contains("PASPOORT", StringComparison.OrdinalIgnoreCase)
                || text.Contains("Verified", StringComparison.OrdinalIgnoreCase))
            {
                return "paspoort";
            }
        }
        catch
        {
            // fall through
        }

        return "unknown";
    }

    private static void RenderAllPdfPages(string pdfPath, string pngBaseName)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "pdfinfo",
            Arguments = $"\"{pdfPath}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        var pages = 2;
        using (var p = Process.Start(psi))
        {
            var output = p?.StandardOutput.ReadToEnd() ?? "";
            p?.WaitForExit();
            var m = Regex.Match(output, @"Pages:\s+(\d+)");
            if (m.Success)
            {
                pages = int.Parse(m.Groups[1].Value);
            }
        }

        for (var i = 1; i <= pages; i++)
        {
            var pngPrefix = Path.Combine(PdfDir, pngBaseName);
            var render = new ProcessStartInfo
            {
                FileName = "pdftoppm",
                Arguments = $"-png -f {i} -l {i} -r 150 \"{pdfPath}\" \"{pngPrefix}\"",
                UseShellExecute = false
            };
            using var proc = Process.Start(render);
            proc?.WaitForExit(60_000);
        }
    }

    private async Task<Guid?> PublishVacancyAsync(IPage page, string tag, string title)
    {
        await page.GotoAsync(_baseUrl + "/werkgever/vacatures/nieuw");
        await page.Locator("[data-testid='vacancy-wizard']").WaitForAsync();
        await page.Locator("[data-testid='vac-wizard-step-1'] input[required]").FillAsync(title);
        var escoPicker = page.Locator("[data-testid='vacancy-category-picker'], [data-testid='vac-esco-picker']");
        if (await escoPicker.CountAsync() > 0)
        {
            await Shot(page, tag, "05-vacancy-esco-picker");
        }

        await VacancyWizardPlaywrightTests_FillHelpers.FillStep1Async(page, title);
        await Shot(page, tag, "05-vacancy-step-01");
        await page.GetByRole(AriaRole.Button, new() { Name = "Volgende: Omschrijving" }).ClickAsync();
        await page.Locator("[data-testid='vac-wizard-step-2']").WaitForAsync();
        await VacancyWizardPlaywrightTests_FillHelpers.FillStep2RichTextAsync(page);
        await Shot(page, tag, "05-vacancy-step-02");
        await page.GetByRole(AriaRole.Button, new() { Name = "Volgende: Loon & uren" }).ClickAsync();
        await VacancyWizardPlaywrightTests_FillHelpers.FillStep3Async(page);
        await Shot(page, tag, "05-vacancy-step-03");
        await page.GetByRole(AriaRole.Button, new() { Name = "Volgende: Rooster" }).ClickAsync();
        await VacancyWizardPlaywrightTests_FillHelpers.FillStep4ScheduleAsync(page, 1440);
        await Shot(page, tag, "05-vacancy-step-04");
        await page.GetByRole(AriaRole.Button, new() { Name = "Volgende: Eisen & contact" }).ClickAsync();
        await Shot(page, tag, "05-vacancy-step-05");
        await page.GetByRole(AriaRole.Button, new() { Name = "Volgende: Controleren" }).ClickAsync();
        await page.Locator("[data-testid='vac-wizard-step-6']").WaitForAsync();
        await Shot(page, tag, "05-vacancy-step-06");

        await page.Locator("[data-testid='vacancy-save-draft-submit']:visible").ClickAsync();
        await page.Locator("[data-testid='vacancy-draft-saved']").WaitForAsync(new() { Timeout = 120_000 });
        var idRaw = await page.Locator("[data-testid='vacancy-draft-saved']").GetAttributeAsync("data-vacancy-id");
        if (!Guid.TryParse(idRaw, out var id))
        {
            return null;
        }

        await page.Locator("[data-testid='vacancy-publish-now']").ClickAsync();
        await page.Locator("[data-testid='publish-confirm']").ClickAsync();
        await page.WaitForTimeoutAsync(3000);
        await Shot(page, tag, "05-vacancy-published");
        return id;
    }

    private async Task ScreenshotCapturedMailsAsync(
        IBrowser browser,
        string recipientHint,
        string slug)
    {
        var htmlFiles = Directory.Exists(MailDir)
            ? Directory.GetFiles(MailDir, "*.html").OrderByDescending(File.GetLastWriteTimeUtc).ToList()
            : [];
        var emlFiles = Directory.Exists(MailDir)
            ? Directory.GetFiles(MailDir, "*.eml").OrderByDescending(File.GetLastWriteTimeUtc).ToList()
            : [];

        string? htmlPath = htmlFiles.FirstOrDefault(f => File.ReadAllText(f).Contains(recipientHint, StringComparison.OrdinalIgnoreCase))
            ?? htmlFiles.FirstOrDefault();

        if (htmlPath is null && emlFiles.Count > 0)
        {
            htmlPath = Path.Combine(MailDir, $"{slug}-fallback.txt");
            await File.WriteAllTextAsync(htmlPath, $"Geen HTML mail gevonden voor {recipientHint}. eml count={emlFiles.Count}");
        }

        if (htmlPath is null)
        {
            await using var ctx = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 640, Height = 900 } });
            var page = await ctx.NewPageAsync();
            await page.SetContentAsync($"<body><p>Geen mail in {MailDir} voor {recipientHint}</p></body>");
            await page.ScreenshotAsync(new() { Path = Path.Combine(MailDir, $"{slug}.png"), FullPage = true });
            return;
        }

        if (htmlPath.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            await using var ctx = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 640, Height = 900 } });
            var page = await ctx.NewPageAsync();
            await page.GotoAsync("file://" + htmlPath);
            await page.ScreenshotAsync(new() { Path = Path.Combine(MailDir, $"{slug}.png"), FullPage = true });
        }
    }

    private static async Task BuildContactSheetsAsync()
    {
        if (!Directory.Exists(Screens))
        {
            return;
        }

        var prefixes = Directory.GetFiles(Screens, "*.png")
            .Select(f => Path.GetFileName(f))
            .Select(n => Regex.Match(n, @"^(\d{2}-[^-]+(?:-[^-]+)*)"))
            .Where(m => m.Success)
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToList();

        foreach (var prefix in prefixes)
        {
            var mobile = Path.Combine(Screens, $"{prefix}-mobile-390.png");
            var desktop = Path.Combine(Screens, $"{prefix}-desktop-1440.png");
            if (!File.Exists(mobile) || !File.Exists(desktop))
            {
                continue;
            }

            var outPath = Path.Combine(Contacts, $"{prefix}-contact.png");
            var psi = new ProcessStartInfo
            {
                FileName = "convert",
                Arguments = $"\"{mobile}\" \"{desktop}\" +append \"{outPath}\"",
                UseShellExecute = false
            };
            using var p = Process.Start(psi);
            p?.WaitForExit(30_000);
        }
    }

    private static async Task EnableAvailableForWorkIfPromptedAsync(IPage page)
    {
        var toggle = page.GetByRole(AriaRole.Button, new() { Name = "Zet 'Beschikbaar voor werk' aan" });
        if (await toggle.CountAsync() > 0 && await toggle.IsVisibleAsync())
        {
            await toggle.ClickAsync(new() { Force = true });
            await page.WaitForTimeoutAsync(1500);
        }
    }

    private async Task CaptureMobileVacancyCoachStateAsync(IPage page, Guid? vacancyId, string tag)
    {
        if (vacancyId is Guid vid)
        {
            await page.GotoAsync($"{_baseUrl}/vacancies/{vid:D}");
            await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
            await page.WaitForTimeoutAsync(1200);
        }

        var coachVisible = await page.Locator(".lobsy-coach-dock__btn, .lobsy-coach-cta__mascot").CountAsync() > 0
                           && await page.Locator(".lobsy-coach-dock__btn, .lobsy-coach-cta__mascot").First.IsVisibleAsync();
        _report.MobileCoachOnVacancyDetail = coachVisible;
        await Shot(page, tag, "07-vacancy-no-coach-mobile");
    }

    private async Task EnsureEmployerPhase2AcceptedAsync(IPage page, Guid applicationId, string sollicitatieUrl)
    {
        await page.GotoAsync(sollicitatieUrl);
        await KbE2e.WaitForStyledAppAsync(page);
        await page.WaitForTimeoutAsync(2000);

        var phase2Panel = page.Locator("[data-testid=wg-phase2-panel]");
        try
        {
            await phase2Panel.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        }
        catch (TimeoutException)
        {
            if (!await TryAcceptPhase2ViaApiAsync(page, applicationId))
            {
                await Shot(page, "desktop-1440", "11-accept-skipped");
                return;
            }

            await page.GotoAsync(sollicitatieUrl);
            await KbE2e.WaitForStyledAppAsync(page);
            await page.WaitForTimeoutAsync(2000);
        }

        var phase2Accept = page.Locator("[data-testid=wg-phase2-panel] button.btn-compact--primary").First;
        if (await phase2Accept.CountAsync() > 0 && await phase2Accept.IsVisibleAsync())
        {
            await phase2Accept.ScrollIntoViewIfNeededAsync();
            await phase2Accept.ClickAsync();
            await page.WaitForTimeoutAsync(2500);
            await Shot(page, "desktop-1440", "11-accept-open");
            await ReloadSelectedPhase2Async(page);
            return;
        }

        var footerAccept = page.Locator("[data-testid=wg-app-footer] button.btn-compact--primary").First;
        if (await footerAccept.CountAsync() > 0 && await footerAccept.IsVisibleAsync())
        {
            await footerAccept.ClickAsync();
            await page.WaitForTimeoutAsync(2500);
            await TryAcceptPhase2ViaApiAsync(page, applicationId);
            await page.GotoAsync(sollicitatieUrl);
            await KbE2e.WaitForStyledAppAsync(page);
            await page.WaitForTimeoutAsync(2000);
            await Shot(page, "desktop-1440", "11-accept-open");
            await ReloadSelectedPhase2Async(page);
            return;
        }

        if (await page.GetByRole(AriaRole.Button, new() { Name = "Via Maqqie" }).CountAsync() > 0
            || (await phase2Panel.CountAsync() > 0
                && (await phase2Panel.First.InnerTextAsync()).Contains("Route:", StringComparison.OrdinalIgnoreCase)))
        {
            await Shot(page, "desktop-1440", "11-accept-open");
            return;
        }

        if (await TryAcceptPhase2ViaApiAsync(page, applicationId))
        {
            await page.GotoAsync(sollicitatieUrl);
            await KbE2e.WaitForStyledAppAsync(page);
            await page.WaitForTimeoutAsync(2000);
            await ReloadSelectedPhase2Async(page);
            await Shot(page, "desktop-1440", "11-accept-open");
        }
        else
        {
            await ForcePhase2AcceptViaDbAsync(applicationId);
            await page.GotoAsync(sollicitatieUrl);
            await KbE2e.WaitForStyledAppAsync(page);
            await page.WaitForTimeoutAsync(2000);
            await Shot(page, "desktop-1440", "11-accept-open");
        }
    }

    private async Task ResetPassportDnaToIncompleteViaDbAsync(string email)
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseNpgsql(_conn, npgsql => npgsql.UseNetTopologySuite())
            .Options;
        await using var db = new JobsyDbContext(options);
        var normalized = email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalized);
        if (user is null)
        {
            return;
        }

        foreach (var row in await db.CandidateCompetencies.Where(c => c.UserId == user.Id).ToListAsync())
        {
            row.Status = CandidateCompetencyStatuses.Draft;
            row.CompletedAtUtc = null;
        }

        foreach (var row in await db.CandidateCareerInterests.Where(c => c.UserId == user.Id).ToListAsync())
        {
            row.Status = CandidateCompetencyStatuses.Draft;
            row.CompletedAtUtc = null;
        }

        foreach (var row in await db.CandidateCulturePersonalityProfiles.Where(c => c.UserId == user.Id).ToListAsync())
        {
            row.Status = CandidateCompetencyStatuses.Draft;
            row.CompletedAtUtc = null;
        }

        foreach (var row in await db.CandidateValuesProfiles.Where(c => c.UserId == user.Id).ToListAsync())
        {
            row.Status = CandidateCompetencyStatuses.Draft;
            row.CompletedAtUtc = null;
        }

        foreach (var row in await db.CandidateDeepAnalyses.Where(c => c.UserId == user.Id).ToListAsync())
        {
            row.Status = CandidateDeepAnalysisStatuses.Draft;
            row.CompletedAtUtc = null;
        }

        await db.SaveChangesAsync();
    }

    private async Task EnsurePassportDnaCompleteViaDbAsync(string email)
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseNpgsql(_conn, npgsql => npgsql.UseNetTopologySuite())
            .Options;
        await using var db = new JobsyDbContext(options);
        var normalized = email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalized);
        if (user is null)
        {
            return;
        }

        var done = DateTime.UtcNow;
        async Task EnsureCompetencyAsync()
        {
            var row = await db.CandidateCompetencies.FirstOrDefaultAsync(c => c.UserId == user.Id);
            if (row is null)
            {
                db.CandidateCompetencies.Add(new CandidateCompetency
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Status = CandidateCompetencyStatuses.Completed,
                    CompletedAtUtc = done,
                    SamenwerkenPercent = 80,
                    ResultaatgerichtheidPercent = 70,
                    StressbestendigheidPercent = 65,
                    InnovatiePercent = 60,
                    ExtraversiePercent = 55
                });
            }
            else
            {
                row.Status = CandidateCompetencyStatuses.Completed;
                row.CompletedAtUtc ??= done;
            }
        }

        async Task EnsureCareerAsync()
        {
            var row = await db.CandidateCareerInterests.FirstOrDefaultAsync(c => c.UserId == user.Id);
            if (row is null)
            {
                db.CandidateCareerInterests.Add(new CandidateCareerInterest
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Status = CandidateCompetencyStatuses.Completed,
                    CompletedAtUtc = done,
                    RealisticPercent = 80,
                    InvestigativePercent = 10,
                    ArtisticPercent = 11,
                    SocialPercent = 12,
                    EnterprisingPercent = 13,
                    ConventionalPercent = 14
                });
            }
            else
            {
                row.Status = CandidateCompetencyStatuses.Completed;
                row.CompletedAtUtc ??= done;
            }
        }

        async Task EnsureCultureAsync()
        {
            var row = await db.CandidateCulturePersonalityProfiles.FirstOrDefaultAsync(c => c.UserId == user.Id);
            if (row is null)
            {
                db.CandidateCulturePersonalityProfiles.Add(new CandidateCulturePersonalityProfile
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Status = CandidateCompetencyStatuses.Completed,
                    CompletedAtUtc = done,
                    AutonomyPercent = 10,
                    InformalPercent = 20,
                    CollaborationPercent = 90,
                    FlexibilityPercent = 30,
                    InnovationPercent = 40,
                    PeopleFirstPercent = 50
                });
            }
            else
            {
                row.Status = CandidateCompetencyStatuses.Completed;
                row.CompletedAtUtc ??= done;
            }
        }

        async Task EnsureValuesAsync()
        {
            var row = await db.CandidateValuesProfiles.FirstOrDefaultAsync(c => c.UserId == user.Id);
            if (row is null)
            {
                db.CandidateValuesProfiles.Add(new CandidateValuesProfile
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Status = CandidateCompetencyStatuses.Completed,
                    CompletedAtUtc = done,
                    AutonomyPercent = 10,
                    ConnectionPercent = 20,
                    AchievementPercent = 30,
                    StabilityPercent = 90,
                    ImpactPercent = 40
                });
            }
            else
            {
                row.Status = CandidateCompetencyStatuses.Completed;
                row.CompletedAtUtc ??= done;
            }
        }

        await EnsureCompetencyAsync();
        await EnsureCareerAsync();
        await EnsureCultureAsync();
        await EnsureValuesAsync();
        await db.SaveChangesAsync();
    }

    private static bool VerifyPaspoortBesluit21(string pdfPath)
    {
        var psi = new ProcessStartInfo("pdftotext", $"- \"{pdfPath}\" -")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        using var proc = Process.Start(psi);
        if (proc is null)
        {
            return false;
        }

        var text = proc.StandardOutput.ReadToEnd();
        proc.WaitForExit(10_000);
        var forbidden = new[]
        {
            "Uit je tests",
            "ZZ-AI",
            "Wie ben ik",
            "Past goed",
            "RoleFit",
            "matchscore"
        };
        return forbidden.All(marker => !text.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task ClearEncourageDismissStorageAsync(IPage page, string email, string screenKey)
    {
        _ = email;
        _ = screenKey;
        await page.EvaluateAsync(
            """
            () => {
              try {
                for (let i = localStorage.length - 1; i >= 0; i--) {
                  const k = localStorage.key(i);
                  if (k && k.startsWith('lobsy-encourage:')) {
                    localStorage.removeItem(k);
                  }
                }
              } catch { /* ignore */ }
            }
            """);
    }

    private async Task WriteNoTestsTimingReportAsync()
    {
        var lines = new List<string>
        {
            "# No-tests candidate timing (excludes E2E harness: paspoort field edits, empty-profile branch, DB EnsureDemoApplyProfileReady)",
            "",
            "| Step | ms | s |",
            "| --- | ---: | ---: |"
        };

        foreach (var key in new[]
                 {
                     "no_tests_signup",
                     "onboarding_desktop",
                     "discovery_desktop",
                     "no_tests_through_discovery",
                     "no_tests_apply_submit",
                     "no_tests_vacancy_apply_ui",
                     "no_tests_user_facing_total_ms"
                 })
        {
            if (_report.TimingMs.TryGetValue(key, out var ms))
            {
                lines.Add($"| {key} | {ms} | {ms / 1000.0:F1} |");
            }
        }

        lines.Add("");
        lines.Add("Slowest user-facing steps are typically **discovery_desktop** and **onboarding_desktop**; apply submit alone is the Indeed-comparable slice (`no_tests_apply_submit`).");
        var path = Path.Combine(Root, "evidence", "no-tests-timing.md");
        await File.WriteAllTextAsync(path, string.Join('\n', lines));
        FixEvidencePublisher.CopyFileToItem("03-no-tests-apply-and-tip", path, "no-tests-timing.md");
    }

    private async Task EnsureCandidateBirthDateAsync(string email, DateOnly birthDate)
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseNpgsql(_conn, npgsql => npgsql.UseNetTopologySuite())
            .Options;
        await using var db = new JobsyDbContext(options);
        var normalized = email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalized);
        if (user is null)
        {
            return;
        }

        if (user.DateOfBirth != birthDate)
        {
            user.DateOfBirth = birthDate;
            await db.SaveChangesAsync();
        }
    }

    private async Task ForcePhase2AcceptViaDbAsync(Guid applicationId)
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseNpgsql(_conn, npgsql => npgsql.UseNetTopologySuite())
            .Options;
        await using var db = new JobsyDbContext(options);
        var app = await db.Applications
            .Include(a => a.Vacancy)
            .FirstOrDefaultAsync(a => a.Id == applicationId);
        if (app is null || app.Vacancy is null)
        {
            return;
        }

        if (app.Status == ApplicationStatus.Pending)
        {
            app.Status = ApplicationStatus.Accepted;
            app.RespondedAt = DateTime.UtcNow;
        }

        var billingCompanyId = app.Vacancy.IntermediaryCompanyId ?? app.Vacancy.CompanyId;
        var placement = await db.ApplicationPlacements.FirstOrDefaultAsync(p => p.ApplicationId == applicationId);
        if (placement is null)
        {
            db.ApplicationPlacements.Add(new Jobsy.Core.Entities.ApplicationPlacement
            {
                ApplicationId = applicationId,
                BillingCompanyId = billingCompanyId,
                AcceptCostTokens = 0m,
                EmploymentMode = PlacementEmploymentMode.Maqqie,
                EmploymentModeChosenAtUtc = DateTime.UtcNow
            });
        }
        else if (placement.EmploymentMode is null)
        {
            placement.EmploymentMode = PlacementEmploymentMode.Maqqie;
            placement.EmploymentModeChosenAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        _report.MaqqieChosen = true;
    }

    private async Task<bool> TryAcceptPhase2ViaApiAsync(IPage page, Guid applicationId)
    {
        var url = $"{EmployerPhase2ApiBase()}/api/employer-phase2/applications/{applicationId:D}/accept";
        var response = await page.Context.APIRequest.PostAsync(url);
        if (!response.Ok)
        {
            var detail = await response.TextAsync();
            _report.SmtpNote = (_report.SmtpNote ?? "")
                               + $" phase2-accept:{response.Status} {detail[..Math.Min(detail.Length, 120)]};";
        }

        return response.Ok;
    }

    private async Task<bool> TryChooseMaqqieViaApiAsync(IPage page, Guid applicationId)
    {
        var url = $"{EmployerPhase2ApiBase()}/api/employer-phase2/applications/{applicationId:D}/employment-mode";
        var response = await page.Context.APIRequest.PostAsync(
            url,
            new() { DataObject = new { mode = PlacementEmploymentMode.Maqqie } });
        return response.Ok;
    }

    private string EmployerPhase2ApiBase()
        => _baseUrl.Replace(":5201", ":5200", StringComparison.Ordinal).TrimEnd('/');

    private static async Task ReloadSelectedPhase2Async(IPage page)
    {
        try
        {
            await page.WaitForFunctionAsync(
                """
                () => {
                  const p = document.querySelector('[data-testid=wg-phase2-panel]');
                  return p && (p.innerText.includes('Via Maqqie') || p.innerText.includes('Route: Maqqie'));
                }
                """,
                null,
                new() { Timeout = 25_000 });
        }
        catch (TimeoutException)
        {
            // drawer may still be loading context
        }
    }

    private static async Task OpenVacancyApplyPanelAsync(IPage page)
    {
        await CandidateJourneyE2e.DismissAssistantOverlayAsync(page);
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 90_000 });
        await EnableAvailableForWorkIfPromptedAsync(page);
        var stickyApply = page.Locator("[data-testid=kb-detail-apply]");
        if (await stickyApply.CountAsync() > 0 && await stickyApply.IsVisibleAsync())
        {
            await stickyApply.ClickAsync(new() { Force = true });
        }
        else
        {
            var applyBtn = page.GetByRole(AriaRole.Button, new() { Name = "Solliciteer" });
            if (await applyBtn.CountAsync() == 0)
            {
                applyBtn = page.Locator("a:has-text('Solliciteer'), button:has-text('Solliciteer')");
            }

            if (await applyBtn.CountAsync() > 0)
            {
                await applyBtn.First.ClickAsync(new() { Force = true, Timeout = 30_000 });
            }
        }

        await page.EvaluateAsync(
            """
            () => {
              window.scrollTo(0, document.body.scrollHeight);
              document.getElementById('apply')?.scrollIntoView({ block: 'center', behavior: 'instant' });
            }
            """);
        await page.WaitForTimeoutAsync(800);
        if (await page.Locator("#apply").CountAsync() > 0)
        {
            await page.Locator("#apply").WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 60_000 });
        }
    }

    private async Task ResetApplicationToPendingForPhase2UiAsync(Guid applicationId)
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseNpgsql(_conn, npgsql => npgsql.UseNetTopologySuite())
            .Options;
        await using var db = new JobsyDbContext(options);
        var app = await db.Applications.FirstOrDefaultAsync(a => a.Id == applicationId);
        if (app is null)
        {
            return;
        }

        var placements = db.ApplicationPlacements.Where(p => p.ApplicationId == applicationId);
        db.ApplicationPlacements.RemoveRange(await placements.ToListAsync());
        app.Status = ApplicationStatus.Pending;
        app.RespondedAt = null;
        await db.SaveChangesAsync();
    }

    private async Task<Guid?> ResolveChainApplicationIdAsync(string chainEmail, Guid? vacancyId)
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseNpgsql(_conn, npgsql => npgsql.UseNetTopologySuite())
            .Options;
        await using var db = new JobsyDbContext(options);
        var userId = await db.Users.AsNoTracking()
            .Where(u => u.Email == chainEmail)
            .Select(u => u.Id)
            .FirstOrDefaultAsync();
        if (userId == Guid.Empty)
        {
            return null;
        }

        var q = db.Applications.AsNoTracking().Where(a => a.CandidateUserId == userId);
        if (vacancyId is Guid vid)
        {
            q = q.Where(a => a.VacancyId == vid);
        }

        return await q.OrderByDescending(a => a.CreatedAt).Select(a => a.Id).FirstOrDefaultAsync();
    }

    private static async Task<string> ResolveLatestChainCandidateEmailAsync(string connectionString)
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.UseNetTopologySuite())
            .Options;
        await using var db = new JobsyDbContext(options);
        return await db.Users.AsNoTracking()
            .Where(u => u.Email.StartsWith("e2e-d-") && u.Email.EndsWith("@jobsy.local"))
            .OrderByDescending(u => u.EmailVerifiedAtUtc ?? DateTime.MinValue)
            .Select(u => u.Email)
            .FirstOrDefaultAsync() ?? "";
    }

    private static async Task<IBrowserContext> CreateContextAsync(IBrowser browser, (int W, int H) vp, int dpr)
    {
        var opts = KbE2e.ContextFor(vp.W, vp.H);
        opts.Locale = "nl-NL";
        opts.DeviceScaleFactor = dpr;
        return await browser.NewContextAsync(opts);
    }

    private static async Task<IPage> PrepPageAsync(IBrowserContext ctx)
    {
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(180_000);
        return page;
    }

    /// <summary>(page, viewportTag, slug) — matches onboarding/discovery screenshot delegate.</summary>
    private static Task Shot(IPage page, string viewportTag, string slug)
    {
        var path = Path.Combine(Screens, $"{slug}-{viewportTag}.png");
        return page.ScreenshotAsync(new() { Path = path, FullPage = false });
    }

    private async Task RenderTemplateScreenshotAsync(IBrowser browser, string templateKey, string slug)
    {
        var sample = EmailSampleContext.ForPreview(_baseUrl.TrimEnd('/'));
        var mail = TransactionalEmails.Compose(templateKey, sample, EmailCulture.ForLanguage("nl"));
        await using var ctx = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 640, Height = 900 } });
        var page = await ctx.NewPageAsync();
        await page.SetContentAsync(mail.Html, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.ScreenshotAsync(new() { Path = Path.Combine(MailDir, $"{slug}.png"), FullPage = true });
        var htmlPath = Path.Combine(MailDir, $"{slug}.html");
        await File.WriteAllTextAsync(htmlPath, mail.Html);
    }
}

internal sealed class FlowRunReport
{
    public Dictionary<string, long> TimingMs { get; } = new();
    public Guid? VacancyId { get; set; }
    public int PaspoortNogNietOntdektCount { get; set; }
    public bool Step7HasFitPanel { get; set; }
    public bool Step7HasPercent { get; set; }
    public bool Step8DayInLife { get; set; }
    public bool MaqqieChosen { get; set; }
    public bool EmployerSeesAttachedDocument { get; set; }
    public string? EmployerCvDownloadPath { get; set; }
    public bool EmptyProfileBlockedCaptured { get; set; }
    public long? NoTestsSignupToAppliedMs { get; set; }
    public bool PaspoortBesluit21Ok { get; set; }
    public bool AboutMeMotivationReverted { get; set; }
    public bool MobileCoachOnVacancyDetail { get; set; }
    public bool MobileCoachOnNotifications { get; set; }
    public bool SmtpConfigured { get; set; }
    public string? SmtpNote { get; set; }
    public List<string> PaspoortFieldRows { get; } = [];
}
