using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class BedrijvenkaartSsrCompanyPinPopupPlaywrightTests : IAsyncLifetime
{
    private const string BakkerijCompanyId = "11111111-1111-1111-1111-111111111101";

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
    public async Task Single_company_pin_popup_shows_name_and_company_link(
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
        var cardRequests = new List<string>();
        page.Request += (_, req) =>
        {
            if (req.Url.Contains("/api/vacancies/", StringComparison.Ordinal)
                && req.Url.Contains("/card", StringComparison.Ordinal))
            {
                cardRequests.Add(req.Url);
            }
        };

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

        var opened = await page.EvaluateAsync<bool>(
            """
            (id) => {
              if (!window.jobMap || !window.jobMap.__testOpenVacancyById) return false;
              return window.jobMap.__testOpenVacancyById(id);
            }
            """,
            BakkerijCompanyId);
        Assert.True(opened);

        var companyLink = page.Locator("a.map-popup__view[href=\"/12345678\"], a.map-popup__company[href=\"/12345678\"], a.map-popup__title[href=\"/12345678\"]");
        await Assertions.Expect(companyLink.First).ToBeVisibleAsync(new() { Timeout = 30_000 });

        var popupText = await page.Locator("[data-cluster-card], .map-popup").First.InnerTextAsync();
        Assert.Contains("Bakkerij De Korenaar", popupText, StringComparison.Ordinal);
        Assert.DoesNotContain("Vacature niet beschikbaar", popupText, StringComparison.Ordinal);
        Assert.DoesNotContain("Opnieuw proberen", popupText, StringComparison.Ordinal);

        Assert.Empty(cardRequests);
    }
}
