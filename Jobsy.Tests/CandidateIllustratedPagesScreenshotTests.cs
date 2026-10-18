using Jobsy.Core.Features;
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
using System.Security.Claims;

namespace Jobsy.Tests;

[Collection("PlaywrightSmoke")]
public sealed class CandidateIllustratedPagesScreenshotTests : IAsyncLifetime
{
    private const string ArtifactsDirR2 = "/opt/cursor/artifacts/candidate-illustrated-r2";
    private const string ArtifactsDirRole = "/opt/cursor/artifacts/kandidaat-rol-r1";
    private const string ArtifactsDirR5 = "/opt/cursor/artifacts/kandidaat-728-r5";
    private const string ArtifactsDirR6 = "/opt/cursor/artifacts/kandidaat-728-r6";
    private const string ArtifactsDirR7 = "/opt/cursor/artifacts/kandidaat-728-r7";
    private const string ArtifactsDirR8 = "/opt/cursor/artifacts/kandidaat-728-r8";
    private static readonly Guid SampleVacancyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid PhotoVacancyId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private IllustratedPagesWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "true");
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
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

    [Fact]
    public async Task Capture_four_public_pages_and_banenkaart_popups_r2()
    {
        Directory.CreateDirectory(ArtifactsDirR2);
        await CapturePageAsync(ArtifactsDirR2, "/login", "login", auth: false);
        await CapturePageAsync(ArtifactsDirR2, "/", "start", auth: false);
        await CapturePageAsync(ArtifactsDirR2, "/candidate/paspoort", "kompas", auth: true);
        await CapturePageAsync(ArtifactsDirR2, "/banenkaart?weergave=kaart", "banenkaart", auth: true);
        await CaptureBanenkaartPopupsAsync();
    }

    [Fact]
    public async Task Capture_eight_candidate_pages_at_1440_390_and_1024()
    {
        Directory.CreateDirectory(ArtifactsDirRole);
        await CapturePageAsync(ArtifactsDirRole, "/vacancies/" + SampleVacancyId, "vacature-no-photo", auth: true);
        await CapturePageAsync(ArtifactsDirRole, "/vacancies/" + PhotoVacancyId, "vacature-photo", auth: true);
        await CapturePageAsync(ArtifactsDirRole, "/candidate/match", "match", auth: true);
        await CapturePageAsync(ArtifactsDirRole, "/candidate/applications", "sollicitaties", auth: true);
        await CapturePageAsync(ArtifactsDirRole, "/candidate/liked", "bewaard", auth: true);
        await CapturePageAsync(ArtifactsDirRole, "/account-maken", "account-maken", auth: false);
        await CapturePageAsync(ArtifactsDirRole, "/account-maken/code", "account-code", auth: false);
        await CapturePageAsync(ArtifactsDirRole, "/carriere", "carriere", auth: true);
        await CapturePageAsync(ArtifactsDirRole, "/candidate/settings?pane=mail", "instellingen", auth: true);
    }

    [Fact]
    public async Task Capture_kandidaat_728_r5_delivery_pngs()
    {
        Directory.CreateDirectory(ArtifactsDirR5);

        await CaptureBanenkaartPopupsAsync(ArtifactsDirR5, viewportOnly: true, popupElementShot: true);

        await CaptureIllustratedViewportAsync(
            ArtifactsDirR5,
            _web!.ServerAddress,
            "/candidate/paspoort",
            "kompas-tip",
            auth: true,
            sizes: [(1440, 900, "1440"), (390, 844, "390")],
            prepare: PrepareKompasTopScreenshotAsync);

        await using (var emptyWeb = new IllustratedPagesWebFactory(emptyAppsAndLikes: true))
        {
            _ = emptyWeb.ServerAddress;
            await CaptureIllustratedViewportAsync(
                ArtifactsDirR5,
                emptyWeb.ServerAddress,
                "/candidate/applications",
                "sollicitaties-leeg",
                auth: true,
                sizes: [(1440, 900, "1440")],
                prepare: WaitForApplicationsEmptyAsync);

            await CaptureIllustratedViewportAsync(
                ArtifactsDirR5,
                emptyWeb.ServerAddress,
                "/candidate/liked",
                "bewaard-leeg",
                auth: true,
                sizes: [(1440, 900, "1440")],
                prepare: WaitForSavedEmptyAsync);
        }

        await CaptureIllustratedViewportAsync(
            ArtifactsDirR5,
            _web.ServerAddress,
            "/candidate/settings?pane=mail",
            "instellingen",
            auth: true,
            sizes: [(1440, 900, "1440"), (390, 844, "390")],
            prepare: WaitForSettingsMailPaneAsync);

        var pngs = Directory.GetFiles(ArtifactsDirR5, "*.png");
        Assert.Equal(10, pngs.Length);
    }

    [Fact]
    public async Task Capture_kandidaat_728_r6_delivery_pngs()
    {
        Directory.CreateDirectory(ArtifactsDirR6);
        if (Directory.Exists(ArtifactsDirR5))
        {
            Directory.Delete(ArtifactsDirR5, recursive: true);
        }

        await CaptureBanenkaartPopupsAsync(
            ArtifactsDirR6,
            viewportOnly: true,
            popupElementShot: true,
            captures:
            [
                (1440, 900, "1440", "b1000001-0000-0000-0000-000000000001", "popup-photo"),
                (390, 844, "390", "b1000008-0000-0000-0000-000000000008", "popup-no-photo"),
            ]);

        await CaptureIllustratedViewportAsync(
            ArtifactsDirR6,
            _web!.ServerAddress,
            "/candidate/paspoort",
            "kompas-tip",
            auth: true,
            sizes: [(1440, 900, "1440"), (390, 844, "390")],
            prepare: PrepareKompasTopScreenshotAsync);

        await using (var emptyWeb = new IllustratedPagesWebFactory(emptyAppsAndLikes: true))
        {
            _ = emptyWeb.ServerAddress;
            await CaptureIllustratedViewportAsync(
                ArtifactsDirR6,
                emptyWeb.ServerAddress,
                "/candidate/applications",
                "sollicitaties-leeg",
                auth: true,
                sizes: [(1440, 900, "1440")],
                prepare: WaitForApplicationsEmptyAsync);

            await CaptureIllustratedViewportAsync(
                ArtifactsDirR6,
                emptyWeb.ServerAddress,
                "/candidate/liked",
                "bewaard-leeg",
                auth: true,
                sizes: [(1440, 900, "1440")],
                prepare: WaitForSavedEmptyAsync);
        }

        await CaptureIllustratedViewportAsync(
            ArtifactsDirR6,
            _web.ServerAddress,
            "/candidate/settings?pane=mail",
            "instellingen",
            auth: true,
            sizes: [(1440, 900, "1440"), (390, 844, "390")],
            prepare: WaitForSettingsMailPaneAsync);

        var pngs = Directory.GetFiles(ArtifactsDirR6, "*.png");
        Assert.Equal(8, pngs.Length);
    }

    [Fact]
    public async Task Capture_kandidaat_728_r7_delivery_pngs()
    {
        Directory.CreateDirectory(ArtifactsDirR7);
        if (Directory.Exists(ArtifactsDirR6))
        {
            Directory.Delete(ArtifactsDirR6, recursive: true);
        }

        await CaptureBanenkaartPopupsAsync(
            ArtifactsDirR7,
            viewportOnly: true,
            popupElementShot: true,
            captures:
            [
                (390, 844, "390", "b1000008-0000-0000-0000-000000000008", "popup-no-photo"),
            ]);

        await CaptureIllustratedViewportAsync(
            ArtifactsDirR7,
            _web!.ServerAddress,
            "/candidate/paspoort",
            "kompas-tip",
            auth: true,
            sizes: [(1440, 900, "1440")],
            prepare: PrepareKompasTopScreenshotAsync);

        await using (var emptyWeb = new IllustratedPagesWebFactory(emptyAppsAndLikes: true))
        {
            _ = emptyWeb.ServerAddress;
            await CaptureIllustratedViewportAsync(
                ArtifactsDirR7,
                emptyWeb.ServerAddress,
                "/candidate/applications",
                "sollicitaties-leeg",
                auth: true,
                sizes: [(1440, 900, "1440")],
                prepare: WaitForApplicationsEmptyAsync);

            await CaptureIllustratedViewportAsync(
                ArtifactsDirR7,
                emptyWeb.ServerAddress,
                "/candidate/liked",
                "bewaard-leeg",
                auth: true,
                sizes: [(1440, 900, "1440")],
                prepare: WaitForSavedEmptyAsync);
        }

        await CaptureIllustratedViewportAsync(
            ArtifactsDirR7,
            _web.ServerAddress,
            "/candidate/settings?pane=mail",
            "instellingen",
            auth: true,
            sizes: [(1440, 900, "1440")],
            prepare: WaitForSettingsMailPaneAsync);

        var pngs = Directory.GetFiles(ArtifactsDirR7, "*.png");
        Assert.Equal(5, pngs.Length);
    }

    [Fact]
    public async Task Capture_kandidaat_728_r8_delivery_pngs()
    {
        Directory.CreateDirectory(ArtifactsDirR8);
        if (Directory.Exists(ArtifactsDirR7))
        {
            Directory.Delete(ArtifactsDirR7, recursive: true);
        }

        await CaptureBanenkaartPopupsAsync(
            ArtifactsDirR8,
            viewportOnly: true,
            popupElementShot: true,
            captures:
            [
                (390, 844, "390", "b1000008-0000-0000-0000-000000000008", "popup-no-photo"),
            ]);

        await CaptureIllustratedViewportAsync(
            ArtifactsDirR8,
            _web!.ServerAddress,
            "/candidate/paspoort",
            "kompas-tip",
            auth: true,
            sizes: [(1440, 900, "1440")],
            prepare: PrepareKompasTopScreenshotAsync);

        await using (var emptyWeb = new IllustratedPagesWebFactory(emptyAppsAndLikes: true))
        {
            _ = emptyWeb.ServerAddress;
            await CaptureIllustratedViewportAsync(
                ArtifactsDirR8,
                emptyWeb.ServerAddress,
                "/candidate/applications",
                "sollicitaties-leeg",
                auth: true,
                sizes: [(1440, 900, "1440")],
                prepare: WaitForApplicationsEmptyAsync);

            await CaptureIllustratedViewportAsync(
                ArtifactsDirR8,
                emptyWeb.ServerAddress,
                "/candidate/liked",
                "bewaard-leeg",
                auth: true,
                sizes: [(1440, 900, "1440")],
                prepare: WaitForSavedEmptyAsync);
        }

        await CaptureIllustratedViewportAsync(
            ArtifactsDirR8,
            _web.ServerAddress,
            "/candidate/settings?pane=mail",
            "instellingen",
            auth: true,
            sizes: [(1440, 900, "1440")],
            prepare: WaitForSettingsMailPaneAsync);

        var pngs = Directory.GetFiles(ArtifactsDirR8, "*.png");
        Assert.Equal(5, pngs.Length);
    }

    private async Task CaptureBanenkaartPopupsAsync(
        string artifactsDir = ArtifactsDirR2,
        bool viewportOnly = false,
        bool popupElementShot = false,
        (int Width, int Height, string Tag, string VacancyId, string Suffix)[]? captures = null)
    {
        const string withPhotoId = "b1000001-0000-0000-0000-000000000001";
        const string noPhotoId = "b1000008-0000-0000-0000-000000000008";

        var captureList = captures ??
        [
            (1440, 900, "1440", withPhotoId, "popup-photo"),
            (1440, 900, "1440", noPhotoId, "popup-no-photo"),
            (390, 844, "390", withPhotoId, "popup-photo"),
            (390, 844, "390", noPhotoId, "popup-no-photo"),
        ];

        foreach (var (width, height, tag, vacancyId, suffix) in captureList)
        {
            await using var context = await _browser!.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                Locale = "nl-NL",
                IsMobile = width < 500,
                HasTouch = width < 500
            });
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(120_000);

            await page.GotoAsync(
                $"{_web!.ServerAddress}/__test/sign-in?return={Uri.EscapeDataString("/banenkaart?weergave=kaart")}",
                new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.GotoAsync($"{_web.ServerAddress}/banenkaart?weergave=kaart", new() { WaitUntil = WaitUntilState.NetworkIdle });
            await WaitForBanenkaartReadyAsync(page, preferListForCards: false);
            await page.WaitForFunctionAsync(
                "() => window.jobMap && typeof window.jobMap.__testOpenVacancyById === 'function'",
                null,
                new() { Timeout = 120_000 });
            await page.EvaluateAsync(
                """
                async (id) => {
                  const map = window.jobMap;
                  if (map.__testGetMap && map.__testGetMap()) {
                    map.__testGetMap().jumpTo({ center: [4.59, 52.22], zoom: 10.5, animate: false });
                  }
                  await map.reloadPins('/api/vacancies/pins?transport=Fiets&maxMinutes=30&radiusKm=40');
                  for (let i = 0; i < 40 && map.__testGetPinCount() < 1; i++) {
                    await new Promise(r => setTimeout(r, 100));
                  }
                  map.__testOpenVacancyById(id);
                }
                """,
                vacancyId);
            await page.Locator(".map-popup--illustrated:not(.map-popup--skeleton)").WaitForAsync(new() { Timeout = 90_000 });
            await page.AddStyleTagAsync(new()
            {
                Content = """
                    .kb-start-prompt { display: none !important; }
                    .jobsy-discovery.kb-start-prompt-open::before { display: none !important; }
                    .lobsy-assistant-tab, .pub-assistant-rail, .assistant-rail, [data-assistant-rail] { display: none !important; }
                    """
            });
            await page.WaitForTimeoutAsync(400);

            var popup = page.Locator(".map-popup--illustrated:not(.map-popup--skeleton)").First;
            await popup.WaitForAsync(new() { Timeout = 90_000 });
            await popup.ScrollIntoViewIfNeededAsync();

            if (suffix == "popup-photo")
            {
                var photo = popup.Locator(".map-popup__photo:not(.map-popup__photo--figure)");
                Assert.True(await photo.CountAsync() > 0, "Vacaturefoto moet zichtbaar zijn in popup met foto.");
            }
            else
            {
                var figure = popup.Locator(".map-popup__photo--illustrated, .map-popup__photo--figure");
                Assert.True(await figure.CountAsync() > 0, "Illustratie-fallback verwacht in popup zonder foto.");
            }

            var popupText = await popup.InnerTextAsync();
            Assert.DoesNotContain("% past bij jou", popupText, StringComparison.OrdinalIgnoreCase);
            if (suffix == "popup-no-photo" && tag == "390")
            {
                Assert.DoesNotContain("Waarom:", popupText, StringComparison.Ordinal);
                var figureSrc = await popup.Locator(".map-popup__photo--illustrated, .map-popup__photo--figure").First.GetAttributeAsync("src");
                Assert.Contains("job-schoonmaak", figureSrc ?? "", StringComparison.OrdinalIgnoreCase);
            }

            var file = Path.Combine(artifactsDir, $"banenkaart-{suffix}-{tag}.png");
            if (popupElementShot)
            {
                await page.Locator(".map-cluster-card").First.ScreenshotAsync(new() { Path = file });
            }
            else if (viewportOnly)
            {
                await page.ScreenshotAsync(new() { Path = file });
            }
            else
            {
                await popup.ScreenshotAsync(new() { Path = file });
            }

            Assert.True(File.Exists(file));
        }
    }

    private async Task CaptureIllustratedViewportAsync(
        string artifactsDir,
        string serverAddress,
        string path,
        string name,
        bool auth,
        (int Width, int Height, string Tag)[] sizes,
        Func<IPage, int, Task>? prepare = null)
    {
        foreach (var (width, height, tag) in sizes)
        {
            await using var context = await _browser!.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                Locale = "nl-NL",
                IsMobile = width < 500,
                HasTouch = width < 500
            });
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(120_000);

            if (auth)
            {
                await page.GotoAsync(
                    $"{serverAddress}/__test/sign-in?return={Uri.EscapeDataString(path)}",
                    new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            }

            await page.GotoAsync($"{serverAddress}{path}", new() { WaitUntil = WaitUntilState.NetworkIdle });
            if (prepare is not null)
            {
                await prepare(page, width);
            }

            await page.AddStyleTagAsync(new()
            {
                Content = """
                    .pub-header, .jobsy-chrome__header, .bottom-nav, .candidate-bottom-nav,
                    .lobsy-coach-dock, .pub-coach-dock { position: static !important; }
                    .passport-layout__card--desktop { position: static !important; }
                    .lobsy-assistant-tab, .pub-assistant-rail, .assistant-rail, [data-assistant-rail] { display: none !important; }
                    """
            });
            await page.WaitForTimeoutAsync(600);

            var file = Path.Combine(artifactsDir, $"{name}-{tag}.png");
            await page.ScreenshotAsync(new() { Path = file, FullPage = false });
            Assert.True(File.Exists(file));
        }
    }

    private static async Task PrepareKompasTopScreenshotAsync(IPage page, int width)
    {
        await page.EvaluateAsync("() => window.scrollTo(0, 0)");
        await page.Locator(".passport-page--illustrated, .passport-layout").First.WaitForAsync(new() { Timeout = 60_000 });
        if (width < 500)
        {
            await page.AddStyleTagAsync(new()
            {
                Content = """
                    .passport-layout__card--desktop { display: flex !important; flex-direction: column; }
                    .passport-layout__card--mobile { display: none !important; }
                    """
            });
        }

        await page.Locator(".passport-tabs").WaitForAsync(new() { Timeout = 30_000 });
    }

    private static async Task WaitForApplicationsEmptyAsync(IPage page, int _)
    {
        await page.Locator(".kb-apps--illustrated .cand-empty").WaitForAsync(new() { Timeout = 60_000 });
    }

    private static async Task WaitForSavedEmptyAsync(IPage page, int _)
    {
        await page.Locator(".kb-saved--illustrated .cand-empty").WaitForAsync(new() { Timeout = 60_000 });
    }

    private static async Task WaitForSettingsMailPaneAsync(IPage page, int _)
    {
        await page.Locator(".cand-settings").WaitForAsync(new() { Timeout = 60_000 });
    }

    private async Task CapturePageAsync(string artifactsDir, string path, string name, bool auth)
    {
        foreach (var (width, height, tag) in new[] { (1440, 900, "1440"), (1024, 900, "1024"), (390, 844, "390") })
        {
            await using var context = await _browser!.NewContextAsync(new()
            {
                ViewportSize = new() { Width = width, Height = height },
                Locale = "nl-NL",
                IsMobile = width < 500,
                HasTouch = width < 500
            });
            await PlaywrightCookieConsent.AcceptAsync(context);
            var page = await context.NewPageAsync();
            page.SetDefaultTimeout(120_000);

            if (auth)
            {
                await page.GotoAsync(
                    $"{_web!.ServerAddress}/__test/sign-in?return={Uri.EscapeDataString(path)}",
                    new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            }

            await page.GotoAsync($"{_web!.ServerAddress}{path}", new() { WaitUntil = WaitUntilState.NetworkIdle });
            if (name == "banenkaart")
            {
                await WaitForBanenkaartReadyAsync(page);
            }
            else if (name == "kompas")
            {
                await page.Locator(".passport-overview").WaitForAsync(new() { Timeout = 60_000 });
            }

            await page.AddStyleTagAsync(new()
            {
                Content = """
                    .pub-header, .jobsy-chrome__header, .bottom-nav, .candidate-bottom-nav,
                    .lobsy-coach-dock, .pub-coach-dock { position: static !important; }
                    .passport-layout__card--desktop { position: static !important; }
                    """
            });
            await page.WaitForTimeoutAsync(name == "banenkaart" ? 1200 : 600);

            var file = Path.Combine(artifactsDir, $"{name}-{tag}.png");
            await page.ScreenshotAsync(new() { Path = file, FullPage = true });
            Assert.True(File.Exists(file));
        }
    }

    private static async Task WaitForBanenkaartReadyAsync(IPage page, bool preferListForCards = true)
    {
        var later = page.Locator(".kb-start-prompt__later");
        if (await later.CountAsync() > 0)
        {
            await later.ClickAsync();
        }

        if (preferListForCards)
        {
            var listBtn = page.Locator("[data-testid=kb-view-lijst]").First;
            if (await listBtn.CountAsync() > 0 && await listBtn.IsVisibleAsync())
            {
                await listBtn.ClickAsync();
            }
        }

        await page.WaitForFunctionAsync(
            """
            () => {
              if (document.querySelector('.job-card') || document.querySelector('[data-testid=kb-list-row]')) return true;
              const m = window.jobMap;
              return m && m.__testGetPinCount && m.__testGetPinCount() >= 1;
            }
            """,
            null,
            new() { Timeout = 90_000 });

        var error = page.Locator(".state-message.error");
        if (await error.CountAsync() > 0)
        {
            var text = await error.First.InnerTextAsync();
            Assert.Fail($"Banenkaart load error: {text}");
        }
    }

    private sealed class IllustratedPagesWebFactory : WebWebApplicationFactory
    {
        public string ServerAddress { get; private set; } = "";
        private IHost? _kestrel;
        private readonly bool _emptyAppsAndLikes;

        public IllustratedPagesWebFactory(bool emptyAppsAndLikes = false)
        {
            _emptyAppsAndLikes = emptyAppsAndLikes;
            _ = CreateClient();
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
                services.RemoveAll<IHostedService>();
                services.AddSingleton<IStartupFilter>(new IllustratedPagesStartupFilter());
                services.RemoveAll<IFeatureFlags>();
                services.AddSingleton<IFeatureFlags>(new PassportOnFlags());
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton<IHttpClientFactory>(new StubHttpClientFactory(_emptyAppsAndLikes));
                services.RemoveAll<Jobsy.Web.Services.JobsyApiClient>();
                services.AddScoped(_ => new Jobsy.Web.Services.JobsyApiClient(
                    new HttpClient(new StubApiHandler(_emptyAppsAndLikes), disposeHandler: false)
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

    private sealed class IllustratedPagesStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            => app =>
            {
                app.Use(async (ctx, nxt) =>
                {
                    var path = ctx.Request.Path.Value ?? "";
                    if (HttpMethods.IsGet(ctx.Request.Method)
                        && ShouldStubVacancyCatalog(path))
                    {
                        if (path.EndsWith("/image", StringComparison.OrdinalIgnoreCase))
                        {
                            var thumb = IllustratedPagesVacancyStub.ThumbnailForImageRequest(path);
                            if (thumb is not null)
                            {
                                ctx.Response.Redirect(thumb);
                                return;
                            }

                            ctx.Response.StatusCode = StatusCodes.Status404NotFound;
                            return;
                        }

                        ctx.Response.ContentType = "application/json; charset=utf-8";
                        await ctx.Response.WriteAsync(IllustratedPagesVacancyStub.JsonForPath(path));
                        return;
                    }

                    if (path.StartsWith("/api/travel/isochrones", StringComparison.OrdinalIgnoreCase))
                    {
                        ctx.Response.ContentType = "application/json; charset=utf-8";
                        await ctx.Response.WriteAsync(IllustratedPagesVacancyStub.JsonForPath(path));
                        return;
                    }

                    if (!ctx.Request.Path.Equals("/__test/sign-in", StringComparison.OrdinalIgnoreCase))
                    {
                        await nxt();
                        return;
                    }

                    var identity = new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                        new Claim(ClaimTypes.Role, "Candidate"),
                        new Claim(ClaimTypes.Name, "Sanne de Vries")
                    ],
                    CookieAuthenticationDefaults.AuthenticationScheme);
                    await ctx.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));
                    var ret = ctx.Request.Query["return"].ToString();
                    if (string.IsNullOrWhiteSpace(ret) || !ret.StartsWith('/'))
                    {
                        ret = "/candidate/paspoort";
                    }

                    ctx.Response.Redirect(ret);
                });
                next(app);
            };

        private static bool ShouldStubVacancyCatalog(string path)
        {
            if (!path.StartsWith("/api/vacancies", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (path.Contains(SampleVacancyId.ToString(), StringComparison.OrdinalIgnoreCase)
                || path.Contains(PhotoVacancyId.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return path.Contains("/discover", StringComparison.OrdinalIgnoreCase)
                   || path.Contains("/pins", StringComparison.OrdinalIgnoreCase)
                   || path.Contains("/map-view", StringComparison.OrdinalIgnoreCase)
                   || path.Contains("/cards", StringComparison.OrdinalIgnoreCase);
        }
    }

    private sealed class PassportOnFlags : IFeatureFlags
    {
        private static readonly FeatureFlagSnapshot Snap = new(EmployersEnabled: true, CandidatePassportEnabled: true);

        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Snap);

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Snap.IsEnabled(feature));

        public void Invalidate()
        {
        }
    }

    private sealed class StubHttpClientFactory(bool emptyAppsAndLikes) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new StubApiHandler(emptyAppsAndLikes), disposeHandler: true) { BaseAddress = new Uri("http://api.test/") };
    }

    private sealed class StubApiHandler(bool emptyAppsAndLikes) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            var json = StubApiHandler.BuildJson(path, emptyAppsAndLikes);
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }

        internal static string BuildJson(string path, bool emptyAppsAndLikes = false)
        {
            var kompas = PassportDeepOnboardingScreenshotTests.BuildKompas(completed: true);
            kompas.Profile!.FullName = "Sanne de Vries";
            kompas.Profile.FirstName = "Sanne";
            if (kompas.WhoAmI is not null)
            {
                kompas.WhoAmI.Story = "Sanne pakt taken concreet op en houdt het overzicht, ook als het druk wordt.";
            }
            var descriptionSections =
                "<h2>Wat ga je doen?</h2><p>Je helpt met voeren, schoonmaken en aandacht voor de dieren.</p>" +
                "<h2>Wat vragen we?</h2><p>Je bent zorgvuldig en vindt het fijn om met dieren te werken.</p>" +
                "<h2>Wat bieden we?</h2><p>Een warm team, vaste uren en begeleiding.</p>";
            var sampleVacancy = $$"""
                {"id":"{{SampleVacancyId}}","title":"Medewerker dierenpension","companyName":"De Kooger Dierenopvang","companyAddress":"Kooger Dorpsweg 12, Koog aan de Zaan","status":"Active","openForWorkRequired":false,"fitPercent":81,"fitBand":"Strong","fitGate":"Open","fitDimensions":{"culture":84,"values":null,"competencies":76,"interests":90},"fitWhyKinds":["culture","interest","competency"],"wageVisible":true,"hourlyWage":13.9,"minHoursPerWeek":16,"maxHoursPerWeek":24,"flexibleTimes":false,"latitude":52.44,"longitude":4.83,"workTypes":["PartTime"],"startDate":"2026-01-01","endDate":"2026-12-31","kind":"Job","imageUrl":null,"companyLogoUrl":null,"travelMinutes":9,"matchGaps":["Werken in het weekend"],"descriptionHtml":"{{descriptionSections}}"}
                """;
            var photoVacancy = $$"""
                {"id":"{{PhotoVacancyId}}","title":"Medewerker dierenpension","companyName":"De Kooger Dierenopvang","companyAddress":"Kooger Dorpsweg 12, Koog aan de Zaan","status":"Active","openForWorkRequired":false,"fitPercent":81,"fitBand":"Strong","fitGate":"Open","fitDimensions":{"culture":84,"values":null,"competencies":76,"interests":90},"fitWhyKinds":["culture","interest","competency"],"wageVisible":true,"hourlyWage":13.9,"minHoursPerWeek":16,"maxHoursPerWeek":24,"flexibleTimes":false,"latitude":52.44,"longitude":4.83,"workTypes":["PartTime"],"startDate":"2026-01-01","endDate":"2026-12-31","kind":"Job","imageUrl":"/images/candidate-illustrated/finale-hero.jpg","companyLogoUrl":null,"travelMinutes":9,"matchGaps":["Werken in het weekend"],"descriptionHtml":"{{descriptionSections}}"}
                """;
            const string sampleApplication =
                """[{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","vacancyId":"11111111-1111-1111-1111-111111111111","vacancyTitle":"Zorgmedewerker dagbesteding","companyName":"Kasteelhof Zorg","locationLabel":"Zaandam","status":"EmployerContacting","createdAt":"2026-10-02T10:00:00Z","timeline":[{"key":"Sent","labelKey":"Kb.Timeline.Sent","state":"Done","occurredAtUtc":"2026-10-02T10:00:00Z"},{"key":"Seen","labelKey":"Kb.Timeline.Seen","state":"Done","occurredAtUtc":"2026-10-04T10:00:00Z"},{"key":"Interview","labelKey":"Kb.Timeline.Interview","state":"Current","occurredAtUtc":"2026-10-09T10:00:00Z"},{"key":"Outcome","labelKey":"Kb.Timeline.Outcome","state":"Pending","occurredAtUtc":null}],"nextStepKey":"Kb.Next.Practice"}]""";
            const string sampleLike =
                """[{"vacancyId":"11111111-1111-1111-1111-111111111111","vacancyTitle":"Medewerker dierenpension","companyName":"De Kooger Dierenopvang","locationLabel":"Zaandam","fitPercent":81,"fitBand":"Strong","createdAt":"2026-10-10T10:00:00Z","savedStateKind":"Open","imageUrl":null,"companyLogoUrl":null}]""";

            if (path.Contains("/kompas", StringComparison.OrdinalIgnoreCase))
            {
                return System.Text.Json.JsonSerializer.Serialize(kompas);
            }

            if (path.Contains("/site/status", StringComparison.OrdinalIgnoreCase))
            {
                return """{"maintenance":false,"expectedEndUtc":null}""";
            }

            if (path.Contains("/me/email-preferences", StringComparison.OrdinalIgnoreCase))
            {
                return """{"reminderEmailsEnabled":true,"optional":[{"key":"PushBom","enabled":true},{"key":"VacancyEngagementReminder","enabled":true}]}""";
            }

            if (path.Contains("/me/reminder-whatsapp", StringComparison.OrdinalIgnoreCase))
            {
                return """{"available":true,"optedIn":false,"phone":null}""";
            }

            if (path.Contains("/me/career", StringComparison.OrdinalIgnoreCase))
            {
                return """{"hasPlan":true,"dreamTitle":"Dierenarts","currentStoneIndex":1,"stones":[{"title":"Nu","state":"Done"},{"title":"Mbo 4 paraveterinair","state":"Current"}]}""";
            }

            if (path.Contains("/applications", StringComparison.OrdinalIgnoreCase))
            {
                return emptyAppsAndLikes ? "[]" : sampleApplication;
            }

            if (path.Contains("/likes", StringComparison.OrdinalIgnoreCase)
                || path.Contains("/engagement/likes", StringComparison.OrdinalIgnoreCase))
            {
                return emptyAppsAndLikes ? "[]" : sampleLike;
            }

            if (path.Contains("/engagement/shares", StringComparison.OrdinalIgnoreCase))
            {
                return "[]";
            }

            if (path.Contains("/travel", StringComparison.OrdinalIgnoreCase))
            {
                return """{"travelMinutes":9,"distanceKm":2.4}""";
            }

            if (path.Contains("vacanc", StringComparison.OrdinalIgnoreCase))
            {
                if (path.Contains(PhotoVacancyId.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return photoVacancy;
                }

                if (path.Contains(SampleVacancyId.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return sampleVacancy;
                }

                return IllustratedPagesVacancyStub.JsonForPath(path);
            }

            if (path.Contains("/categories", StringComparison.OrdinalIgnoreCase))
            {
                return "[]";
            }

            if (path.Contains("/me/profile", StringComparison.OrdinalIgnoreCase))
            {
                return """
                    {
                      "fullName": "Sanne de Vries",
                      "firstName": "Sanne",
                      "openForWork": true,
                      "preferences": {
                        "homeAddress": "1506 AB Zaandam",
                        "workRegion": null,
                        "aboutMe": "Ik werk graag in teamverband."
                      }
                    }
                    """;
            }

            return "{}";
        }
    }
}
