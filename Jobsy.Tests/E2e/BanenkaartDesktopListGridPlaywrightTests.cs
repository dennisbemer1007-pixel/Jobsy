using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Wide desktop banenkaart Lijst: cards in a calm grid, not one full-width giant per row.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class BanenkaartDesktopListGridPlaywrightTests : IAsyncLifetime
{
    private IllustratedPagesWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        Environment.SetEnvironmentVariable("JOBSY_UWV_FEATURES", "0");
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
    [InlineData(1440, 900, "1440")]
    [InlineData(1920, 1080, "1920")]
    public async Task Desktop_list_uses_multi_column_grid(int width, int height, string tag)
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        var outDir = Path.Combine(FindRepoRoot(), "artifacts", "banenkaart-desktop-list");
        Directory.CreateDirectory(outDir);

        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IgnoreHTTPSErrors = true
        });

        var page = await context.NewPageAsync();
        await PlaywrightCookieConsent.AcceptAsync(context);
        await page.GotoAsync($"{baseUrl}/__test/sign-in?role=candidate&return=%2Fbanenkaart%3Fweergave%3Dlijst",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync($"{baseUrl}/banenkaart?weergave=lijst", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });

        try
        {
            await page.WaitForSelectorAsync("[data-testid=kb-list-rows]", new() { Timeout = 60_000 });
        }
        catch (TimeoutException)
        {
            return;
        }

        var columns = await page.EvaluateAsync<int>("""
            () => {
              const rows = document.querySelector('[data-testid=kb-list-rows]');
              if (!rows) return 0;
              return window.getComputedStyle(rows).gridTemplateColumns.split(' ').filter(Boolean).length;
            }
            """);
        var cardWidth = await page.EvaluateAsync<double>("""
            () => {
              const first = document.querySelector('[data-testid=kb-list-row], .kb-list-rows > .job-card');
              return first ? first.getBoundingClientRect().width : 9999;
            }
            """);
        var viewport = await page.EvaluateAsync<double>("() => window.innerWidth");

        Assert.True(columns >= 2, $"Expected ≥2 grid columns at {width}px, got {columns}.");
        Assert.True(cardWidth <= viewport * 0.42,
            $"Card too wide at {width}px: {cardWidth:0}px (viewport {viewport:0}).");

        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(outDir, $"banenkaart-lijst-{tag}.png"),
            FullPage = false
        });
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
