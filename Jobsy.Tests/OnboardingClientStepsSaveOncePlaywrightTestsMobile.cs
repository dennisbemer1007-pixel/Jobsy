using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>Mobile (390px, touch) onboarding client-form gate — mirrors desktop save-once tests.</summary>
[Collection("PlaywrightSmoke")]
public sealed class OnboardingClientStepsSaveOncePlaywrightTestsMobile : IAsyncLifetime
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
    public Task Address_suggest_waits_for_three_chars_mobile_390_touch()
        => OnboardingClientStepsPlaywrightSupport.RunAddressSuggestMinThreeCharsAsync(_browser!, mobile: true);

    [Fact]
    public Task Step1_through_3_mobile_390_touch_save_once_on_volgende()
        => OnboardingClientStepsPlaywrightSupport.RunSteps1And2SaveOnceAsync(_browser!, mobile: true);

    [Fact]
    public Task Happy_path_through_consent_step_mobile_390_touch()
        => OnboardingClientStepsPlaywrightSupport.RunHappyPathTowardDreamAsync(_browser!, mobile: true);
}
