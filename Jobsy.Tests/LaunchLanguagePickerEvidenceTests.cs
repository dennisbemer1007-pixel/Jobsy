using System.Net;
using System.Security.Claims;
using Jobsy.Core.Authorization;
using JobsyClaimTypes = Jobsy.Core.Authorization.JobsyClaimTypes;
using Jobsy.Core.Features;
using Jobsy.Tests.E2e;
using Jobsy.Tests.TestSupport;
using Jobsy.Web.Hosting;
using Jobsy.Web.Localization;
using Jobsy.Web.Services;
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

[CollectionDefinition("LaunchLangEvidenceSerial", DisableParallelization = true)]
public sealed class LaunchLangEvidenceSerialCollection;

[Collection("LaunchLangEvidenceSerial")]
public sealed class LaunchLanguagePickerEvidenceTests : IAsyncLifetime
{
    public const string ArtifactDir = "/opt/cursor/artifacts";

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private LaunchLangEvidenceWebFactory? _webFactory;

    public async ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(ArtifactDir);
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
        if (_webFactory is not null)
        {
            await _webFactory.DisposeAsync();
            _webFactory = null;
        }
    }

    [Fact]
    public async Task Capture_launch_language_evidence_jpegs()
    {
        var baseUrl = ResolveBaseUrl();
        Assert.False(string.IsNullOrWhiteSpace(baseUrl));

        await CapturePublicHeaderAsync(baseUrl, 390, 844, "launch-lang-public-header-mobile-390.jpg");
        await CapturePublicHeaderAsync(baseUrl, 1440, 900, "launch-lang-public-header-desktop-1440.jpg");
        await CapturePublicFooterLinksAsync(baseUrl, 390, 844, "launch-lang-public-footer-mobile-390.jpg");
        await CapturePublicFooterLinksAsync(baseUrl, 1440, 900, "launch-lang-public-footer-desktop-1440.jpg");
        await CaptureCandidateHeaderAsync(baseUrl, 390, 844, "launch-lang-candidate-header-mobile-390.jpg");
        await CaptureCandidateHeaderAsync(baseUrl, 1440, 900, "launch-lang-candidate-header-desktop-1440.jpg");
        await CaptureEmployerTopBarAsync(baseUrl, 390, 844, "launch-lang-employer-wgtopbar-mobile-390.jpg");
        await CaptureEmployerTopBarAsync(baseUrl, 1440, 900, "launch-lang-employer-wgtopbar-desktop-1440.jpg");
        await CaptureFallbackCookiePlAsync(baseUrl, "launch-lang-fallback-cookie-pl-nl.jpg");
        await CaptureFallbackCookieArAcceptEnAsync(baseUrl, "launch-lang-fallback-cookie-ar-accept-en.jpg");

        foreach (var file in Directory.GetFiles(ArtifactDir, "launch-lang-*.jpg"))
        {
            Assert.True(new FileInfo(file).Length > 10_000, $"Evidence too small: {Path.GetFileName(file)}");
        }
    }

    private string ResolveBaseUrl()
    {
        var external = (Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL") ?? "").Trim().TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(external))
        {
            return external;
        }

        _webFactory ??= new LaunchLangEvidenceWebFactory();
        return _webFactory.EnsureServerAddress();
    }

    private async Task CapturePublicHeaderAsync(string baseUrl, int width, int height, string fileName)
    {
        await using var session = await OpenPageAsync(width, height);
        var page = session.Page;
        await page.GotoAsync(baseUrl + "/", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        await page.EvaluateAsync("() => window.scrollTo(0, 0)");
        var header = page.Locator("header.pub-header").First;
        await header.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        var lang = await ResolvePublicLangMenuAsync(page, width);
        await OpenPublicLangMenuAsync(lang);
        await AssertOnlyNlEnPubOptionsAsync(lang);
        await ScreenshotHeaderWithLangMenuAsync(
            page,
            header,
            fileName,
            extraBottomPx: width < 900 ? 480 : 180);
    }

    private async Task CapturePublicFooterLinksAsync(string baseUrl, int width, int height, string fileName)
    {
        await using var session = await OpenPageAsync(width, height);
        var page = session.Page;
        await page.GotoAsync(
            baseUrl + "/?_variant=zw",
            new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        var footerLink = page.Locator("footer.pub-footer a.pub-footer__link[href='#pub-lang']").First;
        await footerLink.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await footerLink.ScrollIntoViewIfNeededAsync();
        await OpenPublicLangFromFooterLinkAsync(page, width);
        var lang = await ResolvePublicLangMenuAsync(page, width);
        await lang.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30_000 });
        await AssertOnlyNlEnPubOptionsAsync(lang);
        await footerLink.ScrollIntoViewIfNeededAsync();
        var footer = page.Locator("footer.pub-footer").First;
        await footer.ScreenshotAsync(LocatorJpeg(fileName));
        await lang.Locator(".pub-lang__menu .pub-lang__option").First.WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5_000
        });
    }

    private async Task CaptureCandidateHeaderAsync(string baseUrl, int width, int height, string fileName)
    {
        await using var session = await OpenPageAsync(width, height);
        var page = session.Page;
        await SignInCandidateAsync(page, baseUrl);
        await page.GotoAsync(
            baseUrl + "/account/mail-instellingen",
            new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        var header = page.Locator("header.app-header").First;
        await header.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 120_000 });
        await OpenLanguageSelectorAsync(page);
        await AssertOnlyNlEnSelectorOptionsAsync(page);
        await ScreenshotHeaderWithLangMenuAsync(page, header, fileName);
    }

    private async Task CaptureEmployerTopBarAsync(string baseUrl, int width, int height, string fileName)
    {
        await using var session = await OpenPageAsync(width, height);
        var page = session.Page;
        if (!await TryRealEmployerLoginAsync(page, baseUrl))
        {
            await page.GotoAsync(
                baseUrl + "/__test/sign-in?role=EnterpriseManager&email=ondernemer@jobsy.local&return="
                          + Uri.EscapeDataString("/werkgever"),
                new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 120_000 });
        }
        else
        {
            await page.GotoAsync(
                baseUrl + "/werkgever",
                new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 120_000 });
        }

        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        Assert.Contains("/werkgever", page.Url, StringComparison.OrdinalIgnoreCase);
        await WaitForBlazorShellAsync(page);
        var topbar = page.Locator("header.wg-topbar").First;
        await topbar.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 120_000 });
        await EnsureStylesheetAsync(page, "werkgever.css");
        if (width < 1024)
        {
            await page.EvaluateAsync("""
                () => {
                  var sel = document.querySelector('header.wg-topbar .language-selector');
                  if (sel) sel.style.display = 'block';
                }
                """);
        }

        await OpenLanguageSelectorAsync(page);
        await AssertOnlyNlEnSelectorOptionsAsync(page);
        await ScreenshotViewportTopAsync(page, width, fileName, chromeHeight: width < 1024 ? 260 : 220);
    }

    private async Task CaptureFallbackCookiePlAsync(string baseUrl, string fileName)
    {
        await using var session = await OpenPageAsync(1440, 900);
        var page = session.Page;
        await SetCultureCookieAsync(session.Context, baseUrl, "pl");
        await page.GotoAsync(baseUrl + "/", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        var html = await page.ContentAsync();
        Assert.DoesNotContain("dir=\"rtl\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("lang=\"nl\"", html, StringComparison.OrdinalIgnoreCase);
        var header = page.Locator("header.pub-header").First;
        await header.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await header.ScreenshotAsync(LocatorJpeg(fileName));
    }

    private async Task CaptureFallbackCookieArAcceptEnAsync(string baseUrl, string fileName)
    {
        await using var session = await OpenPageAsync(1440, 900, extraHeaders: new Dictionary<string, string>
        {
            ["Accept-Language"] = "en-US,en;q=0.9,ar;q=0.8"
        });
        var page = session.Page;
        await SetCultureCookieAsync(session.Context, baseUrl, "ar");
        await page.GotoAsync(baseUrl + "/", new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
        await PlaywrightCookieConsent.AcceptOnPageAsync(page);
        var html = await page.ContentAsync();
        Assert.DoesNotContain("dir=\"rtl\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("lang=\"en\"", html, StringComparison.OrdinalIgnoreCase);
        var header = page.Locator("header.pub-header").First;
        await header.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await header.ScreenshotAsync(LocatorJpeg(fileName));
    }

    private const string SeedDemoPassword = "Jobsy123!";

    private static async Task SignInCandidateAsync(IPage page, string baseUrl)
    {
        if (await KbE2e.TryLoginAsync(page, baseUrl, "kandidaat@jobsy.local", SeedDemoPassword))
        {
            return;
        }

        await page.GotoAsync(
            baseUrl + "/__test/sign-in?role=Candidate&email=kandidaat@jobsy.local&return="
                      + Uri.EscapeDataString("/account/mail-instellingen"),
            new() { WaitUntil = WaitUntilState.Load, Timeout = 120_000 });
    }

    private static async Task<bool> TryRealEmployerLoginAsync(IPage page, string baseUrl)
    {
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("JOBSY_E2E_BASE_URL")))
        {
            return await KbE2e.TryLoginAsync(page, baseUrl, "ondernemer@jobsy.local", SeedDemoPassword);
        }

        return false;
    }

    private static async Task EnsureStylesheetAsync(IPage page, string hrefFragment)
    {
        await page.EvaluateAsync(
            """
            (fragment) => {
              if (document.querySelector('link[href*="' + fragment + '"]')) return;
              const link = document.createElement('link');
              link.rel = 'stylesheet';
              link.href = '/css/features/' + fragment + '?v=evidence';
              document.head.appendChild(link);
            }
            """,
            hrefFragment);
        await page.WaitForFunctionAsync(
            """
            (fragment) => {
              const bar = document.querySelector('header.wg-topbar');
              if (!bar) return false;
              const styles = getComputedStyle(bar);
              return styles.display === 'flex';
            }
            """,
            hrefFragment,
            new() { Timeout = 15_000 });
    }

    private static async Task WaitForBlazorShellAsync(IPage page)
    {
        await page.WaitForFunctionAsync(
            "() => !!(window.Blazor && window.Blazor._internal)",
            null,
            new() { Timeout = 90_000 });
        await page.WaitForFunctionAsync(
            """
            () => {
              const sk = document.querySelector('.page-content-skeleton, [aria-busy="true"]');
              const chrome = document.querySelector('header.wg-topbar, header.app-header');
              return !!chrome && !sk;
            }
            """,
            null,
            new() { Timeout = 90_000 });
    }

    private static async Task OpenLanguageSelectorAsync(IPage page)
    {
        await page.EvaluateAsync("""
            () => {
              document.documentElement.setAttribute('data-header-menu', 'language');
              document.querySelectorAll('[data-menu-id]').forEach(function (root) {
                var id = root.getAttribute('data-menu-id');
                var on = id === 'language';
                root.classList.toggle('is-open', on);
                root.classList.toggle('is-closed', !on);
                var trigger = root.querySelector('[data-menu-trigger]');
                if (trigger) trigger.setAttribute('aria-expanded', on ? 'true' : 'false');
              });
            }
            """);
        await page.Locator(".language-selector__menu").First.WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 30_000
        });
    }

    private static async Task<ILocator> ResolvePublicLangMenuAsync(IPage page, int width)
    {
        if (width >= 900)
        {
            return page.Locator("header.pub-header .pub-header__actions details.pub-lang").First;
        }

        await OpenPublicHamburgerAsync(page);
        return page.Locator("header.pub-header .pub-header__mobile .pub-menu__panel details.pub-lang").First;
    }

    private static async Task OpenPublicLangFromFooterLinkAsync(IPage page, int width)
    {
        if (width >= 900)
        {
            await page.Locator("header.pub-header .pub-header__actions details.pub-lang").First
                .EvaluateAsync("el => el.setAttribute('open', '')");
            return;
        }

        await OpenPublicHamburgerAsync(page);
        await page.Locator("header.pub-header .pub-header__mobile .pub-menu__panel details.pub-lang").First
            .EvaluateAsync("el => el.setAttribute('open', '')");
    }

    private static async Task OpenPublicHamburgerAsync(IPage page)
    {
        var menu = page.Locator("header.pub-header .pub-header__mobile details.pub-menu").First;
        if (await menu.CountAsync() == 0)
        {
            return;
        }

        await menu.EvaluateAsync("el => el.setAttribute('open', '')");
        await menu.Locator(".pub-menu__panel").WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 30_000
        });
    }

    private static async Task OpenPublicLangMenuAsync(ILocator details)
    {
        await details.WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 30_000 });
        await details.EvaluateAsync("el => el.setAttribute('open', '')");
        await details.Locator(".pub-lang__menu .pub-lang__option").First.WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 45_000
        });
    }

    private static async Task ScreenshotViewportTopAsync(
        IPage page,
        int viewportWidth,
        string fileName,
        int chromeHeight)
    {
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(ArtifactDir, fileName),
            Type = ScreenshotType.Jpeg,
            Quality = 92,
            Clip = new Clip
            {
                X = 0,
                Y = 0,
                Width = viewportWidth,
                Height = chromeHeight
            }
        });
    }

    private static async Task ScreenshotHeaderWithLangMenuAsync(
        IPage page,
        ILocator header,
        string fileName,
        int extraBottomPx = 180)
    {
        var box = await header.BoundingBoxAsync();
        Assert.NotNull(box);
        await page.ScreenshotAsync(new()
        {
            Path = Path.Combine(ArtifactDir, fileName),
            Type = ScreenshotType.Jpeg,
            Quality = 92,
            Clip = new Clip
            {
                X = Math.Max(0, box.X),
                Y = Math.Max(0, box.Y),
                Width = box.Width,
                Height = box.Height + extraBottomPx
            }
        });
    }

    private static async Task AssertOnlyNlEnSelectorOptionsAsync(IPage page)
    {
        var options = page.Locator(".language-selector__menu .language-selector__option");
        Assert.Equal(2, await options.CountAsync());
        var text = string.Join("|", await options.AllInnerTextsAsync());
        Assert.Contains("Nederlands", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("English", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Polski", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Română", text, StringComparison.Ordinal);
        Assert.DoesNotContain("العربية", text, StringComparison.Ordinal);
    }

    private static async Task AssertOnlyNlEnPubOptionsAsync(ILocator details)
    {
        var options = details.Locator(".pub-lang__menu .pub-lang__option");
        Assert.Equal(2, await options.CountAsync());
        var text = string.Join("|", await options.AllInnerTextsAsync());
        Assert.Contains("Nederlands", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("English", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Polski", text, StringComparison.OrdinalIgnoreCase);
    }

    private static LocatorScreenshotOptions LocatorJpeg(string fileName) => new()
    {
        Path = Path.Combine(ArtifactDir, fileName),
        Type = ScreenshotType.Jpeg,
        Quality = 92
    };

    private static async Task SetCultureCookieAsync(IBrowserContext context, string baseUrl, string lang)
    {
        var uri = new Uri(baseUrl);
        await context.AddCookiesAsync(
        [
            new Microsoft.Playwright.Cookie
            {
                Name = CultureState.CookieName,
                Value = lang,
                Url = baseUrl.TrimEnd('/') + "/"
            }
        ]);
    }

    private async Task<PageSession> OpenPageAsync(
        int width,
        int height,
        IReadOnlyDictionary<string, string>? extraHeaders = null)
    {
        var context = await _browser!.NewContextAsync(new()
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            Locale = "nl-NL",
            ExtraHTTPHeaders = extraHeaders
        });
        await PlaywrightCookieConsent.AcceptAsync(context);
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(60_000);
        return new PageSession(context, page);
    }

    private sealed class PageSession(IBrowserContext context, IPage page) : IAsyncDisposable
    {
        public IBrowserContext Context => context;
        public IPage Page => page;

        public async ValueTask DisposeAsync() => await context.DisposeAsync();
    }

    internal sealed class LaunchLangEvidenceWebFactory : WebWebApplicationFactory
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
                    ["Localization:EnabledCultures"] = "nl,en",
                    ["Seed:Enabled"] = "true"
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IStartupFilter>(new TestSignInFilter());
                services.RemoveAll<IVacancyMapApiForwarder>();
                services.AddSingleton<IVacancyMapApiForwarder>(_ => new NoopForwarder());
                services.RemoveAll<IFeatureFlags>();
                services.AddSingleton<IFeatureFlags>(new AlwaysOnFeatureFlags());
                services.Configure<HostOptions>(options =>
                {
                    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
                });
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton<IHttpClientFactory>(_ => new EvidenceHttpClientFactory());
                services.RemoveAll<JobsyApiClient>();
                services.AddScoped(sp => new JobsyApiClient(
                    sp.GetRequiredService<IHttpClientFactory>().CreateClient("JobsyApiEvidence"),
                    sp.GetRequiredService<MeGetCache>()));
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

    private sealed class NoopForwarder : IVacancyMapApiForwarder
    {
        public Task ForwardAsync(HttpContext http, string apiPath, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class EvidenceHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new EvidenceApiHandler()) { BaseAddress = new Uri("http://api.test/") };
    }

    private sealed class EvidenceApiHandler : HttpMessageHandler
    {
        private static readonly Guid OrgId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath.TrimEnd('/') ?? "";
            string json = path switch
            {
                "/api/site/status" => """{"maintenance":false}""",
                "/api/settings/feature-flags" => """{"employersEnabled":true,"candidatePassportEnabled":true,"schoolsEnabled":false}""",
                "/api/companies/mine" => $$"""[{"id":"{{OrgId}}","name":"Horeca Westland","parentCompanyId":null,"branchLabel":null}]""",
                "/api/me" => """{"email":"ondernemer@jobsy.local","fullName":"Demo Ondernemer","role":"EnterpriseManager","preferences":{"language":"nl"}}""",
                _ when path.StartsWith("/api/werkgever/dashboard", StringComparison.Ordinal) =>
                    """{"vacancyCount":2,"applicationCount":0,"tokenBalance":12,"activeVacancies":2,"newApplications":0}""",
                _ when path.StartsWith("/api/werkgever/todos", StringComparison.Ordinal) => "[]",
                _ when path.StartsWith("/api/werkgever/counts", StringComparison.Ordinal) =>
                    """{"applications":0,"vacancies":2,"publishQueue":0}""",
                _ when path.StartsWith("/api/companies/", StringComparison.Ordinal) && path.Contains("token", StringComparison.Ordinal) =>
                    """{"balance":12}""",
                _ when path.StartsWith("/api/regions", StringComparison.Ordinal) => "[]",
                _ when path.StartsWith("/api/notifications", StringComparison.Ordinal) => "[]",
                _ => "{}"
            };

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class TestSignInFilter : IStartupFilter
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

                    var role = ctx.Request.Query["role"].ToString();
                    if (string.IsNullOrWhiteSpace(role))
                    {
                        role = JobsyRoles.Candidate;
                    }

                    var email = ctx.Request.Query["email"].ToString();
                    if (string.IsNullOrWhiteSpace(email))
                    {
                        email = "evidence@jobsy.local";
                    }

                    var identity = new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                        new Claim(ClaimTypes.Role, role),
                        new Claim(ClaimTypes.Email, email),
                        new Claim(ClaimTypes.Name, email.Split('@')[0]),
                        new Claim(JobsyClaimTypes.MfaVerified, "1"),
                        new Claim(JobsyClaimTypes.SessionVersion, "0")
                    ],
                    CookieAuthenticationDefaults.AuthenticationScheme);
                    await ctx.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));
                    var ret = ctx.Request.Query["return"].ToString();
                    if (string.IsNullOrWhiteSpace(ret) || !ret.StartsWith('/'))
                    {
                        ret = "/";
                    }

                    ctx.Response.Redirect(ret);
                });
                next(app);
            };
    }
}
