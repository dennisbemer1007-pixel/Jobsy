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

    private static readonly (int Width, int Height, bool Mobile, string Tag)[] AddressViewports =
    [
        (1440, 900, false, "desktop-1440"),
        (1024, 768, false, "tablet-1024"),
        (390, 844, true, "mobile-390"),
    ];

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

    [Theory]
    [MemberData(nameof(AddressQueryCases))]
    public async Task Onboarding_pdok_address_suggestions_blur_resolves_and_volgende_works(
        string query,
        int width,
        int height,
        bool mobile,
        string tag)
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

        await FunctionalOnboardingInteractivityHelpers.RegisterFreshCandidateAsync(page, baseUrl, "func-adres");
        await FunctionalOnboardingInteractivityHelpers.BeginKennismakenStepAsync(page, baseUrl);
        await FunctionalAddressE2eHelpers.FillMinimalStep1ForVolgendeAsync(page);

        var address = FunctionalAddressE2eHelpers.OnboardingAddressInput(page);
        await FunctionalAddressE2eHelpers.TypeAddressQueryAsync(address, query);
        await FunctionalAddressE2eHelpers.WaitForSuggestionWithHouseNumberAsync(page, houseNumber: 27);
        await FunctionalE2eArtifacts.CaptureAsync(page, "onboarding-personal-step", tag);

        await FunctionalAddressE2eHelpers.BlurAddressWithoutPickingAsync(page, "onboarding-home-address");
        await FunctionalAddressE2eHelpers.WaitForOnboardingAddressResolvedAsync(page);
        await FunctionalAddressE2eHelpers.AssertVolgendeAdvancesToStep2Async(page, mobile);
    }

    public static IEnumerable<object[]> ViewportCases()
        => Viewports.Select(v => new object[] { v.Width, v.Height, v.Mobile, v.Tag });

    public static IEnumerable<object[]> AddressQueryCases()
    {
        var queries = new[]
        {
            "Dirk van den Walelaan 27, Zoetermeer",
            "2722DD 27",
        };

        foreach (var q in queries)
        {
            foreach (var v in AddressViewports)
            {
                yield return new object[] { q, v.Width, v.Height, v.Mobile, v.Tag };
            }
        }
    }

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
