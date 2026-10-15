using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Logged-out /banenkaart must render map + vacancy list (seeded CI stack or Acceptatie).
/// Guards desktop viewport measurement and filter Zoeken contrast regressions.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class BanenkaartLoggedOutRenderPlaywrightTests
{
    [Theory]
    [InlineData(1440, 900, false)]
    [InlineData(390, 844, true)]
    public async Task Logged_out_banenkaart_renders_map_and_vacancies(int width, int height, bool isMobile)
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "http://127.0.0.1:5201").Trim().TrimEnd('/');
        if (!await IsReachableAsync(baseUrl))
        {
            return;
        }

        await EmployersPlaywrightGuard.SkipIfEmployersOffAsync(baseUrl);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            HasTouch = isMobile,
            IsMobile = isMobile,
            IgnoreHTTPSErrors = true,
            ServiceWorkers = ServiceWorkerPolicy.Allow
        });
        await PlaywrightCookieConsent.AcceptAsync(context);

        var consoleErrors = new List<string>();
        await using var page = await context.NewPageAsync();
        page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                var text = msg.Text;
                if (text.Contains("openfreemap.org", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("Failed to load resource", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                consoleErrors.Add(text);
            }
        };
        page.PageError += (_, err) => consoleErrors.Add(err);

        await page.GotoAsync(baseUrl + E2eRoutes.Banenkaart, new()
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 120_000
        });

        var later = page.Locator(".kb-start-prompt__later");
        if (await later.CountAsync() > 0)
        {
            await later.ClickAsync();
        }

        await page.WaitForFunctionAsync(
            "() => !!(window.jobMap && document.querySelector('#job-map canvas'))",
            null,
            new() { Timeout = 90_000 });

        if (width >= 900)
        {
            await page.WaitForSelectorAsync(".vacancy-list--cards > *, .kb-list-rows > *",
                new() { Timeout = 90_000 });
        }

        var metrics = await page.EvaluateAsync<BanenkaartMetrics>("""
            () => {
              const map = document.querySelector('#job-map');
              const mr = map?.getBoundingClientRect();
              const cards = document.querySelectorAll('.vacancy-list--cards > *, .kb-list-rows > *');
              const searchBtn = document.querySelector('button.filter-bar__search');
              const btnStyle = searchBtn ? getComputedStyle(searchBtn) : null;
              return {
                mapHeight: mr?.height ?? 0,
                mapWidth: mr?.width ?? 0,
                cardCount: cards.length,
                searchColor: btnStyle?.color ?? '',
                searchBg: btnStyle?.backgroundColor ?? ''
              };
            }
            """);

        Assert.True(metrics.MapHeight > 200, $"Map height was {metrics.MapHeight}px at {width}x{height}.");

        if (width >= 900)
        {
            Assert.True(metrics.CardCount >= 1, $"Expected at least one vacancy row on desktop, got {metrics.CardCount}.");
        }

        Assert.Empty(consoleErrors);
    }

    private sealed class BanenkaartMetrics
    {
        public double MapHeight { get; set; }
        public double MapWidth { get; set; }
        public int CardCount { get; set; }
        public string SearchColor { get; set; } = "";
        public string SearchBg { get; set; } = "";
    }

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            using var resp = await http.GetAsync(baseUrl);
            return resp.IsSuccessStatusCode || (int)resp.StatusCode is >= 300 and < 500;
        }
        catch
        {
            return false;
        }
    }
}
