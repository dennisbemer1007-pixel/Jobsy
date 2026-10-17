using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Jobsy.Core.Email;
using Jobsy.Core.Email.Model;
using Jobsy.Tests.E2e;
using Jobsy.Tests.Werkgever;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2eFlow;

internal sealed class E2eFlowRunner
{
    public const string Root = "/cursor/stores/self/e2e-flow";
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

        var mobileEmail = $"e2e-m-{Guid.NewGuid():N}@jobsy.local";
        var chainEmail = $"e2e-d-{Guid.NewGuid():N}@jobsy.local";
        const string vacancyTitle = "Orderpicker";
        Guid? vacancyId = null;

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
        var swDeskOnb = Stopwatch.StartNew();
        await using (var ctx = await CreateContextAsync(browser, KbE2e.Desktop, 1))
        {
            var page = await PrepPageAsync(ctx);
            await E2eFlowOnboarding.SignUpFreshAsync(page, _baseUrl, chainEmail, "E2eDesk");
            await Shot(page, "desktop-1440", "01-signup-done");
            await E2eFlowOnboarding.CompleteOnboardingWizardWithScreenshotsAsync(
                page, _baseUrl, "desktop-1440", Shot);
            swDeskOnb.Stop();
            _report.TimingMs["onboarding_desktop"] = swDeskOnb.ElapsedMilliseconds;

            var swDisc = Stopwatch.StartNew();
            await E2eFlowOnboarding.CompleteDiscoveryMaxDepthAsync(page, _baseUrl, "desktop-1440", Shot);
            swDisc.Stop();
            _report.TimingMs["discovery_desktop"] = swDisc.ElapsedMilliseconds;
            _report.TimingMs["onboarding_plus_discovery_desktop"] = swDeskOnb.ElapsedMilliseconds + swDisc.ElapsedMilliseconds;

            await RunPaspoortChecksAsync(page, "desktop-1440");

            vacancyId = await PublishVacancyAsync(page, "desktop-1440", vacancyTitle);
            _report.VacancyId = vacancyId;
        }

        if (vacancyId is null)
        {
            throw new InvalidOperationException("Vacancy publish failed.");
        }

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

            if (mobile)
            {
                var open = page.Locator("[data-testid=kb-filter-open], button:has-text('Filter')").First;
                if (await open.CountAsync() > 0)
                {
                    await open.ClickAsync();
                    await page.WaitForTimeoutAsync(600);
                    await Shot(page, tag, "06-banenkaart-filter-sheet");
                }
            }
            else
            {
                var filters = page.Locator("[data-testid=kb-filter-drawer-toggle], button:has-text('Filters')").First;
                if (await filters.CountAsync() > 0)
                {
                    await filters.ClickAsync();
                    await page.WaitForTimeoutAsync(600);
                    await Shot(page, tag, "06-banenkaart-filter-drawer");
                }
            }

            await CandidateJourneyE2e.SignInExistingCandidateViaEmailCodeAsync(page, _baseUrl, chainEmail);
            await page.GotoAsync($"{_baseUrl}{E2eRoutes.Banenkaart}?weergave=lijst&q={Uri.EscapeDataString(vacancyTitle)}");
            await KbE2e.WaitForStyledAppAsync(page);
            await page.WaitForTimeoutAsync(1500);
            await Shot(page, tag, "06-banenkaart-logged-in");
        }

        // —— 7–9 Chain candidate vacancy + apply ——
        var swApply = Stopwatch.StartNew();
        foreach (var (tag, mobile) in new[] { ("desktop-1440", false), ("mobile-390", true) })
        {
            await using var ctx = await CreateContextAsync(browser, mobile ? KbE2e.Mobile : KbE2e.Desktop, mobile ? 2 : 1);
            var page = await PrepPageAsync(ctx);
            await CandidateJourneyE2e.SignInExistingCandidateViaEmailCodeAsync(page, _baseUrl, chainEmail);
            await page.GotoAsync($"{_baseUrl}/vacancies/{vacancyId:D}");
            await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
            await page.WaitForTimeoutAsync(2000);
            await Shot(page, tag, "07-vacancy-match");

            var body = await page.InnerTextAsync("body");
            var hasFit = await page.Locator("[data-testid=kb-fit-panel], .kb-fit-panel").CountAsync() > 0;
            var hasPct = Regex.IsMatch(body, @"\d+\s*%") || body.Contains("match", StringComparison.OrdinalIgnoreCase);
            _report.Step7HasFitPanel = hasFit;
            _report.Step7HasPercent = hasPct;

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
            await CandidateJourneyE2e.SubmitVacancyApplyViaUiAsync(page, _baseUrl, chainEmail);
            await Shot(page, tag, "09-applied");
        }

        swApply.Stop();
        _report.TimingMs["apply_flow"] = swApply.ElapsedMilliseconds;

        await Task.Delay(3_000);
        await ScreenshotCapturedMailsAsync(browser, VacancyWizardE2e.EmployerEmail, "10-employer-new-application");
        await RenderTemplateScreenshotAsync(browser, "EmployerNewApplication", "10-employer-new-application-template");
        await RenderTemplateScreenshotAsync(browser, "ApplicationConfirmation", "09-candidate-application-confirmation-template");

        // —— 10–12 Employer ——
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

            await page.GotoAsync(_baseUrl + "/werkgever/sollicitaties");
            await KbE2e.WaitForStyledAppAsync(page);
            await page.WaitForTimeoutAsync(1500);
            await Shot(page, "desktop-1440", "11-applicants-list");

            var accept = page.GetByRole(AriaRole.Button, new() { Name = "Accepteren" }).First;
            if (await accept.CountAsync() == 0)
            {
                accept = page.Locator("button:has-text('Accepteren')").First;
            }

            if (await accept.CountAsync() > 0)
            {
                await accept.ClickAsync();
                await page.WaitForTimeoutAsync(2000);
                await Shot(page, "desktop-1440", "11-accept-open");
                var maqqie = page.GetByRole(AriaRole.Button, new() { Name = "Maqqie" }).First;
                if (await maqqie.CountAsync() == 0)
                {
                    maqqie = page.Locator("button:has-text('Maqqie')").First;
                }

                if (await maqqie.CountAsync() > 0)
                {
                    await maqqie.ClickAsync();
                    await page.WaitForTimeoutAsync(1500);
                    await Shot(page, "desktop-1440", "12-maqqie-chosen");
                    _report.MaqqieChosen = true;
                }
            }
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
            await Shot(page, "mobile-390", "13-candidate-notifications");
        }

        await File.WriteAllTextAsync(
            Path.Combine(Root, "metrics.json"),
            JsonSerializer.Serialize(_report, new JsonSerializerOptions { WriteIndented = true }));
        await BuildContactSheetsAsync();
    }

    private async Task RunPaspoortChecksAsync(IPage page, string tag)
    {
        await page.GotoAsync(_baseUrl + "/candidate/paspoort?tab=dna");
        await CandidateJourneyE2e.WaitForCandidateChromeAsync(page);
        await page.WaitForTimeoutAsync(1500);
        var body = await page.InnerTextAsync("body");
        _report.PaspoortNogNietOntdektCount = Regex.Matches(body, "Nog niet ontdekt", RegexOptions.IgnoreCase).Count;
        await Shot(page, tag, "03-paspoort-overview");

        var pdf = await CandidateJourneyE2e.FetchPaspoortPdfAsync(page, _baseUrl);
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

        if (pdf is { Length: > 5000 })
        {
            var pdfPath = Path.Combine(PdfDir, $"paspoort-{tag}.pdf");
            await File.WriteAllBytesAsync(pdfPath, pdf);
            RenderAllPdfPages(pdfPath, tag);
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

        await File.WriteAllTextAsync(Path.Combine(Root, "evidence", "paspoort-edit-log.txt"), edits.ToString());
    }

    private static async Task<string> TryEditPaspoortTabAsync(IPage page, string tab)
    {
        var lines = new List<string> { $"## tab={tab}" };
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

        foreach (var acc in await page.Locator(".passport-data__accordions button").AllAsync())
        {
            try
            {
                await acc.ClickAsync(new() { Timeout = 3_000 });
                await page.WaitForTimeoutAsync(400);
            }
            catch
            {
                // skip
            }
        }

        var inputs = page.Locator(".passport-data input:visible, .passport-data select:visible");
        var count = await inputs.CountAsync();
        lines.Add($"visible inputs in tab: {count}");
        return string.Join("\n", lines);
    }

    private static void RenderAllPdfPages(string pdfPath, string tag)
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
            var png = Path.Combine(PdfDir, $"paspoort-{tag}-page{i}.png");
            var render = new ProcessStartInfo
            {
                FileName = "pdftoppm",
                Arguments = $"-png -f {i} -l {i} -r 150 \"{pdfPath}\" \"{png[..^4]}\"",
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
    public bool SmtpConfigured { get; set; }
    public string? SmtpNote { get; set; }
}
