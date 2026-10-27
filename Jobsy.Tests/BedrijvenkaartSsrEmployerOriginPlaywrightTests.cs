using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class BedrijvenkaartSsrEmployerOriginPlaywrightTests : IAsyncLifetime
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
    public async Task Employer_uses_branch_origin_and_sees_companies(
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

        var root = page.Locator("[data-testid=\"bedrijvenkaart-ssr-root\"]");
        await root.WaitForAsync(new() { Timeout = 60_000 });

        var originLat = await root.GetAttributeAsync("data-bk-origin-lat");
        Assert.NotNull(originLat);
        Assert.True(double.TryParse(originLat, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var lat));
        Assert.InRange(lat, 51.9, 52.1);

        var pinCount = await page.EvaluateAsync<int>(
            """
            () => window.jobMap && window.jobMap.__testGetPinCount ? window.jobMap.__testGetPinCount() : 0
            """);
        Assert.True(pinCount > 0, "Expected at least one company pin for this origin.");

        if (string.Equals(role, "employer", StringComparison.OrdinalIgnoreCase))
        {
            Assert.InRange(lat, 51.98, 52.01);
        }
    }
}
