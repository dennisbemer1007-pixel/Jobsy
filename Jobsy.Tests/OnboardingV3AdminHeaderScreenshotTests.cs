using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>Captures onboarding v3 screenshots when RUN_ONBOARDING_V3_SCREENSHOTS=1.</summary>
[Collection("PlaywrightSmoke")]
public sealed class OnboardingV3AdminHeaderScreenshotTests : IAsyncLifetime
{
    private const string OutDir = "/opt/cursor/artifacts/onboarding-adminheader";
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("RUN_ONBOARDING_V3_SCREENSHOTS"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        Directory.CreateDirectory(OutDir);
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
    }

    [Theory]
    [InlineData(390, 844, "step00-welcome")]
    [InlineData(1440, 900, "step00-welcome")]
    [InlineData(390, 844, "step01")]
    [InlineData(1440, 900, "step01")]
    [InlineData(390, 844, "step02")]
    [InlineData(1440, 900, "step02")]
    [InlineData(390, 844, "step06")]
    [InlineData(1440, 900, "step06")]
    [InlineData(390, 844, "step04")]
    [InlineData(1440, 900, "step04")]
    [InlineData(390, 844, "step08")]
    [InlineData(1440, 900, "step08")]
    public async Task Capture_step_screenshots(int width, int height, string label)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("RUN_ONBOARDING_V3_SCREENSHOTS"), "1", StringComparison.Ordinal))
        {
            return;
        }

        await CaptureAsync(width, height, label, fullPage: true);
    }

    [Theory]
    [InlineData(390, 844, "step01-viewport")]
    [InlineData(390, 844, "step06-viewport")]
    public async Task Capture_viewport_footer_screenshots(int width, int height, string label)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("RUN_ONBOARDING_V3_SCREENSHOTS"), "1", StringComparison.Ordinal))
        {
            return;
        }

        await CaptureAsync(width, height, label, fullPage: false);
    }

    private async Task CaptureAsync(int width, int height, string label, bool fullPage)
    {
        await using var factory = new OnboardingClientStepsPlaywrightSupport.ClientStepsWebFactory();
        var baseLabel = label.Replace("-viewport", "", StringComparison.Ordinal);
        factory.WelcomeDismissed = baseLabel != "step00-welcome";
        var root = factory.EnsureServerAddress();
        await using var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new() { Width = width, Height = height },
            IgnoreHTTPSErrors = true,
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(120_000);

        var step = baseLabel switch
        {
            "step01" => 1,
            "step02" => 2,
            "step04" => 4,
            "step06" => 6,
            "step08" => 8,
            _ => 0,
        };
        var url = step == 0
            ? "/candidate/start"
            : $"/candidate/start?step={step}";
        await page.GotoAsync(root + "/__test/sign-in?return=" + Uri.EscapeDataString(url));
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        if (step == 0)
        {
            await page.Locator(".ob-v3-welcome, .ob-wizard__welcome").WaitForAsync();
        }
        else
        {
            await page.Locator("[data-ob-client-step]").First.WaitForAsync();
        }

        await Assertions.Expect(page.Locator("[data-testid='lobsy-shell-topbar'], .admin-topbar").First).ToBeVisibleAsync();

        var file = Path.Combine(OutDir, $"{label}-{width}.png");
        await page.ScreenshotAsync(new() { Path = file, FullPage = fullPage });
        Assert.True(File.Exists(file));
    }
}
