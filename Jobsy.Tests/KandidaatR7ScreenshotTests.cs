using Jobsy.Core.Features;
using Jobsy.Web.Media;
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
public sealed class KandidaatR7ScreenshotTests : IAsyncLifetime
{
    private const string ArtifactsDir = "/opt/cursor/artifacts/kandidaat-r7";
    private static readonly Guid NoPhotoVacancyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid PhotoVacancyId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private IllustratedPagesWebFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public async ValueTask InitializeAsync()
    {
        foreach (var legacy in new[]
                 {
                     "/opt/cursor/artifacts/kandidaat-r6",
                     "/opt/cursor/artifacts/carriere-r4",
                     "/opt/cursor/artifacts/carriere-r3",
                     "/opt/cursor/artifacts/vacature-r3",
                     "/opt/cursor/artifacts/kandidaat-rol-r2",
                     "/opt/cursor/artifacts/kandidaat-rol-r1"
                 })
        {
            if (Directory.Exists(legacy))
            {
                Directory.Delete(legacy, recursive: true);
            }
        }

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
    public async Task Capture_kandidaat_r7_review_shots()
    {
        Directory.CreateDirectory(ArtifactsDir);
        await CapturePageAsync("/carriere", "carriere-met-droombaan", careerEmpty: false);
        await CapturePageAsync("/carriere?empty=1", "carriere-zonder-droombaan", careerEmpty: true);
        await CapturePageAsync("/vacancies/" + NoPhotoVacancyId, "vacature-zonder-foto", careerEmpty: false);
        await CapturePageAsync("/vacancies/" + PhotoVacancyId, "vacature-met-foto", careerEmpty: false);

        var pngs = Directory.GetFiles(ArtifactsDir, "*.png");
        Assert.InRange(pngs.Length, 8, 10);
    }

    private async Task CapturePageAsync(string path, string name, bool careerEmpty)
    {
        foreach (var (width, height, tag) in new[] { (1440, 900, "1440"), (390, 844, "390") })
        {
            CombinedStubApiHandler.CareerPlanEmpty = careerEmpty;
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
                $"{_web!.ServerAddress}/__test/sign-in?return={Uri.EscapeDataString(path)}",
                new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.GotoAsync($"{_web.ServerAddress}{path}", new() { WaitUntil = WaitUntilState.NetworkIdle });

            await page.EvaluateAsync("document.documentElement.classList.add('playwright-capture')");

            if (path.StartsWith("/carriere", StringComparison.Ordinal) && !careerEmpty)
            {
                await page.Locator("[data-testid='career-step-learning']").WaitForAsync(new() { Timeout = 60_000 });
            }

            if (path.Contains("/vacancies/", StringComparison.Ordinal))
            {
                await page.Locator("[data-testid='kb-detail-readiness'], [data-testid='kb-travel-card']")
                    .First
                    .WaitForAsync(new() { Timeout = 60_000 });
            }

            await page.WaitForTimeoutAsync(600);

            var file = Path.Combine(ArtifactsDir, $"{name}-{tag}.png");
            var mobileViewport = width < 500;
            await page.ScreenshotAsync(new()
            {
                Path = file,
                FullPage = !mobileViewport
            });
            Assert.True(File.Exists(file));
        }
    }

    private sealed class IllustratedPagesWebFactory : WebWebApplicationFactory
    {
        public string ServerAddress { get; private set; } = "";
        private IHost? _kestrel;

        public IllustratedPagesWebFactory()
        {
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
                services.AddSingleton<IStartupFilter>(new CandidateSignInFilter());
                services.RemoveAll<IFeatureFlags>();
                services.AddSingleton<IFeatureFlags>(new PassportOnFlags());
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton<IHttpClientFactory>(new StubHttpClientFactory());
                services.RemoveAll<Jobsy.Web.Services.JobsyApiClient>();
                services.AddScoped(_ => new Jobsy.Web.Services.JobsyApiClient(
                    new HttpClient(new CombinedStubApiHandler(), disposeHandler: false)
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

    private sealed class CandidateSignInFilter : IStartupFilter
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
                        new Claim(ClaimTypes.Name, "Sanne de Vries")
                    ],
                    CookieAuthenticationDefaults.AuthenticationScheme);
                    await ctx.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));
                    var ret = ctx.Request.Query["return"].ToString();
                    if (string.IsNullOrWhiteSpace(ret) || !ret.StartsWith('/'))
                    {
                        ret = "/carriere";
                    }

                    ctx.Response.Redirect(ret);
                });
                next(app);
            };
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

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
            => new(new CombinedStubApiHandler(), disposeHandler: true) { BaseAddress = new Uri("http://api.test/") };
    }

    internal sealed class CombinedStubApiHandler : HttpMessageHandler
    {
        internal static bool CareerPlanEmpty { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            var descriptionSections =
                "<h2>Wat ga je doen?</h2><p>Je helpt met voeren, schoonmaken en aandacht voor de dieren.</p>" +
                "<h2>Wat vragen we?</h2><p>Je bent zorgvuldig en vindt het fijn om met dieren te werken.</p>";
            var descJson = System.Text.Json.JsonSerializer.Serialize(descriptionSections);
            var noPhotoVacancy = $$"""
                {"id":"{{NoPhotoVacancyId}}","title":"Medewerker dierenpension","companyName":"De Kooger Dierenopvang","companyAddress":"Kooger Dorpsweg 12, Koog aan de Zaan","categoryName":"Dierenverzorging","status":"Active","openForWorkRequired":false,"fitBand":"Strong","fitGate":"Open","fitDimensions":{"culture":84,"values":null,"competencies":76,"interests":90},"fitWhyKinds":["culture","interest","competency"],"wageVisible":true,"hourlyWage":13.9,"minHoursPerWeek":16,"maxHoursPerWeek":24,"flexibleTimes":false,"latitude":52.44,"longitude":4.83,"workTypes":["PartTime"],"startDate":"2026-01-01","endDate":"2026-12-31","kind":"Job","imageUrl":null,"companyLogoUrl":null,"travelMinutes":9,"description":{{descJson}}}
                """;
            var photoVacancy = $$"""
                {"id":"{{PhotoVacancyId}}","title":"Medewerker dierenpension","companyName":"De Kooger Dierenopvang","companyAddress":"Kooger Dorpsweg 12, Koog aan de Zaan","categoryName":"Dierenverzorging","status":"Active","openForWorkRequired":false,"fitBand":"Strong","fitGate":"Open","fitDimensions":{"culture":84,"values":null,"competencies":76,"interests":90},"fitWhyKinds":["culture","interest","competency"],"wageVisible":true,"hourlyWage":13.9,"minHoursPerWeek":16,"maxHoursPerWeek":24,"flexibleTimes":false,"latitude":52.44,"longitude":4.83,"workTypes":["PartTime"],"startDate":"2026-01-01","endDate":"2026-12-31","kind":"Job","imageUrl":"{{CandidateIllustratedImages.VacancyDierenStockPhoto}}","companyLogoUrl":null,"travelMinutes":9,"description":{{descJson}}}
                """;
            var incompleteProfile = """
                {"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","email":"sanne@example.nl","fullName":"Sanne de Vries","firstName":"Sanne","lastName":"de Vries","role":"Candidate","openForWork":false,"emailVerified":true,"preferences":{"homeAddress":"1506 AB Zaandam","preferredTransport":"Fiets","employers":[],"educations":[],"availability":{}}}
                """;
            var passportCourses = """
                [
                  {"offerId":"33333333-3333-3333-3333-333333333301","title":"Paraveterinair","providerName":"ROC Amstelland","delivery":"Online","durationValue":8,"durationUnit":"weken","isFree":true,"type":"Mbo"},
                  {"offerId":"33333333-3333-3333-3333-333333333302","title":"Dierenwelzijn in de praktijk","providerName":"NL Kenniscentrum Dieren","delivery":"Blended","durationValue":12,"durationUnit":"weken","isFree":false,"type":"Cursus","location":"Haarlem"},
                  {"offerId":"33333333-3333-3333-3333-333333333303","title":"EHBO voor huisdieren","providerName":"Stichting Dierenhulp","delivery":"Online","durationValue":4,"durationUnit":"weken","isFree":true,"type":"Workshop"}
                ]
                """;
            var json = path.Contains("/career-path/dream-options", StringComparison.OrdinalIgnoreCase)
                ? """
                  {"suggestions":[
                    {"catalogKey":"dierenarts","title":"Dierenarts","description":"Past bij je zorgzaamheid","noScore":true},
                    {"catalogKey":"zorg","title":"Zorgmedewerker","description":"Sluit aan op je paspoort","noScore":true}],
                  "results":[]}
                  """
                : path.Contains("/career-path/archived", StringComparison.OrdinalIgnoreCase)
                    ? "[]"
                    : path.Contains("/career-path", StringComparison.OrdinalIgnoreCase)
                        ? CareerPlanJson(CareerPlanEmpty)
                        : path.Contains("/career-interests", StringComparison.OrdinalIgnoreCase)
                            ? """{"scores":{"isComplete":true,"realistic":3,"investigative":2,"artistic":2,"social":4,"enterprising":2,"conventional":2}}"""
                            : path.Contains("/applications", StringComparison.OrdinalIgnoreCase)
                                ? "[]"
                                : path.Contains("/likes", StringComparison.OrdinalIgnoreCase) || path.Contains("/engagement/likes", StringComparison.OrdinalIgnoreCase)
                                    ? "[]"
                                    : path.Contains("/travel", StringComparison.OrdinalIgnoreCase)
                                        ? """{"travelMinutes":9,"distanceKm":2.4}"""
                                        : path.Contains("/me/profile", StringComparison.OrdinalIgnoreCase)
                                            ? incompleteProfile
                                            : path.Contains("training-offers/passport", StringComparison.OrdinalIgnoreCase)
                                                ? passportCourses
                                                : path.Contains("training-offers", StringComparison.OrdinalIgnoreCase)
                                                    ? passportCourses
                                                    : path.Contains("vacanc", StringComparison.OrdinalIgnoreCase)
                                                        ? path.Contains(PhotoVacancyId.ToString(), StringComparison.OrdinalIgnoreCase)
                                                            ? photoVacancy
                                                            : path.Contains(NoPhotoVacancyId.ToString(), StringComparison.OrdinalIgnoreCase)
                                                                ? noPhotoVacancy
                                                                : "[]"
                                                        : path.Contains("/onboarding", StringComparison.OrdinalIgnoreCase)
                                                            ? """{"isComplete":true}"""
                                                            : "{}";
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }

        private static string CareerPlanJson(bool empty)
            => empty
                ? "null"
                : """
                  {"dreamTitle":"Dierenarts","fromAi":false,"planLanguage":"nl","goalReached":false,"matchPercent":48,"carriedOverCount":0,"steps":[
                  {"id":"career-step-1","order":1,"title":"Mbo 4 paraveterinair","status":"Active","summary":"Leer assisteren bij behandelingen en dierenhulp in de praktijk.","skillsGap":["Dierenhulp","Hygiëne kliniek"],"courses":["Paraveterinair"],"actionKinds":["Courses","Vacancies"],"stepFitBand":"Growing"},
                  {"id":"career-step-2","order":2,"title":"Werkervaring dierenkliniek","status":"Open","summary":"Bouw minstens een jaar ervaring op in een dierenkliniek.","skillsGap":["Kliniekervaring"],"yearsExperienceNeeded":1,"courses":[],"actionKinds":["Vacancies"],"stepFitBand":"Open"}]}
                  """;
    }
}
