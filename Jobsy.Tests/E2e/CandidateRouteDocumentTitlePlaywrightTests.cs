using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
public sealed class CandidateRouteDocumentTitlePlaywrightTests : IAsyncLifetime
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
    [InlineData("/candidate/paspoort", "Mijn paspoort", "My passport")]
    [InlineData("/banenkaart", "Banenkaart", "Job map")]
    [InlineData("/candidate/applications", "Mijn sollicitaties", "My applications")]
    public async Task Direct_load_and_header_nav_use_route_title(string path, string nlFragment, string enFragment)
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 900 },
            IgnoreHTTPSErrors = true,
        });

        var page = await context.NewPageAsync();
        await PlaywrightCookieConsent.AcceptAsync(context);
        await page.GotoAsync(
            $"{baseUrl}/__test/sign-in?role=candidate&return={Uri.EscapeDataString("/candidate/ontdekkingsreis")}",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded });

        await page.GotoAsync($"{baseUrl}{path}", new() { WaitUntil = WaitUntilState.Load, Timeout = 90_000 });
        await WaitForCircuitReadyAsync(page);
        await WaitForTitleFragmentAsync(page, nlFragment);
        var directTitle = await page.TitleAsync();
        Assert.Contains(nlFragment, directTitle, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Lobsy", directTitle, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("loopbaangids in je eigen taal", directTitle, StringComparison.OrdinalIgnoreCase);

        await page.GotoAsync($"{baseUrl}/candidate/ontdekkingsreis", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.Locator(".journey-overview").WaitForAsync(new() { Timeout = 60_000 });
        await WaitForCircuitReadyAsync(page);

        var link = page.Locator($".candidate-header-nav a[href='{path}']").First;
        await link.ClickAsync(new() { Timeout = 15_000 });
        await page.WaitForURLAsync(
            u => NormalizePath(u).Equals(NormalizePath(path), StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 15_000 });
        await WaitForCircuitReadyAsync(page);
        await WaitForTitleFragmentAsync(page, nlFragment);

        var navTitle = await page.TitleAsync();
        Assert.Contains(nlFragment, navTitle, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("loopbaangids in je eigen taal", navTitle, StringComparison.OrdinalIgnoreCase);

        await page.Context.AddCookiesAsync(
        [
            new()
            {
                Name = "Jobsy.Culture",
                Value = "en",
                Url = baseUrl + "/",
            },
        ]);
        await page.GotoAsync($"{baseUrl}{path}", new() { WaitUntil = WaitUntilState.Load, Timeout = 90_000 });
        await WaitForCircuitReadyAsync(page);
        await WaitForTitleFragmentAsync(page, enFragment);
        var enTitle = await page.TitleAsync();
        Assert.Contains(enFragment, enTitle, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task WaitForTitleFragmentAsync(IPage page, string fragment)
    {
        await page.WaitForFunctionAsync(
            """
            (fragment) => {
              const title = document.title || '';
              return title.toLowerCase().includes(String(fragment).toLowerCase())
                && !title.toLowerCase().includes('loopbaangids in je eigen taal');
            }
            """,
            fragment,
            new() { Timeout = 15_000 });
    }

    private static async Task WaitForCircuitReadyAsync(IPage page)
    {
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 60_000 });
        await page.WaitForTimeoutAsync(400);
    }

    private static string NormalizePath(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return uri.AbsolutePath.TrimEnd('/');
        }

        return url.TrimEnd('/');
    }
}
