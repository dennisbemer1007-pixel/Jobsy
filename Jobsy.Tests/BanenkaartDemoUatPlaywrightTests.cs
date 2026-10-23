using System.Diagnostics;
using Microsoft.Playwright;

namespace Jobsy.Tests;

/// <summary>
/// Demo UAT regressions (acc findings 1–17): filter drawer, catalog/map sync, list, keyword.
/// Runs against Release publish + IllustratedPagesWebFactory (same as polish suite).
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class BanenkaartDemoUatPlaywrightTests : IAsyncLifetime
{
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
        _web = new IllustratedPagesWebFactory();
        _baseUrl = _web.ServerAddress.TrimEnd('/');
        await EnsurePublishedAsync();
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
    [InlineData(1280, 720, false)]
    [InlineData(390, 844, true)]
    public async Task Uat01_travel_chip_and_slider_stay_in_sync(int width, int height, bool touch)
    {
        await using var ctx = await NewContextAsync(width, height, touch);
        var page = await OpenBanenkaartAsync(ctx);
        await OpenFilterSheetAsync(page, width);
        await page.Locator("[data-testid=kb-sheet-travel-30]").ClickAsync();
        var slider = page.Locator("[data-testid=kb-sheet-travel-slider]");
        Assert.Equal("30", await slider.InputValueAsync());
        Assert.Contains("is-selected", await page.Locator("[data-testid=kb-sheet-travel-30]").GetAttributeAsync("class") ?? "");
        await slider.FillAsync("45");
        await page.Locator("[data-testid=kb-sheet-travel-slider]").DispatchEventAsync("input");
        Assert.Contains("is-selected", await page.Locator("[data-testid=kb-sheet-travel-45]").GetAttributeAsync("class") ?? "");
    }

    [Theory]
    [InlineData(1280, 720, false)]
    [InlineData(390, 844, true)]
    public async Task Uat02_transport_change_keeps_travel_minutes(int width, int height, bool touch)
    {
        await using var ctx = await NewContextAsync(width, height, touch);
        var page = await OpenBanenkaartAsync(ctx);
        await OpenFilterSheetAsync(page, width);
        await page.Locator("[data-testid=kb-sheet-travel-30]").ClickAsync();
        await page.Locator("[data-testid=kb-sheet-transport-Auto]").ClickAsync();
        Assert.Equal("30", await page.Locator("[data-testid=kb-sheet-travel-slider]").InputValueAsync());
    }

    [Theory]
    [InlineData(1280, 720, false)]
    [InlineData(390, 844, true)]
    public async Task Uat03_filter_section_labels_match_content(int width, int height, bool touch)
    {
        await using var ctx = await NewContextAsync(width, height, touch);
        var page = await OpenBanenkaartAsync(ctx);
        await OpenFilterSheetAsync(page, width);
        var categoryHeading = await page.Locator("[data-filter-section=category] h3").InnerTextAsync();
        var branchHeading = await page.Locator("[data-filter-section=branch] h3").InnerTextAsync();
        Assert.Contains("Soort werk", categoryHeading, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Sector", branchHeading, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(1280, 720, false)]
    [InlineData(390, 844, true)]
    public async Task Uat17_keyword_search_keeps_first_character(int width, int height, bool touch)
    {
        await using var ctx = await NewContextAsync(width, height, touch);
        var page = await OpenBanenkaartAsync(ctx);
        var input = width >= 900
            ? page.Locator("#discovery-keyword-desktop")
            : page.Locator("#discovery-keyword-mobile");
        await input.FillAsync("Den Haag");
        var q = await input.InputValueAsync();
        Assert.Equal("Den Haag", q);
        var synced = await page.EvaluateAsync<string>(
            "() => window.jobsyBanenkaartCatalog && window.jobsyBanenkaartCatalog.readKeywordFromUi"
            + " ? window.jobsyBanenkaartCatalog.readKeywordFromUi() : ''");
        Assert.Equal("Den Haag", synced);
    }

    [Theory]
    [InlineData(1280, 720, false)]
    [InlineData(390, 844, true)]
    public async Task Uat18_nearby_hero_is_full_width_strip_above_panes(int width, int height, bool touch)
    {
        await using var ctx = await NewContextAsync(width, height, touch);
        var page = await OpenBanenkaartAsync(ctx);
        var strip = page.Locator("[data-testid=kb-nearby-banner].kb-nearby-banner--strip");
        await strip.WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 30_000 });

        await strip.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15_000 });

        var stripAboveChrome = await page.EvaluateAsync<bool>(
            """
            () => {
              const root = document.querySelector('[data-banenkaart-ssr="1"]');
              const strip = root && root.querySelector(':scope > [data-testid=kb-nearby-banner].kb-nearby-banner--strip');
              const chrome = root && root.querySelector(':scope > .jobsy-chrome');
              if (!strip || !chrome) return false;
              return !!(strip.compareDocumentPosition(chrome) & Node.DOCUMENT_POSITION_FOLLOWING);
            }
            """);
        Assert.True(stripAboveChrome, "Illustration strip must sit above the filter/search chrome.");

        var inPane = await page.Locator(".vacancy-pane [data-testid=kb-nearby-banner]").CountAsync();
        Assert.Equal(0, inPane);
        var box = await strip.BoundingBoxAsync();
        var root = page.Locator("[data-testid=banenkaart-ssr-root]");
        var rootBox = await root.BoundingBoxAsync();
        var chromeBox = await page.Locator("[data-banenkaart-ssr=\"1\"] > .jobsy-chrome").BoundingBoxAsync();
        Assert.NotNull(box);
        Assert.NotNull(rootBox);
        Assert.NotNull(chromeBox);
        Assert.True(box!.Width >= rootBox!.Width * 0.92, "Hero strip should span the discovery width.");
        Assert.True(box.Y + box.Height <= chromeBox!.Y + 2,
            "Strip must sit above filter bar (strip bottom=" + (box.Y + box.Height) + ", chrome top=" + chromeBox.Y + ").");
        Assert.InRange(box.Height, touch ? 50 : 52, touch ? 70 : 80);
    }

    [Fact]
    public async Task Uat06_load_more_is_client_button_not_page_link()
    {
        await using var ctx = await NewContextAsync(1280, 720, false);
        var page = await OpenBanenkaartAsync(ctx);
        var btn = page.Locator("[data-testid=kb-load-more]");
        if (await btn.CountAsync() == 0)
        {
            return;
        }

        var tag = await btn.EvaluateAsync<string>("el => el.tagName");
        Assert.Equal("BUTTON", tag);
        var href = await btn.GetAttributeAsync("href");
        Assert.Null(href);
    }

    private async Task<IBrowserContext> NewContextAsync(int width, int height, bool touch)
    {
        var ctx = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new() { Width = width, Height = height },
            HasTouch = touch,
            IsMobile = touch,
            IgnoreHTTPSErrors = true,
        });
        await PlaywrightCookieConsent.AcceptAsync(ctx);
        return ctx;
    }

    private async Task<IPage> OpenBanenkaartAsync(IBrowserContext ctx)
    {
        var page = await ctx.NewPageAsync();
        await page.GotoAsync(_baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await DismissStartPromptAsync(page);
        await page.WaitForFunctionAsync(
            "() => !!window.jobsyBanenkaartCatalog && window.jobsyBanenkaartCatalog.isReady()",
            null,
            new() { Timeout = 90_000 });
        return page;
    }

    private static async Task DismissStartPromptAsync(IPage page)
    {
        var later = page.Locator("[data-testid=kb-start-later]");
        if (await later.CountAsync() > 0)
        {
            await later.First.ClickAsync(new() { Timeout = 5_000 });
        }
    }

    private static async Task OpenFilterSheetAsync(IPage page, int width)
    {
        var btn = width >= 900
            ? page.Locator("[data-testid=kb-panel-head] [data-testid=kb-filters-button]")
            : page.Locator(".kb-filter-bar [data-testid=kb-filters-button]");
        await btn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 90_000 });
        await btn.ClickAsync(new() { Timeout = 60_000 });
        await page.Locator("[data-testid=kb-filter-sheet]").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
    }

    private static async Task EnsurePublishedAsync()
    {
        const string publishDir = "/tmp/pub";
        var dll = Path.Combine(publishDir, "Jobsy.Web.dll");
        if (File.Exists(dll) && File.GetLastWriteTimeUtc(dll) > DateTime.UtcNow.AddHours(-2))
        {
            return;
        }

        if (Directory.Exists(publishDir))
        {
            Directory.Delete(publishDir, recursive: true);
        }

        var root = Directory.GetCurrentDirectory();
        while (!File.Exists(Path.Combine(root, "Jobsy.sln")) && Directory.GetParent(root) is not null)
        {
            root = Directory.GetParent(root)!.FullName;
        }

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = root,
            ArgumentList = { "publish", "Jobsy.Web/Jobsy.Web.csproj", "-c", "Release", "-o", publishDir, "/p:UseAppHost=false" },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var pub = Process.Start(psi)!;
        var stderr = await pub.StandardError.ReadToEndAsync();
        await pub.WaitForExitAsync();
        Assert.True(pub.ExitCode == 0, "publish failed: " + stderr);
    }
}
