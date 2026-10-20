using System.Text;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
public sealed class OnboardingRustigKompasClickAuditPlaywrightTests
{
    private static readonly (int Width, int Height, bool Mobile, string Tag)[] Viewports =
    [
        (1440, 900, false, "1440"),
        (390, 844, true, "390"),
    ];

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Audit_kompas_clicks(int width, int height, bool mobile, string tag)
    {
        if ((Environment.GetEnvironmentVariable("JOBSY_ONBOARDING_KOMPAS_CLICK_AUDIT") ?? "").Trim() != "1")
        {
            return;
        }

        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "http://127.0.0.1:5201").Trim().TrimEnd('/');
        if (!await SmokeReachability.IsReachableAsync(baseUrl))
        {
            Assert.Fail($"Stack not reachable at {baseUrl}");
        }

        var reportDir = Environment.GetEnvironmentVariable("JOBSY_ONBOARDING_RUSTIG_ARTIFACTS")
                        ?? "/opt/cursor/artifacts/onboarding-rustig-kompas";
        Directory.CreateDirectory(reportDir);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var ctx = await browser.NewContextAsync(
            FunctionalOnboardingPlaywrightTestSupport.CreateContextOptions(baseUrl, width, height, mobile));
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await OnboardingRustigKompasE2eFlow.ReachRustigKompasAsync(page, baseUrl, mobile);
        await page.Locator("[data-ob-rustig-screen=kompas]").WaitForAsync(new() { Timeout = 60_000 });

        var rows = new List<(string Control, string Result)>();
        await AuditAsync(page, rows, "Later verder", page.GetByRole(AriaRole.Button, new() { Name = "Later verder" }));
        var complete = page.Locator(".ob-finale__btn").First;
        await AuditAsync(page, rows, "Naar Lobsy", complete);

        var reportPath = Path.Combine(reportDir, $"click-audit-kompas-{tag}.txt");
        var sb = new StringBuilder();
        sb.AppendLine($"viewport={tag}");
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

            Assert.Fail($"Click audit kompas/{tag}: {control}={result}; see {reportPath}");
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

        try
        {
            await locator.First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
            await FunctionalOnboardingInteractivityHelpers.AssertHitTargetAsync(page, locator.First, label);
            rows.Add((label, "ok"));
        }
        catch (Exception ex)
        {
            rows.Add((label, ex.Message));
        }
    }
}
