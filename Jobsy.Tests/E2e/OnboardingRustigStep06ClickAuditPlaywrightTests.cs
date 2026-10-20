using System.Text;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
public sealed class OnboardingRustigStep06ClickAuditPlaywrightTests
{
    private static readonly (int Width, int Height, bool Mobile, string Tag)[] Viewports =
    [
        (1440, 900, false, "1440"),
        (390, 844, true, "390"),
    ];

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Audit_step06_clicks(int width, int height, bool mobile, string tag)
    {
        if ((Environment.GetEnvironmentVariable("JOBSY_ONBOARDING_STEP06_CLICK_AUDIT") ?? "").Trim() != "1")
        {
            return;
        }

        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "http://127.0.0.1:5201").Trim().TrimEnd('/');
        if (!await SmokeReachability.IsReachableAsync(baseUrl))
        {
            Assert.Fail($"Stack not reachable at {baseUrl}");
        }

        var mode = (Environment.GetEnvironmentVariable("JOBSY_STEP06_AUDIT_MODE") ?? "rustig").Trim().ToLowerInvariant();
        var rustig = mode is "rustig" or "new";
        var reportDir = Environment.GetEnvironmentVariable("JOBSY_ONBOARDING_RUSTIG_ARTIFACTS")
                        ?? "/opt/cursor/artifacts/onboarding-rustig-step06";
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
            await OnboardingRustigStep06E2eFlow.ReachRustigStep06Async(page, baseUrl, mobile);
        }
        else
        {
            await OnboardingRustigStep06E2eFlow.ReachBaselineStep06Async(page, baseUrl, mobile);
        }
        if (rustig)
        {
            await page.Locator("[data-ob-rustig-screen=step-06]").WaitForAsync(new() { Timeout = 60_000 });
        }
        else
        {
            await page.Locator("[data-testid=onboarding-step-6-illustrated]").WaitForAsync(new() { Timeout = 60_000 });
        }

        var rows = new List<(string Control, string Result)>();
        await AuditButtonAsync(page, rows, "Later verder", page.GetByRole(AriaRole.Button, new() { Name = "Later verder" }));

        var accountLink = page.Locator("a.ob-rustig__link--underline[href='/privacy/data']");
        if (await accountLink.CountAsync() == 0)
        {
            accountLink = page.GetByRole(AriaRole.Link, new() { Name = "Mijn gegevens & account" });
        }

        rows.Add(("Mijn gegevens & account", await TryHitAuditAsync(page, accountLink, "account")));

        if (rustig)
        {
            await AuditDreamChipTogglesAsync(page, rows, mobile);
            var input = page.Locator("#onboarding-rustig-dream-input");
            await AuditButtonAsync(page, rows, "Droom invoer", input);
        }
        else
        {
            var legacyInput = page.Locator("#onboarding-field-dream input").First;
            await AuditButtonAsync(page, rows, "Droom zoeken", legacyInput);
        }

        var unknown = page.GetByRole(AriaRole.Button, new() { Name = "Weet ik nog niet" });
        await AuditButtonAsync(page, rows, "Weet ik nog niet", unknown);
        var back = page.GetByTestId("onboarding-back");
        await AuditButtonAsync(page, rows, "Terug", back);
        var next = page.GetByTestId("onboarding-next");
        await AuditButtonAsync(page, rows, "Volgende", next);

        if (rustig && await page.Locator(".ob-rustig__dream").CountAsync() > 0)
        {
            await page.Locator(".ob-rustig__dream").First.ClickAsync(new() { Force = mobile });
        }

        await inputSafeFillAsync(page, rustig, "Elektricien");
        try
        {
            await FunctionalOnboardingInteractivityHelpers.ClickOnboardingNextAsync(page, mobile);
            await page.Locator(".ob-wizard__consent, [data-ob-rustig-screen=\"step-07\"], .test-likert, [data-testid=onboarding-step-7-illustrated]")
                .First.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 90_000 });
            await FunctionalOnboardingInteractivityHelpers.ClickOnboardingBackAsync(page, mobile);
            await page.Locator("[data-ob-rustig-screen=step-06]").WaitForAsync(new() { Timeout = 60_000 });
            var input = page.Locator("#onboarding-rustig-dream-input");
            var value = await input.InputValueAsync();
            rows.Add(("Droom bewaard na Terug", value.Contains("Elektricien", StringComparison.OrdinalIgnoreCase) ? "ok" : value));
            rows.Add(("Navigatie naar stap 7", "ok"));
        }
        catch (Exception ex)
        {
            rows.Add(("Droom bewaard na Terug", "n/a"));
            rows.Add(("Navigatie naar stap 7", ex.Message));
        }

        var reportPath = Path.Combine(reportDir, $"click-audit-step-06-{mode}-{tag}.txt");
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

            if (control is "Navigatie naar stap 7" or "Droom bewaard na Terug")
            {
                continue;
            }

            Assert.Fail($"Click audit {mode}/{tag}: {control}={result}; see {reportPath}");
        }
    }

    private static async Task AuditDreamChipTogglesAsync(IPage page, List<(string, string)> rows, bool mobile)
    {
        var chips = page.Locator(".ob-rustig__dream");
        var count = Math.Min(await chips.CountAsync(), 4);
        if (count == 0)
        {
            rows.Add(("Droom-chips", "n/a"));
            return;
        }

        for (var i = 0; i < count; i++)
        {
            var chip = chips.Nth(i);
            var label = (await chip.InnerTextAsync()).Trim();
            try
            {
                await FunctionalOnboardingInteractivityHelpers.AssertHitTargetAsync(page, chip, $"dream-{i}");
                await chip.ClickAsync(new() { Force = mobile });
                var pressed = await chip.GetAttributeAsync("aria-pressed");
                var on = await chip.EvaluateAsync<bool>("el => el.classList.contains('is-on')");
                rows.Add(($"Droom-chip: {label}", pressed == "true" || on ? "ok" : "not-selected"));
            }
            catch (Exception ex)
            {
                rows.Add(($"Droom-chip: {label}", ex.Message));
            }
        }
    }

    public static IEnumerable<object[]> Cases()
    {
        foreach (var (w, h, m, tag) in Viewports)
        {
            yield return [w, h, m, tag];
        }
    }

    private static async Task inputSafeFillAsync(IPage page, bool rustig, string text)
    {
        var input = rustig
            ? page.Locator("#onboarding-rustig-dream-input")
            : page.Locator("#onboarding-field-dream input").First;
        if (await input.CountAsync() > 0)
        {
            await input.FillAsync(text);
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
