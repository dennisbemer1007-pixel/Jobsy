using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Instellingen calm links must open profile reset / delete dialogs (not a silent same-tab reload).
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class CandidateSettingsDangerPlaywrightTests : IAsyncLifetime
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
    [InlineData(1280, 900)]
    [InlineData(390, 844)]
    public async Task Settings_calm_links_open_reset_and_delete_dialogs(int width, int height)
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IsMobile = width < 500,
            HasTouch = width < 500,
            IgnoreHTTPSErrors = true,
        });

        var page = await context.NewPageAsync();
        await PlaywrightCookieConsent.AcceptAsync(context);
        var signIn = $"{baseUrl}/__test/sign-in?role=candidate&return={Uri.EscapeDataString("/candidate/settings?pane=mail")}";
        await page.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync($"{baseUrl}/candidate/settings?pane=mail", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });

        await page.GetByRole(AriaRole.Button, new() { Name = "Begin opnieuw" }).ClickAsync();
        await page.WaitForURLAsync(u => u.Contains("/candidate/paspoort", StringComparison.OrdinalIgnoreCase), new() { Timeout = 30_000 });
        await page.Locator(".profile-reset-modal").WaitForAsync(new() { Timeout = 15_000 });
        await page.Locator(".profile-reset-modal button").Filter(new() { HasText = "Annuleren" }).First.ClickAsync();
        await page.Locator(".profile-reset-modal").WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 10_000 });

        await page.GotoAsync($"{baseUrl}/candidate/settings?pane=mail", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GetByRole(AriaRole.Button, new() { Name = "Account verwijderen" }).ClickAsync();
        await page.WaitForURLAsync(u => u.Contains("/candidate/paspoort", StringComparison.OrdinalIgnoreCase), new() { Timeout = 30_000 });
        await page.Locator(".unsubscribe-modal").WaitForAsync(new() { Timeout = 15_000 });
    }

    [Theory]
    [InlineData(1280, 900)]
    [InlineData(390, 844)]
    public async Task Paspoort_danger_query_opens_dialog(int width, int height)
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IsMobile = width < 500,
            HasTouch = width < 500,
            IgnoreHTTPSErrors = true,
        });

        var page = await context.NewPageAsync();
        await PlaywrightCookieConsent.AcceptAsync(context);
        var signIn = $"{baseUrl}/__test/sign-in?role=candidate&return={Uri.EscapeDataString("/candidate/paspoort?tab=data&danger=reset")}";
        await page.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync($"{baseUrl}/candidate/paspoort?tab=data&danger=reset", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.Locator(".profile-reset-modal").WaitForAsync(new() { Timeout = 30_000 });
        await page.WaitForURLAsync(
            u => u.Contains("/candidate/paspoort", StringComparison.OrdinalIgnoreCase)
                 && !u.Contains("danger=", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 15_000 });
    }
}
