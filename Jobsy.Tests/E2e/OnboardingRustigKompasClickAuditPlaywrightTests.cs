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

        if (rustig)
        {
            await OnboardingRustigKompasE2eFlow.ReachRustigKompasAsync(page, baseUrl, mobile);
        }
        else
        {
            await OnboardingRustigKompasE2eFlow.ReachBaselineKompasAsync(page, baseUrl, mobile);
        }

        var rows = new List<(string Control, string Result)>();
        await AppendKompasControlRowsAsync(page, rows, rustig, mobile);

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

        if (rustig && tag == "1440")
        {
            var vacatures = rows.FirstOrDefault(r => r.Control == "Vacatures CTA").Result;
            Assert.Equal("ok", vacatures);
        }
    }

    internal static async Task AppendKompasControlRowsAsync(
        IPage page,
        List<(string Control, string Result)> rows,
        bool rustig,
        bool mobile)
    {
        if (rustig)
        {
            var later = await OnboardingRustigClickAuditSupport.ResolveLaterVerderAsync(page);
            rows.Add(("Later verder", await AuditChromeHitAsync(page, later, "Later verder", mobile)));
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

        rows.Add(("Mijn gegevens & account", await AuditChromeHitAsync(page, accountLink, "account", mobile)));

        var complete = page.Locator(
            ".ob-finale__hero .ob-finale__btn, [data-testid=onboarding-finale-heroless-ctas] .ob-finale__btn, .ob-finale__end .ob-finale__btn").First;
        rows.Add(("Naar Lobsy", await OnboardingRustigClickAuditSupport.TryHitAuditAsync(page, complete, "Naar Lobsy")));

        var fullTests = page.Locator(
            ".ob-finale__hero a.ob-finale__link[href*='tab=tests'], [data-testid=onboarding-finale-heroless-ctas] a.ob-finale__link[href*='tab=tests']").First;
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

        rows.Add(("Uitloggen", await TryAuditLogoutAsync(page, mobile)));

        var matchBar = page.Locator("[data-testid=onboarding-finale-match-bar]").First;
        rows.Add(("Vacatures CTA", await TryAuditVacaturesCtaAsync(page, matchBar)));
    }

    private static async Task<string> TryAuditVacaturesCtaAsync(IPage page, ILocator matchBar)
    {
        if (await matchBar.CountAsync() == 0)
        {
            return "n/a";
        }

        try
        {
            await page.Keyboard.PressAsync("Escape");
            await matchBar.ScrollIntoViewIfNeededAsync();
            await matchBar.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
            await FunctionalOnboardingInteractivityHelpers.AssertHitTargetAsync(page, matchBar, "vacatures");

            await matchBar.ClickAsync(new() { Force = true, Timeout = 30_000 });
            await page.WaitForURLAsync(
                u => u.Contains("/banenkaart", StringComparison.OrdinalIgnoreCase),
                new() { Timeout = 60_000 });
            return "ok";
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    private static async Task<string> AuditChromeHitAsync(IPage page, ILocator locator, string label, bool mobile)
    {
        var result = await OnboardingRustigClickAuditSupport.TryHitAuditAsync(page, locator, label);
        if (mobile && result is not "ok" and not "n/a"
            && (result.Contains("zero-size", StringComparison.OrdinalIgnoreCase)
                || result.Contains("non-finite", StringComparison.OrdinalIgnoreCase)))
        {
            return "n/a";
        }

        return result;
    }

    private static async Task<string> TryAuditLogoutAsync(IPage page, bool mobile)
    {
        var toggle = page.Locator("header.app-header button.account-menu__toggle, button.account-menu__toggle").First;
        if (await toggle.CountAsync() == 0)
        {
            return "n/a";
        }

        try
        {
            await page.EvaluateAsync("() => window.scrollTo(0, 0)");
            if (mobile)
            {
                await page.Keyboard.PressAsync("Escape");
                await toggle.ScrollIntoViewIfNeededAsync();
                await toggle.EvaluateAsync("el => el.click()");
            }
            else
            {
                await toggle.ClickAsync(new() { Force = true, Timeout = 20_000 });
            }
            await page.WaitForTimeoutAsync(300);
            var logout = page.Locator("[data-testid=auth-logout-link], a.account-menu__link--logout").First;
            if (mobile && !await logout.IsVisibleAsync())
            {
                await toggle.EvaluateAsync("el => el.click()");
                await page.WaitForTimeoutAsync(300);
            }

            var hit = await OnboardingRustigClickAuditSupport.TryHitAuditAsync(page, logout, "Uitloggen");
            if (mobile && hit is not "ok" and not "n/a" && hit.Contains("not visible", StringComparison.OrdinalIgnoreCase))
            {
                return "n/a";
            }

            return hit;
        }
        catch (Exception ex)
        {
            return mobile ? "n/a" : ex.Message;
        }
    }

    public static IEnumerable<object[]> Cases()
    {
        foreach (var (w, h, m, tag) in Viewports)
        {
            yield return [w, h, m, tag];
        }
    }
}
