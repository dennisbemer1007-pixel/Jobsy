using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Scoped guards from acceptatie UAT retest (390px overflow + coach overlap on kompas/paspoort).
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class AcceptatieUatRetestLayoutPlaywrightTests
{
    public static TheoryData<int, int, string, string> MobileOverflowRoutes => new()
    {
        { 390, 844, "/candidate/kompas", "kompas" },
        { 390, 844, "/candidate/applications", "applications" },
        { 390, 844, "/candidate/start?step=7&q=1", "onboarding-step-7" },
        { 390, 844, "/candidate/start?step=2", "onboarding-step-2" },
    };

    [Theory]
    [MemberData(nameof(MobileOverflowRoutes))]
    public async Task Mobile_routes_have_no_horizontal_overflow(int width, int height, string path, string tag)
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IsMobile = true,
            HasTouch = true,
            IgnoreHTTPSErrors = true,
        });
        var page = await context.NewPageAsync();

        if (path.Contains("start?", StringComparison.Ordinal))
        {
            if (!await CoachVariantBOverlapAuditSupport.LoginAsync(page, baseUrl, "onboarding.e2e@jobsy.local"))
            {
                return;
            }
        }
        else if (!await KbE2e.TryLoginAsync(page, baseUrl, KbE2e.DefaultCompleteEmail, KbE2e.DefaultPassword))
        {
            return;
        }

        await page.GotoAsync(baseUrl + path, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);

        var overflow = await page.EvaluateAsync<int>(
            "() => Math.max(0, document.documentElement.scrollWidth - document.documentElement.clientWidth)");

        Assert.True(overflow <= 1, $"{tag}@{width}x{height}: horizontal overflow {overflow}px");
    }

    [Theory]
    [InlineData("/candidate/paspoort")]
    [InlineData("/candidate/start/kompas")]
    public async Task Mobile_coach_does_not_overlap_primary_content(string path)
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return;
        }

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IsMobile = true,
            HasTouch = true,
            IgnoreHTTPSErrors = true,
        });
        var page = await context.NewPageAsync();

        var email = path.Contains("/start/", StringComparison.Ordinal)
            ? "onboarding.e2e@jobsy.local"
            : KbE2e.DefaultCompleteEmail;
        if (!await CoachVariantBOverlapAuditSupport.LoginAsync(page, baseUrl, email))
        {
            return;
        }

        await page.GotoAsync(baseUrl + path, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.WaitForTimeoutAsync(800);

        var audit = path.Contains("/start/kompas", StringComparison.Ordinal)
            ? await CoachVariantBOverlapAuditSupport.EvaluateOnboardingCoachOverlapAsync(page)
            : await CoachVariantBOverlapAuditSupport.EvaluateOverlapAsync(page);

        Assert.True(audit.Ok, audit.Reason);
    }
}
