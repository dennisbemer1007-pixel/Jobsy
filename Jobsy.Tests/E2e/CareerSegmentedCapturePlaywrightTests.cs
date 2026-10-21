using Microsoft.Playwright;

namespace Jobsy.Tests.E2e;

[Collection("PlaywrightSmoke")]
public sealed class CareerSegmentedCapturePlaywrightTests : IAsyncLifetime
{
    private IllustratedPagesWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        Environment.SetEnvironmentVariable("JOBSY_UWV_FEATURES", "1");
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
    [InlineData(1280, 900, "voor-jou")]
    [InlineData(390, 844, "voor-jou")]
    [InlineData(1280, 900, "omscholen")]
    [InlineData(390, 844, "omscholen")]
    [InlineData(1280, 900, "kansberoepen")]
    [InlineData(390, 844, "kansberoepen")]
    public async Task Capture_carriere_segmented_tab(int width, int height, string tab)
    {
        var outDir = Path.Combine(FindRepoRoot(), "artifacts");
        Directory.CreateDirectory(outDir);
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = height },
            IsMobile = width < 640,
            HasTouch = width < 640,
            IgnoreHTTPSErrors = true,
        });
        var page = await OpenCarriereAsync(context, baseUrl);
        await ClickTabAsync(page, tab);
        var tag = $"{width}-{tab}";
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(outDir, $"carriere-segmented-product-{tag}-viewport.png"),
            FullPage = false,
        });
    }

    [Fact]
    public async Task Mobile_lead_not_overlapped_by_tabs()
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IsMobile = true,
            HasTouch = true,
            IgnoreHTTPSErrors = true,
        });
        var page = await OpenCarriereAsync(context, baseUrl);
        var ok = await page.EvaluateAsync<bool>("""
            () => {
              const lead = document.querySelector('header.uwv-carriere-showcase-hero .cand-hero__lead');
              const tabs = document.querySelector('.carriere-segmented__nav');
              if (!lead || !tabs) return false;
              const lb = lead.getBoundingClientRect();
              const tb = tabs.getBoundingClientRect();
              return tb.top >= lb.bottom - 2;
            }
            """);
        Assert.True(ok, "Hero lead must sit fully above segmented tabs on 390px.");
    }

    private static async Task<IPage> OpenCarriereAsync(IBrowserContext context, string baseUrl)
    {
        var page = await context.NewPageAsync();
        await PlaywrightCookieConsent.AcceptAsync(context);
        var signIn = $"{baseUrl}/__test/sign-in?role=candidate&return={Uri.EscapeDataString("/carriere")}";
        await page.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GotoAsync($"{baseUrl}/carriere", new() { WaitUntil = WaitUntilState.Load, Timeout = 90_000 });
        await page.Locator("[data-testid=uwv-carriere-sections]").WaitForAsync(new() { Timeout = 60_000 });
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 60_000 });
        return page;
    }

    private static async Task ClickTabAsync(IPage page, string tab)
    {
        var id = tab switch
        {
            "omscholen" => "carriere-tab-omscholen",
            "kansberoepen" => "carriere-tab-kansberoepen",
            _ => "carriere-tab-voor-jou",
        };
        if (tab != "voor-jou")
        {
            await page.Locator($"#{id}").ClickAsync();
            await page.WaitForTimeoutAsync(200);
        }
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

        return Directory.GetCurrentDirectory();
    }
}
