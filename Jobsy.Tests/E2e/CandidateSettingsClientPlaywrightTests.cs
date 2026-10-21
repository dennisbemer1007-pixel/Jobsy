using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
public sealed class CandidateSettingsClientPlaywrightTests : IAsyncLifetime
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
    [InlineData(PlaywrightViewportContexts.DesktopWidth, PlaywrightViewportContexts.DesktopHeight)]
    [InlineData(PlaywrightViewportContexts.MobileWidth, PlaywrightViewportContexts.MobileHeight)]
    public async Task Mail_toggles_stay_client_side_until_one_save_post(int width, int height)
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(PlaywrightViewportContexts.ForSize(width, height));
        var page = await context.NewPageAsync();
        await PlaywrightCookieConsent.AcceptAsync(context);

        var saves = 0;
        await page.RouteAsync("**/account/mail-instellingen", route =>
        {
            if (route.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                saves++;
            }

            return route.ContinueAsync();
        });

        var signIn = $"{baseUrl}/__test/sign-in?role=candidate&return={Uri.EscapeDataString("/candidate/settings?pane=mail")}";
        await page.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync($"{baseUrl}/candidate/settings?pane=mail", new() { WaitUntil = WaitUntilState.Load, Timeout = 90_000 });

        var reminder = page.Locator("input[name='reminder_emails']").First;
        await reminder.WaitForAsync(new() { Timeout = 15_000 });
        for (var i = 0; i < 7; i++)
        {
            await reminder.ClickAsync(new() { Force = width < 500 });
            await page.WaitForTimeoutAsync(40);
        }

        Assert.Equal(0, saves);

        await page.Locator("form.cand-mail-settings-form button[type='submit']").ClickAsync();
        await page.WaitForURLAsync(
            u => u.Contains("saved=1", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 30_000 });

        Assert.Equal(1, saves);
        await page.GetByText("Opgeslagen.").WaitForAsync(new() { Timeout = 10_000 });
    }
}
