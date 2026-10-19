using System.Text;
using Jobsy.Core.Features;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Jobsy.Web.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class HomeLogoFadeScreenshotTests : IAsyncLifetime
{
    private const string OutDir = "/opt/cursor/artifacts/home-logo-fade";

    private HomeLogoFadeWebFactory? _factory;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private string _baseUrl = "";

    public async ValueTask InitializeAsync()
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        _factory = new HomeLogoFadeWebFactory();
        _baseUrl = _factory.EnsureServerAddress();
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }
    }

    [Fact]
    public async Task Capture_home_logo_and_illustration_fades_r10()
    {
        Directory.CreateDirectory(OutDir);

        foreach (var (w, h, slug, mobile) in new (int W, int H, string Slug, bool Mobile)[]
                 {
                     (1920, 900, "1920", false),
                     (1440, 900, "1440", false),
                     (1024, 768, "1024", false),
                     (390, 844, "390", true),
                 })
        {
            await CaptureFullPageAsync(w, h, $"full-{slug}-r10", mobile);
            await CaptureCropAsync(w, h, ".pub-header__brand", $"crop-header-logo-{slug}-r10", mobile);
            await CaptureCropAsync(w, h, "#top.pub-landing__hero", $"crop-hero-edges-{slug}-r10", mobile);
            for (var i = 0; i < 3; i++)
            {
                await CaptureCropAsync(w, h, $".pub-landing__step-art--{i + 1}", $"crop-step-art-{i + 1}-{slug}-r10", mobile, scroll: true);
            }

            if (!mobile)
            {
                await CaptureHeroEdgeZoomAsync(w, h, slug, "left", "r10");
                await CaptureHeroEdgeZoomAsync(w, h, slug, "right", "r10");
                await CaptureHeroBottomZoomAsync(w, h, slug, "r10");
                if (slug is "1920" or "1440")
                {
                    await AssertHeroSideSeamColorDeltaAsync(w, h, slug);
                }

                await AssertHeroCreamColumnsSmoothOnFullPageAsync(w, h, slug);
            }

            await AssertHeroCtasReceiveClicksAsync(w, h, mobile);
        }

        await CaptureCropAsync(390, 844, ".pub-landing__hero-art-band", "crop-hero-mob-art-390-r10", mobile: true);
        await AssertStep2DnaCirclesMarginAsync(390, 844);

        var logoHeight = await MeasureLogoHeightAsync(1024, 768);
        Assert.InRange(logoHeight, 34, 42);
    }

    [Fact]
    public async Task Capture_home_logo_and_illustration_fades_r9()
    {
        Directory.CreateDirectory(OutDir);

        foreach (var (w, h, slug, mobile) in new (int W, int H, string Slug, bool Mobile)[]
                 {
                     (1920, 900, "1920", false),
                     (1440, 900, "1440", false),
                     (1024, 768, "1024", false),
                     (390, 844, "390", true),
                 })
        {
            await CaptureFullPageAsync(w, h, $"full-{slug}-r9", mobile);
            await CaptureCropAsync(w, h, ".pub-header__brand", $"crop-header-logo-{slug}-r9", mobile);
            await CaptureCropAsync(w, h, "#top.pub-landing__hero", $"crop-hero-edges-{slug}-r9", mobile);
            for (var i = 0; i < 3; i++)
            {
                await CaptureCropAsync(w, h, $".pub-landing__step-art--{i + 1}", $"crop-step-art-{i + 1}-{slug}-r9", mobile, scroll: true);
            }

            if (!mobile)
            {
                await CaptureHeroEdgeZoomAsync(w, h, slug, "left", "r9");
                await CaptureHeroEdgeZoomAsync(w, h, slug, "right", "r9");
                await CaptureHeroBottomZoomAsync(w, h, slug, "r9");
                if (slug is "1920" or "1440")
                {
                    await AssertHeroSideSeamColorDeltaAsync(w, h, slug);
                }

                await AssertHeroCreamColumnsSmoothOnFullPageAsync(w, h, slug);
            }

            await AssertHeroCtasReceiveClicksAsync(w, h, mobile);
        }

        await CaptureCropAsync(390, 844, ".pub-landing__hero-art-band", "crop-hero-mob-art-390-r9", mobile: true);
        await AssertStep2DnaCirclesMarginAsync(390, 844);

        var logoHeight = await MeasureLogoHeightAsync(1024, 768);
        Assert.InRange(logoHeight, 34, 42);
    }

    [Fact]
    public async Task Capture_home_logo_and_illustration_fades_r8()
    {
        Directory.CreateDirectory(OutDir);

        foreach (var (w, h, slug, mobile) in new (int W, int H, string Slug, bool Mobile)[]
                 {
                     (1920, 900, "1920", false),
                     (1440, 900, "1440", false),
                     (1024, 768, "1024", false),
                     (390, 844, "390", true),
                 })
        {
            await CaptureFullPageAsync(w, h, $"full-{slug}-r8", mobile);
            await CaptureCropAsync(w, h, ".pub-header__brand", $"crop-header-logo-{slug}-r8", mobile);
            await CaptureCropAsync(w, h, "#top.pub-landing__hero", $"crop-hero-edges-{slug}-r8", mobile);
            for (var i = 0; i < 3; i++)
            {
                await CaptureCropAsync(w, h, $".pub-landing__step-art--{i + 1}", $"crop-step-art-{i + 1}-{slug}-r8", mobile, scroll: true);
            }

            if (!mobile)
            {
                await CaptureHeroEdgeZoomAsync(w, h, slug, "left", "r8");
                await CaptureHeroEdgeZoomAsync(w, h, slug, "right", "r8");
                await CaptureHeroBottomZoomAsync(w, h, slug, "r8");
                if (slug is "1920" or "1440")
                {
                    await AssertHeroSideSeamColorDeltaAsync(w, h, slug);
                }

                await AssertHeroCreamColumnsSmoothOnFullPageAsync(w, h, slug);
            }

            await AssertHeroCtasReceiveClicksAsync(w, h, mobile);
        }

        await CaptureCropAsync(1024, 768, ".pub-landing__kaart-ill-art", "crop-kaart-art-edges-1024-r8", scroll: true);
        await CaptureCropAsync(1024, 768, ".pub-landing__emp-art", "crop-emp-art-edges-1024-r8", scroll: true);
        await CaptureCropAsync(390, 844, ".pub-landing__hero-art-band", "crop-hero-mob-art-390-r8", mobile: true);
        await AssertStep2DnaCirclesMarginAsync(390, 844);

        var logoHeight = await MeasureLogoHeightAsync(1024, 768);
        Assert.InRange(logoHeight, 34, 42);
    }

    [Fact]
    public async Task Capture_home_logo_and_illustration_fades_r7()
    {
        Directory.CreateDirectory(OutDir);

        foreach (var (w, h, slug, mobile) in new (int W, int H, string Slug, bool Mobile)[]
                 {
                     (1920, 900, "1920", false),
                     (1440, 900, "1440", false),
                     (1024, 768, "1024", false),
                     (390, 844, "390", true),
                 })
        {
            await CaptureFullPageAsync(w, h, $"full-{slug}-r7", mobile);
            await CaptureCropAsync(w, h, ".pub-header__brand", $"crop-header-logo-{slug}-r7", mobile);
            await CaptureCropAsync(w, h, "#top.pub-landing__hero", $"crop-hero-edges-{slug}-r7", mobile);
            for (var i = 0; i < 3; i++)
            {
                await CaptureCropAsync(w, h, $".pub-landing__step-art--{i + 1}", $"crop-step-art-{i + 1}-{slug}-r7", mobile, scroll: true);
            }

            if (!mobile)
            {
                await CaptureHeroEdgeZoomAsync(w, h, slug, "left", "r7");
                await CaptureHeroEdgeZoomAsync(w, h, slug, "right", "r7");
                await CaptureHeroBottomZoomAsync(w, h, slug, "r7");
                if (slug is "1920" or "1440")
                {
                    await AssertHeroSideSeamColorDeltaAsync(w, h, slug);
                }

                await AssertHeroTopBandSmoothnessAsync(w, h, slug);
            }

            await AssertHeroCtasReceiveClicksAsync(w, h, mobile);
        }

        await CaptureCropAsync(1024, 768, ".pub-landing__kaart-ill-art", "crop-kaart-art-edges-1024-r7", scroll: true);
        await CaptureCropAsync(1024, 768, ".pub-landing__emp-art", "crop-emp-art-edges-1024-r7", scroll: true);
        await CaptureCropAsync(390, 844, ".pub-landing__hero-art-band", "crop-hero-mob-art-390-r7", mobile: true);
        await AssertStep2DnaCirclesMarginAsync(390, 844);

        var logoHeight = await MeasureLogoHeightAsync(1024, 768);
        Assert.InRange(logoHeight, 34, 42);
    }

    [Fact]
    public async Task Capture_home_logo_and_illustration_fades_r6()
    {
        Directory.CreateDirectory(OutDir);

        foreach (var (w, h, slug, mobile) in new (int W, int H, string Slug, bool Mobile)[]
                 {
                     (1920, 900, "1920", false),
                     (1440, 900, "1440", false),
                     (1024, 768, "1024", false),
                     (390, 844, "390", true),
                 })
        {
            await CaptureFullPageAsync(w, h, $"full-{slug}-r6", mobile);
            await CaptureCropAsync(w, h, ".pub-header__brand", $"crop-header-logo-{slug}-r6", mobile);
            await CaptureCropAsync(w, h, "#top.pub-landing__hero", $"crop-hero-edges-{slug}-r6", mobile);
            for (var i = 0; i < 3; i++)
            {
                await CaptureCropAsync(w, h, $".pub-landing__step-art--{i + 1}", $"crop-step-art-{i + 1}-{slug}-r6", mobile, scroll: true);
            }

            if (!mobile)
            {
                await CaptureHeroEdgeZoomAsync(w, h, slug, "left", "r6");
                await CaptureHeroEdgeZoomAsync(w, h, slug, "right", "r6");
                await CaptureHeroBottomZoomAsync(w, h, slug, "r6");
                if (slug is "1920" or "1440")
                {
                    await AssertHeroSideSeamColorDeltaAsync(w, h, slug);
                }
            }

            await AssertHeroCtasReceiveClicksAsync(w, h, mobile);
        }

        await CaptureCropAsync(1024, 768, ".pub-landing__kaart-ill-art", "crop-kaart-art-edges-1024-r6", scroll: true);
        await CaptureCropAsync(1024, 768, ".pub-landing__emp-art", "crop-emp-art-edges-1024-r6", scroll: true);
        await CaptureCropAsync(390, 844, ".pub-landing__hero-art-band", "crop-hero-mob-art-390-r6", mobile: true);
        await AssertStep2LeftCircleVisibleAsync(390, 844);

        var logoHeight = await MeasureLogoHeightAsync(1024, 768);
        Assert.InRange(logoHeight, 34, 42);
    }

    private async Task AssertHeroSideSeamColorDeltaAsync(int w, int h, string slug)
    {
        await using var context = await NewContextAsync(w, h);
        var page = await OpenHomeAsync(context);
        var bounds = await page.EvaluateAsync<HeroArtBounds>(
            """
            () => {
              const hero = document.querySelector('.pub-landing__hero--illustrated');
              if (!hero) return null;
              const heroR = hero.getBoundingClientRect();
              const vw = window.innerWidth;
              const artH = parseFloat(getComputedStyle(hero).getPropertyValue('--pub-hero-art-h').trim()) || 680;
              const iw = 2048;
              const ih = 720;
              const scale = Math.min(vw / iw, artH / ih);
              const rw = iw * scale;
              const bandLeft = heroR.left + heroR.width / 2 - vw / 2;
              const imgLeft = bandLeft + (vw - rw) / 2;
              const imgRight = imgLeft + rw;
              return { imgLeft, imgRight, imgTop: heroR.bottom - ih * scale, scale };
            }
            """);
        Assert.NotNull(bounds);

        var hero = page.Locator("#top.pub-landing__hero").First;
        var box = await hero.BoundingBoxAsync();
        Assert.NotNull(box);
        var bytes = await hero.ScreenshotAsync(new() { Type = ScreenshotType.Png });
        using var image = Image.Load<Rgb24>(bytes);

        AssertSideStripDelta(image, box, bounds!.ImgLeft, left: true, slug);
        AssertSideStripDelta(image, box, bounds.ImgRight, left: false, slug);
    }

    private static void AssertSideStripDelta(Image<Rgb24> image, LocatorBoundingBoxResult box, double seamViewportX, bool left, string slug)
    {
        const int strip = 20;
        var seamX = (int)Math.Round(seamViewportX - box.X);
        seamX = Math.Clamp(seamX, 0, image.Width - strip);
        int xOutsideStart;
        int xInsideStart;
        if (left)
        {
            if (seamX <= 0)
            {
                xOutsideStart = 0;
                xInsideStart = strip;
            }
            else
            {
                xOutsideStart = seamX - strip;
                xInsideStart = seamX;
            }
        }
        else if (seamX >= image.Width - 1)
        {
            xOutsideStart = image.Width - (2 * strip);
            xInsideStart = image.Width - strip;
        }
        else
        {
            xOutsideStart = seamX;
            xInsideStart = seamX + strip;
        }
        var y0 = (int)(image.Height * 0.58);
        var y1 = (int)(image.Height * 0.94);
        var (or, og, ob) = MeanRgb(image, xOutsideStart, y0, strip, y1 - y0);
        var (ir, ig, ib) = MeanRgb(image, xInsideStart, y0, strip, y1 - y0);
        var edge = left ? "left" : "right";
        Assert.True(Math.Abs(or - ir) < 3, $"{slug} {edge} R delta {Math.Abs(or - ir)} (outside {or}, inside {ir}).");
        Assert.True(Math.Abs(og - ig) < 3, $"{slug} {edge} G delta {Math.Abs(og - ig)} (outside {og}, inside {ig}).");
        Assert.True(Math.Abs(ob - ib) < 3, $"{slug} {edge} B delta {Math.Abs(ob - ib)} (outside {ob}, inside {ib}).");
    }

    private static (byte R, byte G, byte B) MeanRgb(Image<Rgb24> image, int x0, int y0, int w, int h)
    {
        long r = 0, g = 0, b = 0, n = 0;
        var xStart = Math.Clamp(x0, 0, Math.Max(0, image.Width - 1));
        var x1 = Math.Clamp(x0 + w, xStart, image.Width);
        var yStart = Math.Clamp(y0, 0, Math.Max(0, image.Height - 1));
        var y1 = Math.Clamp(y0 + h, yStart, image.Height);
        for (var y = yStart; y < y1; y++)
        {
            for (var x = xStart; x < x1; x++)
            {
                var p = image[x, y];
                r += p.R;
                g += p.G;
                b += p.B;
                n++;
            }
        }

        Assert.True(n > 0);
        return ((byte)(r / n), (byte)(g / n), (byte)(b / n));
    }

    private async Task AssertHeroCreamColumnsSmoothOnFullPageAsync(int w, int h, string slug)
    {
        await using var context = await NewContextAsync(w, h);
        var page = await OpenHomeAsync(context);
        var layout = await page.EvaluateAsync<HeroPageLayout>(
            """
            () => {
              const hero = document.querySelector('.pub-landing__hero--illustrated');
              if (!hero) return null;
              const heroR = hero.getBoundingClientRect();
              const vw = window.innerWidth;
              const artH = parseFloat(getComputedStyle(hero).getPropertyValue('--pub-hero-art-h').trim()) || 680;
              const iw = 2048;
              const ih = 720;
              const scale = Math.min(vw / iw, artH / ih);
              const rh = ih * scale;
              const scrollY = window.scrollY || window.pageYOffset || 0;
              return {
                heroTop: heroR.top + scrollY,
                artBottom: heroR.bottom + scrollY,
                imgTop: heroR.bottom + scrollY - rh
              };
            }
            """);
        Assert.NotNull(layout);

        await page.WaitForTimeoutAsync(600);
        var bytes = await page.ScreenshotAsync(new() { Type = ScreenshotType.Png, FullPage = true });
        using var image = Image.Load<Rgb24>(bytes);

        var y0 = Math.Max(0, (int)Math.Floor(layout!.HeroTop));
        var y1 = Math.Min(image.Height - 2, (int)Math.Ceiling(layout.ArtBottom));
        var artHeight = layout.ArtBottom - layout.ImgTop;
        var yOpaqueStart = (int)Math.Floor(layout.ImgTop + 130);
        var yOpaqueEnd = (int)Math.Floor(layout.ImgTop + artHeight * 0.88);
        var yHeroCreamEnd = (int)Math.Floor(layout.ImgTop + artHeight * 0.96);
        foreach (var viewportX in new[] { 200, 700, 1100, 1300 })
        {
            var x = Math.Clamp(viewportX, 1, image.Width - 2);
            AssertCreamColumnSmooth(image, x, y0, y1, yOpaqueStart, yOpaqueEnd, yHeroCreamEnd, slug, viewportX);
        }
    }

    private static void AssertCreamColumnSmooth(Image<Rgb24> image, int x, int y0, int y1, int yOpaqueStart, int yOpaqueEnd, int yHeroCreamEnd, string slug, int viewportX)
    {
        for (var y = y0; y < y1; y++)
        {
            if (y >= yOpaqueStart && y < yOpaqueEnd)
            {
                continue;
            }

            if (y >= yHeroCreamEnd)
            {
                continue;
            }

            var a = image[x, y];
            var b = image[x, y + 1];
            if (!IsHeroCreamLike(a) || !IsHeroCreamLike(b))
            {
                continue;
            }

            const int maxRowStep = 2;
            Assert.True(Math.Abs(a.R - b.R) <= maxRowStep, $"{slug} full-page x={viewportX} row {y}→{y + 1} R step {Math.Abs(a.R - b.R)}.");
            Assert.True(Math.Abs(a.G - b.G) <= maxRowStep, $"{slug} full-page x={viewportX} row {y}→{y + 1} G step {Math.Abs(a.G - b.G)}.");
            Assert.True(Math.Abs(a.B - b.B) <= maxRowStep, $"{slug} full-page x={viewportX} row {y}→{y + 1} B step {Math.Abs(a.B - b.B)}.");
        }
    }

    private static bool IsHeroCreamLike(Rgb24 p) =>
        p.R >= 248 && p.G >= 242 && p.B >= 226
        && p.R - p.G <= 10 && p.G - p.B <= 18;

    private sealed class HeroPageLayout
    {
        public double HeroTop { get; set; }
        public double ArtBottom { get; set; }
        public double ImgTop { get; set; }
    }

    private async Task AssertHeroTopBandSmoothnessAsync(int w, int h, string slug)
    {
        await using var context = await NewContextAsync(w, h);
        var page = await OpenHomeAsync(context);
        var bounds = await page.EvaluateAsync<HeroArtBounds>(
            """
            () => {
              const hero = document.querySelector('.pub-landing__hero--illustrated');
              if (!hero) return null;
              const heroR = hero.getBoundingClientRect();
              const vw = window.innerWidth;
              const artH = parseFloat(getComputedStyle(hero).getPropertyValue('--pub-hero-art-h').trim()) || 680;
              const iw = 2048;
              const ih = 720;
              const scale = Math.min(vw / iw, artH / ih);
              const rw = iw * scale;
              const rh = ih * scale;
              const bandLeft = heroR.left + heroR.width / 2 - vw / 2;
              const imgLeft = bandLeft + (vw - rw) / 2;
              const imgTop = heroR.bottom - rh;
              return { imgLeft, imgRight: imgLeft + rw, imgTop, scale };
            }
            """);
        Assert.NotNull(bounds);

        var hero = page.Locator("#top.pub-landing__hero").First;
        var box = await hero.BoundingBoxAsync();
        Assert.NotNull(box);
        var bytes = await hero.ScreenshotAsync(new() { Type = ScreenshotType.Png });
        using var image = Image.Load<Rgb24>(bytes);

        var seamY = (int)Math.Round(bounds!.ImgTop - box!.Y);
        foreach (var viewportX in new[] { 700, 1100, 1300 })
        {
            var x = (int)Math.Round(viewportX - box.X);
            if (x < 1 || x >= image.Width - 1)
            {
                continue;
            }

            AssertVerticalStripSmooth(image, x, seamY, slug, viewportX);
        }
    }

    private static void AssertVerticalStripSmooth(Image<Rgb24> image, int x, int seamY, string slug, int viewportX)
    {
        var y0 = Math.Clamp(seamY - 4, 0, image.Height - 22);
        var y1 = Math.Clamp(seamY + 26, y0 + 2, image.Height - 1);
        for (var y = y0; y < y1; y++)
        {
            var a = image[x, y];
            var b = image[x, y + 1];
            Assert.True(Math.Abs(a.R - b.R) <= 1, $"{slug} x={viewportX} row {y}→{y + 1} R step {Math.Abs(a.R - b.R)}.");
            Assert.True(Math.Abs(a.G - b.G) <= 1, $"{slug} x={viewportX} row {y}→{y + 1} G step {Math.Abs(a.G - b.G)}.");
            Assert.True(Math.Abs(a.B - b.B) <= 1, $"{slug} x={viewportX} row {y}→{y + 1} B step {Math.Abs(a.B - b.B)}.");
        }

        var yStart = Math.Clamp(seamY, 0, image.Height - 21);
        var top = image[x, yStart];
        var below = image[x, yStart + 20];
        Assert.True(Math.Abs(top.R - below.R) <= 3, $"{slug} x={viewportX} 20px R total {Math.Abs(top.R - below.R)}.");
        Assert.True(Math.Abs(top.G - below.G) <= 3, $"{slug} x={viewportX} 20px G total {Math.Abs(top.G - below.G)}.");
        Assert.True(Math.Abs(top.B - below.B) <= 3, $"{slug} x={viewportX} 20px B total {Math.Abs(top.B - below.B)}.");
    }

    private async Task AssertStep2DnaCirclesMarginAsync(int w, int h)
    {
        await using var context = await NewContextAsync(w, h, mobile: true);
        var page = await OpenHomeAsync(context);
        var loc = page.Locator(".pub-landing__step-art--2").First;
        await loc.ScrollIntoViewIfNeededAsync();
        await page.WaitForTimeoutAsync(600);
        var bytes = await loc.ScreenshotAsync(new() { Type = ScreenshotType.Png });
        using var image = Image.Load<Rgb24>(bytes);

        var minBlueX = int.MaxValue;
        var maxBlueX = int.MinValue;
        var blueRows = 0;
        for (var y = (int)(image.Height * 0.05); y < (int)(image.Height * 0.62); y++)
        {
            for (var x = 0; x < image.Width; x++)
            {
                if (!IsDnaCirclePixel(image[x, y]))
                {
                    continue;
                }

                minBlueX = Math.Min(minBlueX, x);
                maxBlueX = Math.Max(maxBlueX, x);
                blueRows++;
            }
        }

        Assert.True(blueRows >= 80, $"Expected DNA circle pixels on step 2 @390, got {blueRows}.");
        Assert.True(minBlueX >= 8, $"Left DNA circle margin {minBlueX}px is below 8px.");
        Assert.True(maxBlueX <= image.Width - 9, $"Right DNA circle margin {image.Width - maxBlueX}px is below 8px.");

        var maxLeftColumnBlueSpan = 0;
        for (var x = 0; x < image.Width / 3; x++)
        {
            var span = 0;
            for (var y = 0; y < image.Height; y++)
            {
                if (IsDnaCirclePixel(image[x, y]))
                {
                    span++;
                }
            }

            maxLeftColumnBlueSpan = Math.Max(maxLeftColumnBlueSpan, span);
        }

        Assert.True(maxLeftColumnBlueSpan >= 18, $"Left circle column span too thin ({maxLeftColumnBlueSpan}px).");

        var leftQuarterBlue = 0;
        for (var y = (int)(image.Height * 0.12); y < (int)(image.Height * 0.5); y++)
        {
            for (var x = 8; x < image.Width / 4; x++)
            {
                if (IsDnaCirclePixel(image[x, y]))
                {
                    leftQuarterBlue++;
                }
            }
        }

        Assert.True(leftQuarterBlue >= 24, $"Leftmost circle not fully in frame (hits={leftQuarterBlue}).");

        AssertStep2SideBandQuality(image);
    }

    private static void AssertStep2SideBandQuality(Image<Rgb24> image)
    {
        var y0 = (int)(image.Height * 0.06);
        var y1 = (int)(image.Height * 0.58);
        AssertNoHardVerticalEdgesInBand(image, 2, Math.Min(30, image.Width / 3), y0, y1, "left");
        AssertNoHardVerticalEdgesInBand(image, Math.Max(2, image.Width - 31), image.Width - 2, y0, y1, "right");
        AssertNoDuplicateBlueCirclesInLeftBand(image, Math.Min(30, image.Width / 3), y0, y1);
    }

    private static void AssertNoHardVerticalEdgesInBand(Image<Rgb24> image, int xStart, int xEnd, int y0, int y1, string side)
    {
        for (var x = xStart + 1; x <= xEnd; x++)
        {
            var (lr, lg, lb) = MeanColumnRgb(image, x - 1, y0, y1);
            var (rr, rg, rb) = MeanColumnRgb(image, x, y0, y1);
            if (IsDnaCirclePixel(new Rgb24(lr, lg, lb)) || IsDnaCirclePixel(new Rgb24(rr, rg, rb)))
            {
                continue;
            }

            var maxStep = Math.Max(Math.Abs(lr - rr), Math.Max(Math.Abs(lg - rg), Math.Abs(lb - rb)));
            Assert.True(maxStep < 22, $"Hard vertical edge on {side} at x≈{x} (Δ={maxStep}).");
        }
    }

    private static void AssertNoDuplicateBlueCirclesInLeftBand(Image<Rgb24> image, int xEnd, int y0, int y1)
    {
        var strongColumns = new List<int>();
        for (var x = 0; x <= xEnd; x++)
        {
            var span = 0;
            for (var y = y0; y < y1; y++)
            {
                if (IsDnaCirclePixel(image[x, y]))
                {
                    span++;
                }
            }

            if (span >= 16)
            {
                strongColumns.Add(x);
            }
        }

        if (strongColumns.Count < 2)
        {
            return;
        }

        var groups = 1;
        for (var i = 1; i < strongColumns.Count; i++)
        {
            if (strongColumns[i] - strongColumns[i - 1] > 6)
            {
                groups++;
            }
        }

        Assert.True(groups < 2, $"Mirrored duplicate blue circles in left band (columns: {string.Join(",", strongColumns)}).");
    }

    private static (byte R, byte G, byte B) MeanColumnRgb(Image<Rgb24> image, int x, int y0, int y1)
    {
        x = Math.Clamp(x, 0, image.Width - 1);
        y0 = Math.Clamp(y0, 0, image.Height - 1);
        y1 = Math.Clamp(y1, y0 + 1, image.Height);
        long r = 0, g = 0, b = 0, n = 0;
        for (var y = y0; y < y1; y++)
        {
            var p = image[x, y];
            r += p.R;
            g += p.G;
            b += p.B;
            n++;
        }

        return ((byte)(r / n), (byte)(g / n), (byte)(b / n));
    }

    private static bool IsDnaCirclePixel(Rgb24 p) =>
        p.B > p.R + 16 && p.B > p.G + 6 && p.B > 115;

    private async Task AssertStep2LeftCircleVisibleAsync(int w, int h)
    {
        await AssertStep2DnaCirclesMarginAsync(w, h);
    }

    private sealed class HeroArtBounds
    {
        public double ImgLeft { get; set; }
        public double ImgRight { get; set; }
        public double ImgTop { get; set; }
        public double Scale { get; set; }
    }

    private async Task CaptureHeroEdgeZoomAsync(int w, int h, string slug, string edge, string rev)
    {
        await using var context = await NewContextAsync(w, h);
        var page = await OpenHomeAsync(context);
        var hero = page.Locator("#top.pub-landing__hero").First;
        await hero.WaitForAsync(new() { Timeout = 60_000 });
        var box = await hero.BoundingBoxAsync();
        Assert.NotNull(box);
        const float clipWidth = 140f;
        var clipHeight = (float)Math.Min(420, Math.Max(280, box.Height - 140));
        var clipY = (float)(box.Y + 100);
        var clipX = edge == "left"
            ? (float)box.X
            : (float)(box.X + box.Width - clipWidth);
        await page.WaitForTimeoutAsync(600);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(OutDir, $"crop-hero-edge-{edge}-{slug}-{rev}.png"),
            Clip = new() { X = clipX, Y = clipY, Width = clipWidth, Height = clipHeight },
        });
    }

    private async Task CaptureHeroBottomZoomAsync(int w, int h, string slug, string rev)
    {
        await using var context = await NewContextAsync(w, h);
        var page = await OpenHomeAsync(context);
        var hero = page.Locator("#top.pub-landing__hero").First;
        await hero.WaitForAsync(new() { Timeout = 60_000 });
        var box = await hero.BoundingBoxAsync();
        Assert.NotNull(box);
        var clipHeight = 220f;
        var clipWidth = (float)Math.Min(520, box.Width);
        var clipX = (float)(box.X + (box.Width - clipWidth) / 2);
        var clipY = (float)(box.Y + box.Height - clipHeight);
        await page.WaitForTimeoutAsync(600);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(OutDir, $"crop-hero-bottom-{slug}-{rev}.png"),
            Clip = new() { X = clipX, Y = clipY, Width = clipWidth, Height = clipHeight },
        });
    }

    private async Task AssertHeroCtasReceiveClicksAsync(int w, int h, bool mobile)
    {
        await using var context = await NewContextAsync(w, h, mobile);
        var page = await OpenHomeAsync(context);
        foreach (var selector in new[] { "[data-kpi=\"LandingCtaTest\"]", ".pub-landing__cta-login" })
        {
            var btn = page.Locator(selector).First;
            await btn.WaitForAsync(new() { Timeout = 30_000 });
            var hit = await btn.EvaluateAsync<bool>(
                """
                el => {
                  const r = el.getBoundingClientRect();
                  const x = r.left + r.width / 2;
                  const y = r.top + r.height / 2;
                  const top = document.elementFromPoint(x, y);
                  return top === el || (top instanceof Node && el.contains(top));
                }
                """);
            Assert.True(hit, $"Expected {selector} to receive pointer hits at {w}px.");
        }
    }

    private async Task CaptureFullPageAsync(int w, int h, string name, bool mobile = false)
    {
        await using var context = await NewContextAsync(w, h, mobile);
        var page = await OpenHomeAsync(context);
        await page.WaitForTimeoutAsync(900);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(OutDir, $"{name}.png"),
            FullPage = true
        });
    }

    private async Task CaptureCropAsync(int w, int h, string selector, string name, bool mobile = false, bool scroll = false)
    {
        await using var context = await NewContextAsync(w, h, mobile);
        var page = await OpenHomeAsync(context);
        var loc = page.Locator(selector).First;
        await loc.WaitForAsync(new() { Timeout = 60_000 });
        if (scroll)
        {
            await loc.ScrollIntoViewIfNeededAsync();
        }

        await page.WaitForTimeoutAsync(600);
        await loc.ScreenshotAsync(new() { Path = Path.Combine(OutDir, $"{name}.png") });
    }

    private async Task<double> MeasureLogoHeightAsync(int w, int h)
    {
        await using var context = await NewContextAsync(w, h);
        var page = await OpenHomeAsync(context);
        return await page.EvaluateAsync<double>(
            """
            () => {
              const el = document.querySelector('.pub-header__logo');
              if (!el) return 0;
              return el.getBoundingClientRect().height;
            }
            """);
    }

    private async Task<IPage> OpenHomeAsync(IBrowserContext context)
    {
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{_baseUrl}/?lang=nl", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 120_000 });
        await page.Locator(".pub-landing--calm .pub-landing__hero--illustrated h1").WaitForAsync(new() { Timeout = 60_000 });
        return page;
    }

    private async Task<IBrowserContext> NewContextAsync(int w, int h, bool mobile = false)
    {
        return await _browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = w, Height = h },
            Locale = "nl-NL",
            IsMobile = mobile,
            HasTouch = mobile,
        });
    }

    private sealed class HomeLogoFadeWebFactory : WebApplicationFactory<Jobsy.Web.WebAssemblyMarker>
    {
        private IHost? _kestrel;
        public string ServerAddress { get; private set; } = "";

        public string EnsureServerAddress()
        {
            if (string.IsNullOrEmpty(ServerAddress))
            {
                _ = CreateClient();
            }

            return ServerAddress;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ApiBaseUrl"] = "http://api.test/",
                    ["Landing:ForceVariant"] = "on",
                    ["CLOUDFLARE_ORIGIN_SECRET"] = "",
                    ["JobsyAuth:Jwt:PrivateKeyPem"] = Jobsy.Core.Security.JobsyAccessToken.DevelopmentPrivateKeyPem
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEmployersSwitch>();
                services.AddSingleton<IEmployersSwitch>(new AlwaysOnEmployersSwitch());
                services.RemoveAll<IFeatureFlags>();
                services.AddSingleton<IFeatureFlags>(new StubFlags());
                services.RemoveAll<Jobsy.Web.Services.JobsyApiClient>();
                services.AddScoped(_ => new Jobsy.Web.Services.JobsyApiClient(
                    new HttpClient(new StubHandler()) { BaseAddress = new Uri("http://api.test/") }));
            });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var testHost = builder.Build();
            builder.ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder.UseKestrel();
                webHostBuilder.UseUrls("http://127.0.0.1:0");
            });
            _kestrel = builder.Build();
            _kestrel.Start();
            var addresses = _kestrel.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
            ServerAddress = addresses!.Addresses.First(a => a.StartsWith("http://", StringComparison.Ordinal));
            testHost.Start();
            return testHost;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _kestrel?.Dispose();
            }

            base.Dispose(disposing);
        }

        private sealed class StubFlags : IFeatureFlags
        {
            public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
                => ValueTask.FromResult(new FeatureFlagSnapshot(EmployersEnabled: false, CandidatePassportEnabled: true));

            public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
                => ValueTask.FromResult(false);

            public void Invalidate()
            {
            }
        }

        private sealed class StubHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var path = request.RequestUri?.AbsolutePath ?? "";
                if (path.Contains("feature", StringComparison.OrdinalIgnoreCase)
                    || path.Contains("platform", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                    {
                        Content = new StringContent("""{"employersEnabled":false,"candidatePassportEnabled":true}""", Encoding.UTF8, "application/json")
                    });
                }

                if (path.Contains("landing-stats", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                    {
                        Content = new StringContent("""{"jobsWithin30Min":38,"medianCommuteMin":14}""", Encoding.UTF8, "application/json")
                    });
                }

                if (path.Contains("landing-price", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                    {
                        Content = new StringContent("""{"priceEuro":9.95}""", Encoding.UTF8, "application/json")
                    });
                }

                if (path.Contains("site/legal", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                    {
                        Content = new StringContent("""{"tradeName":"Jobsy","kvk":""}""", Encoding.UTF8, "application/json")
                    });
                }

                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound)
                {
                    Content = new StringContent("""{"code":"not_found"}""", Encoding.UTF8, "application/json")
                });
            }
        }
    }
}
