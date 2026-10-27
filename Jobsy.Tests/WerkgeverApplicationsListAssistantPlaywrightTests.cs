using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class WerkgeverApplicationsListAssistantPlaywrightTests : IAsyncLifetime
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
        _web = new IllustratedPagesWebFactory();
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
    public async Task List_ssr_assistant_uses_kpi_greeting_and_pipeline_send_icon()
    {
        await using var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new() { Width = 1280, Height = 720 },
            IgnoreHTTPSErrors = true,
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();

        await page.GotoAsync(_baseUrl + "/__test/sign-in?role=employer&return=/werkgever/sollicitaties?view=lijst", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);

        await page.Locator("[data-ssr-coach-open]").ClickAsync();
        await page.WaitForFunctionAsync(
            """
            () => {
              const chat = document.querySelector('[data-ssr-assistant-chat]');
              if (!chat) return false;
              const text = chat.textContent || '';
              return text.indexOf('KPI') !== -1 || text.indexOf("KPI's") !== -1;
            }
            """,
            null,
            new() { Timeout = 30_000 });

        var sendHtml = await page.Locator(".lobsy-assistant__send").First.InnerHTMLAsync();
        Assert.Contains("→", sendHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("Verstuur", sendHtml, StringComparison.Ordinal);

        var readIcon = await page.Locator(".read-aloud__icon svg").CountAsync();
        Assert.True(readIcon > 0, "Expected SVG read-aloud icon, not emoji.");
    }
}
