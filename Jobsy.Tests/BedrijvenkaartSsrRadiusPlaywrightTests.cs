using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class BedrijvenkaartSsrRadiusPlaywrightTests : IAsyncLifetime
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
    public async Task Radius_10km_requests_lat_lng_and_filters_list(string role, int width, int height, bool touch)
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
        var bffUrls = new List<string>();
        page.Request += (_, req) =>
        {
            if (req.Url.Contains("/api/bedrijvenkaart-client/pins", StringComparison.Ordinal))
            {
                bffUrls.Add(req.Url);
            }
        };

        await page.GotoAsync(
            _baseUrl + "/__test/sign-in?role=" + role + "&return=" + Uri.EscapeDataString("/bedrijvenkaart/ssr"),
            new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);

        var pinsResponse = page.WaitForResponseAsync(r =>
            r.Url.Contains("/api/bedrijvenkaart-client/pins", StringComparison.OrdinalIgnoreCase)
            && r.Url.Contains("radiusKm=10", StringComparison.OrdinalIgnoreCase)
            && r.Ok);
        await page.Locator("button[data-bk-radius-km=\"10\"]").ClickAsync();
        var response = await pinsResponse;

        Assert.NotEmpty(bffUrls);
        var last = bffUrls[^1];
        Assert.Contains("radiusKm=10", last, StringComparison.Ordinal);
        Assert.Contains("lat=", last, StringComparison.Ordinal);
        Assert.Contains("lng=", last, StringComparison.Ordinal);

        var pinsJson = await response.JsonAsync();
        var pinCount = pinsJson?.ValueKind == System.Text.Json.JsonValueKind.Array
            ? pinsJson.Value.GetArrayLength()
            : 0;
        Assert.True(pinCount >= 2, $"Expected at least 2 pins within 10 km, got {pinCount}.");
    }
}
