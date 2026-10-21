using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class OnboardingClientStepsSaveOncePlaywrightTests : IAsyncLifetime
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
    public Task Address_suggest_waits_for_three_chars_desktop()
        => OnboardingClientStepsPlaywrightSupport.RunAddressSuggestMinThreeCharsAsync(_browser!, mobile: false);

    [Fact]
    public Task Step1_through_3_desktop_save_once_on_volgende()
        => OnboardingClientStepsPlaywrightSupport.RunSteps1And2SaveOnceAsync(_browser!, mobile: false);

    [Fact]
    public Task Happy_path_through_consent_step_desktop()
        => OnboardingClientStepsPlaywrightSupport.RunHappyPathTowardDreamAsync(_browser!, mobile: false);
}
