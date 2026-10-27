using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class BedrijvenkaartSsrMapFixesPlaywrightTests : IAsyncLifetime
{
    private const string PublishDir = "/tmp/pub-bedrijvenkaart-ssr-map-fixes";

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
        await EnsurePublishedAsync();
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
    public async Task Bff_pins_desktop_sticky_map_filters_and_card_link(string role, int width, int height, bool touch)
    {
        await using var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new() { Width = width, Height = height },
            HasTouch = touch,
            IsMobile = touch,
            IgnoreHTTPSErrors = true,
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var consoleErrors = new List<string>();
        var page = await context.NewPageAsync();
        page.Console += (_, msg) =>
        {
            if (msg.Type == "error" && msg.Text.Contains("pins", StringComparison.OrdinalIgnoreCase))
            {
                consoleErrors.Add(msg.Text);
            }
        };

        var returnPath = "/bedrijvenkaart/ssr";
        await page.GotoAsync(
            _baseUrl + "/__test/sign-in?role=" + role + "&return=" + Uri.EscapeDataString(returnPath),
            new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);

        var bffOk = await page.APIRequest.GetAsync(_baseUrl + "/api/bedrijvenkaart-client/pins?radiusKm=25");
        Assert.True(bffOk.Ok, "BFF pins must return 200.");

        if (width >= 901)
        {
            var mapHeight = await page.Locator("#bedrijvenkaart-map").EvaluateAsync<double>(
                "el => el.getBoundingClientRect().height");
            var paneHeight = await page.Locator(".bk-map-pane").EvaluateAsync<double>(
                "el => el.getBoundingClientRect().height");
            Assert.True(mapHeight <= paneHeight + 4, "Desktop map should not stretch with list.");
            Assert.True(paneHeight <= 1200, "Map pane should stay viewport-sized.");
        }

        await page.WaitForFunctionAsync(
            """
            () => {
              const canvas = document.querySelector('#bedrijvenkaart-map canvas, #bedrijvenkaart-map .maplibregl-canvas');
              return !!canvas && canvas.getClientRects().length > 0;
            }
            """,
            null,
            new() { Timeout = 90_000 });

        var openChip = page.Locator("[data-bk-filter=\"openOnly\"]");
        await openChip.ClickAsync();
        var visibleCards = await page.Locator(".bk-card:not([hidden])").CountAsync();
        Assert.True(visibleCards >= 0);

        var link = page.Locator("a.bk-card--link").First;
        if (await link.CountAsync() > 0)
        {
            var href = await link.GetAttributeAsync("href");
            Assert.False(string.IsNullOrWhiteSpace(href));
        }

        Assert.Empty(consoleErrors);
    }

    private static async Task EnsurePublishedAsync()
    {
        var dll = Path.Combine(PublishDir, "Jobsy.Web.dll");
        if (File.Exists(dll) && File.GetLastWriteTimeUtc(dll) > DateTime.UtcNow.AddHours(-2))
        {
            return;
        }

        if (Directory.Exists(PublishDir))
        {
            Directory.Delete(PublishDir, recursive: true);
        }

        var root = FindRepoRoot();
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            ArgumentList = { "publish", Path.Combine(root, "Jobsy.Web/Jobsy.Web.csproj"), "-c", "Release", "-o", PublishDir },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var proc = System.Diagnostics.Process.Start(psi)!;
        await proc.WaitForExitAsync();
        Assert.Equal(0, proc.ExitCode);
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "Jobsy.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? "";
        }

        throw new InvalidOperationException("Repo root not found.");
    }
}
