using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Hits primary controls on illustrated candidate/public shells — fails if hero art swallows clicks.
/// </summary>
[Collection("FunctionalE2e")]
[Trait("Suite", "FunctionalE2e")]
public sealed class FunctionalIllustratedHeroSweepPlaywrightTests
{
    private static readonly (string Path, string ControlSelector, string Label)[] Pages =
    [
        ("/banenkaart", "[data-testid=kb-keyword-search-desktop], [data-testid=kb-keyword-search]", "banenkaart search"),
        ("/carriere", ".career-card .btn-primary, .career-card button, .career-stepper button, .panel-page .btn-primary", "carrière"),
        ("/carriere/leren", ".career-card a, .career-card button, .panel-page button", "leren & groeien"),
        ("/candidate/paspoort", ".passport-page button, .passport-page a, .passport-layout button", "paspoort"),
        ("/candidate/match", ".app-main button, .app-main a", "match"),
        ("/candidate/ontdekkingsreis", ".app-main button, .app-main a", "ontdekkingsreis"),
        ("/candidate/applications", ".apps-page button, .apps-page a, .app-main input", "sollicitaties"),
        ("/candidate/liked", ".app-main button, .app-main a, .app-main input", "bewaard"),
        ("/bedrijvenkaart", "input[type=search], input[inputmode=search], .jobsy-search input", "bedrijvenkaart"),
        ("/", ".pub-landing__hero-copy .btn-primary, .pub-landing__hero-copy a.btn-primary", "landing"),
    ];

    [Theory]
    [InlineData(1440, 900, false)]
    [InlineData(390, 844, true)]
    public async Task Illustrated_pages_first_primary_control_is_not_blocked_by_hero_art(int width, int height, bool mobile)
    {
        var (run, baseUrl) = await TryBeginAsync();
        if (!run)
        {
            return;
        }

        await FunctionalE2eAuth.WarmupStackAsync(baseUrl);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var ctx = await FunctionalE2eAuth.NewCandidateContextAsync(browser, baseUrl, new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IsMobile = mobile,
            HasTouch = mobile,
            Locale = "nl-NL",
        });
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        foreach (var (path, controlSelector, label) in Pages)
        {
            if (path == "/banenkaart")
            {
                await KbDiscoveryE2eHelpers.PrepareBanenkaartAsync(page, baseUrl);
                var search = await KbDiscoveryE2eHelpers.WaitForVisibleKeywordSearchAsync(page, width);
                await FunctionalOnboardingInteractivityHelpers.AssertHitTargetAsync(page, search, $"{label} @ {path}");
                continue;
            }

            var needsAuth = path.StartsWith("/candidate", StringComparison.Ordinal)
                || path.StartsWith("/carriere", StringComparison.Ordinal);
            await page.GotoAsync($"{baseUrl}{path}", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            if (needsAuth)
            {
                await KbE2e.WaitForStyledAppAsync(page);
            }

            var control = page.Locator(controlSelector).First;
            if (await control.CountAsync() == 0)
            {
                continue;
            }

            await control.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 45_000 });
            await FunctionalOnboardingInteractivityHelpers.AssertHitTargetAsync(page, control, $"{label} @ {path}");
        }

        var tag = mobile ? "mobile-390" : "desktop-1440";
        await FunctionalE2eArtifacts.CaptureAsync(page, "hero-sweep-final", tag);
    }

    private static async Task<(bool Run, string BaseUrl)> TryBeginAsync()
    {
        var baseUrl = FunctionalE2eGate.TryBaseUrl() ?? "";
        if (FunctionalE2eGate.IsRequired)
        {
            baseUrl = await FunctionalE2eGate.RequireReachableBaseUrlAsync();
            return (true, baseUrl);
        }

        if (string.IsNullOrWhiteSpace(baseUrl) || !await SmokeReachability.IsReachableAsync(baseUrl))
        {
            return (false, baseUrl);
        }

        return (true, baseUrl);
    }
}
