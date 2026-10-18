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
    private const string ArtifactsDir = "/opt/cursor/artifacts/kandidaat-rol-r1";
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
    public async Task Capture_eight_candidate_pages_at_1440_390_and_1024()
    {
        Directory.CreateDirectory(ArtifactsDir);
        await CapturePageAsync("/vacancies/" + SampleVacancyId, "vacature-no-photo", auth: true);
        await CapturePageAsync("/vacancies/" + PhotoVacancyId, "vacature-photo", auth: true);
        await CapturePageAsync("/candidate/match", "match", auth: true);
        await CapturePageAsync("/candidate/applications", "sollicitaties", auth: true);
        await CapturePageAsync("/candidate/liked", "bewaard", auth: true);
        await CapturePageAsync("/account-maken", "account-maken", auth: false);
        await CapturePageAsync("/account-maken/code", "account-code", auth: false);
        await CapturePageAsync("/carriere", "carriere", auth: true);
        await CapturePageAsync("/candidate/settings?pane=mail", "instellingen", auth: true);
    }

    private async Task CapturePageAsync(string path, string name, bool auth)
    {
        foreach (var (width, height, tag) in new[] { (1440, 900, "1440"), (390, 844, "390"), (1024, 900, "1024") })
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
            await page.AddStyleTagAsync(new()
            {
                Content = """
                    .pub-header, .jobsy-chrome__header, .bottom-nav, .candidate-bottom-nav,
                    .lobsy-coach-dock, .pub-coach-dock { position: static !important; }
                    .passport-layout__card--desktop { position: static !important; }
                    """
            });
            await page.WaitForTimeoutAsync(600);

            var file = Path.Combine(ArtifactsDir, $"{name}-{tag}.png");
            await page.ScreenshotAsync(new() { Path = file, FullPage = true });
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
                    new HttpClient(new StubApiHandler(), disposeHandler: false)
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
                        ret = "/candidate/paspoort";
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
            => new(new StubApiHandler(), disposeHandler: true) { BaseAddress = new Uri("http://api.test/") };
    }

    private sealed class StubApiHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            var kompas = PassportDeepOnboardingScreenshotTests.BuildKompas(completed: true);
            kompas.Profile!.FullName = "Sanne de Vries";
            kompas.Profile.FirstName = "Sanne";
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
            var sampleApplication = """
                [{"id":"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa","vacancyId":"11111111-1111-1111-1111-111111111111","vacancyTitle":"Zorgmedewerker dagbesteding","companyName":"Kasteelhof Zorg","locationLabel":"Zaandam","status":"EmployerContacting","createdAt":"2026-10-02T10:00:00Z","timeline":[{"key":"Sent","labelKey":"Kb.Timeline.Sent","state":"Done","occurredAtUtc":"2026-10-02T10:00:00Z"},{"key":"Seen","labelKey":"Kb.Timeline.Seen","state":"Done","occurredAtUtc":"2026-10-04T10:00:00Z"},{"key":"Interview","labelKey":"Kb.Timeline.Interview","state":"Current","occurredAtUtc":"2026-10-09T10:00:00Z"},{"key":"Outcome","labelKey":"Kb.Timeline.Outcome","state":"Pending","occurredAtUtc":null}],"nextStepKey":"Kb.Next.Practice"}]
                """;
            var sampleLike = """
                [{"vacancyId":"11111111-1111-1111-1111-111111111111","vacancyTitle":"Medewerker dierenpension","companyName":"De Kooger Dierenopvang","locationLabel":"Zaandam","fitPercent":81,"fitBand":"Strong","createdAt":"2026-10-10T10:00:00Z","savedStateKind":"Open","imageUrl":null,"companyLogoUrl":null}]
                """;
            var json = path.Contains("/kompas", StringComparison.OrdinalIgnoreCase)
                ? System.Text.Json.JsonSerializer.Serialize(kompas)
                : path.Contains("/site/status", StringComparison.OrdinalIgnoreCase)
                    ? """{"maintenance":false,"expectedEndUtc":null}"""
                    : path.Contains("/me/email-preferences", StringComparison.OrdinalIgnoreCase)
                        ? """{"reminderEmailsEnabled":true,"optional":[{"key":"PushBom","enabled":true},{"key":"VacancyEngagementReminder","enabled":true}]}"""
                    : path.Contains("/me/reminder-whatsapp", StringComparison.OrdinalIgnoreCase)
                        ? """{"available":true,"optedIn":false,"phone":null}"""
                    : path.Contains("/me/career", StringComparison.OrdinalIgnoreCase)
                        ? """{"hasPlan":true,"dreamTitle":"Dierenarts","currentStoneIndex":1,"stones":[{"title":"Nu","state":"Done"},{"title":"Mbo 4 paraveterinair","state":"Current"}]}"""
                    : path.Contains("/applications", StringComparison.OrdinalIgnoreCase)
                        ? sampleApplication
                    : path.Contains("/likes", StringComparison.OrdinalIgnoreCase) || path.Contains("/engagement/likes", StringComparison.OrdinalIgnoreCase)
                        ? sampleLike
                    : path.Contains("/engagement/shares", StringComparison.OrdinalIgnoreCase)
                        ? "[]"
                    : path.Contains("/travel", StringComparison.OrdinalIgnoreCase)
                        ? """{"travelMinutes":9,"distanceKm":2.4}"""
                    : path.Contains("vacanc", StringComparison.OrdinalIgnoreCase)
                        ? path.Contains(PhotoVacancyId.ToString(), StringComparison.OrdinalIgnoreCase)
                            ? photoVacancy
                            : path.Contains(SampleVacancyId.ToString(), StringComparison.OrdinalIgnoreCase)
                            ? sampleVacancy
                            : path.Contains("pins", StringComparison.OrdinalIgnoreCase)
                                ? "[]"
                                : path.Contains("map", StringComparison.OrdinalIgnoreCase)
                                    ? """{"vacancies":[],"clusters":[]}"""
                                    : "[]"
                        : path.Contains("/categories", StringComparison.OrdinalIgnoreCase)
                            ? "[]"
                            : path.Contains("/me/profile", StringComparison.OrdinalIgnoreCase)
                                ? """{"preferences":{"homeAddress":"1506 AB Zaandam","workRegion":null}}"""
                            : "{}";
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}
