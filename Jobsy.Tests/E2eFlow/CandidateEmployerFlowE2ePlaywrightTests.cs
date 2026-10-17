using System.Diagnostics;
using System.Text.Json;
using Jobsy.Tests.E2e;
using Jobsy.Tests.Werkgever;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Jobsy.Tests.E2eFlow;

/// <summary>
/// Full candidate→employer flow check. Run:
/// JOBSY_E2E_FLOW_CHECK=1 JOBSY_E2E_BASE_URL=http://127.0.0.1:5201 ConnectionStrings__JobsyDb=… dotnet test --filter FlowCheck
/// Artifacts: /cursor/stores/self/e2e-flow/
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class CandidateEmployerFlowE2ePlaywrightTests
{
    private static readonly string Root = "/cursor/stores/self/e2e-flow";
    private static readonly string Screens = Path.Combine(Root, "screens");
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    private static bool ShouldRun =>
        string.Equals(Environment.GetEnvironmentVariable("JOBSY_E2E_FLOW_CHECK"), "1", StringComparison.Ordinal);

    private sealed class FlowMetrics
    {
        public List<StepMetric> Steps { get; } = [];
        public Dictionary<string, object> Timings { get; } = new();
        public List<string> ConsoleErrors { get; } = [];
        public List<string> Network404 { get; } = [];
    }

    private sealed record StepMetric(int N, string Slug, string Verdict, string? Issue);

    [Fact]
    public async Task Full_flow_check_with_screenshots_and_metrics()
    {
        if (!ShouldRun)
        {
            return;
        }

        Directory.CreateDirectory(Screens);
        Directory.CreateDirectory(Path.Combine(Root, "mail"));
        Directory.CreateDirectory(Path.Combine(Root, "pdf"));
        Directory.CreateDirectory(Path.Combine(Root, "contactsheets"));

        Environment.SetEnvironmentVariable("JOBSY_E2E_ARTIFACTS_ROOT", Root);

        var baseUrl = await KbE2e.TryReadyBaseUrlAsync()
                      ?? throw new InvalidOperationException("JOBSY_E2E_BASE_URL unreachable");

        await CandidateJourneyE2e.EnableLiveFeatureFlagsAsync(baseUrl);
        Assert.True(await CandidateJourneyE2e.WaitForFlagsAsync(baseUrl));

        var metrics = new FlowMetrics();
        var flowSw = Stopwatch.StartNew();

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        var vacancyTitle = $"E2E Flow A {Guid.NewGuid():N}"[..28];
        Guid? vacancyId = null;
        string candidateEmail = $"flow-c-{Guid.NewGuid():N}@jobsy.local";
        const string candidateFirst = "Flow";
        long onboardingMsMobile = 0, onboardingMsDesktop = 0;
        long discoveryMsMobile = 0, discoveryMsDesktop = 0;

        // —— Mobile candidate: onboarding + discovery ——
        await using (var ctx = await CreateContextAsync(browser, KbE2e.Mobile, deviceScaleFactor: 2))
        {
            var page = await PrepPageAsync(ctx, metrics);
            var sw = Stopwatch.StartNew();
            var code = await SignUpAsync(page, baseUrl, candidateEmail, candidateFirst, "mobile-390");
            Assert.False(string.IsNullOrWhiteSpace(code));
            await RunOnboardingAsync(page, baseUrl, "mobile-390", metrics, 1);
            onboardingMsMobile = sw.ElapsedMilliseconds;
            sw.Restart();
            await RunDiscoveryDeepTestsAsync(page, baseUrl, "mobile-390", metrics, 2);
            discoveryMsMobile = sw.ElapsedMilliseconds;
            await PaspoortPdfAsync(page, baseUrl, "mobile-390", metrics, 3);
            await PaspoortEditSmokeAsync(page, baseUrl, "mobile-390", metrics, 4);
        }

        // —— Desktop candidate: onboarding UX parity ——
        var desktopEmail = $"flow-d-{Guid.NewGuid():N}@jobsy.local";
        await using (var ctx = await CreateContextAsync(browser, KbE2e.Desktop))
        {
            var page = await PrepPageAsync(ctx, metrics);
            var sw = Stopwatch.StartNew();
            _ = await SignUpAsync(page, baseUrl, desktopEmail, "FlowDesk", "desktop-1440");
            await RunOnboardingAsync(page, baseUrl, "desktop-1440", metrics, 1);
            onboardingMsDesktop = sw.ElapsedMilliseconds;
            sw.Restart();
            await RunDiscoveryDeepTestsAsync(page, baseUrl, "desktop-1440", metrics, 2);
            discoveryMsDesktop = sw.ElapsedMilliseconds;
            await PaspoortPdfAsync(page, baseUrl, "desktop-1440", metrics, 3);
            await PaspoortEditSmokeAsync(page, baseUrl, "desktop-1440", metrics, 4);
        }

        // —— Employer: wizard publish (desktop) ——
        await using (var ctx = await CreateContextAsync(browser, KbE2e.Desktop))
        {
            var page = await PrepPageAsync(ctx, metrics);
            Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, VacancyWizardE2e.EmployerEmail, VacancyWizardE2e.EmployerPassword));
            vacancyId = await PublishVacancyWizardAsync(page, baseUrl, vacancyTitle, "desktop-1440", metrics, 5);
            Assert.NotNull(vacancyId);
        }

        // —— Banenkaart guest + logged in ——
        foreach (var (tag, mobile) in new[] { ("desktop-1440", false), ("mobile-390", true) })
        {
            await using var ctx = await CreateContextAsync(browser, mobile ? KbE2e.Mobile : KbE2e.Desktop, mobile ? 2 : 1);
            var page = await PrepPageAsync(ctx, metrics);
            if (mobile)
            {
                await ShotNamed(page, tag, "06-banenkaart-guest");
            }
            else
            {
                await page.GotoAsync($"{baseUrl}{E2eRoutes.Banenkaart}?weergave=lijst&q={Uri.EscapeDataString(vacancyTitle)}");
                await KbE2e.WaitForStyledAppAsync(page);
                await page.WaitForTimeoutAsync(2500);
                await ShotNamed(page, tag, "06-banenkaart-guest");
            }

            if (mobile)
            {
                var filter = page.Locator("[data-testid=kb-filter-open], button:has-text('Filters')").First;
                if (await filter.CountAsync() > 0)
                {
                    await filter.ClickAsync();
                    await page.WaitForTimeoutAsync(600);
                    await ShotNamed(page, tag, "06-banenkaart-filter-sheet");
                }
            }
            else
            {
                var drawer = page.Locator("[data-testid=kb-filter-drawer], .kb-filter-drawer").First;
                if (await drawer.CountAsync() > 0)
                {
                    await ShotNamed(page, tag, "06-banenkaart-filter-drawer");
                }
            }

            Assert.True(await SignInViaCodeAsync(page, baseUrl, candidateEmail));
            await page.GotoAsync($"{baseUrl}{E2eRoutes.Banenkaart}?weergave=lijst&q={Uri.EscapeDataString(vacancyTitle)}");
            await KbE2e.WaitForStyledAppAsync(page);
            await page.WaitForTimeoutAsync(1500);
            await ShotNamed(page, tag, "06-banenkaart-logged-in");
            metrics.Steps.Add(new(6, "banenkaart", "PARTIAL", null));
        }

        // —— Vacancy detail: match, day-in-life, apply (mobile + desktop) ——
        var applyMetrics = new ApplyMetrics();
        foreach (var (tag, mobile) in new[] { ("mobile-390", true), ("desktop-1440", false) })
        {
            await using var ctx = await CreateContextAsync(browser, mobile ? KbE2e.Mobile : KbE2e.Desktop, mobile ? 2 : 1);
            var page = await PrepPageAsync(ctx, metrics);
            Assert.True(await SignInViaCodeAsync(page, baseUrl, candidateEmail));
            var swApply = Stopwatch.StartNew();
            applyMetrics.Screens++;
            await page.GotoAsync($"{baseUrl}/vacancies/{vacancyId:D}", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
            await ShotNamed(page, tag, "07-vacancy-match");
            var body = await page.InnerTextAsync("body");
            var hasMatch = body.Contains("match", StringComparison.OrdinalIgnoreCase)
                           || body.Contains("past", StringComparison.OrdinalIgnoreCase)
                           || await page.Locator("[data-testid=kb-match], .kb-match, .match-hero").CountAsync() > 0;
            metrics.Steps.Add(new(7, "vacancy-match", hasMatch ? "PASS" : "PARTIAL", hasMatch ? null : "Geen duidelijke matchscore in UI"));

            applyMetrics.Screens++;
            var dayLink = page.Locator("a:has-text('dag in het leven'), button:has-text('dag in het leven'), [data-testid=day-in-life]").First;
            if (await dayLink.CountAsync() > 0)
            {
                await dayLink.ClickAsync();
                await page.WaitForTimeoutAsync(800);
                await ShotNamed(page, tag, "08-day-in-life");
                metrics.Steps.Add(new(8, "day-in-life", "PASS", null));
            }
            else
            {
                await ShotNamed(page, tag, "08-day-in-life-missing");
                metrics.Steps.Add(new(8, "day-in-life", "FAIL", "Geen 'dag in het leven' entry op vacaturedetail"));
            }

            if (tag == "desktop-1440")
            {
                applyMetrics.Clicks++;
                await CandidateJourneyE2e.SubmitVacancyApplyViaUiAsync(page, baseUrl, candidateEmail);
                applyMetrics.Screens++;
                await ShotNamed(page, tag, "09-applied");
                applyMetrics.ElapsedMs = swApply.ElapsedMilliseconds;
                metrics.Steps.Add(new(9, "apply", "PASS", null));
            }
            else
            {
                await ShotNamed(page, tag, "09-apply-panel");
            }
        }

        // —— Employer notifications + accept + Maqqie ——
        await using (var ctx = await CreateContextAsync(browser, KbE2e.Desktop))
        {
            var page = await PrepPageAsync(ctx, metrics);
            Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, VacancyWizardE2e.EmployerEmail, VacancyWizardE2e.EmployerPassword));
            await page.GotoAsync(baseUrl + "/werkgever/meldingen");
            await KbE2e.WaitForStyledAppAsync(page);
            await ShotNamed(page, "desktop-1440", "10-employer-notifications");
            await RenderMailStubShotAsync(browser, candidateEmail, VacancyWizardE2e.EmployerEmail, "10-employer-email");

            await page.GotoAsync(baseUrl + "/werkgever/sollicitaties");
            await KbE2e.WaitForStyledAppAsync(page);
            await page.WaitForTimeoutAsync(1200);
            await ShotNamed(page, "desktop-1440", "11-employer-applicants");
            var accept = page.GetByRole(AriaRole.Button, new() { Name = "Accepteren" }).First;
            if (await accept.CountAsync() == 0)
            {
                accept = page.Locator("button:has-text('Accepteren')").First;
            }

            if (await accept.CountAsync() > 0)
            {
                await accept.ClickAsync();
                await page.WaitForTimeoutAsync(1500);
                await ShotNamed(page, "desktop-1440", "11-accept-dialog");
                var maqqie = page.GetByRole(AriaRole.Button, new() { Name = "Maqqie" }).First;
                if (await maqqie.CountAsync() == 0)
                {
                    maqqie = page.Locator("button:has-text('Maqqie')").First;
                }

                if (await maqqie.CountAsync() > 0)
                {
                    await maqqie.ClickAsync();
                    await page.WaitForTimeoutAsync(1200);
                    await ShotNamed(page, "desktop-1440", "12-maqqie-chosen");
                    metrics.Steps.Add(new(12, "maqqie", "PASS", "Stub export via FileMaqqieHoursExporter; geen live Maqqie API"));
                }
                else
                {
                    metrics.Steps.Add(new(12, "maqqie", "PARTIAL", "Maqqie-knop niet gevonden na accept"));
                }

                metrics.Steps.Add(new(11, "employer-accept", "PASS", null));
            }
            else
            {
                metrics.Steps.Add(new(11, "employer-accept", "FAIL", "Geen Accepteren-knop"));
            }
        }

        // —— Candidate acceptance notification ——
        await using (var ctx = await CreateContextAsync(browser, KbE2e.Mobile, 2))
        {
            var page = await PrepPageAsync(ctx, metrics);
            Assert.True(await SignInViaCodeAsync(page, baseUrl, candidateEmail));
            await page.GotoAsync(baseUrl + "/notifications");
            await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
            await ShotNamed(page, "mobile-390", "13-candidate-notifications");
            await RenderMailStubShotAsync(browser, candidateEmail, candidateEmail, "13-candidate-email");
            metrics.Steps.Add(new(13, "candidate-accept-notify", "PARTIAL", "E-mail via stub; zie mail-screenshot"));
        }

        flowSw.Stop();
        metrics.Timings["onboardingMsMobile"] = onboardingMsMobile;
        metrics.Timings["onboardingMsDesktop"] = onboardingMsDesktop;
        metrics.Timings["discoveryMsMobile"] = discoveryMsMobile;
        metrics.Timings["discoveryMsDesktop"] = discoveryMsDesktop;
        metrics.Timings["applyAutomatedMs"] = applyMetrics.ElapsedMs;
        metrics.Timings["applyScreens"] = applyMetrics.Screens;
        metrics.Timings["applyClicks"] = applyMetrics.Clicks;
        metrics.Timings["totalAutomatedMs"] = flowSw.ElapsedMilliseconds;
        metrics.Timings["vacancyTitle"] = vacancyTitle;
        metrics.Timings["vacancyId"] = vacancyId?.ToString("D") ?? "";

        await File.WriteAllTextAsync(Path.Combine(Root, "metrics.json"), JsonSerializer.Serialize(metrics, JsonOpts));
    }

    private sealed class ApplyMetrics
    {
        public int Screens;
        public int Clicks;
        public long ElapsedMs;
    }

    private static async Task<IBrowserContext> CreateContextAsync(IBrowser browser, (int Width, int Height) vp, int deviceScaleFactor = 1)
    {
        var opts = KbE2e.ContextFor(vp.Width, vp.Height);
        opts.Locale = "nl-NL";
        opts.DeviceScaleFactor = deviceScaleFactor;
        return await browser.NewContextAsync(opts);
    }

    private static async Task<IPage> PrepPageAsync(IBrowserContext ctx, FlowMetrics metrics)
    {
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);
        page.Console += (_, msg) =>
        {
            if (msg.Type is "error" or "warning")
            {
                metrics.ConsoleErrors.Add($"{msg.Type}: {msg.Text}");
            }
        };
        page.Response += (_, resp) =>
        {
            if (resp.Status == 404 && !resp.Url.Contains("favicon", StringComparison.OrdinalIgnoreCase))
            {
                metrics.Network404.Add(resp.Url);
            }
        };
        return page;
    }

    private static async Task ShotNamed(IPage page, string viewportTag, string slug)
    {
        string name;
        var parts = slug.Split('-', 2);
        if (parts.Length == 2 && int.TryParse(parts[0], out var n))
        {
            name = $"{n:D2}-{parts[1]}-{viewportTag}.png";
        }
        else
        {
            name = $"{slug}-{viewportTag}.png";
        }

        var path = Path.Combine(Screens, name);
        await page.ScreenshotAsync(new() { Path = path, FullPage = false });
        await VerifyPngDimensionsAsync(path, viewportTag);
    }

    private static async Task VerifyPngDimensionsAsync(string path, string viewportTag)
    {
        if (!File.Exists(path))
        {
            return;
        }

        var (expW, expH, dpr) = viewportTag.StartsWith("mobile", StringComparison.Ordinal)
            ? (390, 844, 2)
            : (1440, 900, 1);
        expW *= dpr;
        expH *= dpr;
        await Task.Run(() =>
        {
            try
            {
                var bytes = File.ReadAllBytes(path);
                if (bytes.Length < 24)
                {
                    return;
                }

                var w = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
                var h = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
                File.AppendAllText(
                    Path.Combine(Root, "png-dimensions.log"),
                    $"{Path.GetFileName(path)}: {w}x{h} (expected ~{expW}x{expH})\n");
            }
            catch
            {
                // ignore
            }
        });
    }

    private static async Task<string?> SignUpAsync(IPage page, string baseUrl, string email, string firstName, string tag)
    {
        await page.GotoAsync(baseUrl + "/account-maken", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.WaitForSelectorAsync("#signup-title, .pub-signup__title, input[name=email]", new() { Timeout = 90_000 });
        await KbE2e.WaitForStyledAppAsync(page);
        await page.Locator("input[name=email], input[type=email]").First.FillAsync(email);
        await page.Locator("input[name=firstName]").First.FillAsync(firstName);
        await page.Locator("button.login-submit").First.ClickAsync();
        await page.WaitForURLAsync("**/account-maken/code**");
        var code = await CandidateJourneyE2e.FetchEmailCodeAsync(baseUrl, email);
        await page.Locator("input[name=code], input[inputmode=numeric]").First.FillAsync(code!);
        await page.Locator("button.login-submit, button[type=submit]").First.ClickAsync();
        await page.WaitForTimeoutAsync(1200);
        await ShotNamed(page, tag, "01-signup-done");
        return code;
    }

    private static async Task<bool> SignInViaCodeAsync(IPage page, string baseUrl, string email)
    {
        await page.GotoAsync(baseUrl + "/login");
        await page.Locator("input[type=email]").First.FillAsync(email);
        await page.Locator("button.login-submit").First.ClickAsync();
        await page.WaitForTimeoutAsync(800);
        var code = await CandidateJourneyE2e.FetchEmailCodeAsync(baseUrl, email);
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        await page.Locator("input[name=code], input[inputmode=numeric]").First.FillAsync(code);
        await page.Locator("button.login-submit").First.ClickAsync();
        await page.WaitForTimeoutAsync(1200);
        return true;
    }

    private static async Task RunOnboardingAsync(IPage page, string baseUrl, string tag, FlowMetrics metrics, int stepN)
    {
        await page.GotoAsync(baseUrl + "/candidate/start");
        await KbE2e.WaitForStyledAppAsync(page);
        var screen = 0;
        for (var i = 1; i <= 14; i++)
        {
            screen++;
            await page.EvaluateAsync("() => { const m = document.querySelector('.app-main, .ob-wizard__content'); if (m) m.scrollTop = 0; }");
            await page.WaitForTimeoutAsync(350);
            await ShotNamed(page, tag, $"{stepN:D2}-onboarding-{i:D2}");
            if (await page.Locator(".ob-wizard__finish").CountAsync() > 0)
            {
                await CandidateJourneyE2e.ClickPrimaryAsync(page);
                break;
            }

            if (await page.Locator(".test-flow, fieldset.q-likert").CountAsync() > 0)
            {
                await CandidateJourneyE2e.AnswerLikertUntilDoneAsync(page, 8, 4);
                await CandidateJourneyE2e.TryAdvanceMiniTestWizardAsync(page);
                continue;
            }

            await FillOnboardingFieldsAsync(page);
            var skip = page.Locator(".ob-wizard__ghost").Filter(new() { HasText = "Weet ik nog niet" }).First;
            if (await skip.CountAsync() > 0)
            {
                await skip.ClickAsync();
                continue;
            }

            await CandidateJourneyE2e.ClickPrimaryAsync(page);
            await page.WaitForTimeoutAsync(500);
        }

        await ShotNamed(page, tag, $"{stepN:D2}-onboarding-finish");
        metrics.Steps.Add(new(stepN, "onboarding", "PARTIAL", $"Automated {screen} screens; handmatige UX-review"));
    }

    private static async Task FillOnboardingFieldsAsync(IPage page)
    {
        var fields = page.Locator(".ob-fields input:visible");
        if (await fields.CountAsync() >= 3)
        {
            if (string.IsNullOrWhiteSpace(await fields.Nth(1).InputValueAsync()))
            {
                await fields.Nth(1).FillAsync("Jansen");
            }

            if (string.IsNullOrWhiteSpace(await fields.Nth(2).InputValueAsync()))
            {
                await fields.Nth(2).FillAsync("2671GB");
            }

            await page.WaitForTimeoutAsync(400);
            var suggestion = page.Locator(".ob-wizard__suggestions button").First;
            if (await suggestion.CountAsync() > 0)
            {
                await suggestion.ClickAsync();
            }
        }

        foreach (var sel in new[]
                 {
                     ".ob-wizard__presets button:not(.is-selected)",
                     ".ob-wizard__transport button:not(.is-selected)",
                     ".ob-wizard__education-grid button:not(.is-selected)"
                 })
        {
            var pick = page.Locator(sel).First;
            if (await pick.CountAsync() > 0)
            {
                await pick.ClickAsync();
            }
        }

        var birth = page.Locator("[data-testid=onboarding-birth-date] input[type=date]").First;
        if (await birth.CountAsync() > 0 && string.IsNullOrWhiteSpace(await birth.InputValueAsync()))
        {
            await birth.FillAsync(DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-22).ToString("yyyy-MM-dd"));
        }
    }

    private static async Task RunDiscoveryDeepTestsAsync(IPage page, string baseUrl, string tag, FlowMetrics metrics, int stepN)
    {
        await page.GotoAsync(baseUrl + "/candidate/ontdekkingsreis");
        await KbE2e.WaitForStyledAppAsync(page, "ontdekkingsreis.css");
        var rounds = 0;
        for (var r = 0; r < 240; r++)
        {
            rounds++;
            if (await page.Locator(".journey-end, .journey-klaar, [data-screen=klaar]").CountAsync() > 0)
            {
                break;
            }

            if (await page.Locator(".journey-shed").CountAsync() > 0)
            {
                await CandidateJourneyE2e.SelectMaxShedDepthAsync(page);
                await CandidateJourneyE2e.ClickPrimaryAsync(page);
                continue;
            }

            if (await page.Locator(".test-flow, fieldset.q-likert").CountAsync() > 0)
            {
                await CandidateJourneyE2e.AnswerLikertUntilDoneAsync(page, 30, 4);
                _ = await CandidateJourneyE2e.TryAdvanceDiscoveryStepAsync(page);
                continue;
            }

            _ = await CandidateJourneyE2e.TryAdvanceDiscoveryStepAsync(page);
        }

        await ShotNamed(page, tag, $"{stepN:D2}-discovery-results");
        metrics.Steps.Add(new(stepN, "discovery-tests", "PARTIAL", $"~{rounds} automation rounds"));
    }

    private static async Task PaspoortPdfAsync(IPage page, string baseUrl, string tag, FlowMetrics metrics, int stepN)
    {
        await page.GotoAsync(baseUrl + "/candidate/paspoort?tab=dna");
        await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
        await ShotNamed(page, tag, $"{stepN:D2}-paspoort-overview");
        var pdf = await CandidateJourneyE2e.FetchPaspoortPdfAsync(page, baseUrl);
        if (pdf is null || pdf.Length < 5000)
        {
            var btn = page.Locator("[data-testid=passport-download-pdf]").First;
            if (await btn.CountAsync() > 0)
            {
                await btn.ClickAsync(new() { Force = true });
                await page.WaitForTimeoutAsync(1000);
                pdf = await CandidateJourneyE2e.FetchPaspoortPdfAsync(page, baseUrl);
            }
        }

        if (pdf is { Length: > 5000 })
        {
            var pdfDir = Path.Combine(Root, "pdf");
            var pdfPath = Path.Combine(pdfDir, $"paspoort-{tag}.pdf");
            await File.WriteAllBytesAsync(pdfPath, pdf);
            for (var p = 1; p <= 2; p++)
            {
                var png = Path.Combine(pdfDir, $"paspoort-{tag}-page{p}.png");
                await RenderPdfPageAsync(pdfPath, png, p);
            }

            await ShotNamed(page, tag, $"{stepN:D2}-paspoort-pdf-ui");
            metrics.Steps.Add(new(stepN, "paspoort-pdf", "PARTIAL", null));
        }
        else
        {
            metrics.Steps.Add(new(stepN, "paspoort-pdf", "FAIL", "PDF download leeg/timeout"));
        }
    }

    private static Task RenderPdfPageAsync(string pdfPath, string pngPath, int page)
    {
        return Task.Run(() =>
        {
            var psi = new ProcessStartInfo
            {
                FileName = "pdftoppm",
                Arguments = $"-png -f {page} -l {page} -r 150 \"{pdfPath}\" \"{pngPath[..^4]}\"",
                RedirectStandardError = true,
                UseShellExecute = false
            };
            using var proc = Process.Start(psi);
            proc?.WaitForExit(60_000);
        });
    }

    private static async Task PaspoortEditSmokeAsync(IPage page, string baseUrl, string tag, FlowMetrics metrics, int stepN)
    {
        await page.GotoAsync(baseUrl + "/candidate/paspoort?tab=data");
        await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
        await ShotNamed(page, tag, $"{stepN:D2}-paspoort-data");
        var about = page.Locator("textarea").First;
        if (await about.CountAsync() > 0)
        {
            var val = $"E2E edit {DateTime.UtcNow:HHmmss}";
            await about.FillAsync(val);
            var save = page.GetByRole(AriaRole.Button, new() { Name = "Opslaan" }).First;
            if (await save.CountAsync() > 0)
            {
                await save.ClickAsync();
                await page.WaitForTimeoutAsync(800);
            }

            await page.ReloadAsync();
            await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
            var body = await page.InnerTextAsync("body");
            metrics.Steps.Add(new(stepN, "paspoort-edit", body.Contains(val, StringComparison.Ordinal) ? "PARTIAL" : "FAIL",
                body.Contains(val, StringComparison.Ordinal) ? "Alleen about-me getest" : "About-me niet persistent"));
        }
    }

    private static async Task<Guid?> PublishVacancyWizardAsync(
        IPage page,
        string baseUrl,
        string title,
        string tag,
        FlowMetrics metrics,
        int stepN)
    {
        await page.GotoAsync(baseUrl + "/werkgever/vacatures/nieuw");
        await page.Locator("[data-testid='vacancy-wizard']").WaitForAsync();
        await VacancyWizardPlaywrightTests_FillHelpers.FillStep1Async(page, title);
        await page.GetByRole(AriaRole.Button, new() { Name = "Volgende: Omschrijving" }).ClickAsync();
        await page.Locator("[data-testid='vac-wizard-step-2']").WaitForAsync();
        await VacancyWizardPlaywrightTests_FillHelpers.FillStep2RichTextAsync(page);
        await page.GetByRole(AriaRole.Button, new() { Name = "Volgende: Loon & uren" }).ClickAsync();
        await VacancyWizardPlaywrightTests_FillHelpers.FillStep3Async(page);
        await page.GetByRole(AriaRole.Button, new() { Name = "Volgende: Rooster" }).ClickAsync();
        await VacancyWizardPlaywrightTests_FillHelpers.FillStep4ScheduleAsync(page, 1440);
        await page.GetByRole(AriaRole.Button, new() { Name = "Volgende: Eisen & contact" }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Volgende: Controleren" }).ClickAsync();
        await page.Locator("[data-testid='vac-wizard-step-6']").WaitForAsync();
        await ShotNamed(page, tag, $"{stepN:D2}-vacancy-wizard-review");
        await page.Locator("[data-testid='vacancy-save-draft-submit']:visible").ClickAsync();
        await page.Locator("[data-testid='vacancy-draft-saved']").WaitForAsync(new() { Timeout = 120_000 });
        var idRaw = await page.Locator("[data-testid='vacancy-draft-saved']").GetAttributeAsync("data-vacancy-id");
        if (!Guid.TryParse(idRaw, out var id))
        {
            id = Guid.Empty;
        }
        await page.Locator("[data-testid='vacancy-publish-now']").ClickAsync();
        await page.Locator("[data-testid='publish-confirm']").ClickAsync();
        await page.WaitForTimeoutAsync(2500);
        await ShotNamed(page, tag, $"{stepN:D2}-vacancy-published");
        metrics.Steps.Add(new(stepN, "vacancy-wizard", id != Guid.Empty ? "PASS" : "FAIL", null));
        return id == Guid.Empty ? null : id;
    }

    private static async Task RenderMailStubShotAsync(IBrowser browser, string to, string queryEmail, string slug)
    {
        var api = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "http://127.0.0.1:5201")
            .Replace(":5201", ":5200", StringComparison.Ordinal);
        string body;
        try
        {
            using var http = new HttpClient();
            var json = await http.GetStringAsync($"{api}/api/e2e/last-email-code?email={Uri.EscapeDataString(queryEmail)}");
            body = $"<html><body style='font-family:sans-serif;padding:24px'><h1>E-mail (dev stub)</h1><p>To: {to}</p><pre>{System.Net.WebUtility.HtmlEncode(json)}</pre><p>Geen SMTP-catcher geconfigureerd; echte HTML staat in PlatformLog (Subject only).</p></body></html>";
        }
        catch
        {
            body = $"<html><body><p>Geen captured mail voor {queryEmail}</p></body></html>";
        }

        await using var ctx = await browser.NewContextAsync(new() { ViewportSize = new() { Width = 600, Height = 800 } });
        var page = await ctx.NewPageAsync();
        await page.SetContentAsync(body);
        await page.ScreenshotAsync(new() { Path = Path.Combine(Root, "mail", $"{slug}.png") });
    }
}

/// <summary>Expose private fill helpers from vacancy wizard tests.</summary>
internal static class VacancyWizardPlaywrightTests_FillHelpers
{
    public static Task FillStep1Async(IPage page, string title) =>
        Invoke(typeof(VacancyWizardPlaywrightTests), "FillStep1Async", page, title);

    public static Task FillStep2RichTextAsync(IPage page) =>
        Invoke(typeof(VacancyWizardPlaywrightTests), "FillStep2RichTextAsync", page);

    public static Task FillStep3Async(IPage page) =>
        Invoke(typeof(VacancyWizardPlaywrightTests), "FillStep3Async", page);

    public static Task FillStep4ScheduleAsync(IPage page, int width) =>
        Invoke(typeof(VacancyWizardPlaywrightTests), "FillStep4ScheduleAsync", page, width);

    private static Task Invoke(Type t, string name, params object?[] args)
    {
        var mi = t.GetMethod(name, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        if (mi is null)
        {
            throw new MissingMethodException(t.Name, name);
        }

        var result = mi.Invoke(null, args);
        return result as Task ?? Task.CompletedTask;
    }
}
