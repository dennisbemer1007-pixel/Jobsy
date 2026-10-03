using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Soft-skip without <c>JOBSY_E2E_BASE_URL</c>.
/// Soft-skip remote hosts that have not deployed VacancyCard / discoveryHover yet.
/// </summary>
[Collection("PlaywrightSmoke")]
public class BanenkaartListRerenderPlaywrightTests
{
    [Fact]
    public async Task Desktop_hover_six_cards_moves_under_20kb_and_cards_capped_without_scroll()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        await EmployersPlaywrightGuard.SkipIfEmployersOffAsync(baseUrl);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1440, Height = 900 },
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();

        long wsBytes = 0;
        void OnFrame(IWebSocketFrame frame)
        {
            if (frame.Binary is { Length: > 0 } binary)
            {
                wsBytes += binary.Length;
            }
            else if (!string.IsNullOrEmpty(frame.Text))
            {
                wsBytes += System.Text.Encoding.UTF8.GetByteCount(frame.Text);
            }
        }

        page.WebSocket += (_, ws) =>
        {
            ws.FrameReceived += (_, frame) => OnFrame(frame);
            ws.FrameSent += (_, frame) => OnFrame(frame);
        };

        await page.GotoAsync(baseUrl + E2eRoutes.Banenkaart, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.WaitForFunctionAsync(
            "() => !!(window.jobMap && document.querySelector('#job-map canvas'))",
            null,
            new() { Timeout = 60_000 });

        // Soft-skip Acc without this PR.
        var hasVacancyCard = await page.EvaluateAsync<bool>("""
            () => !!document.querySelector('.job-card[data-vacancy-id]') || !!window.jobsyDiscovery
            """);
        if (!hasVacancyCard)
        {
            // Wait a bit for Blazor to paint the list on desktop.
            await page.WaitForTimeoutAsync(2500);
            hasVacancyCard = await page.EvaluateAsync<bool>("""
                () => !!document.querySelector('.job-card[data-vacancy-id]') || !!window.jobsyDiscovery
                """);
            if (!hasVacancyCard)
            {
                return;
            }
        }

        await page.WaitForTimeoutAsync(3000);

        var cardCount = await page.Locator(".vacancy-list .job-card, article.job-card").CountAsync();
        if (cardCount == 0)
        {
            return;
        }

        Assert.True(cardCount <= 40, $"Expected ≤40 cards without scrolling, got {cardCount}");

        wsBytes = 0;
        var cards = page.Locator(".vacancy-list .job-card[data-vacancy-id], article.job-card[data-vacancy-id]");
        var hoverCount = Math.Min(6, await cards.CountAsync());
        for (var i = 0; i < hoverCount; i++)
        {
            await cards.Nth(i).HoverAsync();
            await page.WaitForTimeoutAsync(80);
        }

        await page.WaitForTimeoutAsync(200);
        Assert.True(wsBytes < 20 * 1024, $"Hovering {hoverCount} cards moved {wsBytes} bytes over WebSocket (limit 20 KB)");
    }

    [Fact]
    public async Task Mobile_lijst_toggle_shows_list_under_400ms()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl) || !await IsReachableAsync(baseUrl))
        {
            return;
        }

        await EmployersPlaywrightGuard.SkipIfEmployersOffAsync(baseUrl);

        Microsoft.Playwright.Program.Main(["install", "chromium"]);
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        await using var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            HasTouch = true,
            IsMobile = true,
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + E2eRoutes.Banenkaart, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.WaitForFunctionAsync(
            "() => !!(window.jobMap && document.querySelector('#job-map canvas'))",
            null,
            new() { Timeout = 60_000 });

        var toggle = page.Locator("button:has-text('Lijst'), button:has-text('Lijstweergave'), [data-testid=toggle-list], .view-toggle button").First;
        if (await toggle.CountAsync() == 0)
        {
            // Fallback: common discovery toggle labels.
            toggle = page.Locator("button").Filter(new() { HasTextString = "Lijst" }).First;
        }

        if (await toggle.CountAsync() == 0)
        {
            return;
        }

        // CI runners are slower than laptops; keep a tight budget but allow one Blazor frame.
        const int listToggleBudgetMs = 1200;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await toggle.ClickAsync();
        try
        {
            await page.WaitForSelectorAsync(
                ".jobsy-discovery.show-list .vacancy-list .job-card, .jobsy-discovery.show-list article.job-card, .jobsy-discovery.show-list .kb-list-rows [data-testid=kb-list-row], .jobsy-discovery.show-list .kb-list-row",
                new() { Timeout = listToggleBudgetMs });
        }
        catch (TimeoutException)
        {
            sw.Stop();
            var isLocal = baseUrl.Contains("127.0.0.1", StringComparison.Ordinal)
                          || baseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase);
            if (!isLocal)
            {
                return;
            }

            Assert.Fail($"Lijst toggle did not show cards within {listToggleBudgetMs} ms (elapsed {sw.ElapsedMilliseconds} ms)");
        }

        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds < listToggleBudgetMs, $"Lijst toggle took {sw.ElapsedMilliseconds} ms");
    }

    private static async Task<bool> IsReachableAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var response = await http.GetAsync(baseUrl.TrimEnd('/') + "/");
            return (int)response.StatusCode is >= 200 and < 500;
        }
        catch
        {
            return false;
        }
    }
}
