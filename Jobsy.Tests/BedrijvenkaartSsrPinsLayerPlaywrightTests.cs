using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class BedrijvenkaartSsrPinsLayerPlaywrightTests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IllustratedPagesWebFactory? _web;
    private string _baseUrl = "";

    public async ValueTask InitializeAsync()
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        _web = new IllustratedPagesWebFactory(companyMapInitiativeEnabled: false);
        _baseUrl = _web.ServerAddress.TrimEnd('/');
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
    [InlineData("candidate", 1440, 900, false)]
    [InlineData("candidate", 390, 844, true)]
    [InlineData("employer", 1440, 900, false)]
    [InlineData("employer", 390, 844, true)]
    public async Task Jobsy_pins_source_has_features_after_bedrijvenkaart_ssr_load(
        string role,
        int width,
        int height,
        bool touch)
    {
        await using var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new() { Width = width, Height = height },
            HasTouch = touch,
            IsMobile = touch,
            IgnoreHTTPSErrors = true,
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();

        await page.GotoAsync(
            _baseUrl + "/__test/sign-in?role=" + role + "&return=" + Uri.EscapeDataString("/bedrijvenkaart/ssr"),
            new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);

        await page.WaitForFunctionAsync(
            """
            () => {
              const boot = document.getElementById('bedrijvenkaart-ssr-pins-all');
              let seedCount = 0;
              try {
                const parsed = JSON.parse(boot && boot.textContent ? boot.textContent : '[]');
                seedCount = Array.isArray(parsed) ? parsed.length : 0;
              } catch (e) { }
              if (!seedCount) return false;
              const pinCount = window.jobMap && window.jobMap.__testGetPinCount
                ? window.jobMap.__testGetPinCount()
                : 0;
              if (pinCount > 0) return true;
              const map = window.jobMap && window.jobMap.__testGetMap && window.jobMap.__testGetMap();
              if (!map || !map.isStyleLoaded || !map.isStyleLoaded()) return false;
              const src = map.getSource && map.getSource('jobsy-pins');
              return !!(src && src._data && Array.isArray(src._data.features) && src._data.features.length > 0);
            }
            """,
            null,
            new() { Timeout = 120_000 });

        var featureCount = await page.EvaluateAsync<int>(
            """
            () => {
              const map = window.jobMap && window.jobMap.__testGetMap && window.jobMap.__testGetMap();
              if (map) {
                const src = map.getSource('jobsy-pins');
                if (src && src._data && Array.isArray(src._data.features) && src._data.features.length > 0) {
                  return src._data.features.length;
                }
              }
              return window.jobMap && window.jobMap.__testGetPinCount ? window.jobMap.__testGetPinCount() : 0;
            }
            """);

        Assert.True(featureCount > 0, "jobsy-pins source or marker set should contain at least one company pin after load.");
    }
}
