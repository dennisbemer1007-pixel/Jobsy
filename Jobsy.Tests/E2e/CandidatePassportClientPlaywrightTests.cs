using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
public sealed class CandidatePassportClientPlaywrightTests : IAsyncLifetime
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
    public async Task Motivation_section_types_client_side_one_post_save_persists(int width, int height)
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(PlaywrightViewportContexts.ForSize(width, height));
        var page = await context.NewPageAsync();
        await PlaywrightCookieConsent.AcceptAsync(context);

        var profilePuts = 0;
        var bffPosts = 0;
        await page.RouteAsync("**/api/me/profile", route =>
        {
            if (route.Request.Method.Equals("PUT", StringComparison.OrdinalIgnoreCase))
            {
                profilePuts++;
            }

            return route.ContinueAsync();
        });
        await page.RouteAsync("**/candidate/paspoort/sections/motivation", route =>
        {
            if (route.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                bffPosts++;
            }

            return route.ContinueAsync();
        });

        var returnUrl = Uri.EscapeDataString("/candidate/paspoort?tab=data&section=motivation");
        await page.GotoAsync($"{baseUrl}/__test/sign-in?role=candidate&return={returnUrl}", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync($"{baseUrl}/candidate/paspoort?tab=data&section=motivation", new()
        {
            WaitUntil = WaitUntilState.Load,
            Timeout = 90_000,
        });

        var motivation = page.Locator("[data-testid='passport-default-motivation']").First;
        await motivation.WaitForAsync(new() { Timeout = 20_000 });

        const string marker = "PW-PASSPORT-MOTIV-7c21";
        await motivation.ClickAsync(new() { Force = width < 500 });
        for (var i = 0; i < marker.Length; i++)
        {
            await motivation.PressSequentiallyAsync(marker[i].ToString(), new() { Delay = 25 });
        }

        Assert.Equal(0, profilePuts);
        Assert.Equal(0, bffPosts);

        await page.Locator("[data-testid='passport-motivation-form'] button[type='submit']").ClickAsync();
        await page.WaitForURLAsync(
            u => u.Contains("saved=1", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 30_000 });

        Assert.Equal(0, profilePuts);
        Assert.Equal(1, bffPosts);

        await page.ReloadAsync(new() { WaitUntil = WaitUntilState.Load });
        await motivation.WaitForAsync(new() { Timeout = 15_000 });
        var value = await motivation.InputValueAsync();
        Assert.Contains(marker, value, StringComparison.Ordinal);
    }
}
