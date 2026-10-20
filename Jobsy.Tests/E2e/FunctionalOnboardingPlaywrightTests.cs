using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Required functional gate: candidate onboarding must stay interactive (chips, address, date, geo, Volgende/Terug).
/// Filter: <c>Suite=FunctionalE2e</c>. CI sets <c>JOBSY_E2E_FUNCTIONAL_GATE=1</c>.
/// </summary>
[Collection("FunctionalE2e")]
[Trait("Suite", "FunctionalE2e")]
public sealed class FunctionalOnboardingPlaywrightTests
{
    private static readonly (int Width, int Height, bool Mobile, string Tag)[] Viewports =
    [
        (1440, 900, false, "desktop-1440"),
        (390, 844, true, "mobile-390"),
    ];

    [Theory]
    [MemberData(nameof(ViewportCases))]
    public async Task Start_welcome_beginnen_opens_step1_with_candidate_header(int width, int height, bool mobile, string tag)
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
        await using var ctx = await browser.NewContextAsync(CreateContextOptions(baseUrl, width, height, mobile));
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await FunctionalOnboardingInteractivityHelpers.RegisterFreshCandidateAsync(page, baseUrl, "func-start");
        await FunctionalOnboardingInteractivityHelpers.OpenStartWelcomeAsync(page, baseUrl);
        await FunctionalOnboardingInteractivityHelpers.AssertCandidateHeaderVisibleAsync(page);
        await FunctionalOnboardingInteractivityHelpers.ClickBeginnenOnWelcomeAsync(page, mobile);
        await FunctionalOnboardingInteractivityHelpers.AssertCandidateHeaderVisibleAsync(page);
        await page.Locator("[data-testid=onboarding-step-1-illustrated]").WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 90_000,
        });
        await FunctionalOnboardingInteractivityHelpers.CaptureOnboardingStartHotfixAsync(page, tag);
    }

    [Theory]
    [MemberData(nameof(ViewportCases))]
    public async Task Logout_works_from_start_welcome_and_from_step1(int width, int height, bool mobile, string tag)
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
        await using var ctx = await browser.NewContextAsync(CreateContextOptions(baseUrl, width, height, mobile));
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await FunctionalOnboardingInteractivityHelpers.RegisterFreshCandidateAsync(page, baseUrl, $"func-logout-{tag}");
        await FunctionalOnboardingInteractivityHelpers.OpenStartWelcomeAsync(page, baseUrl);
        await FunctionalOnboardingInteractivityHelpers.AssertCandidateHeaderVisibleAsync(page);
        await FunctionalOnboardingInteractivityHelpers.ClickBeginnenOnWelcomeAsync(page, mobile);
        await FunctionalOnboardingInteractivityHelpers.AssertCandidateHeaderVisibleAsync(page);
        await FunctionalOnboardingInteractivityHelpers.LogoutViaHeaderAsync(page, mobile);
    }

    [Theory]
    [MemberData(nameof(ViewportCases))]
    public async Task Wizard_controls_hit_test_after_illustration_loads_on_step_transitions(int width, int height, bool mobile, string tag)
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
        await using var ctx = await browser.NewContextAsync(CreateContextOptions(baseUrl, width, height, mobile));
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(180_000);

        await FunctionalOnboardingInteractivityHelpers.RegisterFreshCandidateAsync(page, baseUrl, $"func-illustration-{tag}");
        await FunctionalOnboardingInteractivityHelpers.BeginKennismakenStepAsync(page, baseUrl);
        await FunctionalOnboardingInteractivityHelpers.FillKennismakenBasicsExceptWorkStatusAsync(page);
        await FunctionalOnboardingInteractivityHelpers.SelectAnyWorkStatusChipAsync(page);

        for (var step = 1; step <= 5; step++)
        {
            await FunctionalOnboardingInteractivityHelpers.WaitForStepIllustrationLoadedAsync(page, step);
            await FunctionalOnboardingInteractivityHelpers.AssertOnboardingStepInteractiveAsync(page, step);
            if (step == 1)
            {
                await FunctionalOnboardingInteractivityHelpers.ClickWizardNextWhenVisibleAsync(page);
                await page.Locator("[data-testid=onboarding-step-2-illustrated]").WaitForAsync(new() { Timeout = 90_000 });
                continue;
            }

            if (step == 2)
            {
                await page.Locator(".ob-wizard__presets .lobsy-choice-tile").First.ClickAsync(new() { Force = true });
            }

            await FunctionalOnboardingInteractivityHelpers.ClickWizardNextWhenVisibleAsync(page);
            if (step < 5)
            {
                await page.Locator($"[data-testid=onboarding-step-{step + 1}-illustrated]").WaitForAsync(new() { Timeout = 90_000 });
            }
        }

        await FunctionalE2eArtifacts.CaptureAsync(page, "wizard-illustration-hit-test", tag);
    }

    [Fact]
    public async Task Four_mini_tests_finish_on_kompas_without_reopening_completed_leuk()
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
        await using var ctx = await browser.NewContextAsync(CreateContextOptions(baseUrl, 1440, 900, false));
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(240_000);

        await FunctionalOnboardingInteractivityHelpers.CompleteFourMiniTestsToKompasWithResumeChecksAsync(page, baseUrl);
    }

    [Theory]
    [MemberData(nameof(DesktopViewportCases))]
    public async Task Wizard_steps_1_through_10_stay_interactive_after_each_next(int width, int height, bool mobile, string tag)
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
        await using var ctx = await browser.NewContextAsync(CreateContextOptions(baseUrl, width, height, mobile));
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(180_000);

        await FunctionalOnboardingInteractivityHelpers.WalkWizardSteps1Through10WithNextAsync(page, baseUrl);
        await FunctionalE2eArtifacts.CaptureAsync(page, "wizard-steps-1-10-transitions", tag);
    }

    [Theory]
    [MemberData(nameof(ViewportCases))]
    public async Task Wizard_chips_on_steps_1_3_toggle_within_300ms(int width, int height, bool mobile, string tag)
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
        await using var ctx = await browser.NewContextAsync(CreateContextOptions(baseUrl, width, height, mobile));
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await FunctionalOnboardingInteractivityHelpers.RegisterFreshCandidateAsync(page, baseUrl, $"func-chips-{tag}");
        await FunctionalOnboardingInteractivityHelpers.AssertRapidWizardChipInteractionsSteps1To3Async(page, baseUrl);
        await FunctionalE2eArtifacts.CaptureAsync(page, "wizard-rapid-chips", tag);
    }

    [Theory]
    [MemberData(nameof(ViewportCases))]
    public async Task Kennismaken_step1_stays_interactive_after_slow_typing(int width, int height, bool mobile, string tag)
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
        await using var ctx = await browser.NewContextAsync(CreateContextOptions(baseUrl, width, height, mobile));
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await FunctionalOnboardingInteractivityHelpers.RegisterFreshCandidateAsync(page, baseUrl, $"func-slow-{tag}");
        await FunctionalOnboardingInteractivityHelpers.BeginKennismakenStepAsync(page, baseUrl);

        await FunctionalOnboardingInteractivityHelpers.TypeBirthDateSlowlyAsync(page);
        await FunctionalOnboardingInteractivityHelpers.TypeHomeAddressSlowlyAsync(page);

        var address = page.Locator("#onboarding-home-address").First;
        await FunctionalOnboardingInteractivityHelpers.AssertHitTargetAsync(page, address, "address-after-typing");
        await FunctionalOnboardingInteractivityHelpers.AssertWorkStatusChipTogglesAsync(page);

        await FunctionalE2eArtifacts.CaptureAsync(page, "kennismaken-after-slow-typing", tag);
    }

    [Theory]
    [MemberData(nameof(ViewportCases))]
    public async Task Kennismaken_step1_controls_are_interactive(int width, int height, bool mobile, string tag)
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
        await using var ctx = await browser.NewContextAsync(CreateContextOptions(baseUrl, width, height, mobile));
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        await FunctionalOnboardingInteractivityHelpers.RegisterFreshCandidateAsync(page, baseUrl, "func-kennismaken");
        await FunctionalOnboardingInteractivityHelpers.BeginKennismakenStepAsync(page, baseUrl);

        await FunctionalOnboardingInteractivityHelpers.AssertWorkStatusChipTogglesAsync(page);
        await FunctionalOnboardingInteractivityHelpers.AssertBirthDateFieldInteractiveAsync(page);

        var firstName = page.Locator("input[autocomplete=given-name]").First;
        await FunctionalOnboardingInteractivityHelpers.AssertHitTargetAsync(page, firstName, "firstname");
        await firstName.FillAsync("Func");
        Assert.Equal("Func", await firstName.InputValueAsync());
        var lastName = page.Locator("input[autocomplete=family-name]").First;
        if (await lastName.CountAsync() > 0)
        {
            await lastName.FillAsync("Test");
        }

        await FunctionalOnboardingInteractivityHelpers.ResolveHomeAddressAsync(page, "2671GB");
        await FunctionalOnboardingInteractivityHelpers.AssertGeolocateButtonRespondsAsync(page);

        var next = page.GetByTestId("onboarding-next");
        await Assertions.Expect(next).ToBeEnabledAsync(new() { Timeout = 20_000 });
        await next.ClickAsync(new() { Timeout = 15_000, Force = mobile });
        await page.Locator("[data-testid=onboarding-step-2-illustrated], .ob-wizard__presets, .ob-illustrated-step").First
            .WaitForAsync(new() { Timeout = 90_000 });

        await FunctionalE2eArtifacts.CaptureAsync(page, "kennismaken-after-next", tag);
    }

    [Theory]
    [MemberData(nameof(ViewportCases))]
    public async Task Full_onboarding_registration_through_later_exits_to_banenkaart(int width, int height, bool mobile, string tag)
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
        await using var ctx = await browser.NewContextAsync(CreateContextOptions(baseUrl, width, height, mobile));
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        var page = await ctx.NewPageAsync();
        page.SetDefaultTimeout(180_000);

        await FunctionalOnboardingInteractivityHelpers.RegisterFreshCandidateAsync(page, baseUrl, "func-full");
        await FunctionalOnboardingInteractivityHelpers.RunThroughWizardToFinishAsync(page, baseUrl);

        Assert.True(
            page.Url.Contains("/banenkaart", StringComparison.OrdinalIgnoreCase)
            || await page.Locator(".ob-wizard__finish").CountAsync() > 0,
            "Expected onboarding to reach banenkaart (tests later) or finish screen.");

        await FunctionalE2eArtifacts.CaptureAsync(page, "onboarding-complete", tag);
    }

    public static IEnumerable<object[]> ViewportCases()
        => Viewports.Select(v => new object[] { v.Width, v.Height, v.Mobile, v.Tag });

    public static IEnumerable<object[]> DesktopViewportCases()
        => Viewports.Where(v => !v.Mobile).Select(v => new object[] { v.Width, v.Height, v.Mobile, v.Tag });

    private static BrowserNewContextOptions CreateContextOptions(string baseUrl, int width, int height, bool mobile)
        => new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IsMobile = mobile,
            HasTouch = mobile,
            Locale = "nl-NL",
            Geolocation = new() { Latitude = 52.066f, Longitude = 4.235f, Accuracy = 25 },
            Permissions = ["geolocation"],
            IgnoreHTTPSErrors = true,
            BaseURL = baseUrl,
        };

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
