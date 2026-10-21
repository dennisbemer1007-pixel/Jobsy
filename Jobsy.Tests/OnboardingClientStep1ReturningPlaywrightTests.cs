using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class OnboardingClientStep1ReturningPlaywrightTests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
    }

    [Fact]
    public Task Step1_returning_candidate_sidebar_desktop_1280()
        => OnboardingClientStepsPlaywrightSupport.RunStep1ReturningCandidateAccLayoutAsync(_browser!, mobile: false);

    [Fact]
    public Task Step1_returning_candidate_sidebar_mobile_390_touch()
        => OnboardingClientStepsPlaywrightSupport.RunStep1ReturningCandidateAccLayoutAsync(_browser!, mobile: true);

    [Fact]
    public async Task Capture_step1_work_status_grid_desktop_1280()
    {
        if ((Environment.GetEnvironmentVariable("JOBSY_CAPTURE_ONBOARDING_STEP1") ?? "").Trim() != "1")
        {
            return;
        }

        await OnboardingClientStepsPlaywrightSupport.CaptureStep1ReturningLayoutScreenshotAsync(
            _browser!,
            mobile: false,
            "onboarding-step1-client-desktop-1280.png");
    }

    [Fact]
    public async Task Capture_step1_work_status_grid_mobile_390()
    {
        if ((Environment.GetEnvironmentVariable("JOBSY_CAPTURE_ONBOARDING_STEP1") ?? "").Trim() != "1")
        {
            return;
        }

        await OnboardingClientStepsPlaywrightSupport.CaptureStep1ReturningLayoutScreenshotAsync(
            _browser!,
            mobile: true,
            "onboarding-step1-client-mobile-390.png");
    }
}
