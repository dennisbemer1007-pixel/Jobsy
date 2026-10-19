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

    private async Task AssertStep2LeftCircleVisibleAsync(int w, int h)
    {
        await using var context = await NewContextAsync(w, h, mobile: true);
        var page = await OpenHomeAsync(context);
        var loc = page.Locator(".pub-landing__step-art--2").First;
        await loc.ScrollIntoViewIfNeededAsync();
        await page.WaitForTimeoutAsync(600);
        var bytes = await loc.ScreenshotAsync(new() { Type = ScreenshotType.Png });
        using var image = Image.Load<Rgb24>(bytes);
        var x1 = Math.Max(4, image.Width / 12);
        var blueHits = 0;
        var samples = 0;
        for (var y = (int)(image.Height * 0.08); y < (int)(image.Height * 0.55); y++)
        {
            for (var x = 2; x < x1; x++)
            {
                var p = image[x, y];
                samples++;
                if (p.B > p.R + 18 && p.B > p.G + 8 && p.B > 120)
                {
                    blueHits++;
                }
            }
        }

        Assert.True(samples > 0);
        Assert.True(blueHits >= 12, $"Expected left DNA circle pixels on step 2 @390, got {blueHits} blue hits in {samples} samples.");
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
