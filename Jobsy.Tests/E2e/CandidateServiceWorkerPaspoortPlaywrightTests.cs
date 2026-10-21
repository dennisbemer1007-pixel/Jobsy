using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Mobile paspoort reload must finish the document request (no SW navigation hang).
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class CandidateServiceWorkerPaspoortPlaywrightTests : IAsyncLifetime
{
    private IllustratedPagesWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        _ = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        _web = new IllustratedPagesWebFactory();
        _ = _web.ServerAddress;
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
    [InlineData(390, 844, 10)]
    public async Task Mobile_paspoort_reload_completes_document(int width, int height, int cycles)
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IsMobile = true,
            HasTouch = true,
            IgnoreHTTPSErrors = true,
            ServiceWorkers = ServiceWorkerPolicy.Allow,
        });

        var page = await context.NewPageAsync();
        await PlaywrightCookieConsent.AcceptAsync(context);
        await page.GotoAsync($"{baseUrl}/__test/sign-in?role=candidate&return={Uri.EscapeDataString("/candidate/paspoort")}");
        await page.GotoAsync($"{baseUrl}/candidate/paspoort", new() { WaitUntil = WaitUntilState.Load, Timeout = 90_000 });
        await page.Locator(".passport-page").WaitForAsync(new() { Timeout = 60_000 });

        for (var i = 0; i < cycles; i++)
        {
            var response = await page.RunAndWaitForResponseAsync(
                () => page.ReloadAsync(new() { WaitUntil = WaitUntilState.Load, Timeout = 90_000 }),
                resp => resp.Request.ResourceType == "document" && resp.Url.Contains("/candidate/paspoort", StringComparison.OrdinalIgnoreCase),
                new() { Timeout = 90_000 });
            Assert.True(response.Ok, $"Cycle {i + 1}: document response not OK ({response.Status}).");
            await page.Locator(".passport-page").WaitForAsync(new() { Timeout = 30_000 });
        }
    }
}
