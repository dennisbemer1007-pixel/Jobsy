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
    public async Task Desktop_lead_fully_visible_at_1280()
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 900 },
            IgnoreHTTPSErrors = true,
        });
        var page = await OpenCarriereAsync(context, baseUrl);
        var ok = await page.EvaluateAsync<bool>("""
            () => {
              const lead = document.querySelector('header.uwv-carriere-showcase-hero .cand-hero__lead');
              if (!lead) return false;
              const text = (lead.textContent || '').trim();
              if (!text.includes('Haaglanden')) return false;
              return lead.scrollHeight <= lead.clientHeight + 2 && lead.clientHeight > 12;
            }
            """);
        Assert.True(ok, "Desktop hero lead must show full copy including Haaglanden without bottom clip.");
    }

    [Fact]
    public async Task Segmented_shows_when_uwv_feature_flag_off()
    {
        Environment.SetEnvironmentVariable("JOBSY_UWV_FEATURES", "0");
        using var web = new IllustratedPagesWebFactory();
        _ = web.ServerAddress;
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 900 },
            IgnoreHTTPSErrors = true,
        });
        var page = await context.NewPageAsync();
        await PlaywrightCookieConsent.AcceptAsync(context);
        var baseUrl = web.ServerAddress.TrimEnd('/');
        await page.GotoAsync($"{baseUrl}/__test/sign-in?role=candidate&return={Uri.EscapeDataString("/carriere")}");
        await page.GotoAsync($"{baseUrl}/carriere", new() { WaitUntil = WaitUntilState.Load, Timeout = 90_000 });
        await page.Locator("[data-testid=uwv-carriere-sections]").WaitForAsync(new() { Timeout = 60_000 });
        Environment.SetEnvironmentVariable("JOBSY_UWV_FEATURES", "1");
    }

    [Fact]
    public async Task Capture_empty_profile_candidate_voor_jou_390()
    {
        var outDir = Path.Combine(FindRepoRoot(), "artifacts");
        Directory.CreateDirectory(outDir);
        var filledPath = Path.Combine(outDir, "carriere-segmented-product-390-voor-jou-viewport.png");
        var emptyPath = Path.Combine(outDir, "carriere-segmented-product-390-empty-profile-voor-jou-viewport.png");

        var filledBaseUrl = _web!.ServerAddress.TrimEnd('/');
        await using (var filledContext = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IsMobile = true,
            HasTouch = true,
            IgnoreHTTPSErrors = true,
        }))
        {
            var filledPage = await OpenCarriereAsync(filledContext, filledBaseUrl);
            await filledPage.ScreenshotAsync(new() { Path = filledPath, FullPage = false });
        }

        using var web = new IllustratedPagesWebFactory(emptyAppsAndLikes: true);
        _ = web.ServerAddress;
        var baseUrl = web.ServerAddress.TrimEnd('/');
        await using var context = await _browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IsMobile = true,
            HasTouch = true,
            IgnoreHTTPSErrors = true,
        });
        var page = await context.NewPageAsync();
        await PlaywrightCookieConsent.AcceptAsync(context);
        await page.GotoAsync($"{baseUrl}/__test/sign-in?role=candidate&return={Uri.EscapeDataString("/carriere")}");
        await page.GotoAsync($"{baseUrl}/carriere", new() { WaitUntil = WaitUntilState.Load, Timeout = 90_000 });
        await page.Locator("[data-testid=uwv-carriere-sections]").WaitForAsync(new() { Timeout = 60_000 });
        await page.Locator("[data-testid=carriere-segmented-empty-profile]").WaitForAsync(new() { Timeout = 60_000 });
        await page.GetByRole(AriaRole.Heading, new() { Name = "carrièrekansen" }).WaitForAsync(new() { Timeout = 60_000 });
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 60_000 });
        await WaitForAppHeaderChromeAsync(page);
        await page.ScreenshotAsync(new() { Path = emptyPath, FullPage = false });

        Assert.False(
            File.ReadAllBytes(filledPath).AsSpan().SequenceEqual(File.ReadAllBytes(emptyPath)),
            "Empty-profile capture must differ from the filled 390 voor-jou viewport shot.");
    }

    [Fact]
    public async Task Mobile_empty_profile_lead_to_banner_gap()
    {
        using var web = new IllustratedPagesWebFactory(emptyAppsAndLikes: true);
        _ = web.ServerAddress;
        var baseUrl = web.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 390, Height = 844 },
            IsMobile = true,
            HasTouch = true,
            IgnoreHTTPSErrors = true,
        });
        var page = await context.NewPageAsync();
        await PlaywrightCookieConsent.AcceptAsync(context);
        await page.GotoAsync($"{baseUrl}/__test/sign-in?role=candidate&return={Uri.EscapeDataString("/carriere")}");
        await page.GotoAsync($"{baseUrl}/carriere", new() { WaitUntil = WaitUntilState.Load, Timeout = 90_000 });
        await page.Locator("[data-testid=carriere-segmented-empty-profile]").WaitForAsync(new() { Timeout = 60_000 });
        await page.GetByRole(AriaRole.Heading, new() { Name = "carrièrekansen" }).WaitForAsync(new() { Timeout = 60_000 });
        await page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-lobsy-circuit') === 'ready'",
            null,
            new() { Timeout = 60_000 });
        var gap = await page.EvaluateAsync<double>("""
            () => {
              const lead = document.querySelector('header.uwv-carriere-showcase-hero .cand-hero__lead');
              const banner = document.querySelector('[data-testid=carriere-segmented-empty-profile]');
              const header = document.querySelector('header.uwv-carriere-showcase-hero');
              if (!lead || !banner || !header) return 999;
              return banner.getBoundingClientRect().top - lead.getBoundingClientRect().bottom;
            }
            """);
        var outDir = Path.Combine(FindRepoRoot(), "artifacts");
        Directory.CreateDirectory(outDir);
        File.WriteAllText(Path.Combine(outDir, "carriere-mobile-lead-empty-banner-gap.txt"), $"{gap:F1}");
        Assert.InRange(gap, 0, 16);

        var groupTitle = await page.Locator("#carriere-group-similar").InnerTextAsync();
        Assert.Contains("verkennen", groupTitle, StringComparison.OrdinalIgnoreCase);
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
        var gap = await page.EvaluateAsync<double>("""
            () => {
              const lead = document.querySelector('header.uwv-carriere-showcase-hero .cand-hero__lead');
              const tabs = document.querySelector('.carriere-segmented__nav');
              if (!lead || !tabs) return 999;
              const lb = lead.getBoundingClientRect();
              const tb = tabs.getBoundingClientRect();
              return tb.top - lb.bottom;
            }
            """);
        var outDir = Path.Combine(FindRepoRoot(), "artifacts");
        Directory.CreateDirectory(outDir);
        File.WriteAllText(Path.Combine(outDir, "carriere-mobile-lead-tabs-gap.txt"), $"{gap:F1}");
        Assert.InRange(gap, 0, 16);
    }

    [Fact]
    public async Task Desktop_showcase_uses_single_watercolor_art_at_1280()
    {
        var baseUrl = _web!.ServerAddress.TrimEnd('/');
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1280, Height = 900 },
            IgnoreHTTPSErrors = true,
        });
        var page = await OpenCarriereAsync(context, baseUrl);
        var metrics = await page.EvaluateAsync<string>("""
            () => {
              const header = document.querySelector('header.uwv-carriere-showcase-hero');
              if (!header) return 'no-header';
              const imgs = Array.from(header.querySelectorAll('.cand-compact-hero__art img'));
              const visible = imgs.filter(img => getComputedStyle(img).display !== 'none');
              const after = getComputedStyle(header, '::after');
              return JSON.stringify({
                visible: visible.length,
                afterDisplay: after.display,
                afterBg: after.backgroundImage
              });
            }
            """);
        Assert.Contains("\"visible\":1", metrics, StringComparison.Ordinal);
        Assert.DoesNotContain("url(", metrics, StringComparison.OrdinalIgnoreCase);
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
        await page.Locator("header.uwv-carriere-showcase-hero h1").First.WaitForAsync(new() { Timeout = 60_000 });
        return page;
    }

    private static async Task WaitForAppHeaderChromeAsync(IPage page)
    {
        await page.Locator("[data-testid=app-shell-header]").WaitForAsync(new() { Timeout = 60_000 });
        await page.WaitForFunctionAsync(
            """
            () => {
              const logo = document.querySelector('.lobsy-logo__img');
              const flag = document.querySelector('.language-selector__flag');
              return logo && logo.complete && logo.naturalWidth > 0
                && flag && flag.complete && flag.naturalWidth > 0;
            }
            """,
            null,
            new() { Timeout = 60_000 });
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
