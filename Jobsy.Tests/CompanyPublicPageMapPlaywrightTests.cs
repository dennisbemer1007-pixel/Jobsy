using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class CompanyPublicPageMapPlaywrightTests : IAsyncLifetime
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
    public async Task Company_public_page_map_boots_without_js_invokable_errors(
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
        var pageErrors = new List<string>();
        var page = await context.NewPageAsync();
        page.PageError += (_, err) =>
        {
            if (err.Contains("OnMapTilesReady", StringComparison.OrdinalIgnoreCase)
                || err.Contains("JSInvokable", StringComparison.OrdinalIgnoreCase))
            {
                pageErrors.Add(err);
            }
        };

        await page.GotoAsync(
            _baseUrl + "/__test/sign-in?role=" + role + "&return=" + Uri.EscapeDataString("/"),
            new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.GotoAsync(
            _baseUrl + "/71001001/0001",
            new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });

        await page.Locator("#company-title").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 120_000 });

        await page.WaitForFunctionAsync(
            """
            () => {
              const map = window.jobMap && window.jobMap.__testGetMap && window.jobMap.__testGetMap();
              return !!map && map.isStyleLoaded && map.isStyleLoaded();
            }
            """,
            null,
            new() { Timeout = 90_000 });

        Assert.Empty(pageErrors);
    }
}
