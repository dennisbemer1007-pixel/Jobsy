using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
public sealed class CandidateHeaderNavStableLayoutPlaywrightTests : IAsyncLifetime
{
    private IllustratedPagesWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
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
    public async Task Desktop_header_nav_link_positions_stable_through_hydration()
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 900 },
            IgnoreHTTPSErrors = true,
        });

        var page = await context.NewPageAsync();
        await page.GotoAsync($"{baseUrl}/__test/sign-in?role=candidate&return=/candidate/ontdekkingsreis");
        await page.GotoAsync($"{baseUrl}/candidate/ontdekkingsreis", new() { WaitUntil = WaitUntilState.Load });

        await page.Locator("[data-testid=candidate-header-nav] .candidate-header-nav__link").First
            .WaitForAsync(new() { Timeout = 30_000 });

        var early = await ReadLinkCentersAsync(page);
        Assert.True(early.Length >= 5, "Expected five desktop header nav links on first paint.");

        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 30_000 });

        var late = await ReadLinkCentersAsync(page);
        Assert.Equal(early.Length, late.Length);

        for (var i = 0; i < early.Length; i++)
        {
            Assert.True(Math.Abs(early[i] - late[i]) < 2.5, $"Nav link {i} shifted {Math.Abs(early[i] - late[i]):F1}px during hydration.");
        }
    }

    private static async Task<double[]> ReadLinkCentersAsync(IPage page)
        => await page.EvaluateAsync<double[]>(
            """
            () => Array.from(document.querySelectorAll('[data-testid=candidate-header-nav] .candidate-header-nav__link'))
              .map(a => a.getBoundingClientRect().left + a.getBoundingClientRect().width * 0.5)
            """);
}
