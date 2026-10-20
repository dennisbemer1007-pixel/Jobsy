using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>Captures PO onboarding-rustig mockups at 1440×900 and 390×844.</summary>
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

    private static readonly (string Slug, string Path, int Width, int Height)[] Captures =
    [
        ("step-01-desktop", "/mockups/onboarding-rustig/step-01", 1440, 900),
        ("step-01-mobile", "/mockups/onboarding-rustig/step-01", 390, 844),
        ("step-02-desktop", "/mockups/onboarding-rustig/step-02", 1440, 900),
        ("step-02-mobile", "/mockups/onboarding-rustig/step-02", 390, 844),
        ("step-07-a-desktop", "/mockups/onboarding-rustig/step-07-a", 1440, 900),
        ("step-07-a-mobile", "/mockups/onboarding-rustig/step-07-a", 390, 844),
        ("step-07-b-desktop", "/mockups/onboarding-rustig/step-07-b", 1440, 900),
        ("step-07-b-mobile", "/mockups/onboarding-rustig/step-07-b", 390, 844),
        ("test-leuk-desktop", "/mockups/onboarding-rustig/test-leuk", 1440, 900),
        ("test-leuk-mobile", "/mockups/onboarding-rustig/test-leuk", 390, 844),
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

        foreach (var (slug, path, width, height) in Captures)
        {
            var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
            {
                ViewportSize = new ViewportSize { Width = width, Height = height },
            });

            await page.GotoAsync($"{_baseUrl}{path}", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForSelectorAsync("[data-ob-rustig-screen]", new PageWaitForSelectorOptions { Timeout = 30_000 });
            await page.WaitForTimeoutAsync(400);

            var file = Path.Combine(OutDir, $"{slug}.png");
            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = file,
                FullPage = true,
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
