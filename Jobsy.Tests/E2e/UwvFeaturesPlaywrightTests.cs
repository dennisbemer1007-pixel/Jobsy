using Jobsy.Tests.E2e;
using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
public sealed class UwvFeaturesPlaywrightTests : IAsyncLifetime
{
    private IllustratedPagesWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private string _baseUrl = "";

    public async ValueTask InitializeAsync()
    {
        Environment.SetEnvironmentVariable("JOBSY_UWV_FEATURES", "1");
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        _web = new IllustratedPagesWebFactory();
        _baseUrl = _web.ServerAddress;
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

    [Theory]
    [InlineData("/carriere", "[data-testid='uwv-carriere-sections'], .career-page--illustrated")]
    [InlineData("/banenkaart", "[data-testid='uwv-kansberoepen-toggle'], .jobsy-discovery")]
    [InlineData("/carriere/leren", "[data-testid='uwv-carriere-leren-sections'], .uwv-partners")]
    [InlineData("/candidate/ondernemerscheck?stap=intro", ".uwv-ondernemer-intro-layout")]
    [InlineData("/candidate/wat-drijft-jou", ".uwv-drijfveren")]
    [InlineData("/candidate/adviseur-werk", "[data-testid='uwv-adviseur-share']")]
    public async Task Uwv_route_smoke_with_flag_on(string path, string selector)
    {
        await using var context = await _browser!.NewContextAsync(new() { ViewportSize = new() { Width = 1280, Height = 800 } });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(60_000);

        await page.GotoAsync(_baseUrl.TrimEnd('/') + "/__test/sign-in?role=candidate&return=" + Uri.EscapeDataString(path));
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await Assertions.Expect(page.Locator(selector).First).ToBeVisibleAsync(new() { Timeout = 60_000 });
    }
}
