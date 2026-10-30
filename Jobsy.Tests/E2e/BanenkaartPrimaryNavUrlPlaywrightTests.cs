using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
public sealed class BanenkaartPrimaryNavUrlPlaywrightTests : IAsyncLifetime
{
    private static readonly (string Href, string Probe, string TitleFragment)[] Targets =
    [
        ("/candidate/ontdekkingsreis", ".journey-overview", "Ontdekkingsreis"),
        ("/candidate/paspoort", ".passport-page", "Paspoort"),
        ("/candidate/applications", ".apps-page, .kb-apps", "Sollicitaties"),
        ("/carriere", ".career-page, .career-card", "Carri"),
    ];

    private IllustratedPagesWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        Environment.SetEnvironmentVariable("JOBSY_UWV_FEATURES", "1");
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

    [Fact]
    public async Task From_banenkaart_desktop_header_nav_updates_url_title_and_content()
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 900 },
            IgnoreHTTPSErrors = true,
        });

        var page = await context.NewPageAsync();
        await PlaywrightCookieConsent.AcceptAsync(context);
        await page.GotoAsync($"{baseUrl}/__test/sign-in?role=candidate&return=/banenkaart");
        await page.GotoAsync($"{baseUrl}/banenkaart", new() { WaitUntil = WaitUntilState.Load, Timeout = 90_000 });
        await page.Locator("[data-banenkaart-ssr=\"1\"]").WaitForAsync(new() { Timeout = 60_000 });
        await page.WaitForTimeoutAsync(1500);

        foreach (var (href, probe, titleFragment) in Targets)
        {
            await page.GotoAsync($"{baseUrl}/banenkaart", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.Locator(".jobsy-discovery, .bk-page").First.WaitForAsync(new() { Timeout = 60_000 });

            var link = page.Locator($".candidate-header-nav a[href='{href}']").First;
            await link.ClickAsync(new() { Timeout = 10_000 });

            await page.WaitForURLAsync(
                u => NormalizePath(u) == NormalizePath(href),
                new() { Timeout = 10_000 });

            var title = await page.TitleAsync();
            Assert.Contains(titleFragment, title, StringComparison.OrdinalIgnoreCase);

            await page.Locator(probe).First.WaitForAsync(new() { Timeout = 10_000 });
        }
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
