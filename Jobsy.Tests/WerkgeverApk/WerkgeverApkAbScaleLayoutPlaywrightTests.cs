using Jobsy.Tests.TestSupport;
using Jobsy.Web.Hosting;
using Microsoft.Playwright;

namespace Jobsy.Tests.WerkgeverApk;

/// <summary>
/// Mockup 13: five-dot bipolar scale stays clear of A/B cards at key viewports.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class WerkgeverApkAbScaleLayoutPlaywrightTests
    : IClassFixture<RoleFunctionalWebAppFactory>, IAsyncLifetime
{
    private readonly RoleFunctionalWebAppFactory _api;
    private WerkgeverApkScreenshotPlaywrightTests.WerkgeverApkWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public WerkgeverApkAbScaleLayoutPlaywrightTests(RoleFunctionalWebAppFactory api) => _api = api;

    public async ValueTask InitializeAsync()
    {
        Assert.Equal(0, Microsoft.Playwright.Program.Main(["install", "chromium"]));
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
        var webRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Jobsy.Web"));
        if (Directory.Exists(webRoot))
        {
            WerkgeverApkTemplateLocator.SetWebContentRoot(webRoot);
        }

        _web = new WerkgeverApkScreenshotPlaywrightTests.WerkgeverApkWebFactory(_api.Server.CreateHandler());
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
    [InlineData(1440, 900)]
    [InlineData(1024, 768)]
    [InlineData(390, 844)]
    public async Task Mockup_13_dots_fully_visible_not_overlapping_cards(int width, int height)
    {
        Assert.NotNull(_browser);
        Assert.NotNull(_web);
        var baseUrl = _web.ServerAddress.TrimEnd('/');

        await using var context = await _browser.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            Locale = "nl-NL",
            DeviceScaleFactor = width < 768 ? 2 : 1,
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl + "/werkgever-apk/voorbeeld/13", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await WerkgeverApkAbScaleLayoutAssert.AssertDotsClearOfCardsAsync(page, width, $"{width}px");

        var outDir = "/opt/cursor/artifacts/pakketten";
        Directory.CreateDirectory(outDir);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(outDir, $"13-ab-vraag-{width}.png"),
            FullPage = true,
        });
    }
}