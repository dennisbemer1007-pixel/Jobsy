using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class BedrijvenkaartSsrChromeParityPlaywrightTests : IAsyncLifetime
{
    private const string PublishDir = "/tmp/pub-bedrijvenkaart-ssr-chrome";

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
    public async Task Logged_in_candidate_ssr_loads_banenkaart_chrome_css_and_map_near_top()
    {
        await using var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new() { Width = 1280, Height = 720 },
            IgnoreHTTPSErrors = true,
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();

        await page.GotoAsync(_baseUrl + "/__test/sign-in?role=candidate&return=/bedrijvenkaart/ssr", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);

        var authChromeCss = await page.EvaluateAsync<bool>(
            """
            () => {
              const links = Array.from(document.querySelectorAll('link[rel=stylesheet]'));
              const hasAuth = links.some(l => (l.href || '').indexOf('banenkaart-auth-chrome.css') !== -1);
              const hasPage = links.some(l => (l.href || '').indexOf('banenkaart-page.css') !== -1);
              const hasAppMin = links.some(l => (l.href || '').indexOf('app.min.css') !== -1);
              return hasAuth && hasPage && !hasAppMin;
            }
            """);
        Assert.True(authChromeCss, "Bedrijvenkaart SSR should mirror banenkaart lean auth chrome (no app.min.css).");

        var shellClass = await page.Locator(".app-shell--banenkaart-shell").CountAsync();
        Assert.True(shellClass > 0, "Expected app-shell--banenkaart-shell on bedrijvenkaart/ssr.");

        var panelHidden = await page.EvaluateAsync<bool>(
            """
            () => {
              const panel = document.querySelector('.notification-bell__panel');
              if (!panel) return true;
              const s = getComputedStyle(panel);
              return s.display === 'none' || panel.getClientRects().length === 0;
            }
            """);
        Assert.True(panelHidden, "Notification panel should not flow as unstyled document content.");

        var mapTop = await page.Locator("#bedrijvenkaart-map").EvaluateAsync<double>(
            "el => el.getBoundingClientRect().top");
        Assert.True(mapTop < 900, $"Map should sit below header, not at y~1900 (top={mapTop}px).");
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
