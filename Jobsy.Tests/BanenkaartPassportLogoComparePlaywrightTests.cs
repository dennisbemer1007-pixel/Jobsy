using System.Text.RegularExpressions;
using Microsoft.Playwright;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Jobsy.Tests;

/// <summary>Banenkaart mockup top uses the same horizontal Lobsy logo as candidate chrome.</summary>
[Collection("BanenkaartPlaywright")]
public sealed class BanenkaartPassportLogoComparePlaywrightTests : IAsyncLifetime
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
    public async Task Banenkaart_top_bar_matches_shared_horizontal_lobsy_logo()
    {
        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
            IgnoreHTTPSErrors = true
        });
        var page = await context.NewPageAsync();

        await page.GotoAsync(_baseUrl + "/", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        var referenceBrand = page.Locator(".pub-header__logo.lobsy-logo--horizontal");
        await referenceBrand.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        var refImg = referenceBrand.Locator(".lobsy-logo__img");
        var refSrc = await refImg.GetAttributeAsync("src") ?? "";
        Assert.Contains("lobsy-logo-horizontaal.svg", refSrc, StringComparison.Ordinal);
        var refShot = await referenceBrand.ScreenshotAsync(new() { OmitBackground = true });

        await page.GotoAsync(_baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await page.WaitForSelectorAsync("[data-testid=kb-mockup-top]", new() { Timeout = 60_000 });
        var banenBrand = page.Locator("[data-testid=kb-mockup-top] .brand__logo.lobsy-logo--horizontal");
        await banenBrand.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });

        var banenImg = page.Locator("[data-testid=kb-mockup-top] .lobsy-logo__img");
        await Assertions.Expect(banenImg).ToHaveAttributeAsync("src", new Regex("lobsy-logo-horizontaal\\.svg"));
        await Assertions.Expect(banenImg).ToHaveAttributeAsync("width", "154");
        await Assertions.Expect(banenImg).ToHaveAttributeAsync("height", "44");
        Assert.Equal(refSrc, await banenImg.GetAttributeAsync("src") ?? "");

        var banenShot = await banenBrand.ScreenshotAsync(new() { OmitBackground = true });
        Directory.CreateDirectory("/opt/cursor/artifacts");
        await SaveSideBySideAsync(refShot, banenShot, "/opt/cursor/artifacts/banenkaart-passport-logo-compare.png");

        await using var mobileContext = await _browser.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 390, Height = 844 },
            IgnoreHTTPSErrors = true
        });
        var mobilePage = await mobileContext.NewPageAsync();
        await mobilePage.GotoAsync(_baseUrl + "/banenkaart", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await mobilePage.WaitForSelectorAsync("[data-testid=kb-mockup-top]", new() { Timeout = 60_000 });
        await mobilePage.Locator("[data-testid=kb-mockup-top] .brand__logo").ScreenshotAsync(
            new() { Path = "/opt/cursor/artifacts/banenkaart-logo-mobile.png", OmitBackground = true });
    }

    private static async Task SaveSideBySideAsync(byte[] left, byte[] right, string path)
    {
        using var leftImg = Image.Load<Rgba32>(left);
        using var rightImg = Image.Load<Rgba32>(right);
        var gap = 24;
        var height = Math.Max(leftImg.Height, rightImg.Height);
        var width = leftImg.Width + gap + rightImg.Width;
        using var canvas = new Image<Rgba32>(width, height, new Rgba32(255, 255, 255, 255));
        canvas.Mutate(ctx =>
        {
            ctx.DrawImage(leftImg, new Point(0, (height - leftImg.Height) / 2), 1f);
            ctx.DrawImage(rightImg, new Point(leftImg.Width + gap, (height - rightImg.Height) / 2), 1f);
        });
        await canvas.SaveAsPngAsync(path);
    }
}
