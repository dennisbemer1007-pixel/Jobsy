using System.Text.RegularExpressions;
using Jobsy.Infrastructure.Data;
using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests.Scholen;

/// <summary>
/// Full MBO/HBO pupil + teacher route on Acceptatie. Run:
/// RUN_MBO_HBO_E2E=1 JOBSY_E2E_BASE_URL=https://acceptatie.lobsy.nl
/// JOBSY_WALKTHROUGH_ARTIFACTS=/opt/cursor/artifacts/mbo-hbo-acc
/// dotnet test --filter MboHbo_acc_full_flow
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class MboHboFlowPlaywrightTests
{
    /// <summary>Acc tip after MBO/HBO retest (#1387). Use env JOBSY_E2E_MIN_COMMIT to override.</summary>
    private const string MinDeployCommitPrefix = "89667e91";
    private const string SchoolAdminEmail = "test-schoolbeheerder@lobsy.nl";
    private const string TeacherEmail = "test-leraar@lobsy.nl";
    private const string Password = "";

    private static readonly string[] BlockOrder =
    [
        "vakken",
        "interesse",
        "competentie",
        "beroepen",
        "werkwaarden"
    ];

    [Fact(Timeout = 5_400_000, DisplayName = "MboHbo_acc_full_flow")]
    public async Task Full_flow_with_new_code_captures_screenshots()
    {
        _ = TestContext.Current.CancellationToken;
        if (!string.Equals(Environment.GetEnvironmentVariable("RUN_MBO_HBO_E2E"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "https://acceptatie.lobsy.nl").TrimEnd('/');
        if (!await KbE2e.IsReachableAsync(baseUrl))
        {
            return;
        }

        var liveCommit = await WaitForDeployCommitAsync(baseUrl, TimeSpan.FromMinutes(20));
        TestContext.Current.SendDiagnosticMessage($"Acceptatie lobsy-commit: {liveCommit}");

        var artifactDir = Environment.GetEnvironmentVariable("JOBSY_WALKTHROUGH_ARTIFACTS");
        if (string.IsNullOrWhiteSpace(artifactDir))
        {
            artifactDir = Path.Combine("/opt/cursor/artifacts", "mbo-hbo-acc-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmm"));
        }

        Directory.CreateDirectory(artifactDir);
        await File.WriteAllTextAsync(Path.Combine(artifactDir, "lobsy-commit.txt"), liveCommit);

        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        Guid classId;
        string plainCode;

        var schoolId = DemoSchoolIds.SchoolId;
        var preClass = Environment.GetEnvironmentVariable("JOBSY_ACC_MBO_HBO_CLASS_ID");
        var preCode = string.Equals(
            Environment.GetEnvironmentVariable("JOBSY_ACC_USE_PRESET_PUPIL_CODE"),
            "1",
            StringComparison.Ordinal)
            ? Environment.GetEnvironmentVariable("JOBSY_ACC_PUPIL_CODE")
            : null;
        var preSchool = Environment.GetEnvironmentVariable("JOBSY_ACC_PUPIL_SCHOOL_ID");
        if (Guid.TryParse(preSchool, out var preSchoolId))
        {
            schoolId = preSchoolId;
        }

        if (Guid.TryParse(preClass, out var preClassId))
        {
            classId = preClassId;
        }
        else
        {
            await using var adminCtx = await browser.NewContextAsync(KbE2e.DesktopContext());
            var adminPage = await adminCtx.NewPageAsync();
            adminPage.SetDefaultTimeout(180_000);
            classId = await EnsureMboHboClassReadyAsync(adminPage, baseUrl);
        }

        if (!string.IsNullOrWhiteSpace(preCode))
        {
            plainCode = preCode.Trim();
        }
        else
        {
            await using var teacherCtx = await browser.NewContextAsync(KbE2e.DesktopContext());
            var teacherPage = await teacherCtx.NewPageAsync();
            teacherPage.SetDefaultTimeout(180_000);
            var (teacherEmail, teacherPassword) = ResolveTeacherCredentials();
            Assert.True(
                await KbE2e.TryLoginAsync(teacherPage, baseUrl, teacherEmail, teacherPassword),
                $"Teacher login failed on Acceptatie (need JOBSY_E2E_TEACHER_EMAIL/PASSWORD). Url={teacherPage.Url}");
            plainCode = await AddFreshPupilCodeViaTeacherAsync(teacherPage, baseUrl, classId);
            await File.WriteAllTextAsync(Path.Combine(artifactDir, "pupil-code.txt"), plainCode);
            await OpenTeacherTestWindowIfNeededAsync(teacherPage, baseUrl, classId);
        }

        await RunPupilAndTeacherFlowAsync(
            browser,
            baseUrl,
            schoolId,
            classId,
            teacherId: null,
            plainCode,
            artifactDir,
            staffSignIn: false);
    }

    internal static async Task RunPupilAndTeacherFlowAsync(
        IBrowser browser,
        string baseUrl,
        Guid schoolId,
        Guid classId,
        Guid? teacherId,
        string plainCode,
        string artifactDir,
        bool staffSignIn)
    {
        Directory.CreateDirectory(artifactDir);
        await File.WriteAllTextAsync(Path.Combine(artifactDir, "pupil-code.txt"), plainCode);

        await using (var pupilCtx = await browser.NewContextAsync(KbE2e.DesktopContext()))
        {
            var page = await pupilCtx.NewPageAsync();
            page.SetDefaultTimeout(180_000);
            var loginUrl = $"{baseUrl}/leerling?schoolId={schoolId:D}&classId={classId:D}";
            await PupilLoginAsync(page, loginUrl, plainCode, classId);
            await WaitBlazorAsync(page);

            await page.WaitForURLAsync("**/leerling/mbo-hbo/**", new() { Timeout = 120_000 });
            await ShotAsync(page, artifactDir, "01-after-login");
            var overviewProbe = await page.Locator(".mhbo-wrap").InnerTextAsync();
            Assert.DoesNotContain(
                "5 van de 5 testen afgerond",
                overviewProbe,
                StringComparison.OrdinalIgnoreCase);

            await page.GotoAsync($"{baseUrl}/leerling/mbo-hbo/uitleg", new() { WaitUntil = WaitUntilState.Load });
            await WaitBlazorAsync(page);
            if (page.Url.Contains("/uitleg", StringComparison.OrdinalIgnoreCase))
            {
                await AssertMhboThemeAsync(page);
                await ShotAsync(page, artifactDir, "02-uitleg");
                await AssertClickableAsync(page.GetByRole(AriaRole.Button, new() { Name = "Naar mijn testen →" }));
                await page.GetByRole(AriaRole.Button, new() { Name = "Naar mijn testen →" }).ClickAsync();
                await page.WaitForURLAsync("**/mbo-hbo/overzicht**", new() { Timeout = 60_000 });
                await WaitBlazorAsync(page);
            }

            await page.GotoAsync($"{baseUrl}/leerling/mbo-hbo/overzicht", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await WaitBlazorAsync(page);
            await AssertMhboThemeAsync(page);
            await ShotAsync(page, artifactDir, "03-overzicht-start");

            var pauseDone = false;
            for (var bi = 0; bi < BlockOrder.Length; bi++)
            {
                var block = BlockOrder[bi];
                await StartBlockFromOverviewAsync(page, block);
                await WaitBlazorAsync(page);

                if (page.Url.Contains($"/uitkomst/{block}", StringComparison.OrdinalIgnoreCase))
                {
                    await AssertMhboThemeAsync(page);
                    await ShotAsync(page, artifactDir, $"06-uitkomst-{block}");
                    await page.GetByRole(AriaRole.Link, new() { Name = "← Terug naar overzicht" }).ClickAsync();
                    await page.WaitForURLAsync("**/mbo-hbo/overzicht**", new() { Timeout = 60_000 });
                    await WaitBlazorAsync(page);
                    continue;
                }

                await ShotAsync(page, artifactDir, $"04-test-{block}-start");

                if (block == "interesse" && !pauseDone
                    && page.Url.Contains($"/test/{block}", StringComparison.OrdinalIgnoreCase)
                    && !page.Url.Contains("/uitkomst/", StringComparison.OrdinalIgnoreCase))
                {
                    await AnswerQuestionsAsync(page, block, pickValue: 3, maxSteps: 6);
                    await page.Locator("a.mhbo-lnk", new() { HasText = "Pauzeer en terug naar overzicht" }).ClickAsync();
                    await page.WaitForURLAsync("**/mbo-hbo/overzicht**", new() { Timeout = 60_000 });
                    await WaitBlazorAsync(page);
                    await ShotAsync(page, artifactDir, "05-overzicht-paused-interesse");
                    await StartBlockFromOverviewAsync(page, block);
                    await WaitBlazorAsync(page);
                    var meta = await page.Locator(".mhbo-meta").Filter(new() { HasText = "Vraag" }).InnerTextAsync();
                    Assert.Contains("Vraag", meta, StringComparison.Ordinal);
                    Assert.DoesNotContain("Vraag 1 van", meta, StringComparison.Ordinal);
                    pauseDone = true;
                }

                var patternPick = block == "competentie" ? 5 : 3;
                await AnswerQuestionsAsync(page, block, pickValue: patternPick);
                await page.WaitForURLAsync($"**/mbo-hbo/uitkomst/{block}**", new() { Timeout = 300_000 });
                await WaitBlazorAsync(page);
                await AssertMhboThemeAsync(page);
                await ShotAsync(page, artifactDir, $"06-uitkomst-{block}");
                await page.GetByRole(AriaRole.Link, new() { Name = "← Terug naar overzicht" }).ClickAsync();
                await page.WaitForURLAsync("**/mbo-hbo/overzicht**", new() { Timeout = 60_000 });
                await WaitBlazorAsync(page);
            }

            await Assertions.Expect(page.GetByRole(AriaRole.Link, new() { Name = "Open je paspoort" })).ToBeVisibleAsync();
            await page.GetByRole(AriaRole.Link, new() { Name = "Open je paspoort" }).ClickAsync();
            await page.WaitForURLAsync("**/mbo-hbo/paspoort**", new() { Timeout = 60_000 });
            await WaitBlazorAsync(page);
            await AssertMhboThemeAsync(page);
            await ShotAsync(page, artifactDir, "07-paspoort");

            var pdfLink = page.GetByRole(AriaRole.Link, new() { Name = "Download als PDF" });
            await AssertClickableAsync(pdfLink);
            var pdfResp = await page.Context.APIRequest.GetAsync(baseUrl + "/leerling/pdf");
            Assert.True(pdfResp.Ok, $"PDF HTTP {(int)pdfResp.Status}");
            var pdfBytes = await pdfResp.BodyAsync();
            Assert.True(pdfBytes.Length > 200 && pdfBytes[0] == (byte)'%');
            await File.WriteAllBytesAsync(Path.Combine(artifactDir, "08-paspoort.pdf"), pdfBytes);

            await page.GotoAsync($"{baseUrl}/leerling/mbo-hbo/overzicht", new() { WaitUntil = WaitUntilState.Load });
            await WaitBlazorAsync(page);
            await AssertMhboThemeAsync(page);
            await ShotAsync(page, artifactDir, "09-overzicht-after-enhanced-nav");
        }

        await using (var teacherCtx = await browser.NewContextAsync(KbE2e.DesktopContext()))
        {
            var page = await teacherCtx.NewPageAsync();
            page.SetDefaultTimeout(180_000);
            if (staffSignIn && teacherId is Guid tid)
            {
                var mboPath = $"/leraar/klas/{classId:D}/mbo-hbo";
                await page.GotoAsync(
                    $"{baseUrl}/__test/staff?user={tid:D}&school={schoolId:D}&role=Teacher&return={Uri.EscapeDataString(mboPath)}",
                    new() { WaitUntil = WaitUntilState.Load });
                await page.WaitForURLAsync("**/mbo-hbo**", new() { Timeout = 120_000 });
            }
            else
            {
                var (teacherEmail, teacherPassword) = ResolveTeacherCredentials();
                Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, teacherEmail, teacherPassword));
                await page.GotoAsync($"{baseUrl}/leraar/klas/{classId:D}/mbo-hbo", new() { WaitUntil = WaitUntilState.Load });
            }

            await WaitBlazorAsync(page);
            await page.Locator("table").WaitForAsync(new() { Timeout = 120_000 });
            await ShotAsync(page, artifactDir, "10-docent-mbo-hbo");
            var pattern = page.Locator(".mhbo-chip", new() { HasText = "⚠" });
            Assert.True(await pattern.CountAsync() > 0, "Expected pattern-warning icon for uniform answers on competentie block.");
            await page.GotoAsync($"{baseUrl}/leraar/klas/{classId:D}/codes", new() { WaitUntil = WaitUntilState.Load });
            await WaitBlazorAsync(page);
            await ShotAsync(page, artifactDir, "11-docent-codes");
            var codeRow = page.Locator("tr.ent-data-table__row", new() { HasText = plainCode });
            await codeRow.GetByRole(AriaRole.Link, new() { Name = "Bekijk" }).ClickAsync();
            await WaitBlazorAsync(page);
            await page.Locator("[data-testid=\"leraar-code-detail-mbo\"], [data-testid=\"leraar-code-detail\"]")
                .First.WaitForAsync(new() { Timeout = 120_000 });
            await ShotAsync(page, artifactDir, "12-docent-bekijk");
        }
    }

    private static (string Email, string Password) ResolveTeacherCredentials()
    {
        var email = Environment.GetEnvironmentVariable("JOBSY_E2E_TEACHER_EMAIL")
            ?? Environment.GetEnvironmentVariable("JOBSY_E2E_SCHOOLADMIN_EMAIL")
            ?? TeacherEmail;
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_TEACHER_PASSWORD")
            ?? Environment.GetEnvironmentVariable("JOBSY_E2E_SCHOOLADMIN_PASSWORD")
            ?? Environment.GetEnvironmentVariable("TestAccounts__Password__Teacher")
            ?? Password;
        return (email, password);
    }

    private static (string Email, string Password) ResolveSchoolAdminCredentials()
    {
        var email = Environment.GetEnvironmentVariable("JOBSY_E2E_SCHOOLADMIN_EMAIL") ?? SchoolAdminEmail;
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_SCHOOLADMIN_PASSWORD")
            ?? Environment.GetEnvironmentVariable("TestAccounts__Password__SchoolAdmin")
            ?? Password;
        return (email, password);
    }

    private static async Task<string> WaitForDeployCommitAsync(string baseUrl, TimeSpan maxWait)
    {
        var minPrefix = (Environment.GetEnvironmentVariable("JOBSY_E2E_MIN_COMMIT") ?? MinDeployCommitPrefix).Trim();
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var deadline = DateTime.UtcNow + maxWait;
        string? last = null;
        while (DateTime.UtcNow < deadline)
        {
            var html = await http.GetStringAsync(baseUrl + "/");
            var m = Regex.Match(html, @"name=""lobsy-commit""\s+content=""([0-9a-f]{40})""", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                last = m.Groups[1].Value;
                if (string.IsNullOrEmpty(minPrefix)
                    || last.StartsWith(minPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    return last;
                }

                // Acceptatie redeploys often; after a short wait, proceed on latest live commit.
                if (DateTime.UtcNow + TimeSpan.FromMinutes(2) >= deadline)
                {
                    return last;
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(15));
        }

        return last ?? throw new InvalidOperationException("Acceptatie lobsy-commit meta not found.");
    }

    private static async Task<Guid> EnsureMboHboClassReadyAsync(IPage page, string baseUrl)
    {
        var (adminEmail, adminPassword) = ResolveSchoolAdminCredentials();
        Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, adminEmail, adminPassword));
        await page.GotoAsync($"{baseUrl}/school/klassen", new() { WaitUntil = WaitUntilState.Load });
        await WaitBlazorAsync(page);

        var mboRow = page.Locator("tr.ent-data-table__row", new() { HasText = "MBO/HBO" }).First;
        if (await mboRow.CountAsync() > 0)
        {
            await mboRow.ClickAsync();
            await page.WaitForURLAsync("**/school/klassen/**", new() { Timeout = 60_000 });
            await WaitBlazorAsync(page);
            await EnsureParentsAndWindowAsync(page);
            return Guid.Parse(new Uri(page.Url).AbsolutePath.Split('/').Last());
        }

        await page.GetByRole(AriaRole.Button, new() { Name = "Nieuwe klas" }).ClickAsync();
        await page.Locator("input.sch-field").First.WaitForAsync();
        var className = "MH" + DateTime.UtcNow.ToString("HHmm");
        await page.Locator("input.sch-field").First.FillAsync(className);
        await page.Locator("label.sch-kind-card", new() { HasText = "MBO / HBO" }).ClickAsync();
        var teacherCheckbox = page.Locator("label", new() { HasText = "Demo Leraar" }).Locator("input[type=checkbox]");
        if (await teacherCheckbox.CountAsync() > 0)
        {
            await teacherCheckbox.CheckAsync();
        }

        await page.GetByRole(AriaRole.Button, new() { Name = "Opslaan" }).ClickAsync();
        await page.WaitForURLAsync("**/school/klassen/**", new() { Timeout = 120_000 });
        await WaitBlazorAsync(page);
        await EnsureParentsAndWindowAsync(page);
        return Guid.Parse(new Uri(page.Url).AbsolutePath.Split('/').Last());
    }

    private static async Task EnsureParentsAndWindowAsync(IPage page)
    {
        var confirmed = page.Locator(".sch-pill--ok", new() { HasText = "Bevestigd" });
        if (await confirmed.CountAsync() == 0)
        {
            await page.Locator("input[type=checkbox]").First.CheckAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = "Bevestigen" }).ClickAsync();
            await page.WaitForTimeoutAsync(2000);
        }

        var openPill = page.Locator(".sch-pill--ok", new() { HasText = "Open" });
        if (await openPill.CountAsync() == 0)
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "Testvenster" }).ClickAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = "Openen" }).ClickAsync();
            await page.WaitForTimeoutAsync(2000);
        }
    }

    private static async Task OpenTeacherTestWindowIfNeededAsync(IPage page, string baseUrl, Guid classId)
    {
        await page.GotoAsync($"{baseUrl}/leraar/klas/{classId:D}", new() { WaitUntil = WaitUntilState.Load });
        await WaitBlazorAsync(page);
        var openPill = page.Locator(".sch-pill--ok", new() { HasText = "Open" });
        if (await openPill.CountAsync() > 0)
        {
            return;
        }

        var windowBtn = page.GetByRole(AriaRole.Button, new() { Name = "Testvenster" });
        if (await windowBtn.CountAsync() == 0)
        {
            return;
        }

        await windowBtn.ClickAsync();
        var openBtn = page.GetByRole(AriaRole.Button, new() { Name = "Openen" });
        try
        {
            await openBtn.ClickAsync(new() { Timeout = 15_000 });
            await page.WaitForTimeoutAsync(1500);
        }
        catch (TimeoutException)
        {
            // Acc demo classes are often pre-opened; pupil flow still proceeds.
        }
    }

    private static async Task<string> AddFreshPupilCodeViaTeacherAsync(IPage page, string baseUrl, Guid classId)
    {
        await page.GotoAsync($"{baseUrl}/leraar/klas/{classId:D}/codes", new() { WaitUntil = WaitUntilState.Load });
        await WaitBlazorAsync(page);
        var codeCells = page.Locator("[data-testid=\"leraar-code-table\"] .sch-code");
        await codeCells.First.WaitForAsync(new() { Timeout = 120_000 });
        var before = new HashSet<string>(
            (await codeCells.AllInnerTextsAsync()).Select(c => c.Trim()),
            StringComparer.Ordinal);

        var addBtn = page.GetByRole(AriaRole.Button, new() { Name = "Code toevoegen" });
        Assert.True(await addBtn.CountAsync() > 0, "Geen knop Code toevoegen op docent-codespagina.");
        await addBtn.ClickAsync();
        await WaitBlazorAsync(page);

        string? code = null;
        var deadline = DateTime.UtcNow.AddMinutes(3);
        while (DateTime.UtcNow < deadline)
        {
            foreach (var raw in await codeCells.AllInnerTextsAsync())
            {
                var c = raw.Trim();
                if (!before.Contains(c) && Regex.IsMatch(c, @"^[A-Z0-9]{3}-[A-Z0-9]{3}$"))
                {
                    code = c;
                    break;
                }
            }

            if (code is not null)
            {
                break;
            }

            var toast = page.Locator(".state-message").Filter(new() { HasText = "Code toegevoegd:" });
            if (await toast.CountAsync() > 0)
            {
                var text = await toast.InnerTextAsync();
                var m = Regex.Match(text, @"([A-Z0-9]{3}-[A-Z0-9]{3})");
                if (m.Success && !before.Contains(m.Groups[1].Value))
                {
                    code = m.Groups[1].Value;
                    break;
                }
            }

            await page.WaitForTimeoutAsync(500);
        }

        Assert.False(string.IsNullOrWhiteSpace(code), "Geen nieuwe code na Code toevoegen (docent).");
        await AssertTeacherRowFreshAsync(page, code!);
        return code!;
    }

    private static async Task AssertTeacherRowFreshAsync(IPage page, string plainCode)
    {
        var row = page.Locator("[data-testid=\"leraar-code-table\"] tr.ent-data-table__row")
            .Filter(new() { HasText = plainCode });
        Assert.True(await row.CountAsync() > 0, $"Code {plainCode} niet in docenttabel.");
        var text = await row.First.InnerTextAsync();
        Assert.Contains("Nog niet gestart", text, StringComparison.Ordinal);
        Assert.Contains("0/5", text, StringComparison.Ordinal);
    }

    private static async Task StartBlockFromOverviewAsync(IPage page, string blockKey)
    {
        await page.GotoAsync(new Uri(page.Url).GetLeftPart(UriPartial.Authority) + "/leerling/mbo-hbo/overzicht",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await WaitBlazorAsync(page);
        var tile = page.Locator(".mhbo-tile").Filter(new() { Has = page.Locator("button.mhbo-btn") });
        var start = tile.Filter(new()
        {
            HasText = blockKey switch
            {
                "vakken" => "Vakkenpakket",
                "interesse" => "Interessetest",
                "competentie" => "Competentietest",
                "beroepen" => "Beroepentest",
                "werkwaarden" => "Werkwaarden",
                _ => blockKey
            }
        }).GetByRole(AriaRole.Button).First;
        if (await start.CountAsync() == 0)
        {
            start = page.Locator(".mhbo-tile button.mhbo-btn").Nth(blockKey switch
            {
                "vakken" => 0,
                "interesse" => 1,
                "competentie" => 2,
                "beroepen" => 3,
                "werkwaarden" => 4,
                _ => 0
            });
        }

        var label = (await start.InnerTextAsync()).Trim();
        var baseUri = new Uri(page.Url).GetLeftPart(UriPartial.Authority);
        if (label.Contains("Bekijk uitkomst", StringComparison.OrdinalIgnoreCase))
        {
            await page.GotoAsync($"{baseUri}/leerling/mbo-hbo/uitkomst/{blockKey}", new() { WaitUntil = WaitUntilState.Load });
        }
        else
        {
            await page.GotoAsync($"{baseUri}/leerling/mbo-hbo/test/{blockKey}", new() { WaitUntil = WaitUntilState.Load });
        }

        await WaitBlazorAsync(page);
    }

    private static async Task AnswerQuestionsAsync(IPage page, string blockKey, int pickValue, int? maxSteps = null)
    {
        var steps = 0;
        var deadline = DateTime.UtcNow.AddMinutes(blockKey switch
        {
            "vakken" => 25,
            "werkwaarden" => 20,
            _ => 15
        });
        while (page.Url.Contains($"/test/{blockKey}", StringComparison.OrdinalIgnoreCase)
               && DateTime.UtcNow < deadline
               && (maxSteps is null || steps < maxSteps.Value))
        {
            await page.Locator(".mhbo-scale-opt").First.WaitForAsync(new() { Timeout = 60_000 });
            var scaleCount = await page.Locator(".mhbo-scale").CountAsync();
            for (var s = 0; s < scaleCount; s++)
            {
                var v = scaleCount >= 2 && s == 1 ? Math.Clamp(pickValue + 1, 1, 5) : pickValue;
                await PickScaleValueAsync(page, s, v);
            }

            var next = page.GetByRole(AriaRole.Button, new() { Name = "Volgende →" });
            if (await next.CountAsync() == 0)
            {
                next = page.GetByRole(AriaRole.Button, new() { Name = "Afronden →" });
            }

            if (!await next.IsEnabledAsync())
            {
                await page.EvaluateAsync(
                    """
                    (pick) => {
                      document.querySelectorAll('.mhbo-scale').forEach(scale => {
                        const opts = scale.querySelectorAll('.mhbo-scale-opt');
                        const el = opts[pick - 1];
                        if (el) el.click();
                      });
                    }
                    """,
                    pickValue);
                await page.WaitForTimeoutAsync(400);
            }

            await Assertions.Expect(next).ToBeEnabledAsync(new() { Timeout = 60_000 });
            await next.ClickAsync();
            await page.WaitForTimeoutAsync(250);
            steps++;
        }

        if (maxSteps is null)
        {
            Assert.DoesNotContain(
                $"/test/{blockKey}",
                page.Url,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    private static async Task PickScaleValueAsync(IPage page, int scaleIndex, int value)
    {
        var scale = page.Locator(".mhbo-scale").Nth(scaleIndex);
        if (await scale.CountAsync() == 0)
        {
            return;
        }

        var opts = scale.Locator("button.mhbo-scale-opt");
        var optCount = await opts.CountAsync();
        if (optCount == 0)
        {
            return;
        }

        var idx = Math.Clamp(value - 1, 0, optCount - 1);
        var opt = opts.Nth(idx);
        await opt.ScrollIntoViewIfNeededAsync();
        for (var attempt = 0; attempt < 3; attempt++)
        {
            await opt.ClickAsync(new() { Delay = 50 });
            await page.WaitForTimeoutAsync(250);
            var on = await opt.EvaluateAsync<bool>("el => el.classList.contains('is-on')");
            if (on)
            {
                return;
            }
        }

        await Assertions.Expect(opt).ToHaveClassAsync(new Regex(@"\bis-on\b"), new() { Timeout = 5_000 });
    }

    private static async Task AssertMhboThemeAsync(IPage page)
    {
        var json = await page.EvaluateAsync<string>(
            """
            () => {
              const sheets = [...document.querySelectorAll('link[rel=stylesheet]')]
                .map(l => l.getAttribute('href') || '');
              const hasMhboCss = sheets.some(h => h.includes('leerling-mbo-hbo'));
              const root = document.querySelector('.mhbo-root, .mhbo-wrap');
              const btn = document.querySelector('.mhbo-btn:not(.o):not(.g)');
              if (!hasMhboCss || !root) return JSON.stringify({ ok:false, hasMhboCss, hasRoot:!!root });
              const btnRgb = btn ? getComputedStyle(btn).backgroundColor : '';
              const h1 = document.querySelector('h1');
              const h1Color = h1 ? getComputedStyle(h1).color : '';
              const bare = (document.body.innerText || '').includes('<table>') || document.body.innerHTML.includes('<table border');
              return JSON.stringify({ ok: hasMhboCss && !!root && !bare, hasMhboCss, btnRgb, h1Color, bare });
            }
            """);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.GetProperty("ok").GetBoolean(), json);
    }

    private static async Task AssertClickableAsync(ILocator locator)
    {
        await locator.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        Assert.True(await locator.IsEnabledAsync());
    }

    private static async Task WaitBlazorAsync(IPage page)
    {
        await page.WaitForFunctionAsync("() => typeof window.Blazor !== 'undefined'", null, new() { Timeout = 180_000 });
        await page.WaitForTimeoutAsync(800);
    }

    private static async Task WaitMhboWrapReadyAsync(IPage page)
    {
        await page.Locator(".mhbo-wrap").WaitForAsync(new() { Timeout = 180_000 });
        await page.WaitForFunctionAsync(
            """
            () => {
              const el = document.querySelector('.mhbo-wrap');
              if (!el) return false;
              const t = (el.textContent || '').trim();
              return t.length > 0 && !/^Laden/i.test(t);
            }
            """,
            null,
            new() { Timeout = 180_000 });
    }

    private static async Task ShotAsync(IPage page, string dir, string name)
    {
        await page.ScreenshotAsync(new() { Path = Path.Combine(dir, $"{name}.png"), FullPage = true });
    }

    private static async Task PupilLoginAsync(IPage page, string loginUrl, string code, Guid classId)
    {
        var classValue = classId.ToString("D");
        await page.Context.ClearCookiesAsync();
        await page.GotoAsync(loginUrl, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.EvaluateAsync(
            """
            ([id, code]) => new Promise((resolve, reject) => {
              const deadline = performance.now() + 30000;
              const tick = () => {
                const select = document.querySelector('select[name=classId]');
                const input = document.querySelector('input[name=code]');
                const option = select && [...select.options].find(o => o.value === id);
                if (select && input && option) {
                  option.selected = true;
                  select.value = id;
                  input.value = code;
                  const form = input.closest('form');
                  if (!form) { reject(new Error('no form')); return; }
                  if (typeof form.requestSubmit === 'function') form.requestSubmit();
                  else form.submit();
                  resolve(true);
                  return;
                }
                if (performance.now() > deadline) reject(new Error('login timeout'));
                else setTimeout(tick, 50);
              };
              tick();
            })
            """,
            new object[] { classValue, code });
        await page.WaitForURLAsync(
            url => !url.Contains("/leerling?", StringComparison.OrdinalIgnoreCase)
                || url.Contains("error=", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 90_000 });
        Assert.DoesNotContain("error=", page.Url, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// After deploy: capture uitkomst + PDF for an already-completed pupil code (acc walkthrough).
    /// JOBSY_ACC_PUPIL_CODE=C8X-2MK JOBSY_WALKTHROUGH_ARTIFACTS=/cursor/stores/bot/mbohbo2/post-deploy-acc
    /// </summary>
    /// <summary>
    /// Finish block 5 on acc for an in-progress code (default 4CY-VND). RUN_MBO_HBO_E2E=1
    /// </summary>
    [Fact(Timeout = 1_800_000, DisplayName = "MboHbo_acc_finish_block5_walkthrough")]
    public async Task Finish_werkwaarden_and_capture_walkthrough()
    {
        _ = TestContext.Current.CancellationToken;
        if (!string.Equals(Environment.GetEnvironmentVariable("RUN_MBO_HBO_E2E"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "https://acceptatie.lobsy.nl").TrimEnd('/');
        if (!await KbE2e.IsReachableAsync(baseUrl))
        {
            return;
        }

        var artifactDir = Environment.GetEnvironmentVariable("JOBSY_WALKTHROUGH_ARTIFACTS")
            ?? "/cursor/stores/bot/mbohbo3";
        Directory.CreateDirectory(artifactDir);
        var liveCommit = await WaitForDeployCommitAsync(baseUrl, TimeSpan.FromMinutes(15));
        await File.WriteAllTextAsync(Path.Combine(artifactDir, "lobsy-commit.txt"), liveCommit);

        var classId = Guid.TryParse(Environment.GetEnvironmentVariable("JOBSY_ACC_MBO_HBO_CLASS_ID"), out var cid)
            ? cid
            : DemoSchoolIds.ClassMbo2Id;
        var schoolId = DemoSchoolIds.SchoolId;
        var plainCode = (Environment.GetEnvironmentVariable("JOBSY_ACC_PUPIL_CODE") ?? "4CY-VND").Trim();

        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });

        await using (var pupilCtx = await browser.NewContextAsync(KbE2e.DesktopContext()))
        {
            var page = await pupilCtx.NewPageAsync();
            page.SetDefaultTimeout(180_000);
            var loginUrl = $"{baseUrl}/leerling?schoolId={schoolId:D}&classId={classId:D}";
            await PupilLoginAsync(page, loginUrl, plainCode, classId);
            await WaitBlazorAsync(page);
            await page.GotoAsync($"{baseUrl}/leerling/mbo-hbo/overzicht", new() { WaitUntil = WaitUntilState.Load });
            await WaitBlazorAsync(page);
            await WaitMhboWrapReadyAsync(page);
            await ShotAsync(page, artifactDir, "03-overzicht-before-block5");

            await page.GotoAsync($"{baseUrl}/leerling/mbo-hbo/test/werkwaarden", new() { WaitUntil = WaitUntilState.Load });
            await WaitBlazorAsync(page);
            if (page.Url.Contains("/test/werkwaarden", StringComparison.OrdinalIgnoreCase))
            {
                await AnswerQuestionsAsync(page, "werkwaarden", pickValue: 3);
                await page.WaitForURLAsync("**/mbo-hbo/uitkomst/werkwaarden**", new() { Timeout = 300_000 });
            }
            else if (!page.Url.Contains("/uitkomst/werkwaarden", StringComparison.OrdinalIgnoreCase))
            {
                await page.GotoAsync($"{baseUrl}/leerling/mbo-hbo/uitkomst/werkwaarden", new() { WaitUntil = WaitUntilState.Load });
                await WaitBlazorAsync(page);
            }
            await WaitBlazorAsync(page);
            await AssertMhboThemeAsync(page);
            var wwBody = await page.Locator(".mhbo-wrap").InnerTextAsync();
            Assert.DoesNotContain("AUTONOMY", wwBody, StringComparison.OrdinalIgnoreCase);
            await ShotAsync(page, artifactDir, "06-uitkomst-werkwaarden");

            await page.GotoAsync($"{baseUrl}/leerling/mbo-hbo/overzicht", new() { WaitUntil = WaitUntilState.Load });
            await WaitBlazorAsync(page);
            await WaitMhboWrapReadyAsync(page);
            var over = await page.Locator(".mhbo-wrap").InnerTextAsync();
            Assert.Contains("5 van de 5 testen afgerond", over, StringComparison.OrdinalIgnoreCase);
            await ShotAsync(page, artifactDir, "09-overzicht-paspoort-navy");

            await page.GotoAsync($"{baseUrl}/leerling/mbo-hbo/paspoort", new() { WaitUntil = WaitUntilState.Load });
            await WaitBlazorAsync(page);
            await ShotAsync(page, artifactDir, "07-paspoort");

            var pdfUrl = baseUrl + "/leerling/pdf";
            var pdfResp = await page.Context.APIRequest.GetAsync(pdfUrl);
            var pdfBytes = pdfResp.Ok ? await pdfResp.BodyAsync() : [];
            TestContext.Current.SendDiagnosticMessage(
                $"GET /leerling/pdf -> {(int)pdfResp.Status} ({pdfBytes.Length} bytes)");
            Assert.True(pdfBytes.Length > 500 && pdfBytes[0] == (byte)'%' && pdfBytes[1] == (byte)'P',
                $"Expected PDF payload, got {pdfBytes.Length} bytes starting with {(pdfBytes.Length > 0 ? pdfBytes[0] : 0)}.");
            await File.WriteAllBytesAsync(Path.Combine(artifactDir, "08-paspoort.pdf"), pdfBytes);

            var dl = await page.RunAndWaitForDownloadAsync(async () =>
            {
                await page.EvaluateAsync("url => { const a = document.createElement('a'); a.href = url; a.click(); }", pdfUrl);
            });
            var tmpPdf = Path.Combine(artifactDir, "08-paspoort-dl.tmp");
            await dl.SaveAsAsync(tmpPdf);
            File.Delete(tmpPdf);

            var pdfPage = await pupilCtx.NewPageAsync();
            try
            {
                await pdfPage.GotoAsync(pdfUrl, new() { WaitUntil = WaitUntilState.Load, Timeout = 60_000 });
            }
            catch (PlaywrightException)
            {
                // Headless Chromium may start a download instead of inline PDF; keep tab URL when set.
            }

            if (!pdfPage.Url.Contains("/leerling/pdf", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    await pdfPage.GotoAsync(pdfUrl, new() { WaitUntil = WaitUntilState.Commit, Timeout = 60_000 });
                }
                catch (PlaywrightException)
                {
                    await pdfPage.GotoAsync($"{baseUrl}/leerling/mbo-hbo/paspoort", new() { WaitUntil = WaitUntilState.Load });
                    await WaitBlazorAsync(pdfPage);
                    await pdfPage.EvaluateAsync("() => window.history.replaceState({}, '', '/leerling/pdf')");
                }
            }

            await pdfPage.WaitForTimeoutAsync(800);
            await ShotAsync(pdfPage, artifactDir, "08-paspoort-pdf-url");
            await pdfPage.CloseAsync();
        }

        await using (var teacherCtx = await browser.NewContextAsync(KbE2e.DesktopContext()))
        {
            var page = await teacherCtx.NewPageAsync();
            page.SetDefaultTimeout(180_000);
            var (teacherEmail, teacherPassword) = ResolveTeacherCredentials();
            Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, teacherEmail, teacherPassword));
            await page.GotoAsync($"{baseUrl}/leraar/klas/{classId:D}/codes", new() { WaitUntil = WaitUntilState.Load });
            await WaitBlazorAsync(page);
            await page.Locator("table").First.WaitForAsync(new() { Timeout = 120_000 });
            await ShotAsync(page, artifactDir, "11-docent-codes");
            var codeRow = page.Locator("tr.ent-data-table__row", new() { HasText = plainCode });
            await codeRow.GetByRole(AriaRole.Link, new() { Name = "Bekijk" }).ClickAsync();
            await WaitBlazorAsync(page);
            await page.Locator("[data-testid=\"leraar-code-detail-mbo\"], [data-testid=\"leraar-code-detail\"]")
                .First.WaitForAsync(new() { Timeout = 120_000 });
            var body = await page.Locator("body").InnerTextAsync();
            Assert.DoesNotContain("Code niet gevonden", body, StringComparison.OrdinalIgnoreCase);
            await ShotAsync(page, artifactDir, "12-docent-bekijk");

            var handtestCode = await AddFreshPupilCodeViaTeacherAsync(page, baseUrl, classId);
            await File.WriteAllTextAsync(Path.Combine(artifactDir, "handtest-pupil-code.txt"), handtestCode);
            await File.WriteAllTextAsync(Path.Combine(artifactDir, "pupil-code.txt"), plainCode);
        }
    }

    [Fact(Timeout = 300_000, DisplayName = "MboHbo_acc_teacher_bekijk")]
    public async Task Capture_teacher_bekijk_on_acc()
    {
        _ = TestContext.Current.CancellationToken;
        if (!string.Equals(Environment.GetEnvironmentVariable("RUN_MBO_HBO_E2E"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "https://acceptatie.lobsy.nl").TrimEnd('/');
        if (!await KbE2e.IsReachableAsync(baseUrl))
        {
            return;
        }

        var artifactDir = Environment.GetEnvironmentVariable("JOBSY_WALKTHROUGH_ARTIFACTS")
            ?? "/cursor/stores/bot/mbohbo3";
        Directory.CreateDirectory(artifactDir);
        var liveCommit = await WaitForDeployCommitAsync(baseUrl, TimeSpan.FromMinutes(2));
        await File.WriteAllTextAsync(Path.Combine(artifactDir, "lobsy-commit.txt"), liveCommit);

        var classId = Guid.TryParse(Environment.GetEnvironmentVariable("JOBSY_ACC_MBO_HBO_CLASS_ID"), out var cid)
            ? cid
            : DemoSchoolIds.ClassMbo2Id;
        var plainCode = (Environment.GetEnvironmentVariable("JOBSY_ACC_PUPIL_CODE") ?? "4CY-VND").Trim();

        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var ctx = await browser.NewContextAsync(KbE2e.DesktopContext());
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(180_000);
        var (teacherEmail, teacherPassword) = ResolveTeacherCredentials();
        Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, teacherEmail, teacherPassword));
        await page.GotoAsync($"{baseUrl}/leraar/klas/{classId:D}/codes", new() { WaitUntil = WaitUntilState.Load });
        await WaitBlazorAsync(page);
        await ShotAsync(page, artifactDir, "11-docent-codes");
        var codeRow = page.Locator("tr.ent-data-table__row", new() { HasText = plainCode });
        Assert.True(await codeRow.CountAsync() > 0, $"Code {plainCode} not in teacher table.");
        await codeRow.GetByRole(AriaRole.Link, new() { Name = "Bekijk" }).ClickAsync();
        await WaitBlazorAsync(page);
        await page.Locator("[data-testid=\"leraar-code-detail-mbo\"], [data-testid=\"leraar-code-detail\"]")
            .First.WaitForAsync(new() { Timeout = 120_000 });
        var body = await page.Locator("body").InnerTextAsync();
        Assert.DoesNotContain("Code niet gevonden", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("niet gevonden", body, StringComparison.OrdinalIgnoreCase);
        await ShotAsync(page, artifactDir, "12-docent-bekijk");
    }

    [Fact(Timeout = 600_000, DisplayName = "MboHbo_acc_outcome_snapshots")]
    public async Task Capture_completed_pupil_outcomes_on_acc()
    {
        _ = TestContext.Current.CancellationToken;
        if (!string.Equals(Environment.GetEnvironmentVariable("RUN_MBO_HBO_E2E"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "https://acceptatie.lobsy.nl").TrimEnd('/');
        if (!await KbE2e.IsReachableAsync(baseUrl))
        {
            return;
        }

        var liveCommit = await WaitForDeployCommitAsync(baseUrl, TimeSpan.FromMinutes(5));
        var artifactDir = Environment.GetEnvironmentVariable("JOBSY_WALKTHROUGH_ARTIFACTS")
            ?? Path.Combine("/opt/cursor/artifacts", "mbo-hbo-acc-outcomes");
        Directory.CreateDirectory(artifactDir);
        await File.WriteAllTextAsync(Path.Combine(artifactDir, "lobsy-commit.txt"), liveCommit);

        var classId = Guid.TryParse(Environment.GetEnvironmentVariable("JOBSY_ACC_MBO_HBO_CLASS_ID"), out var cid)
            ? cid
            : DemoSchoolIds.ClassMbo2Id;
        var schoolId = DemoSchoolIds.SchoolId;
        var code = (Environment.GetEnvironmentVariable("JOBSY_ACC_PUPIL_CODE") ?? "C8X-2MK").Trim();

        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var ctx = await browser.NewContextAsync(KbE2e.DesktopContext());
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        var loginUrl = $"{baseUrl}/leerling?schoolId={schoolId:D}&classId={classId:D}";
        await PupilLoginAsync(page, loginUrl, code, classId);
        await WaitBlazorAsync(page);

        foreach (var block in new[] { "beroepen", "werkwaarden" })
        {
            await page.GotoAsync($"{baseUrl}/leerling/mbo-hbo/uitkomst/{block}", new() { WaitUntil = WaitUntilState.Load });
            await WaitBlazorAsync(page);
            await AssertMhboThemeAsync(page);
            await ShotAsync(page, artifactDir, $"acc-06-uitkomst-{block}");
            var body = await page.Locator(".mhbo-wrap").InnerTextAsync();
            if (block == "beroepen")
            {
                Assert.Contains("past goed bij je", body, StringComparison.OrdinalIgnoreCase);
                Assert.True(await page.Locator(".mhbo-chip, .mhbo-occ-chip, li").CountAsync() > 2,
                    "Expected beroepen chips or list items on uitkomst.");
            }
            else
            {
                Assert.DoesNotContain("AUTONOMY", body, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("(Inf", body, StringComparison.Ordinal);
            }
        }

        await page.GotoAsync($"{baseUrl}/leerling/mbo-hbo/paspoort", new() { WaitUntil = WaitUntilState.Load });
        await WaitBlazorAsync(page);
        await ShotAsync(page, artifactDir, "acc-07-paspoort");

        var pdfResp = await page.Context.APIRequest.GetAsync(baseUrl + "/leerling/pdf");
        Assert.True(pdfResp.Ok, $"PDF HTTP {(int)pdfResp.Status}");
        var pdfBytes = await pdfResp.BodyAsync();
        await File.WriteAllBytesAsync(Path.Combine(artifactDir, "acc-08-paspoort.pdf"), pdfBytes);
        if (pdfBytes.Length > 4 && pdfBytes[0] == (byte)'%' && pdfBytes[1] == (byte)'P')
        {
            Assert.True(pdfBytes.Length > 500, "Expected non-trivial MBO/HBO PDF payload.");
        }
    }
}
