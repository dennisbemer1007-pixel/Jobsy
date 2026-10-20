using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>Captures PO onboarding-rustig and site-rustig mockups at 1440×900 and 390×844.</summary>
[Collection("PlaywrightSmoke")]
public sealed class OnboardingRustigMockupScreenshotPlaywrightTests : IAsyncLifetime
{
    private const string EnvRun = "JOBSY_ONBOARDING_RUSTIG_MOCKUP_SCREENSHOTS";

    private static bool ShouldRun =>
        string.Equals(Environment.GetEnvironmentVariable(EnvRun), "1", StringComparison.Ordinal);

    private static string OutDir =>
        string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("JOBSY_ONBOARDING_RUSTIG_ARTIFACTS"))
            ? Path.GetFullPath("/opt/cursor/artifacts/onboarding-rustig-mockups")
            : Environment.GetEnvironmentVariable("JOBSY_ONBOARDING_RUSTIG_ARTIFACTS")!;

    private IllustratedPagesWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private string _baseUrl = "";

    private static readonly (string Slug, string Path, int Width, int Height, string MarkerAttr)[] Captures =
    [
        ("step-01-desktop", "/mockups/onboarding-rustig/step-01", 1440, 900, "data-ob-rustig-screen"),
        ("step-01-mobile", "/mockups/onboarding-rustig/step-01", 390, 844, "data-ob-rustig-screen"),
        ("step-02-desktop", "/mockups/onboarding-rustig/step-02", 1440, 900, "data-ob-rustig-screen"),
        ("step-02-mobile", "/mockups/onboarding-rustig/step-02", 390, 844, "data-ob-rustig-screen"),
        ("step-07-a-desktop", "/mockups/onboarding-rustig/step-07-a", 1440, 900, "data-ob-rustig-screen"),
        ("step-07-a-mobile", "/mockups/onboarding-rustig/step-07-a", 390, 844, "data-ob-rustig-screen"),
        ("step-07-b-desktop", "/mockups/onboarding-rustig/step-07-b", 1440, 900, "data-ob-rustig-screen"),
        ("step-07-b-mobile", "/mockups/onboarding-rustig/step-07-b", 390, 844, "data-ob-rustig-screen"),
        ("test-leuk-a-desktop", "/mockups/onboarding-rustig/test-leuk-a", 1440, 900, "data-ob-rustig-screen"),
        ("test-leuk-a-mobile", "/mockups/onboarding-rustig/test-leuk-a", 390, 844, "data-ob-rustig-screen"),
        ("test-leuk-b-desktop", "/mockups/onboarding-rustig/test-leuk-b", 1440, 900, "data-ob-rustig-screen"),
        ("test-leuk-b-mobile", "/mockups/onboarding-rustig/test-leuk-b", 390, 844, "data-ob-rustig-screen"),
        ("site-partners-desktop", "/mockups/site-rustig/partners?lang=nl", 1440, 900, "data-site-rustig-screen"),
        ("site-partners-mobile", "/mockups/site-rustig/partners?lang=nl", 390, 844, "data-site-rustig-screen"),
        ("site-gratis-dna-desktop", "/mockups/site-rustig/gratis-dna?lang=nl", 1440, 900, "data-site-rustig-screen"),
        ("site-gratis-dna-mobile", "/mockups/site-rustig/gratis-dna?lang=nl", 390, 844, "data-site-rustig-screen"),
        ("site-bedrijf-registreren-desktop", "/mockups/site-rustig/bedrijf-registreren?lang=nl", 1440, 900, "data-site-rustig-screen"),
        ("site-bedrijf-registreren-mobile", "/mockups/site-rustig/bedrijf-registreren?lang=nl", 390, 844, "data-site-rustig-screen"),
        ("site-voorwaarden-desktop", "/mockups/site-rustig/voorwaarden?lang=nl", 1440, 900, "data-site-rustig-screen"),
        ("site-voorwaarden-mobile", "/mockups/site-rustig/voorwaarden?lang=nl", 390, 844, "data-site-rustig-screen"),
        ("site-footer-desktop", "/mockups/site-rustig/footer?lang=nl", 1440, 900, "data-site-rustig-screen"),
        ("site-footer-mobile", "/mockups/site-rustig/footer?lang=nl", 390, 844, "data-site-rustig-screen"),
        ("site-toegankelijkheid-desktop", "/mockups/site-rustig/toegankelijkheid?lang=nl", 1440, 900, "data-site-rustig-screen"),
        ("site-toegankelijkheid-mobile", "/mockups/site-rustig/toegankelijkheid?lang=nl", 390, 844, "data-site-rustig-screen"),
        ("site-onboarding-start-desktop", "/mockups/site-rustig/onboarding-start?lang=nl", 1440, 900, "data-site-rustig-screen"),
        ("site-onboarding-start-mobile", "/mockups/site-rustig/onboarding-start?lang=nl", 390, 844, "data-site-rustig-screen"),
    ];

    public async ValueTask InitializeAsync()
    {
        if (!ShouldRun)
        {
            return;
        }

        Directory.CreateDirectory(OutDir);
        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        _web = new IllustratedPagesWebFactory();
        _ = _web.ServerAddress;
        _baseUrl = _web.ServerAddress;
    }

    [Fact]
    public async Task Capture_all_mockup_screens()
    {
        if (!ShouldRun)
        {
            return;
        }

        foreach (var (slug, path, width, height, marker) in Captures)
        {
            var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
            {
                ViewportSize = new ViewportSize { Width = width, Height = height },
            });

            await page.GotoAsync($"{_baseUrl}{path}", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForSelectorAsync($"[{marker}]", new PageWaitForSelectorOptions { Timeout = 30_000 });
            await page.WaitForTimeoutAsync(500);

            var file = Path.Combine(OutDir, $"{slug}.png");
            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = file,
                FullPage = marker == "data-site-rustig-screen",
            });

            await page.CloseAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        _web?.Dispose();
    }
}
