using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Required functional gate: candidate onboarding must stay interactive (chips, address, date, geo, Volgende/Terug).
/// Filter: <c>Suite=FunctionalE2e</c>. CI sets <c>JOBSY_E2E_FUNCTIONAL_GATE=1</c>.
/// </summary>
[Collection("PlaywrightSmoke")]
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
    public async Task Kennismaken_step1_controls_are_interactive(int width, int height, bool mobile, string tag)
    {
        var (run, baseUrl) = await TryBeginAsync();
        if (!run)
        {
            return;
        }

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

        await FunctionalOnboardingInteractivityHelpers.ResolveHomeAddressAsync(page, "2671GB");
        await FunctionalOnboardingInteractivityHelpers.AssertGeolocateButtonRespondsAsync(page);

        var next = page.GetByTestId("onboarding-next");
        await Assertions.Expect(next).ToBeEnabledAsync(new() { Timeout = 20_000 });
        await next.ClickAsync(new() { Timeout = 15_000, Force = mobile });
        await page.Locator(".ob-wizard__presets, [data-testid=onboarding-step-2-illustrated]").First
            .WaitForAsync(new() { Timeout = 30_000 });

        await MaybeCaptureAsync(page, tag, "kennismaken-after-next");
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

        await MaybeCaptureAsync(page, tag, "onboarding-complete");
    }

    public static IEnumerable<object[]> ViewportCases()
        => Viewports.Select(v => new object[] { v.Width, v.Height, v.Mobile, v.Tag });

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

    private static async Task MaybeCaptureAsync(IPage page, string tag, string slug)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("JOBSY_E2E_CAPTURE"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var dir = Environment.GetEnvironmentVariable("JOBSY_E2E_ARTIFACTS_ROOT")
                  ?? "/opt/cursor/artifacts/functional-sweep";
        Directory.CreateDirectory(dir);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(dir, $"{slug}-{tag}.png"),
            FullPage = false,
        });
    }
}
