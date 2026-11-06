using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Preview SSR must work when <c>BedrijvenkaartSsrPreview</c> is on and CompanyMapInitiative is off (acceptatie).
/// </summary>
[Collection("BanenkaartPlaywright")]
public sealed class BedrijvenkaartSsrPreviewGatePlaywrightTests : IAsyncLifetime
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

    [Fact]
    public async Task Preview_ssr_returns_shell_when_company_map_initiative_off()
    {
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 390, Height = 844 },
            IgnoreHTTPSErrors = true
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        var response = await page.GotoAsync(
            _baseUrl + "/bedrijvenkaart/ssr",
            new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        Assert.NotNull(response);
        Assert.True(response!.Ok);
        await page.WaitForSelectorAsync("[data-bedrijvenkaart-ssr=\"1\"]", new() { Timeout = 60_000 });
        Assert.Contains("/bedrijvenkaart/ssr", page.Url, StringComparison.Ordinal);
        Assert.True(await page.Locator(".bk-shell").IsVisibleAsync());
        Assert.True(await page.Locator("#bedrijvenkaart-map").IsVisibleAsync());
    }
}
