using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Hits primary controls on illustrated candidate/public shells — fails if hero art swallows clicks.
/// </summary>
[Collection("PlaywrightSmoke")]
[Trait("Suite", "FunctionalE2e")]
public sealed class FunctionalIllustratedHeroSweepPlaywrightTests
{
    private static readonly (string Path, string ControlSelector, string Label)[] Pages =
    [
        ("/banenkaart", "[data-testid=kb-keyword-search-desktop], [data-testid=kb-keyword-search]", "banenkaart search"),
        ("/carriere", "a, button, input", "carrière"),
        ("/carriere/leren", "input, button, a", "leren & groeien"),
        ("/candidate/paspoort", "button, a, input", "paspoort"),
        ("/candidate/match", "button, a", "match"),
        ("/candidate/ontdekkingsreis", "button, a", "ontdekkingsreis"),
        ("/candidate/applications", "button, a, input", "sollicitaties"),
        ("/candidate/liked", "button, a, input", "bewaard"),
        ("/bedrijvenkaart", "input, button, a", "bedrijvenkaart"),
        ("/", "a, button, input", "landing"),
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

        var email = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_EMAIL") ?? "kandidaat@jobsy.local";
        var password = Environment.GetEnvironmentVariable("JOBSY_E2E_CANDIDATE_PASSWORD") ?? "Jobsy123!";

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var ctx = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IsMobile = mobile,
            HasTouch = mobile,
            Locale = "nl-NL",
            IgnoreHTTPSErrors = true,
        });
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        var loggedIn = false;
        foreach (var (path, controlSelector, label) in Pages)
        {
            var needsAuth = path.StartsWith("/candidate", StringComparison.Ordinal)
                || path.StartsWith("/carriere", StringComparison.Ordinal);
            if (needsAuth && !loggedIn)
            {
                Assert.True(await KbE2e.TryLoginAsync(page, baseUrl, email, password), "Login failed for hero sweep.");
                loggedIn = true;
            }

            await page.GotoAsync($"{baseUrl}{path}", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            if (path.StartsWith("/candidate", StringComparison.Ordinal) || path.StartsWith("/carriere", StringComparison.Ordinal))
            {
                await page.WaitForFunctionAsync(
                    "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
                    null,
                    new() { Timeout = 90_000 }).ContinueWith(_ => { }, TaskContinuationOptions.OnlyOnFaulted);
            }

            if (path == "/banenkaart" && mobile)
            {
                var filters = page.Locator("[data-testid=kb-filters-button], .kb-filters-button").First;
                if (await filters.CountAsync() > 0 && await filters.IsVisibleAsync())
                {
                    await filters.ClickAsync(new() { Force = true });
                    await page.WaitForTimeoutAsync(400);
                }
            }

            var control = page.Locator(controlSelector).First;
            if (await control.CountAsync() == 0)
            {
                continue;
            }

            await control.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 45_000 });
            await FunctionalOnboardingInteractivityHelpers.AssertHitTargetAsync(page, control, $"{label} @ {path}");
        }
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
