using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Jobsy.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Jobsy.Tests;

/// <summary>
/// Real UI captures for Dennis review. Run: JOBSY_CAPTURE_TESTBLOKKEN=1 dotnet test --filter FullyQualifiedName~TestBlokkenR1CaptureTests
/// </summary>
[Collection("DeepPlaywrightSerial")]
public sealed class TestBlokkenR1CaptureTests : IAsyncLifetime
{
    public const string OutputDir = "/opt/cursor/artifacts/testblokken-r1";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(OutputDir);
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
            Args = ["--disable-dev-shm-usage", "--no-sandbox"]
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
    }

    private sealed record CaptureTarget(string FileStem, string ReturnPath, AssessmentKind Kind, int Answered, bool PaidDeep, string ExpectedBlock);

    private static readonly CaptureTarget[] Targets =
    [
        new("quick-scan-hoe-je-werkt", "/__test/capture-test-block?kind=competence&variant=quick", AssessmentKind.Competence, 6, false, "hoe-je-werkt"),
        new("quick-scan-leuk", "/__test/capture-test-block?kind=career&variant=quick", AssessmentKind.Career, 6, false, "leuk"),
        new("quick-scan-sfeer", "/__test/capture-test-block?kind=culture&variant=quick", AssessmentKind.Culture, 6, false, "sfeer"),
        new("quick-scan-waarden", "/__test/capture-test-block?kind=values&variant=quick", AssessmentKind.Values, 6, false, "waarden"),
        new("verdieping-leuk", "/__test/capture-test-block?kind=career&variant=verdieping", AssessmentKind.Career, 12, false, "leuk"),
        new("diepte-competenties", "/__test/capture-test-block?kind=competence&variant=deep", AssessmentKind.Competence, 35, true, "hoe-je-werkt")
    ];

    [Fact]
    public async Task Capture_testblokken_r1_desktop_and_mobile()
    {
        var capture = (Environment.GetEnvironmentVariable("JOBSY_CAPTURE_TESTBLOKKEN") ?? "").Trim() == "1";
        if (!capture)
        {
            return;
        }

        PurgeLegacyArtifactDirs();

        var onlyStem = (Environment.GetEnvironmentVariable("JOBSY_TESTBLOKKEN_STEM") ?? "").Trim();
        var targets = Targets;
        if (!string.IsNullOrEmpty(onlyStem))
        {
            targets = targets.Where(t => t.FileStem.Equals(onlyStem, StringComparison.OrdinalIgnoreCase)).ToArray();
            Assert.NotEmpty(targets);
        }

        foreach (var target in targets)
        {
            await using var web = new CaptureWebFactory(target);
            var baseUrl = web.EnsureServerAddress().TrimEnd('/');

            foreach (var (width, height, tag) in new (int, int, string)[] { (1440, 900, "1440"), (390, 844, "390") })
            {
                await using var context = await _browser!.NewContextAsync(new()
                {
                    ViewportSize = new ViewportSize { Width = width, Height = height },
                    Locale = "nl-NL",
                    IsMobile = width < 640,
                    HasTouch = width < 640,
                    IgnoreHTTPSErrors = true
                });
                await PlaywrightCookieConsent.AcceptAsync(context);
                await SignInViaHttpAsync(context, baseUrl, target.ReturnPath);

                var page = await context.NewPageAsync();
                page.SetDefaultTimeout(90_000);
                await page.GotoAsync(baseUrl + target.ReturnPath, new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
                await PlaywrightCookieConsent.AcceptOnPageAsync(page);
                await page.Locator(".test-flow").WaitForAsync(new() { Timeout = 90_000 });

                var stage = page.Locator("[data-testid=test-block-stage]");
                await stage.WaitForAsync();
                Assert.Equal(target.ExpectedBlock, await stage.GetAttributeAsync("data-block"));

                await AssertInsightVisibleAsync(page, width);
                await AssertQualityAsync(page, width);

                var path = Path.Combine(OutputDir, $"{target.FileStem}-{tag}.png");
                await page.ScreenshotAsync(new() { Path = path, FullPage = false });
                Assert.True(File.Exists(path) && new FileInfo(path).Length > 10_000, path);
            }
        }

        if (string.IsNullOrEmpty(onlyStem))
        {
            var pngCount = Directory.GetFiles(OutputDir, "*.png").Length;
            Assert.Equal(12, pngCount);
        }
    }

    [Theory]
    [InlineData("quick-scan-hoe-je-werkt")]
    [InlineData("quick-scan-waarden")]
    [InlineData("diepte-competenties")]
    public void Mobhero_r5_page_captures_fail_visible_pixel_gate(string stem)
    {
        var path = Path.Combine("/opt/cursor/artifacts/testblokken-mobhero-r5", $"{stem}-390.png");
        if (!File.Exists(path))
        {
            return;
        }

        using var page = Image.Load<Rgba32>(path);
        var heroTop = (int)(page.Height * 0.12);
        var heroBottom = (int)(page.Height * 0.38);
        var heroHeight = heroBottom - heroTop;
        using var hero = new Image<Rgba32>(page.Width, heroHeight);
        for (var y = 0; y < heroHeight; y++)
        {
            for (var x = 0; x < page.Width; x++)
            {
                hero[x, y] = page[x, heroTop + y];
            }
        }

        using var ms = new MemoryStream();
        hero.SaveAsPng(ms);
        var ex = Record.Exception(() => AssertMobHeroVisiblePixels(ms.ToArray(), stem));
        Assert.NotNull(ex);
    }

    [Theory]
    [InlineData("hoe-je-werkt")]
    [InlineData("leuk")]
    [InlineData("sfeer")]
    [InlineData("waarden")]
    public void Mobhero_composite_pngs_have_visible_figure_pixels(string slug)
    {
        var root = FindRepoRoot();
        var path = Path.Combine(root, "Jobsy.Web/wwwroot/images/tests/figures", $"{slug}-mobhero.png");
        Assert.True(File.Exists(path), $"Missing mobhero composite: {path}");
        var png = File.ReadAllBytes(path);
        AssertMobHeroVisiblePixels(png, slug);
        AssertMobHeroNoHardCreamSeams(png, slug);
    }

    [Fact]
    public void Culture_quick_scan_full_depth_shows_18_not_25()
        => Assert.Equal(18, TestDepthRules.FullCount(AssessmentKind.Culture));

    [Fact]
    public async Task Capture_test_pages_at_390_have_translated_eyebrows_and_filled_mobhero()
    {
        foreach (var target in Targets)
        {
            await using var web = new CaptureWebFactory(target);
            var baseUrl = web.EnsureServerAddress().TrimEnd('/');

            await using var context = await _browser!.NewContextAsync(new()
            {
                ViewportSize = new ViewportSize { Width = 390, Height = 844 },
                DeviceScaleFactor = 1,
                Locale = "nl-NL",
                IsMobile = true,
                HasTouch = true,
                IgnoreHTTPSErrors = true
            });
            await PlaywrightCookieConsent.AcceptAsync(context);
            await SignInViaHttpAsync(context, baseUrl, target.ReturnPath);

            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(90_000);
            await page.GotoAsync(baseUrl + target.ReturnPath, new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
            await PlaywrightCookieConsent.AcceptOnPageAsync(page);
            await page.Locator("[data-testid=test-block-stage]").WaitForAsync();

            var eyebrow = (await page.Locator(".test-shell__eyebrow").InnerTextAsync()).Trim();
            Assert.False(eyebrow.Contains("Culture.Title", StringComparison.Ordinal),
                $"{target.FileStem}: raw culture key in eyebrow: {eyebrow}");
            Assert.False(eyebrow.Contains("Values.Title", StringComparison.Ordinal),
                $"{target.FileStem}: raw values key in eyebrow: {eyebrow}");
            Assert.DoesNotMatch(new Regex(@"\b(Culture|Values)\.Title\b"), eyebrow);

            var tabs = page.Locator("[data-testid=test-depth-segments]");
            await tabs.WaitForAsync();
            var hero = page.Locator(".test-block-stage__mobile-hero--band").First;
            await hero.WaitForAsync();
            await page.WaitForFunctionAsync(
                """
                () => {
                  const h = document.querySelector('.test-block-stage__mobile-hero--band, .test-block-stage__mobile-hero--compact');
                  if (!h) return false;
                  const bg = getComputedStyle(h).backgroundImage || '';
                  return bg.includes('mobhero');
                }
                """,
                null,
                new() { Timeout = 30_000 });
            var tabsBox = await tabs.BoundingBoxAsync();
            var heroBox = await hero.BoundingBoxAsync();
            Assert.NotNull(tabsBox);
            Assert.NotNull(heroBox);
            Assert.True(tabsBox!.Y + tabsBox.Height <= heroBox!.Y + 2,
                $"{target.FileStem}: depth tabs must sit above the mobile hero.");

            var scrollWidth = await page.EvaluateAsync<double>("""
                () => {
                  const el = document.documentElement;
                  return Math.max(el.scrollWidth, document.body?.scrollWidth ?? 0);
                }
                """);
            Assert.InRange(scrollWidth, 389, 391);

            var stageWidth = await page.Locator("[data-testid=test-block-stage]").EvaluateAsync<double>(
                "el => el.getBoundingClientRect().width");
            Assert.InRange(stageWidth, 356, 362);

            var flowWidth = await page.Locator(".test-flow").First.EvaluateAsync<double>(
                "el => el.getBoundingClientRect().width");
            Assert.InRange(flowWidth, 322, 332);

            var barOverflow = await page.EvaluateAsync<bool>(""" 
                () => {
                  const card = document.querySelector('.test-shell__card');
                  const bar = document.querySelector('.test-flow__bar');
                  if (!card || !bar) return false;
                  const cs = getComputedStyle(card);
                  const padEnd = parseFloat(cs.paddingInlineEnd) || 0;
                  const contentRight = card.getBoundingClientRect().right - padEnd;
                  return bar.getBoundingClientRect().right <= contentRight + 1.5;
                }
                """);
            Assert.True(barOverflow, $"{target.FileStem}: progress bar must stay inside card padding.");

            var pageWidth = await page.Locator(".test-page").EvaluateAsync<double>(
                "el => el.getBoundingClientRect().width");
            Assert.InRange(pageWidth, 388, 392);

            await page.WaitForTimeoutAsync(400);

            var heroHeight = await hero.EvaluateAsync<double>("el => el.getBoundingClientRect().height");
            Assert.True(heroHeight is >= 196 and <= 212,
                $"{target.FileStem}: mobile hero band height drifted ({heroHeight:F1}px).");

            var png = await hero.ScreenshotAsync(new() { Type = ScreenshotType.Png });
            AssertMobHeroVisiblePixels(png, target.FileStem);
        }
    }

    private static void AssertMobHeroNoHardCreamSeams(byte[] png, string stem)
    {
        using var image = Image.Load<Rgba32>(png);
        var cream = new Rgba32(245, 242, 238, 255);
        var w = image.Width;
        var h = image.Height;
        var xStart = Math.Max(1, (int)(w * 0.22));
        var xLo = (int)(w * 0.22);
        var xHi = (int)(w * 0.92);
        var yStart = Math.Max(1, (int)(h * 0.08));

        static float Lum(Rgba32 p) => 0.299f * p.R + 0.587f * p.G + 0.114f * p.B;

        static bool NearCream(Rgba32 p, Rgba32 c, int tol = 22)
            => Math.Max(Math.Max(Math.Abs(p.R - c.R), Math.Abs(p.G - c.G)), Math.Abs(p.B - c.B)) <= tol;

        for (var x = xStart; x < w; x++)
        {
            var seam = 0;
            for (var y = 0; y < h; y++)
            {
                var a = image[x - 1, y];
                var b = image[x, y];
                if (Math.Abs(Lum(a) - Lum(b)) <= 8)
                {
                    continue;
                }

                if (NearCream(a, cream) && NearCream(b, cream))
                {
                    seam++;
                }
            }

            if (seam >= Math.Max(28, (int)(h * 0.22)))
            {
                Assert.Fail($"{stem}: vertical cream seam at x={x} ({seam} rows, ΔL>8).");
            }
        }

        for (var y = yStart; y < h; y++)
        {
            var seam = 0;
            for (var x = xLo; x < xHi; x++)
            {
                var a = image[x, y - 1];
                var b = image[x, y];
                if (Math.Abs(Lum(a) - Lum(b)) <= 8)
                {
                    continue;
                }

                if (NearCream(a, cream) && NearCream(b, cream))
                {
                    seam++;
                }
            }

            if (seam >= Math.Max(36, (int)((xHi - xLo) * 0.28)))
            {
                Assert.Fail($"{stem}: horizontal cream seam at y={y} ({seam} cols, ΔL>8).");
            }
        }
    }

    private static Rgba32 SampleMobHeroCreamReference(Image<Rgba32> image)
    {
        var w = image.Width;
        var h = image.Height;
        var anchor = image[Math.Min(8, w - 1), Math.Min(8, h - 1)];
        long r = 0;
        long g = 0;
        long b = 0;
        var count = 0;
        var leftEnd = Math.Max(4, (int)(w * 0.22));
        var sampleBottom = Math.Max(4, (int)(h * 0.45));
        for (var y = 0; y < sampleBottom; y++)
        {
            for (var x = 0; x < leftEnd; x++)
            {
                var p = image[x, y];
                if (p.A < 200)
                {
                    continue;
                }

                var delta = Math.Max(Math.Max(Math.Abs(p.R - anchor.R), Math.Abs(p.G - anchor.G)), Math.Abs(p.B - anchor.B));
                if (delta > 18)
                {
                    continue;
                }

                r += p.R;
                g += p.G;
                b += p.B;
                count++;
            }
        }

        if (count == 0)
        {
            return anchor;
        }

        return new Rgba32((byte)(r / count), (byte)(g / count), (byte)(b / count), 255);
    }

    private static void AssertMobHeroVisiblePixels(byte[] png, string stem)
    {
        using var image = Image.Load<Rgba32>(png);
        var w = image.Width;
        var h = image.Height;
        var refPx = SampleMobHeroCreamReference(image);
        var rightStart = (int)(w * 0.55);
        // Ignore the top band of the hero where pre-composited sky/clouds can match cream but figures sit lower.
        var rowStart = (int)(h * 0.28);
        var strong = 0;
        var total = 0;
        var channelSpreadSum = 0;
        for (var y = rowStart; y < h; y++)
        {
            for (var x = rightStart; x < w; x++)
            {
                var p = image[x, y];
                if (p.A < 200)
                {
                    continue;
                }

                total++;
                var delta = Math.Max(Math.Max(Math.Abs(p.R - refPx.R), Math.Abs(p.G - refPx.G)), Math.Abs(p.B - refPx.B));
                if (delta > 40)
                {
                    strong++;
                    channelSpreadSum += Math.Max(p.R, Math.Max(p.G, p.B)) - Math.Min(p.R, Math.Min(p.G, p.B));
                }
            }
        }

        Assert.True(total > 100, $"{stem}: hero screenshot too small to evaluate.");
        var ratio = (double)strong / total;
        Assert.True(ratio >= 0.25,
            $"{stem}: expected >=25% strong figure pixels in right 45% of mobhero (delta>40 vs cream), got {ratio:P1}.");

        if (strong > 0)
        {
            var avgSpread = channelSpreadSum / (double)strong;
            Assert.True(avgSpread >= 25,
                $"{stem}: figure area looks faded (avg channel spread {avgSpread:F1}); expected opaque illustration.");
        }
    }

    private static void PurgeLegacyArtifactDirs()
    {
        foreach (var dir in new[]
                 {
                     "/opt/cursor/artifacts/screenshots",
                     "/cursor/stores/self/test-blocks",
                     "/cursor/stores/self/insight",
                     Path.Combine(FindRepoRoot(), "artifacts/test-blocks")
                 })
        {
            if (!Directory.Exists(dir))
            {
                continue;
            }

            foreach (var f in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
            {
                try
                {
                    File.Delete(f);
                }
                catch
                {
                    // best effort
                }
            }
        }
    }

    private static async Task SignInViaHttpAsync(IBrowserContext context, string baseUrl, string returnPath)
    {
        var signInUri = new Uri($"{baseUrl}/__test/sign-in?return={Uri.EscapeDataString(returnPath)}");
        var jar = new CookieContainer();
        using var handler = new HttpClientHandler { CookieContainer = jar, AllowAutoRedirect = false };
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
        using var response = await http.GetAsync(signInUri, HttpCompletionOption.ResponseHeadersRead);
        Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.OK,
            $"Sign-in expected redirect, got {(int)response.StatusCode}");

        var baseUri = new Uri(baseUrl);
        var cookies = jar.GetCookies(baseUri).Cast<System.Net.Cookie>()
            .Select(c => new Microsoft.Playwright.Cookie
            {
                Name = c.Name,
                Value = c.Value,
                Domain = baseUri.Host,
                Path = string.IsNullOrEmpty(c.Path) ? "/" : c.Path,
                HttpOnly = c.HttpOnly,
                Secure = c.Secure,
                SameSite = SameSiteAttribute.Lax
            })
            .ToArray();
        await context.AddCookiesAsync(cookies);
    }

    private static async Task AssertInsightVisibleAsync(IPage page, int width)
    {
        if (width >= 1024)
        {
            var panel = page.Locator("[data-testid=test-progress-insight-panel]");
            await panel.WaitForAsync(new() { Timeout = 60_000 });
            var title = await panel.Locator(".test-progress-panel__title").InnerTextAsync();
            Assert.Contains("beeld", title, StringComparison.OrdinalIgnoreCase);
            var listCount = await panel.Locator(".test-progress-panel__list li").CountAsync();
            Assert.True(listCount >= 1, "Expected at least one insight line after one block.");
        }
        else
        {
            var mobile = page.Locator("[data-testid=test-progress-insight-mobile]");
            await mobile.WaitForAsync(new() { Timeout = 60_000 });
            var text = await mobile.InnerTextAsync();
            Assert.Contains("beeld", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static async Task AssertQualityAsync(IPage page, int width)
    {
        var insightText = width >= 1024
            ? await page.Locator("[data-testid=test-progress-insight-panel]").InnerTextAsync()
            : await page.Locator("[data-testid=test-progress-insight-mobile]").InnerTextAsync();
        Assert.False(Regex.IsMatch(insightText, @"\d|%"), $"Insight text must not contain digits or %: {insightText}");

        var cardBox = await page.Locator(".test-shell__card, .ob-wizard__card").First.BoundingBoxAsync();
        Assert.NotNull(cardBox);
        if (width < 1024)
        {
            var heroBox = await page.Locator(".test-block-stage__mobile-hero--band, .test-block-stage__mobile-hero--compact")
                .First.BoundingBoxAsync();
            Assert.NotNull(heroBox);
            Assert.True(heroBox!.Y + heroBox.Height <= cardBox!.Y + 2,
                "Question card must sit below the mobile hero band.");
            Assert.True(heroBox.Height <= 205,
                $"Mobile hero should stay near 200px tall, got {heroBox.Height}px.");
        }
        else
        {
            var bandBox = await page.Locator(".test-block-stage__scene-band").BoundingBoxAsync();
            Assert.NotNull(bandBox);
            Assert.True(cardBox!.Y + cardBox.Height <= bandBox!.Y + 2,
                "Question card must sit above the desktop scene band (not overlapping).");
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

    private sealed class CaptureWebFactory(CaptureTarget target) : WebWebApplicationFactory
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
                    ["CLOUDFLARE_ORIGIN_SECRET"] = "",
                    ["JobsyAuth:Jwt:PrivateKeyPem"] = Jobsy.Core.Security.JobsyAccessToken.DevelopmentPrivateKeyPem,
                    ["E2E:SkipHttpsRedirect"] = "true"
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IStartupFilter>(new CaptureSignInFilter());
                services.RemoveAll<IFeatureFlags>();
                services.AddSingleton<IFeatureFlags>(new CaptureStubFlags());
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton<IHttpClientFactory>(new CaptureStubFactory(target));
                services.RemoveAll<Jobsy.Web.Services.JobsyApiClient>();
                services.AddScoped(_ => new Jobsy.Web.Services.JobsyApiClient(
                    new HttpClient(new CaptureStubHandler(target), disposeHandler: false)
                    {
                        BaseAddress = new Uri("http://api.test/")
                    }));
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

    }

    private sealed class CaptureStubFlags : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(
                EmployersEnabled: false,
                CandidatePassportEnabled: true,
                TestsV2Enabled: true,
                TestItemExamplesEnabled: false,
                TestProgressInsightEnabled: true));

        public async ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => (await GetAsync(cancellationToken)).IsEnabled(feature);

        public void Invalidate()
        {
        }
    }

    private sealed class CaptureSignInFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            => app =>
            {
                app.Use(async (ctx, nxt) =>
                {
                    if (!ctx.Request.Path.Equals("/__test/sign-in", StringComparison.OrdinalIgnoreCase))
                    {
                        await nxt();
                        return;
                    }

                    var identity = new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                        new Claim(ClaimTypes.Role, "Candidate"),
                        new Claim(ClaimTypes.Name, "Testblokken Capture")
                    ],
                    CookieAuthenticationDefaults.AuthenticationScheme);
                    await ctx.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));
                    var ret = ctx.Request.Query["return"].ToString();
                    if (string.IsNullOrWhiteSpace(ret) || !ret.StartsWith('/'))
                    {
                        ret = "/candidate/competencies?stap=vraag";
                    }

                    ctx.Response.Redirect(ret);
                });
                next(app);
            };
    }

    private sealed class CaptureStubFactory(CaptureTarget target) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new CaptureStubHandler(target), disposeHandler: true) { BaseAddress = new Uri("http://api.test/") };
    }

    private sealed class CaptureStubHandler(CaptureTarget target) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            object payload = path switch
            {
                var p when p.Contains("/profile", StringComparison.OrdinalIgnoreCase) => BuildProfile(),
                var p when p.Contains("/test-ai-consent", StringComparison.OrdinalIgnoreCase) => BuildProfile(),
                var p when p.Contains("/competencies", StringComparison.OrdinalIgnoreCase) => BuildQuickScan(AssessmentKind.Competence, target),
                var p when p.Contains("/career-interests", StringComparison.OrdinalIgnoreCase) => BuildQuickScan(AssessmentKind.Career, target),
                var p when p.Contains("/culture", StringComparison.OrdinalIgnoreCase) => BuildQuickScan(AssessmentKind.Culture, target),
                var p when p.Contains("/values", StringComparison.OrdinalIgnoreCase) => BuildQuickScan(AssessmentKind.Values, target),
                var p when p.Contains("/deep-analysis/payment-mode", StringComparison.OrdinalIgnoreCase) => new { mode = "stub" },
                var p when p.Contains("/deep-analysis", StringComparison.OrdinalIgnoreCase) => BuildDeep(target),
                var p when p.Contains("/site/status", StringComparison.OrdinalIgnoreCase) => new { maintenance = false },
                _ => new { }
            };

            var json = JsonSerializer.Serialize(payload, Json);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }

        private static MeProfile BuildProfile()
            => new()
            {
                FullName = "Testblokken Capture",
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)),
                TestAiConsentAt = DateTime.UtcNow.AddDays(-1),
                TestAiConsentVersion = PrivacyConstants.CandidateProfilingConsentVersion
            };

        private static object BuildQuickScan(AssessmentKind kind, CaptureTarget target)
        {
            if (target.PaidDeep || target.Kind != kind)
            {
                return EmptyQuickScan(kind);
            }

            return kind switch
            {
                AssessmentKind.Career => new CandidateCareerInterestState
                {
                    Status = "Draft",
                    QuickScanVersion = AssessmentTestVersions.V3,
                    QuickScanFullCount = 25,
                    QuestionCount = 25,
                    Answers = BuildQuickScanAnswers(kind, target.Answered)
                },
                AssessmentKind.Culture => new CandidateCultureState
                {
                    Status = "Draft",
                    QuickScanVersion = AssessmentTestVersions.V3,
                    QuickScanFullCount = 25,
                    Answers = BuildQuickScanAnswers(kind, target.Answered)
                },
                AssessmentKind.Values => new CandidateValuesState
                {
                    Status = "Draft",
                    QuickScanVersion = AssessmentTestVersions.V3,
                    QuickScanFullCount = 25,
                    Answers = BuildQuickScanAnswers(kind, target.Answered)
                },
                _ => new CandidateCompetencyState
                {
                    Status = "Draft",
                    QuickScanVersion = AssessmentTestVersions.V3,
                    QuickScanFullCount = 25,
                    QuestionCount = 25,
                    IsIndicativeDepth = true,
                    Answers = BuildQuickScanAnswers(kind, target.Answered)
                }
            };
        }

        private static object EmptyQuickScan(AssessmentKind kind)
            => kind switch
            {
                AssessmentKind.Career => new CandidateCareerInterestState { Status = "Draft", QuickScanVersion = AssessmentTestVersions.V3, Answers = [] },
                AssessmentKind.Culture => new CandidateCultureState { Status = "Draft", QuickScanVersion = AssessmentTestVersions.V3, Answers = [] },
                AssessmentKind.Values => new CandidateValuesState { Status = "Draft", QuickScanVersion = AssessmentTestVersions.V3, Answers = [] },
                _ => new CandidateCompetencyState { Status = "Draft", QuickScanVersion = AssessmentTestVersions.V3, Answers = [] }
            };

        private static Dictionary<string, int> BuildQuickScanAnswers(AssessmentKind kind, int answeredCount)
        {
            var ids = TestDepthRules.QuestionIdsFor(
                    kind,
                    TestDepthLevel.Full,
                    AssessmentTestVersions.V3)
                .Take(Math.Clamp(answeredCount, 0, 25));
            return ids.ToDictionary(
                id => id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                id => 3 + (id % 2));
        }

        private static DeepAnalysisState BuildDeep(CaptureTarget target)
        {
            var answers = target.PaidDeep
                ? BuildDeepAnswers(target.Kind, target.Answered)
                : [];
            return new()
            {
                Kind = AssessmentKindLabels.ToSlug(target.Kind),
                IsUnlocked = target.PaidDeep,
                IsCompleted = false,
                QuestionCount = TestDepthRules.Bottom(target.Kind),
                PriceEuro = 2.99m,
                Answers = answers
            };
        }

        private static Dictionary<string, int> BuildDeepAnswers(AssessmentKind kind, int answeredCount)
        {
            var ids = TestDepthRules.Parts(kind)
                .SelectMany(p => p.QuestionIds)
                .Take(Math.Clamp(answeredCount, 0, TestDepthRules.Bottom(kind)));
            return ids.ToDictionary(
                id => id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                id => 3 + (id % 2));
        }
    }
}
