using System.Text;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
public sealed class OnboardingRustigStep08ClickAuditPlaywrightTests
{
    private static readonly (int Width, int Height, bool Mobile, string Tag)[] Viewports =
    [
        (1440, 900, false, "1440"),
        (390, 844, true, "390"),
    ];

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Audit_step08_clicks(int width, int height, bool mobile, string tag)
    {
        if ((Environment.GetEnvironmentVariable("JOBSY_ONBOARDING_STEP08_CLICK_AUDIT") ?? "").Trim() != "1")
        {
            return;
        }

        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "http://127.0.0.1:5201").Trim().TrimEnd('/');
        if (!await SmokeReachability.IsReachableAsync(baseUrl))
        {
            Assert.Fail($"Stack not reachable at {baseUrl}");
        }

        var mode = (Environment.GetEnvironmentVariable("JOBSY_STEP08_AUDIT_MODE") ?? "rustig").Trim().ToLowerInvariant();
        var rustig = mode is "rustig" or "new";
        var reportDir = Environment.GetEnvironmentVariable("JOBSY_ONBOARDING_RUSTIG_ARTIFACTS")
                        ?? "/opt/cursor/artifacts/onboarding-rustig-step08";
        Directory.CreateDirectory(reportDir);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var ctx = await browser.NewContextAsync(
            FunctionalOnboardingPlaywrightTestSupport.CreateContextOptions(baseUrl, width, height, mobile));
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        if (rustig)
        {
            await OnboardingRustigStep08E2eFlow.ReachRustigStep08Async(page, baseUrl, mobile);
            await page.Locator("[data-ob-rustig-screen=step-08]").WaitForAsync(new() { Timeout = 60_000 });
        }
        else
        {
            await OnboardingRustigStep08E2eFlow.ReachBaselineStep08Async(page, baseUrl, mobile);
            await page.Locator("[data-onboarding-step=\"8\"]").WaitForAsync(new() { Timeout = 60_000 });
        }

        var rows = new List<(string Control, string Result)>();
        await AuditAsync(page, rows, "Later verder", page.GetByRole(AriaRole.Button, new() { Name = "Later verder" }));

        var accountLink = page.Locator("a.ob-rustig__link--underline[href='/privacy/data']");
        if (await accountLink.CountAsync() == 0)
        {
            accountLink = page.GetByRole(AriaRole.Link, new() { Name = "Mijn gegevens & account" });
        }

        rows.Add(("Mijn gegevens & account", await TryHitAuditAsync(page, accountLink, "account")));

        var likert = page.Locator(".test-likert button, .lobsy-likert button").First;
        await AuditAsync(page, rows, "Likert 1", likert);

        var flowBack = page.Locator(".test-flow__back").First;
        await AuditAsync(page, rows, "Test terug", flowBack);
        var flowNext = page.Locator(".test-flow__next").First;
        await AuditAsync(page, rows, "Test volgende", flowNext);
        var testsLater = page.GetByTestId("onboarding-tests-later");
        await AuditAsync(page, rows, "Tests later", testsLater);

        var reportPath = Path.Combine(reportDir, $"click-audit-step-08-{mode}-{tag}.txt");
        var sb = new StringBuilder();
        sb.AppendLine($"mode={mode} viewport={tag} rustig={rustig}");
        foreach (var (control, result) in rows)
        {
            sb.AppendLine($"{control}\t{result}");
        }

        await File.WriteAllTextAsync(reportPath, sb.ToString());

        foreach (var (control, result) in rows)
        {
            if (result is "ok" or "n/a")
            {
                continue;
            }

            Assert.Fail($"Click audit {mode}/{tag}: {control}={result}; see {reportPath}");
        }
    }

    public static IEnumerable<object[]> Cases()
    {
        foreach (var (w, h, m, tag) in Viewports)
        {
            yield return [w, h, m, tag];
        }
    }

    private static async Task AuditAsync(IPage page, List<(string, string)> rows, string label, ILocator locator)
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
            return ex.Message;
        }
    }
}
