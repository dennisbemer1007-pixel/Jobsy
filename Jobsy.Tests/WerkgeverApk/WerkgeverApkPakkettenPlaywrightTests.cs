using Jobsy.Tests.TestSupport;
using Jobsy.Web.Hosting;
using Microsoft.Playwright;

namespace Jobsy.Tests.WerkgeverApk;

[Collection("PlaywrightSmoke")]
public sealed class WerkgeverApkPakkettenPlaywrightTests
    : IClassFixture<RoleFunctionalWebAppFactory>, IAsyncLifetime
{
    private const string EnvRun = "JOBSY_WERKGEVER_APK_SCREENSHOTS";
    private static bool ShouldRun =>
        string.Equals(Environment.GetEnvironmentVariable(EnvRun), "1", StringComparison.Ordinal);

    private static string OutDir => "/opt/cursor/artifacts/pakketten";
    private static readonly (string Slug, string Path)[] Pages =
    [
        ("11-pakket-kiezen", "/werkgever-apk/voorbeeld/11"),
        ("12-werkgever-pakket", "/werkgever-apk/voorbeeld/12"),
        ("13-ab-vraag", "/werkgever-apk/voorbeeld/13"),
        ("13b-ab-pauze", "/werkgever-apk/voorbeeld/13b"),
    ];

    private readonly RoleFunctionalWebAppFactory _api;
    private WerkgeverApkScreenshotPlaywrightTests.WerkgeverApkWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public WerkgeverApkPakkettenPlaywrightTests(RoleFunctionalWebAppFactory api) => _api = api;

    public async ValueTask InitializeAsync()
    {
        if (!ShouldRun)
        {
            return;
        }

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
        Directory.CreateDirectory(OutDir);
        var dekking = Path.GetFullPath(Path.Combine(webRoot, "..", "docs", "werkgever-apk", "ab-dekking-CONCEPT.md"));
        if (File.Exists(dekking))
        {
            File.Copy(dekking, Path.Combine(OutDir, "dekking.md"), overwrite: true);
        }
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

    [Fact(Timeout = 600_000)]
    public async Task Mockups_11_12_13_13b_layout_and_header_when_env_set()
    {
        _ = TestContext.Current.CancellationToken;
        if (!ShouldRun || _browser is null || _web is null)
        {
            return;
        }

        var baseUrl = _web.ServerAddress.TrimEnd('/');
        foreach (var (slug, path) in Pages)
        {
            foreach (var (w, h) in new[] { (1440, 900), (390, 844) })
            {
                await using var context = await _browser.NewContextAsync(new()
                {
                    ViewportSize = new ViewportSize { Width = w, Height = h },
                    Locale = "nl-NL"
                });
                var page = await context.NewPageAsync();
                await page.GotoAsync(baseUrl + path, new() { WaitUntil = WaitUntilState.NetworkIdle });
                var topbar = page.Locator("header.topbar");
                await Assertions.Expect(topbar).ToBeVisibleAsync();
                var bg = await topbar.EvaluateAsync<string>(
                    "el => getComputedStyle(el).backgroundColor");
                Assert.Equal("rgb(10, 32, 68)", bg);

                if (slug == "13-ab-vraag")
                {
                    var cards = page.Locator(".ab-card");
                    var count = await cards.CountAsync();
                    Assert.Equal(2, count);
                    var boxA = await cards.Nth(0).BoundingBoxAsync();
                    var boxB = await cards.Nth(1).BoundingBoxAsync();
                    Assert.NotNull(boxA);
                    Assert.NotNull(boxB);
                    Assert.Equal(boxA!.Width, boxB!.Width, 1);
                    Assert.Equal(boxA.Height, boxB.Height, 1);
                }

                await page.ScreenshotAsync(new()
                {
                    Path = Path.Combine(OutDir, $"{slug}-{w}.png"),
                    FullPage = true
                });
            }
        }
    }
}