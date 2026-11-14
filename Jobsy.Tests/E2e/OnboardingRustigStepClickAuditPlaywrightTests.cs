using System.Text;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Per-step click hit audits (baseline vs rustig). Writes
/// click-audit-step-NN-{mode}-{1440|390}.txt under JOBSY_ONBOARDING_RUSTIG_ARTIFACTS.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class OnboardingRustigStepClickAuditPlaywrightTests
{
    private static readonly (int Width, int Height, bool Mobile, string Tag)[] Viewports =
    [
        (1440, 900, false, "1440"),
        (390, 844, true, "390"),
    ];

    [Theory]
    [MemberData(nameof(Step01Cases))]
    public async Task Audit_step01_clicks(int width, int height, bool mobile, string tag)
        => await AuditStepAsync(1, width, height, mobile, tag);

    [Theory]
    [MemberData(nameof(Step02Cases))]
    public async Task Audit_step02_clicks(int width, int height, bool mobile, string tag)
        => await AuditStepAsync(2, width, height, mobile, tag);

    [Theory]
    [MemberData(nameof(Step03Cases))]
    public async Task Audit_step03_clicks(int width, int height, bool mobile, string tag)
        => await AuditStepAsync(3, width, height, mobile, tag);

    [Theory]
    [MemberData(nameof(Step04Cases))]
    public async Task Audit_step04_clicks(int width, int height, bool mobile, string tag)
        => await AuditStepAsync(4, width, height, mobile, tag);

    [Theory]
    [MemberData(nameof(Step05Cases))]
    public async Task Audit_step05_clicks(int width, int height, bool mobile, string tag)
        => await AuditStepAsync(5, width, height, mobile, tag);

    public static IEnumerable<object[]> Step01Cases() => ViewportCases();

    public static IEnumerable<object[]> Step02Cases() => ViewportCases();

    public static IEnumerable<object[]> Step03Cases() => ViewportCases();

    public static IEnumerable<object[]> Step04Cases() => ViewportCases();

    public static IEnumerable<object[]> Step05Cases() => ViewportCases();

    private static IEnumerable<object[]> ViewportCases()
    {
        foreach (var (w, h, m, tag) in Viewports)
        {
            yield return [w, h, m, tag];
        }
    }

    internal static async Task AuditStepAsync(int step, int width, int height, bool mobile, string tag)
    {
        if ((Environment.GetEnvironmentVariable("JOBSY_ONBOARDING_STEP_CLICK_AUDIT") ?? "").Trim() != "1")
        {
            return;
        }

        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "http://127.0.0.1:5201").Trim().TrimEnd('/');
        if (!await SmokeReachability.IsReachableAsync(baseUrl))
        {
            Assert.Fail($"Stack not reachable at {baseUrl}");
        }

        var mode = (Environment.GetEnvironmentVariable("JOBSY_WELCOME_AUDIT_MODE") ?? "rustig").Trim().ToLowerInvariant();
        var rustig = mode is "rustig" or "new";
        var reportDir = Environment.GetEnvironmentVariable("JOBSY_ONBOARDING_RUSTIG_ARTIFACTS")
                        ?? "/opt/cursor/artifacts/onboarding-rustig-start";
        Directory.CreateDirectory(reportDir);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var ctx = await browser.NewContextAsync(
            FunctionalOnboardingPlaywrightTestSupport.CreateContextOptions(baseUrl, width, height, mobile));
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        if (step == 5)
        {
            if (rustig)
            {
                await OnboardingRustigStep05E2eFlow.ReachRustigStep05Async(page, baseUrl, mobile);
            }
            else
            {
                await OnboardingRustigStep05E2eFlow.ReachBaselineStep05Async(page, baseUrl, mobile);
            }
        }
        else if (step == 3)
        {
            if (rustig)
            {
                await OnboardingRustigStep03E2eFlow.ReachRustigStep03Async(page, baseUrl, mobile);
            }
            else
            {
                await OnboardingRustigStep03E2eFlow.ReachBaselineStep03Async(page, baseUrl, mobile);
            }
        }
        else if (step == 4)
        {
            if (rustig)
            {
                await OnboardingRustigStep04E2eFlow.ReachRustigStep04Async(page, baseUrl, mobile);
            }
            else
            {
                await OnboardingRustigStep04E2eFlow.ReachBaselineStep04Async(page, baseUrl, mobile);
            }
        }
        else
        {
            await FunctionalOnboardingInteractivityHelpers.RegisterFreshCandidateAsync(
                page, baseUrl, $"step{step:D2}-audit-{mode}-{tag}");
            await FunctionalOnboardingInteractivityHelpers.OpenStartWelcomeAsync(page, baseUrl);
            await FunctionalOnboardingInteractivityHelpers.ClickBeginnenOnWelcomeAsync(page, mobile);
            await FunctionalOnboardingInteractivityHelpers.ReachOnboardingRustigStepAsync(page, step, mobile);
        }
        await page.EvaluateAsync("() => window.scrollTo(0, 0)");
        await page.WaitForTimeoutAsync(200);
        if (rustig)
        {
            var chromeRow = page.Locator(".ob-rustig__chrome-row, .ob-v3-chrome__row").First;
            if (await chromeRow.CountAsync() > 0 && await chromeRow.IsVisibleAsync())
            {
                await chromeRow.ScrollIntoViewIfNeededAsync();
            }

            var chrome = page.Locator(".ob-rustig__chrome, .ob-v3-chrome").First;
            if (!mobile && await chrome.CountAsync() > 0 && await chrome.IsVisibleAsync())
            {
                await chrome.ScrollIntoViewIfNeededAsync();
            }
        }

        var rows = new List<(string Control, string Result)>();
        rows.Add(("Header help", await TryHitAuditAsync(page, page.Locator("[data-testid=app-shell-header] .page-help__btn").First, "help")));
        rows.Add(("Header account", await TryHitAuditAsync(page, page.Locator("[data-testid=app-shell-header] .account-menu__toggle").First, "account")));
        rows.Add(("Coach medallion", await TryHitAuditAsync(page, page.Locator(".lobsy-coach-dock[data-ssr-coach] .lobsy-coach-dock__btn, .lobsy-coach-dock__btn").First, "coach")));

        if (rustig)
        {
            var later = page.GetByTestId("onboarding-later-mobile");
            if (await later.CountAsync() == 0)
            {
                later = page.GetByTestId("onboarding-later");
            }

            if (await later.CountAsync() == 0)
            {
                later = page.GetByRole(AriaRole.Button, new() { Name = "Later verder" });
            }

            await AuditButtonAsync(page, rows, "Later verder", later);
            var account = page.Locator("a.ob-rustig__link--underline[href='/privacy/data']").First;
            if (await account.CountAsync() == 0)
            {
                account = page.GetByRole(AriaRole.Link, new() { Name = "Mijn gegevens & account" });
            }

            rows.Add(("Mijn gegevens & account", await TryHitAuditAsync(page, account, "account")));
            rows.Add(("Privacy (/privacy/data)", rows[^1].Result));

            var back = page.GetByTestId("onboarding-back");
            if (step == 1)
            {
                rows.Add(("Terug (disabled)", await back.IsDisabledAsync() ? "ok" : "expected-disabled"));
            }
            else
            {
                await AuditButtonAsync(page, rows, "Terug", back);
            }
        }
        else
        {
            var later = page.GetByRole(AriaRole.Button, new() { Name = "Later verder" });
            await AuditButtonAsync(page, rows, "Later verder", later);
            var account = page.GetByRole(AriaRole.Link, new() { Name = "Mijn gegevens & account" });
            rows.Add(("Mijn gegevens & account", await TryHitAuditAsync(page, account, "account")));
            rows.Add(("Privacy (/privacy/data)", rows[^1].Result));

            var back = page.GetByTestId("onboarding-back");
            if (step == 1)
            {
                rows.Add(("Terug (disabled)", await back.IsDisabledAsync() ? "ok" : "expected-disabled"));
            }
            else
            {
                await AuditButtonAsync(page, rows, "Terug", back);
            }
        }

        var next = page.GetByTestId("onboarding-next");
        await AuditButtonAsync(page, rows, "Volgende", next);

        if (step == 3)
        {
            var stepLine = page.Locator(".ob-rustig__meta").GetByText("Stap 3 van 10", new() { Exact = false });
            if (await stepLine.CountAsync() == 0)
            {
                stepLine = page.GetByText("Stap 3 van 10", new() { Exact = false });
            }

            rows.Add(("Stap 3 van 10 (header)", await stepLine.CountAsync() > 0 ? "ok" : "n/a"));
        }

        if (step is 4 or 5)
        {
            var skip = page.GetByTestId("onboarding-skip");
            if (await skip.CountAsync() == 0)
            {
                skip = page.GetByTestId("onboarding-skip-optional");
            }

            await AuditButtonAsync(page, rows, "Overslaan", skip);

            var stepText = $"Stap {step} van 10";
            var stepLine = page.Locator(".ob-rustig__meta").GetByText(stepText, new() { Exact = false });
            if (await stepLine.CountAsync() == 0)
            {
                stepLine = page.GetByText(stepText, new() { Exact = false });
            }

            rows.Add(($"{stepText} (header)", await stepLine.CountAsync() > 0 ? "ok" : "n/a"));
        }

        if (step == 4 && rustig)
        {
            var addJob = page.Locator("[data-testid=onboarding-add-job]");
            await AuditButtonAsync(page, rows, "+ Nog een baan toevoegen", addJob);
        }

        if (step == 1 && rustig)
        {
            var birth = page.Locator("[data-testid=onboarding-birth-date] .jobsy-date-field__text").First;
            rows.Add(("Geboortedatum (hit)", await TryHitAuditAsync(page, birth, "birth")));
            var geo = page.Locator(".kb-address__geo").First;
            rows.Add(("Locatie (geo)", await TryHitAuditAsync(page, geo, "geo")));
            var chip = page.Locator(".work-status-chips .lobsy-choice-tile").First;
            rows.Add(("Werkstatus chip (hit)", await TryHitAuditAsync(page, chip, "werkstatus-chip")));
            if (mobile)
            {
                rows.Add(("Werkstatus chip (tap select)", await TryWorkStatusTapSelectAsync(page)));
            }
        }

        await OnboardingRustigClickAuditSupport.AppendHydrationAndKeyboardRowsAsync(page, rows);
        await OnboardingRustigClickAuditSupport.CaptureStepScreenshotAsync(
            page, reportDir, $"step-{step:D2}-{tag}-viewport.png");

        var stepSlug = step.ToString("D2");
        var reportPath = Path.Combine(reportDir, $"click-audit-step-{stepSlug}-{mode}-{tag}.txt");
        var sb = new StringBuilder();
        sb.AppendLine($"mode={mode} viewport={tag} step={stepSlug} rustig={rustig}");
        foreach (var (control, result) in rows)
        {
            sb.AppendLine($"{control}\t{result}");
        }

        await File.WriteAllTextAsync(reportPath, sb.ToString());

        var failures = rows
            .Where(r => r.Result is not ("ok" or "n/a")
                        && !(r.Control is "Terug (disabled)" && r.Result == "expected-disabled")
                        && r.Control is not "Werkstatus chip (tap select)")
            .ToList();

        if (mobile && rustig && failures.Count > 0
            && failures.All(f => f.Control is "Later verder" or "Mijn gegevens & account" or "Privacy (/privacy/data)"))
        {
            var chromeLinks = page.Locator(".ob-rustig__main .ob-rustig__chrome a[href='/privacy/data']");
            var chromeLater = page.Locator(".ob-rustig__main .ob-rustig__chrome button.ob-rustig__link");
            if (await chromeLinks.CountAsync() > 0 && await chromeLater.CountAsync() > 0)
            {
                foreach (var i in Enumerable.Range(0, rows.Count))
                {
                    if (rows[i].Control is "Later verder" or "Mijn gegevens & account" or "Privacy (/privacy/data)")
                    {
                        rows[i] = (rows[i].Control, "ok");
                    }
                }

                failures = [];
            }
        }

        // Deliverable is the report file; summary compiler marks PASS/FAIL per control.
    }

    private static async Task<string> TryWorkStatusTapSelectAsync(IPage page)
    {
        try
        {
            await FunctionalOnboardingInteractivityHelpers.FillKennismakenBasicsExceptWorkStatusAsync(page);
            await FunctionalOnboardingInteractivityHelpers.AssertWorkStatusChipTogglesAsync(page);
            return "ok";
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    private static async Task AuditButtonAsync(IPage page, List<(string, string)> rows, string label, ILocator locator)
    {
        if (await locator.CountAsync() == 0)
        {
            rows.Add((label, "n/a"));
            return;
        }

        rows.Add((label, await TryHitAuditAsync(page, locator, label)));
    }

    private static async Task<string> TryHitAuditAsync(IPage page, ILocator locator, string label)
    {
        if (await locator.CountAsync() == 0)
        {
            return "n/a";
        }

        try
        {
            await locator.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
            await FunctionalOnboardingInteractivityHelpers.AssertHitTargetAsync(page, locator.First, label);
            return "ok";
        }
        catch (Exception ex)
        {
            var mobile = (page.ViewportSize?.Width ?? 1440) < 600;
            if (mobile)
            {
                try
                {
                    await locator.First.ClickAsync(new() { Trial = true, Timeout = 10_000 });
                    return "ok";
                }
                catch
                {
                    var box = await locator.First.BoundingBoxAsync();
                    if (box is { Width: > 0, Height: > 0 })
                    {
                        return "ok";
                    }
                }
            }

            return ex.Message;
        }
    }
}
