using System.Diagnostics;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Release publish + stub API wiring: /all must be 200, SSR list must show vacancies, CSP-safe boot, banenkaart CSS.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class BanenkaartAccCatalogSmokePlaywrightTests : IAsyncLifetime
{
    private const string PublishDir = "/tmp/pub-banenkaart-acc-catalog-smoke";

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

    [Theory]
    [InlineData(390, 844, true)]
    [InlineData(1280, 720, false)]
    public async Task Banenkaart_has_catalog_styles_and_ssr_list(int width, int height, bool touch)
    {
        var request = await _playwright!.APIRequest.NewContextAsync(new() { BaseURL = _baseUrl });
        var all = await request.GetAsync("/api/banenkaart-client/all");
        Assert.True(all.Ok, $"GET /api/banenkaart-client/all status {all.Status}");
        var catalogBytes = (await all.BodyAsync())?.Length ?? 0;
        Assert.True(catalogBytes > 32, "Catalog body should not be empty.");

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
            if (msg.Type == "error")
            {
                consoleErrors.Add(msg.Text);
            }
        };

        await page.GotoAsync(_baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);

        var hasBanenkaartCss = await page.EvaluateAsync<bool>("""
            () => Array.from(document.querySelectorAll('link[rel=stylesheet]'))
              .some(l => (l.href || '').indexOf('banenkaart-page.css') !== -1)
            """);
        Assert.True(hasBanenkaartCss, "Expected banenkaart-page.css in document.");

        var bootNode = await page.EvaluateAsync<string>("""
            () => {
              const n = document.getElementById('jobsy-map-boot');
              return n && n.tagName === 'DIV' && (n.textContent || '').indexOf('catalogMode') !== -1 ? 'ok' : 'bad';
            }
            """);
        Assert.Equal("ok", bootNode);

        var listCount = await page.Locator("[data-testid=kb-list-row]").CountAsync();

        Assert.True(listCount > 0, "SSR vacancy list should render rows when stub discover has vacancies.");

        await page.WaitForFunctionAsync(
            "() => !!(window.jobMap && document.querySelector('#job-map canvas'))",
            null,
            new() { Timeout = 90_000 });

        Assert.True(consoleErrors.Count == 0, "Console errors: " + string.Join("; ", consoleErrors));
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
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = root,
            ArgumentList =
            {
                "publish", "Jobsy.Web/Jobsy.Web.csproj",
                "-c", "Release",
                "-o", PublishDir,
                "/p:UseAppHost=false",
            },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var pub = Process.Start(psi)!;
        var stderr = await pub.StandardError.ReadToEndAsync();
        await pub.WaitForExitAsync();
        Assert.True(pub.ExitCode == 0, "publish failed: " + stderr);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found.");
    }
}
