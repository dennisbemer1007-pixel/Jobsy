using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class BedrijvenkaartSsrMobileHeaderPlaywrightTests : IAsyncLifetime
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
    public async Task Mobile_header_actions_stay_in_viewport(string role, int width, int height, bool touch)
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

        var header = page.Locator(".app-header").First;
        await header.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });

        var headerWidth = await header.EvaluateAsync<double>("el => el.getBoundingClientRect().width");
        Assert.True(headerWidth <= width + 2, $"Header width {headerWidth}px should not exceed viewport {width}px.");

        if (width <= 900)
        {
            var tagline = page.Locator(".app-shell--banenkaart-shell .brand-tagline");
            if (await tagline.CountAsync() > 0)
            {
                Assert.False(await tagline.IsVisibleAsync(), "Brand tagline should be hidden on narrow bedrijvenkaart SSR.");
            }
        }

        var actions = page.Locator(".app-header__actions").First;
        if (await actions.CountAsync() == 0)
        {
            return;
        }

        var box = await actions.BoundingBoxAsync();
        Assert.NotNull(box);
        Assert.True(box!.X >= -2, $"Header actions should not sit off-screen left (x={box.X}).");
        Assert.True(box.X + box.Width <= width + 2, $"Header actions should fit viewport (x={box.X}, w={box.Width}, vw={width}).");
    }
}
