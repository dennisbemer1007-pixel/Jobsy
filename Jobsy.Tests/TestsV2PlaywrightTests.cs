using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
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

namespace Jobsy.Tests;

/// <summary>
/// Quick-Scan v2 UI contract: transparency block and version-aware test shells (desktop + mobile screenshots).
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class TestsV2PlaywrightTests : IAsyncLifetime
{
    private const string ScreenshotDir = "artifacts/tests-v2-playwright";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(Path.Combine(FindRepoRoot(), ScreenshotDir));
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
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
    }

    [Fact]
    public void Candidate_test_pages_are_quick_scan_version_aware()
    {
        var root = FindRepoRoot();
        foreach (var rel in new[]
                 {
                     "Jobsy.Web/Components/Pages/Candidate/CompetencyTest.razor",
                     "Jobsy.Web/Components/Pages/Candidate/CareerTest.razor",
                     "Jobsy.Web/Components/Pages/Candidate/CultureScan.razor",
                     "Jobsy.Web/Components/Pages/Candidate/ValuesScan.razor"
                 })
        {
            var text = File.ReadAllText(Path.Combine(root, rel));
            Assert.Contains("_quickScanVersion", text, StringComparison.Ordinal);
            Assert.Contains("AssessmentQuickScanCatalog", text, StringComparison.Ordinal);
        }

        var about = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Candidate/Tests/TestAboutPanel.razor"));
        Assert.Contains("data-testid=\"assessment-v2-transparency\"", about, StringComparison.Ordinal);
        var gate = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Candidate/Tests/TestConsentGate.razor"));
        Assert.Contains("TestAboutPanel", gate, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("desktop", 1440, 900, "nl-NL")]
    [InlineData("mobile", 390, 844, "nl-NL")]
    [InlineData("desktop", 1440, 900, "pl-PL")]
    [InlineData("mobile", 390, 844, "ar")]
    public async Task V2_competency_intro_shows_transparency_and_depth_pick(
        string name, int width, int height, string locale)
    {
        using var web = new TestsV2WebFactory(testsV2On: true);
        var baseUrl = web.EnsureServerAddress()
                      + "/__test/sign-in?return="
                      + Uri.EscapeDataString("/candidate/competencies?stap=intro");

        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            Locale = locale
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(45_000);
        await page.GotoAsync(baseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);

        var transparency = page.Locator("[data-testid=assessment-v2-transparency]");
        await transparency.WaitForAsync();
        Assert.Contains("Over deze test", await transparency.InnerTextAsync(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("(v2)", await transparency.InnerTextAsync(), StringComparison.OrdinalIgnoreCase);

        var overflow = await page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1");
        Assert.False(overflow);

        var segments = page.Locator("[data-testid=test-depth-segments]");
        await segments.WaitForAsync();

        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(FindRepoRoot(), ScreenshotDir, $"competency-v2-intro-{locale}-{name}.png"),
            FullPage = true
        });

        if (name == "desktop" && locale.StartsWith("nl", StringComparison.OrdinalIgnoreCase))
        {
            var footerOk = await page.EvaluateAsync<bool>(
                """
                () => {
                  const footer = document.querySelector('.app-footer');
                  const link = document.querySelector('.app-footer__links a');
                  if (!footer || !link) return false;
                  const lr = link.getBoundingClientRect();
                  return lr.left >= 12 && getComputedStyle(footer).position !== 'fixed';
                }
                """);
            Assert.True(footerOk);

            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(FindRepoRoot(), ScreenshotDir, "footer-short-page-desktop-1440.png"),
                FullPage = false
            });
        }

        await page.Locator(".test-shell__footer button.btn-primary").ClickAsync();
        await page.Locator(".test-shell[data-mode=Question]").WaitForAsync(new() { Timeout = 45_000 });
        await segments.WaitForAsync();

        var body = await page.ContentAsync();
        Assert.DoesNotContain("AssessmentV2.Competency.Q", body, StringComparison.Ordinal);
        if (name == "desktop" && locale.StartsWith("nl", StringComparison.OrdinalIgnoreCase))
        {
            var vraagUrl = web.EnsureServerAddress()
                             + "/__test/sign-in?return="
                             + Uri.EscapeDataString("/candidate/competencies?stap=vraag");
            await page.GotoAsync(vraagUrl, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });
            await page.Locator("[data-testid=test-earlier-answers]").WaitForAsync();
            var summaryText = await page.Locator("[data-testid=test-earlier-answers] summary").InnerTextAsync();
            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(FindRepoRoot(), ScreenshotDir, "earlier-answers-collapsed-desktop-1440.png"),
                FullPage = true
            });
            await page.Locator("[data-testid=test-earlier-answers] summary").ClickAsync();
            await page.Locator("details.test-flow__earlier[open]").WaitForAsync();
            Assert.Contains("(18)", summaryText, StringComparison.Ordinal);
            var itemCount = await page.Locator("details.test-flow__earlier li").CountAsync();
            Assert.Equal(18, itemCount);
            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(FindRepoRoot(), ScreenshotDir, "earlier-answers-open-desktop-1440.png"),
                FullPage = true
            });
        }

        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(FindRepoRoot(), ScreenshotDir, $"competency-v2-question-{locale}-{name}.png"),
            FullPage = true
        });

        if (name == "mobile" && locale.StartsWith("nl", StringComparison.OrdinalIgnoreCase))
        {
            await page.Locator("[data-testid=test-earlier-answers]").WaitForAsync();
            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(FindRepoRoot(), ScreenshotDir, "earlier-answers-collapsed-mobile-390.png"),
                FullPage = true
            });
            await page.Locator("[data-testid=test-earlier-answers] summary").ClickAsync();
            await page.Locator("details.test-flow__earlier[open]").WaitForAsync();
            var mobileSummary = await page.Locator("[data-testid=test-earlier-answers] summary").InnerTextAsync();
            Assert.Contains("(18)", mobileSummary, StringComparison.Ordinal);
            Assert.Equal(18, await page.Locator("details.test-flow__earlier li").CountAsync());
            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(FindRepoRoot(), ScreenshotDir, "earlier-answers-open-mobile-390.png"),
                FullPage = true
            });
        }

    }

    [Fact]
<<<<<<< HEAD
    public async Task Intro_header_fits_360_viewport()
    {
        using var web = new TestsV2WebFactory(testsV2On: true);
        var url = web.EnsureServerAddress()
                  + "/__test/sign-in?return="
                  + Uri.EscapeDataString("/candidate/competencies?stap=intro");

        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 360, Height = 740 },
=======
    public async Task Item_example_visible_when_flag_on()
    {
        using var web = new TestsV2WebFactory(testsV2On: true, itemExamplesOn: true);
        var baseUrl = web.EnsureServerAddress()
                      + "/__test/sign-in?return="
                      + Uri.EscapeDataString("/candidate/competencies?stap=vraag");

        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 },
>>>>>>> a44350328 (Add TestItemExamples flag, central catalog, and Quick-Scan UI wiring.)
            Locale = "nl-NL"
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
<<<<<<< HEAD
        await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.Locator(".test-page").WaitForAsync();
        var overflow = await page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1");
        Assert.False(overflow);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(FindRepoRoot(), ScreenshotDir, "competence-intro-mobile-360.png"),
=======
        await page.GotoAsync(baseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.Locator("details.test-example").WaitForAsync();
        var text = await page.Locator("details.test-example p").First.InnerTextAsync();
        Assert.Contains("Voorbeeld", text, StringComparison.OrdinalIgnoreCase);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(FindRepoRoot(), ScreenshotDir, "item-example-competence-desktop-1440.png"),
>>>>>>> a44350328 (Add TestItemExamples flag, central catalog, and Quick-Scan UI wiring.)
            FullPage = true
        });
    }

    [Fact]
    public async Task Footer_screenshots_short_mobile_and_long_desktop()
    {
        using var web = new TestsV2WebFactory(testsV2On: true);
        var baseUrl = web.EnsureServerAddress();
        var shortCandidate = baseUrl
                             + "/__test/sign-in?return="
                             + Uri.EscapeDataString("/candidate/competencies?stap=intro");
        var longCandidate = baseUrl
                            + "/__test/sign-in?return="
                            + Uri.EscapeDataString("/candidate/hoe-werkt-lobsy");

        await using var mobile = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 390, Height = 844 },
            Locale = "nl-NL"
        });
        await PlaywrightCookieConsent.AcceptAsync(mobile);
        var mPage = await mobile.NewPageAsync();
        await mPage.GotoAsync(shortCandidate, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(mPage);
        await mPage.Locator(".test-page").WaitForAsync();
        // Mobile candidate shell hides .app-footer while bottom nav is visible; unwrap for layout evidence.
        await mPage.EvaluateAsync(
            """
            () => {
              document.querySelector('.app-shell')?.classList.remove('has-bottom-nav');
              document.querySelector('.bottom-nav')?.remove();
            }
            """);
        await mPage.Locator(".app-footer").WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 30_000
        });
        await mPage.ScreenshotAsync(new()
        {
            Path = Path.Combine(FindRepoRoot(), ScreenshotDir, "footer-short-page-mobile-390.png"),
            FullPage = false
        });

        await using var desktop = await _browser.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 },
            Locale = "nl-NL"
        });
        await PlaywrightCookieConsent.AcceptAsync(desktop);
        var dPage = await desktop.NewPageAsync();
        await dPage.GotoAsync(longCandidate, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(dPage);
        await dPage.Locator(".app-footer").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await dPage.ScreenshotAsync(new()
        {
            Path = Path.Combine(FindRepoRoot(), ScreenshotDir, "footer-long-page-desktop-1440.png"),
            FullPage = true
        });
    }

    [Theory]
    [InlineData("/candidate/competencies?stap=intro", "competence")]
    [InlineData("/candidate/career?stap=intro", "career")]
    [InlineData("/candidate/culture?stap=intro", "culture")]
    [InlineData("/candidate/values?stap=intro", "values")]
    public async Task V2_test_intro_shared_shell(string path, string slug)
    {
        using var web = new TestsV2WebFactory(testsV2On: true);
        var baseUrl = web.EnsureServerAddress()
                      + "/__test/sign-in?return="
                      + Uri.EscapeDataString(path);

        await using var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 1440, Height = 900 },
            Locale = "nl-NL"
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        await page.GotoAsync(baseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);

        await page.Locator("[data-testid=test-depth-segments]").WaitForAsync();
        await page.Locator(".test-page").WaitForAsync();

        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(FindRepoRoot(), ScreenshotDir, $"{slug}-intro-desktop-1440.png"),
            FullPage = true
        });

        await using var mobile = await _browser.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = 390, Height = 844 },
            Locale = "nl-NL"
        });
        await PlaywrightCookieConsent.AcceptAsync(mobile);
        var mobilePage = await mobile.NewPageAsync();
        await mobilePage.GotoAsync(baseUrl, new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(mobilePage);
        await mobilePage.Locator("[data-testid=test-depth-segments]").WaitForAsync();
        await mobilePage.ScreenshotAsync(new()
        {
            Path = Path.Combine(FindRepoRoot(), ScreenshotDir, $"{slug}-intro-mobile-390.png"),
            FullPage = true
        });
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

    private sealed class TestsV2WebFactory : WebWebApplicationFactory
    {
        private IHost? _kestrel;
        private readonly bool _testsV2On;
        private readonly bool _itemExamplesOn;

        public string ServerAddress { get; private set; } = "";

        public TestsV2WebFactory(bool testsV2On, bool itemExamplesOn = false)
        {
            _testsV2On = testsV2On;
            _itemExamplesOn = itemExamplesOn;
        }

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
                    ["JobsyAuth:Jwt:PrivateKeyPem"] = Jobsy.Core.Security.JobsyAccessToken.DevelopmentPrivateKeyPem
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IStartupFilter>(new SignInFilter());
                services.RemoveAll<IFeatureFlags>();
                services.AddSingleton<IFeatureFlags>(new StubFlags(_testsV2On, _itemExamplesOn));
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton<IHttpClientFactory>(new StubFactory());
                services.RemoveAll<Jobsy.Web.Services.JobsyApiClient>();
                services.AddScoped(_ => new Jobsy.Web.Services.JobsyApiClient(
                    new HttpClient(new StubHandler(), disposeHandler: false)
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

    private sealed class StubFlags(bool testsV2On, bool itemExamplesOn) : IFeatureFlags
    {
        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new FeatureFlagSnapshot(
                EmployersEnabled: false,
                CandidatePassportEnabled: true,
                TestsV2Enabled: testsV2On,
                TestItemExamplesEnabled: itemExamplesOn));

        public async ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => (await GetAsync(cancellationToken)).IsEnabled(feature);

        public void Invalidate()
        {
        }
    }

    private sealed class SignInFilter : IStartupFilter
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
                        new Claim(ClaimTypes.Name, "Tests V2 Demo")
                    ],
                    CookieAuthenticationDefaults.AuthenticationScheme);
                    await ctx.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));
                    var ret = ctx.Request.Query["return"].ToString();
                    if (string.IsNullOrWhiteSpace(ret) || !ret.StartsWith('/'))
                    {
                        ret = "/candidate/competencies?stap=intro";
                    }

                    ctx.Response.Redirect(ret);
                });
                next(app);
            };
    }

    private sealed class StubFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new StubHandler(), disposeHandler: true) { BaseAddress = new Uri("http://api.test/") };
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            object payload = path switch
            {
                var p when p.Contains("/profile", StringComparison.OrdinalIgnoreCase) => BuildProfile(),
                var p when p.Contains("/competencies", StringComparison.OrdinalIgnoreCase) => BuildCompetency(),
                var p when p.Contains("/career-interests", StringComparison.OrdinalIgnoreCase) => BuildCareer(),
                var p when p.Contains("/culture", StringComparison.OrdinalIgnoreCase) => BuildCulture(),
                var p when p.Contains("/values", StringComparison.OrdinalIgnoreCase) => BuildValues(),
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
                FullName = "Tests V2 Demo",
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)),
                TestAiConsentAt = DateTime.UtcNow.AddDays(-1),
                TestAiConsentVersion = PrivacyConstants.CandidateProfilingConsentVersion
            };

        private static CandidateCompetencyState BuildCompetency()
        {
            var ids = TestDepthRules.QuestionIdsFor(
                    AssessmentKind.Competence,
                    TestDepthLevel.Full,
                    AssessmentTestVersions.V3)
                .Take(18)
                .ToList();
            return new()
            {
                Status = "Draft",
                QuickScanVersion = AssessmentTestVersions.V3,
                QuickScanFullCount = 25,
                QuestionCount = 25,
                IsIndicativeDepth = true,
                Answers = ids.ToDictionary(
                    id => id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    id => 3 + (id % 2))
            };
        }

        private static CandidateCareerInterestState BuildCareer()
            => new()
            {
                Status = "Draft",
                QuickScanVersion = AssessmentTestVersions.V3,
                QuickScanFullCount = 25,
                QuestionCount = 25,
                Answers = new Dictionary<string, int>()
            };

        private static CandidateCultureState BuildCulture()
            => new()
            {
                Status = "Draft",
                QuickScanVersion = AssessmentTestVersions.V3,
                QuickScanFullCount = 25,
                Answers = new Dictionary<string, int>()
            };

        private static CandidateValuesState BuildValues()
            => new()
            {
                Status = "Draft",
                QuickScanVersion = AssessmentTestVersions.V3,
                QuickScanFullCount = 25,
                Answers = new Dictionary<string, int>()
            };
    }
}
