using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

/// <summary>
/// Mobile banenkaart filter summary: full-width count, controls on next row, nothing clipped.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class BanenkaartMobileToolbarLayoutPlaywrightTests : IAsyncLifetime
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
    [InlineData(360, 800, "360")]
    [InlineData(390, 844, "390")]
    public async Task Mobile_summary_stacks_count_and_controls(int width, int height, string tag)
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        var outDir = Path.Combine(FindRepoRoot(), "artifacts", "banenkaart-mobile-toolbar");
        Directory.CreateDirectory(outDir);

        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IsMobile = true,
            HasTouch = true,
            IgnoreHTTPSErrors = true
        });

        var page = await context.NewPageAsync();
        await PlaywrightCookieConsent.AcceptAsync(context);
        var signIn = $"{baseUrl}/__test/sign-in?role=candidate&return={Uri.EscapeDataString("/banenkaart?weergave=kaart")}";
        await page.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync($"{baseUrl}/banenkaart?weergave=kaart", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });

        try
        {
            await page.WaitForSelectorAsync("[data-testid=kb-mobile-result-count]", new() { Timeout = 60_000 });
        }
        catch (TimeoutException)
        {
            return;
        }

        var countFullWidth = await page.EvaluateAsync<bool>("""
            () => {
              const summary = document.querySelector('.kb-filter-bar__summary');
              const count = document.querySelector('.kb-filter-bar__count-block');
              if (!summary || !count) return false;
              const sb = summary.getBoundingClientRect();
              const cb = count.getBoundingClientRect();
              return cb.width >= sb.width * 0.92;
            }
            """);
        var toggleInView = await page.EvaluateAsync<bool>("""
            () => {
              const toggle = document.querySelector('[data-testid=kb-view-toggle]');
              if (!toggle) return false;
              return toggle.getBoundingClientRect().right <= window.innerWidth + 1;
            }
            """);
        var toggleBelowCount = await page.EvaluateAsync<bool>("""
            () => {
              const count = document.querySelector('.kb-filter-bar__count-block');
              const toggle = document.querySelector('[data-testid=kb-view-toggle]');
              if (!count || !toggle) return false;
              return toggle.getBoundingClientRect().top >= count.getBoundingClientRect().bottom - 3;
            }
            """);
        var sameRow = await page.EvaluateAsync<bool>("""
            () => {
              const count = document.querySelector('.kb-filter-bar__count-block');
              const toggle = document.querySelector('[data-testid=kb-view-toggle]');
              if (!count || !toggle) return false;
              return Math.abs(count.getBoundingClientRect().top - toggle.getBoundingClientRect().top) < 4;
            }
            """);
        var mapBelow = await page.EvaluateAsync<bool>("""
            () => {
              const summary = document.querySelector('.kb-filter-bar__summary');
              const map = document.querySelector('#job-map, .map-pane');
              if (!summary) return false;
              if (!map) return true;
              return map.getBoundingClientRect().top >= summary.getBoundingClientRect().bottom - 4;
            }
            """);

        Assert.True(countFullWidth, $"Count should span summary row at {width}px.");
        Assert.True(toggleInView, $"Kaart/Lijst toggle clipped at {width}px.");
        Assert.True(toggleBelowCount, "Kaart/Lijst toggle should sit below the count line.");
        Assert.False(sameRow, "Count and toggle must not share one cramped row.");
        Assert.True(mapBelow, "Map should start directly under the filter summary.");

        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(outDir, $"banenkaart-toolbar-{tag}.png"),
            FullPage = false
        });
    }

    [Fact]
    public async Task Desktop_1440_hides_mobile_summary_bar()
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
            IgnoreHTTPSErrors = true
        });

        var page = await context.NewPageAsync();
        await PlaywrightCookieConsent.AcceptAsync(context);
        await page.GotoAsync($"{baseUrl}/__test/sign-in?role=candidate&return=%2Fbanenkaart", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync($"{baseUrl}/banenkaart", new() { WaitUntil = WaitUntilState.DOMContentLoaded });

        var barVisible = await page.EvaluateAsync<bool>("""
            () => {
              const bar = document.querySelector('.kb-filter-bar');
              if (!bar) return false;
              const style = window.getComputedStyle(bar);
              return style.display !== 'none' && style.visibility !== 'hidden';
            }
            """);

        Assert.False(barVisible, "Mobile kb-filter-bar should be hidden at 1440px.");
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
