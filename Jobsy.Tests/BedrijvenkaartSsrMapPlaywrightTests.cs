using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class BedrijvenkaartSsrMapPlaywrightTests : IAsyncLifetime
{
    private const string PublishDir = "/tmp/pub-bedrijvenkaart-ssr-map";

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

    [Fact]
    public async Task Ssr_map_boots_maplibre_and_radius_filter_fetches_without_reload()
    {
        await using var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new() { Width = 1280, Height = 720 },
            IgnoreHTTPSErrors = true,
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();

        await page.GotoAsync(_baseUrl + "/bedrijvenkaart/ssr", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);

        await page.WaitForFunctionAsync(
            """
            () => {
              const canvas = document.querySelector('#bedrijvenkaart-map canvas, #bedrijvenkaart-map .maplibregl-canvas');
              return !!canvas && canvas.getClientRects().length > 0;
            }
            """,
            null,
            new() { Timeout = 90_000 });

        var pinsReq = page.WaitForResponseAsync(r =>
            r.Url.Contains("/api/bedrijvenkaart-client/pins", StringComparison.OrdinalIgnoreCase)
            && r.Ok);
        await page.Locator("[data-bk-radius-km=\"10\"]").ClickAsync();
        await pinsReq;

        var stillSsr = await page.Locator("[data-bedrijvenkaart-ssr=\"1\"]").CountAsync();
        Assert.Equal(1, stillSsr);
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
