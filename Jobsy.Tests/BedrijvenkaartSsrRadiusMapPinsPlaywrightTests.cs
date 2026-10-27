using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class BedrijvenkaartSsrRadiusMapPinsPlaywrightTests : IAsyncLifetime
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
    public async Task Radius_change_updates_map_pin_count_with_list(
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
            () => window.jobMap && window.jobMap.__testGetPinCount && window.jobMap.__testGetPinCount() > 0
            """,
            null,
            new() { Timeout = 120_000 });

        async Task<int> PinCountAsync() =>
            await page.EvaluateAsync<int>("() => window.jobMap.__testGetPinCount()");

        async Task<int> PinsFromApiAsync(int km)
        {
            var wait = page.WaitForResponseAsync(r =>
                r.Url.Contains("/api/bedrijvenkaart-client/pins", StringComparison.OrdinalIgnoreCase)
                && r.Url.Contains($"radiusKm={km}", StringComparison.OrdinalIgnoreCase)
                && r.Ok);
            await page.Locator($"button[data-bk-radius-km=\"{km}\"]").ClickAsync();
            var response = await wait;
            var json = await response.JsonAsync();
            return json?.ValueKind == System.Text.Json.JsonValueKind.Array
                ? json.Value.GetArrayLength()
                : 0;
        }

        var count40 = await PinsFromApiAsync(40);
        var map40 = await PinCountAsync();
        Assert.Equal(count40, map40);
        Assert.True(count40 >= 2);

        var count10 = await PinsFromApiAsync(10);
        var map10 = await PinCountAsync();
        Assert.Equal(count10, map10);
        Assert.True(count10 <= count40);
        if (count40 > count10)
        {
            Assert.True(map10 < map40, "Map pin count should follow the radius filter, not stay on the wider set.");
        }
    }
}
