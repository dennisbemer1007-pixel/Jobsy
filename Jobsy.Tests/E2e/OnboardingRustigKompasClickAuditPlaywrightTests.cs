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

        var mode = (Environment.GetEnvironmentVariable("JOBSY_KOMPAS_AUDIT_MODE") ?? "rustig").Trim().ToLowerInvariant();
        var rustig = mode is "rustig" or "new";

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
        if (rustig)
        {
            await page.Locator("[data-ob-rustig-screen=kompas]").WaitForAsync(new() { Timeout = 60_000 });
        }
        else
        {
            await page.Locator(".ob-finale__wrap, .ob-wizard__finish .ob-finale").First
                .WaitForAsync(new() { Timeout = 60_000 });
        }

        var rows = new List<(string Control, string Result)>();
        await AppendKompasControlRowsAsync(page, rows, rustig);

        var reportPath = Path.Combine(reportDir, $"click-audit-kompas-{mode}-{tag}.txt");
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

            Assert.Fail($"Click audit kompas/{mode}/{tag}: {control}={result}; see {reportPath}");
        }
    }

    internal static async Task AppendKompasControlRowsAsync(IPage page, List<(string Control, string Result)> rows, bool rustig)
    {
        if (rustig)
        {
            var later = page.Locator(".ob-rustig__actions > button.ob-rustig__link").First;
            rows.Add(("Later verder", await OnboardingRustigClickAuditSupport.TryHitAuditAsync(page, later, "Later verder")));
        }
        else
        {
            rows.Add(("Later verder", "n/a"));
        }

        var accountLink = page.Locator("a.ob-rustig__link--underline[href='/privacy/data']");
        if (await accountLink.CountAsync() == 0)
        {
            accountLink = page.GetByRole(AriaRole.Link, new() { Name = "Mijn gegevens & account" });
        }

        rows.Add(("Mijn gegevens & account", await OnboardingRustigClickAuditSupport.TryHitAuditAsync(page, accountLink, "account")));

        var matchBar = page.Locator("[data-testid=onboarding-finale-match-bar], a.ob-finale__hl").First;
        rows.Add(("Vacatures highlight", await OnboardingRustigClickAuditSupport.TryHitAuditAsync(page, matchBar, "vacatures")));

        var complete = page.Locator(".ob-finale__btn").First;
        rows.Add(("Naar Lobsy", await OnboardingRustigClickAuditSupport.TryHitAuditAsync(page, complete, "Naar Lobsy")));

        var fullTests = page.Locator("a.ob-finale__link[href*='tab=tests']").First;
        rows.Add(("Volledige tests", await OnboardingRustigClickAuditSupport.TryHitAuditAsync(page, fullTests, "Volledige tests")));

        var dirCards = page.Locator(".ob-rustig__kompas-dir-card");
        var dirCount = await dirCards.CountAsync();
        if (dirCount == 0)
        {
            rows.Add(("Direction cards", "n/a"));
        }
        else
        {
            for (var i = 0; i < Math.Min(dirCount, 3); i++)
            {
                var card = dirCards.Nth(i);
                var title = (await card.Locator("h3").InnerTextAsync()).Trim();
                rows.Add(($"Direction: {title}", await OnboardingRustigClickAuditSupport.TryHitAuditAsync(page, card, title)));
            }
        }

        var logout = page.Locator("a.account-menu__link--logout, form[action*='logout'] button, a[href*='/account/logout']").First;
        rows.Add(("Uitloggen", await OnboardingRustigClickAuditSupport.TryHitAuditAsync(page, logout, "Uitloggen")));
    }

    public static IEnumerable<object[]> Cases()
    {
        foreach (var (w, h, m, tag) in Viewports)
        {
            yield return [w, h, m, tag];
        }
    }
}
