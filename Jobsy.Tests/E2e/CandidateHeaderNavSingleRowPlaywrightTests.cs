using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
public sealed class CandidateHeaderNavSingleRowPlaywrightTests : IAsyncLifetime
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

    [Theory]
    [InlineData("/candidate/ontdekkingsreis")]
    [InlineData("/carriere")]
    public async Task Exactly_one_header_nav_visible_before_and_after_hydration(string path)
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 900 },
            IgnoreHTTPSErrors = true,
        });

        var page = await context.NewPageAsync();
        await page.GotoAsync(
            $"{baseUrl}/__test/sign-in?role=candidate&return={Uri.EscapeDataString(path)}");
        await page.GotoAsync($"{baseUrl}{path}", new() { WaitUntil = WaitUntilState.Load });

        var early = await CountVisibleHeaderNavsAsync(page);
        Assert.Equal(1, early);

        await page.Locator("[data-testid=candidate-header-nav] .candidate-header-nav__link").First
            .WaitForAsync(new() { Timeout = 30_000 });
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 60_000 });

        var late = await CountVisibleHeaderNavsAsync(page);
        Assert.Equal(1, late);

        var activeHref = await page.EvaluateAsync<string>(
            """
            () => {
              const active = document.querySelector('[data-testid=candidate-header-nav] .candidate-header-nav__link.is-active');
              return active ? active.getAttribute('href') || '' : '';
            }
            """);
        var normalizedActive = activeHref.Split('?', '#')[0].TrimEnd('/');
        var normalizedPath = path.TrimEnd('/');
        Assert.True(
            normalizedActive.EndsWith(normalizedPath, StringComparison.OrdinalIgnoreCase),
            $"Expected active nav for {normalizedPath}, got {normalizedActive}");
    }

    private static async Task<int> CountVisibleHeaderNavsAsync(IPage page)
        => await page.EvaluateAsync<int>(
            """
            () => Array.from(document.querySelectorAll('.app-header .candidate-header-nav'))
              .filter(n => {
                const style = getComputedStyle(n);
                const r = n.getBoundingClientRect();
                return style.display !== 'none' && r.height > 0 && r.width > 0;
              }).length
            """);
}
