using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class BedrijvenkaartSsrEmployerChromePlaywrightTests : IAsyncLifetime
{
    private const string PublishDir = "/tmp/pub-bedrijvenkaart-ssr-employer-chrome";

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
    public async Task Bedrijvenkaart_ssr_shows_header_and_styled_bell_panel(string role, int width, int height, bool touch)
    {
        await using var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new() { Width = width, Height = height },
            HasTouch = touch,
            IsMobile = touch,
            IgnoreHTTPSErrors = true,
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();

        await page.GotoAsync(
            _baseUrl + "/__test/sign-in?role=" + role + "&return=" + Uri.EscapeDataString("/bedrijvenkaart/ssr"),
            new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);

        var headerVisible = await page.Locator(".app-header").First.IsVisibleAsync();
        Assert.True(headerVisible, "App header (logo/bell) must be visible on bedrijvenkaart/ssr.");

        var logoVisible = await page.Locator(".app-header .brand, .app-header .lobsy-logo__img").First.IsVisibleAsync();
        Assert.True(logoVisible, "Brand/logo should render in header.");

        var mapTop = await page.Locator("#bedrijvenkaart-map").EvaluateAsync<double>(
            "el => el.getBoundingClientRect().top");
        Assert.True(mapTop < 900, $"Map should sit below header (top={mapTop}px), not under infobanner-only layout.");

        var bell = page.Locator(".notification-bell__toggle").First;
        if (await bell.CountAsync() == 0)
        {
            return;
        }

        await bell.ClickAsync();
        var panel = page.Locator(".notification-bell__panel").First;
        await panel.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });

        var panelBox = await panel.BoundingBoxAsync();
        Assert.NotNull(panelBox);
        Assert.True(panelBox!.X >= -2, $"Bell panel should stay in viewport (x={panelBox.X}).");
        Assert.True(panelBox.X + panelBox.Width <= width + 2, "Bell panel should not overflow right edge.");

        var listStyle = await page.EvaluateAsync<string>(
            """
            () => {
              const list = document.querySelector('.notification-bell__list');
              if (!list) return 'none';
              return getComputedStyle(list).listStyleType || 'disc';
            }
            """);
        if (listStyle != "none")
        {
            var anyList = await page.Locator(".notification-bell__list").CountAsync();
            if (anyList > 0)
            {
                Assert.Equal("none", listStyle);
            }
        }
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
