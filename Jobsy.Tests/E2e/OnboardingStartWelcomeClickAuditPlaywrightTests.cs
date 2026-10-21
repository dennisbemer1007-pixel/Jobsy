using System.Text;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Manual/CI audit: welcome click targets baseline (rustig off) vs rustig start.
/// Run with JOBSY_ONBOARDING_START_CLICK_AUDIT=1 against local CI stack.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class OnboardingStartWelcomeClickAuditPlaywrightTests
{
    private static readonly (int Width, int Height, bool Mobile, string Tag)[] Viewports =
    [
        (1440, 900, false, "1440"),
        (390, 844, true, "390"),
    ];

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Audit_welcome_clicks(int width, int height, bool mobile, string tag)
    {
        if ((Environment.GetEnvironmentVariable("JOBSY_ONBOARDING_START_CLICK_AUDIT") ?? "").Trim() != "1")
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

        await FunctionalOnboardingInteractivityHelpers.RegisterFreshCandidateAsync(page, baseUrl, $"audit-{mode}-{tag}");
        await FunctionalOnboardingInteractivityHelpers.OpenStartWelcomeAsync(page, baseUrl);

        if (rustig)
        {
            await page.Locator("[data-ob-rustig-screen=start-journey]").WaitForAsync(new() { Timeout = 60_000 });
        }

        var rows = new List<(string Control, string Result)>();
        await AuditButtonAsync(page, rows, "Later verder", page.GetByRole(AriaRole.Button, new() { Name = "Later verder" }));
        var accountLink = page.Locator(".ob-rustig__actions a.ob-rustig__link--underline[href='/privacy/data']");
        if (await accountLink.CountAsync() == 0)
        {
            accountLink = page.GetByRole(AriaRole.Link, new() { Name = "Mijn gegevens & account" });
        }

        var accountHit = await TryHitAuditAsync(page, accountLink, "Mijn gegevens & account");
        rows.Add(("Mijn gegevens & account", accountHit));
        rows.Add(("Privacy (/privacy/data)", accountHit));

        var begin = page.GetByTestId("onboarding-begin");
        await AuditButtonAsync(page, rows, rustig ? "Aan de slag" : "Beginnen", begin);
        await FunctionalOnboardingInteractivityHelpers.ClickBeginnenOnWelcomeAsync(page, mobile);
        rows.Add(("Navigatie naar stap 1", "ok"));

        var reportPath = Path.Combine(reportDir, $"click-audit-{mode}-{tag}.txt");
        var sb = new StringBuilder();
        sb.AppendLine($"mode={mode} viewport={tag} rustig={rustig}");
        foreach (var (control, result) in rows)
        {
            sb.AppendLine($"{control}\t{result}");
        }

        await File.WriteAllTextAsync(reportPath, sb.ToString());

        var step1Ok = rows.Any(r => r.Item1 == "Navigatie naar stap 1" && r.Item2 == "ok");
        foreach (var (control, result) in rows)
        {
            if (result is "ok" or "n/a")
            {
                continue;
            }

            if (!rustig && control is "Later verder" or "Mijn gegevens & account" or "Privacy (/privacy/data)")
            {
                continue;
            }

            if (step1Ok && control is "Beginnen" or "Aan de slag")
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
