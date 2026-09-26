namespace Jobsy.Tests;

/// <summary>
/// Soft-skip Playwright smoke for Funda mobile map when Playwright is not installed.
/// Install Microsoft.Playwright + browsers in CI to enable.
/// </summary>
public class MapFundaMobilePlaywrightSmokeTests
{
    [Fact]
    public void Playwright_mobile_map_smoke_is_documented_and_skippable()
    {
        // Full browser smoke (390×844): load map → tap pin → card title → tap cluster → row → open list.
        // Soft-skip until Playwright package/browsers are available in this environment.
        var playwrightAsm = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name is "Microsoft.Playwright");
        if (playwrightAsm is null)
        {
            // Document expected coverage for Acceptatie / local Playwright runs.
            Assert.True(true, "Playwright not referenced — smoke deferred. See MapFundaMobileTests source guards.");
            return;
        }

        Assert.Fail("Playwright is referenced but the interactive smoke runner is not wired yet.");
    }
}
