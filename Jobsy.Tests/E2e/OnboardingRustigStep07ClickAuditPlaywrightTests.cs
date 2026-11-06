using System.Text;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
public sealed class OnboardingRustigStep07ClickAuditPlaywrightTests
{
    private static readonly (int Width, int Height, bool Mobile, string Tag)[] Viewports =
    [
        (1440, 900, false, "1440"),
        (390, 844, true, "390"),
    ];

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Audit_step07_clicks(int width, int height, bool mobile, string tag)
    {
        if ((Environment.GetEnvironmentVariable("JOBSY_ONBOARDING_STEP07_CLICK_AUDIT") ?? "").Trim() != "1")
        {
            return;
        }

        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "http://127.0.0.1:5201").Trim().TrimEnd('/');
        if (!await SmokeReachability.IsReachableAsync(baseUrl))
        {
            Assert.Fail($"Stack not reachable at {baseUrl}");
        }

        var mode = (Environment.GetEnvironmentVariable("JOBSY_STEP07_AUDIT_MODE") ?? "rustig").Trim().ToLowerInvariant();
        var rustig = mode is "rustig" or "new";
        var reportDir = Environment.GetEnvironmentVariable("JOBSY_ONBOARDING_RUSTIG_ARTIFACTS")
                        ?? "/opt/cursor/artifacts/onboarding-rustig-step07";
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
            await OnboardingRustigStep07E2eFlow.ReachRustigStep07Async(page, baseUrl, mobile);
            await page.Locator("[data-ob-rustig-screen=step-07]").WaitForAsync(new() { Timeout = 60_000 });
        }
        else
        {
            await OnboardingRustigStep07E2eFlow.ReachBaselineStep07Async(page, baseUrl, mobile);
            await page.Locator("[data-testid=onboarding-step-7-illustrated], .ob-wizard__consent").First
                .WaitForAsync(new() { Timeout = 60_000 });
        }

        var rows = new List<(string Control, string Result)>();
        await AuditButtonAsync(page, rows, "Later verder", page.GetByRole(AriaRole.Button, new() { Name = "Later verder" }));

        var accountLink = page.Locator("a.ob-rustig__link--underline[href='/privacy/data']");
        if (await accountLink.CountAsync() == 0)
        {
            accountLink = page.GetByRole(AriaRole.Link, new() { Name = "Mijn gegevens & account" });
        }

        rows.Add(("Mijn gegevens & account", await TryHitAuditAsync(page, accountLink, "account")));

        var consentSwitch = page.Locator("#onboarding-field-consent label.ob-wizard__consent, .ob-rustig__consent").First;
        await AuditButtonAsync(page, rows, "Toestemming toggle", consentSwitch);

        var more = page.Locator("details summary").First;
        await AuditButtonAsync(page, rows, "Meer info", more);

        var back = page.GetByTestId("onboarding-back");
        await AuditButtonAsync(page, rows, "Terug", back);
        var next = page.GetByTestId("onboarding-next");
        await AuditButtonAsync(page, rows, "Volgende", next);

        try
        {
            var checkbox = page.Locator("#onboarding-field-consent input[type=checkbox]");
            if (await checkbox.CountAsync() > 0 && !await checkbox.IsCheckedAsync())
            {
                await checkbox.ClickAsync(new() { Force = mobile });
            }

            await FunctionalOnboardingInteractivityHelpers.ClickOnboardingNextAsync(page, mobile);
            await page.Locator("[data-ob-rustig-screen=\"step-08\"], [data-onboarding-step=\"8\"], .test-likert")
                .First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 90_000 });
            rows.Add(("Navigatie naar stap 8", "ok"));
        }
        catch (Exception ex)
        {
            rows.Add(("Navigatie naar stap 8", ex.Message));
        }

        var reportPath = Path.Combine(reportDir, $"click-audit-step-07-{mode}-{tag}.txt");
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

            if (control is "Navigatie naar stap 8")
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
            return ex.Message;
        }
    }
}
