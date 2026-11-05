using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Bunit;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Web.Localization;
using Jobsy.Web.Scholen;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Jobsy.Tests.TestSupport;

namespace Jobsy.Tests.Scholen;

public class PupilDimensionLabelTests
{
    [Fact]
    public void Value_and_culture_labels_exist_in_every_language()
    {
        string[] valueCodes =
        [
            SchwartzValuesCatalog.Autonomy,
            SchwartzValuesCatalog.Connection,
            SchwartzValuesCatalog.Achievement,
            SchwartzValuesCatalog.Stability,
            SchwartzValuesCatalog.Impact
        ];
        string[] cultureCodes =
        [
            CulturePersonalityCatalog.Autonomy,
            CulturePersonalityCatalog.Informal,
            CulturePersonalityCatalog.Collaboration,
            CulturePersonalityCatalog.Flexibility,
            CulturePersonalityCatalog.Innovation,
            CulturePersonalityCatalog.PeopleFirst
        ];

        foreach (var lang in new[] { "nl", "en", "pl", "ro", "ar" })
        {
            foreach (var code in valueCodes)
            {
                var text = UiStrings.Get("School.Dim.Val." + code, lang);
                Assert.False(PupilDimensionText.IsRawEnumName(text), lang + " " + code);
                Assert.DoesNotContain("School.Dim.", text, StringComparison.Ordinal);
            }

            foreach (var code in cultureCodes)
            {
                var text = UiStrings.Get("School.Dim.Cult." + code, lang);
                Assert.False(PupilDimensionText.IsRawEnumName(text), lang + " " + code);
                Assert.DoesNotContain("School.Dim.", text, StringComparison.Ordinal);
            }
        }

        Assert.Equal("Iets goed afmaken", UiStrings.Get("School.Dim.Val.Achievement", "nl"));
        Assert.Equal("Rust en duidelijkheid", UiStrings.Get("School.Dim.Val.Stability", "nl"));
        Assert.Equal("Informeel en open", UiStrings.Get("School.Dim.Cult.Informal", "nl"));
    }
}

public class PupilFirstLoadBunitTests : JobsyBunitContext

{
    private readonly ScriptedHandler _handler = new();

    public PupilFirstLoadBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        DefaultWaitTimeout = TimeSpan.FromSeconds(15);
        Services.AddLogging();
        Services.AddSingleton<AuthenticationStateProvider>(new AnonymousAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        });
        Services.AddSingleton<IAntiforgery>(new AlwaysValidAntiforgery());
        Services.AddSingleton<IPupilQuestionSetRegistry>(new PupilQuestionSetRegistry());
        Services.AddSingleton(new JobsyApiClient(new HttpClient(_handler)
        {
            BaseAddress = new Uri("http://api.test/")
        }));
        Services.AddSingleton<IFeatureFlags>(new PupilFirstLoadFeatureFlags());
    }

    [Fact]
    public void Ssr_journey_endpoints_handle_session_loss_via_load_retry()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var endpoints = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Hosting/PupilLeerlingJourneyEndpoints.cs"));
        Assert.Contains("PupilLoadRetry", endpoints, StringComparison.Ordinal);
        Assert.Contains("LoadProgressAsync", endpoints, StringComparison.Ordinal);
        Assert.Contains("Leerling.Session.Lost", endpoints, StringComparison.Ordinal);
        Assert.Contains("/leerling?error=expired", endpoints, StringComparison.Ordinal);
    }

    private sealed class PupilFirstLoadFeatureFlags : IFeatureFlags
    {
        private static readonly FeatureFlagSnapshot Snap = new(false, false, SchoolsEnabled: true);

        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Snap);

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Snap.IsEnabled(feature));

        public void Invalidate()
        {
        }
    }

    private sealed class ScriptedHandler : HttpMessageHandler
    {
        public HttpStatusCode Status { get; set; } = HttpStatusCode.InternalServerError;

        public bool Throw { get; set; }

        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Calls++;
            if (Throw)
            {
                throw new HttpRequestException("offline");
            }

            return Task.FromResult(new HttpResponseMessage(Status)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json"),
                RequestMessage = request
            });
        }
    }

    private sealed class AnonymousAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    private sealed class AlwaysValidAntiforgery : IAntiforgery
    {
        public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext)
            => new("req", "cookie", "form", "header");

        public AntiforgeryTokenSet GetTokens(HttpContext httpContext) => GetAndStoreTokens(httpContext);

        public Task<bool> IsRequestValidAsync(HttpContext httpContext) => Task.FromResult(true);

        public void ValidateRequest(HttpContext httpContext) => _ = httpContext;

        public Task ValidateRequestAsync(HttpContext httpContext) => Task.CompletedTask;

        public void SetCookieTokenAndHeader(HttpContext httpContext) => _ = httpContext;
    }
}

public class LoggedInPupilIntroBunitTests : JobsyBunitContext

{


    public LoggedInPupilIntroBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        Services.AddSingleton<AuthenticationStateProvider>(new PupilAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        });
        Services.AddSingleton<IAntiforgery>(new PupilFirstLoadBunitTestsAntiforgery());
        Services.AddSingleton(new JobsyApiClient(new HttpClient(new EmptyHandler())
        {
            BaseAddress = new Uri("http://api.test/")
        }));
    }

    [Fact]
    public void Leerling_login_is_ssr_not_blazor()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        Assert.False(File.Exists(Path.Combine(root, "Jobsy.Web/Components/Pages/Leerling/LeerlingLogin.razor")));
        var html = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Scholen/PupilLeerlingLoginSsrHtml.cs"));
        Assert.Contains("Leerling.Login.AlreadyIn", html, StringComparison.Ordinal);
        Assert.Contains("Leerling.Login.Heading", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Busy_status_does_not_repeat_the_progress_fraction()
    {
        var culture = Services.GetRequiredService<CultureState>();
        await culture.SetLanguageAsync("nl");
        var label = PupilCodeStatusText.Label(culture, PupilCodeStatus.InProgress, DateTime.UtcNow);
        Assert.Equal("Bezig", label);
        Assert.DoesNotContain("/", label, StringComparison.Ordinal);
    }

    private sealed class PupilAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var identity = new ClaimsIdentity(
                [new Claim(PupilClaimTypes.PupilCodeId, Guid.NewGuid().ToString("D"))],
                authenticationType: "pupil");
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
        }
    }

    private sealed class EmptyHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", Encoding.UTF8, "application/json")
            });
    }

    private sealed class PupilFirstLoadBunitTestsAntiforgery : IAntiforgery
    {
        public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext)
            => new("req", "cookie", "form", "header");

        public AntiforgeryTokenSet GetTokens(HttpContext httpContext) => GetAndStoreTokens(httpContext);

        public Task<bool> IsRequestValidAsync(HttpContext httpContext) => Task.FromResult(true);

        public void ValidateRequest(HttpContext httpContext) => _ = httpContext;

        public Task ValidateRequestAsync(HttpContext httpContext) => Task.CompletedTask;

        public void SetCookieTokenAndHeader(HttpContext httpContext) => _ = httpContext;
    }
}

public class TeacherSchoolLabelBunitTests : JobsyBunitContext

{


    private static readonly Guid ClassId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public TeacherSchoolLabelBunitTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        Services.AddSingleton<AuthenticationStateProvider>(new AnonymousAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<IFeatureFlags>(new SchoolsOn());
        Services.AddSingleton(new JobsyApiClient(new HttpClient(new PortalHandler())
        {
            BaseAddress = new Uri("http://api.test/")
        }));
    }

    [Fact]
    public async Task Teacher_and_school_pages_hide_raw_driver_names()
    {
        var culture = Services.GetRequiredService<CultureState>();
        await culture.SetLanguageAsync("nl");

        var groupDto = JsonSerializer.Deserialize<TeacherGroupInsightsDto>(PortalHandler.GroupJson, JobsyApiClient.ApiJson)!;
        var groupMarkup = TeacherPortalReadOnlySsrHtml.BuildGroupBody(ClassId, groupDto, culture);
        AssertNoRawEnums(groupMarkup);
        Assert.Contains("Informeel en open", groupMarkup, StringComparison.Ordinal);
        Assert.Contains("Iets goed afmaken", groupMarkup, StringComparison.Ordinal);

        var overviewDto = JsonSerializer.Deserialize<TeacherClassOverviewDto>(PortalHandler.OverviewJson, JobsyApiClient.ApiJson)!;
        var overviewMarkup = TeacherKlasOverviewSsrHtml.BuildBody(ClassId, overviewDto, 0, [], null, culture);
        AssertNoRawEnums(overviewMarkup);
        Assert.Contains("Rust en duidelijkheid", overviewMarkup, StringComparison.Ordinal);
        Assert.Contains("Weet ik nog niet: 4", overviewMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("sch-chip-list\"></div>", overviewMarkup, StringComparison.Ordinal);

        var classes = JsonSerializer.Deserialize<List<SchoolPortalClassListItemDto>>(PortalHandler.ClassesJson, JobsyApiClient.ApiJson)!;
        var results = JsonSerializer.Deserialize<SchoolPortalResultsDto>(PortalHandler.ResultsJson, JobsyApiClient.ApiJson)!;
        var schoolMarkup = SchoolPortalSsrHtml.Results(
            classes,
            PupilQuestionSet.Vo,
            false,
            ClassId,
            results,
            null,
            culture);
        AssertNoRawEnums(schoolMarkup);
        Assert.Contains("Iets goed afmaken", schoolMarkup, StringComparison.Ordinal);
        Assert.Contains("Zelf kiezen", schoolMarkup, StringComparison.Ordinal);
        Assert.Contains("Weet ik nog niet: 6", schoolMarkup, StringComparison.Ordinal);
    }

    private static void AssertNoRawEnums(string markup)
    {
        foreach (var name in new[]
                 {
                     "Achievement", "Connection", "Stability", "Autonomy", "Impact",
                     "Informal", "PeopleFirst", "Innovation"
                 })
        {
            Assert.DoesNotMatch(
                new Regex($@"(?<![A-Za-z]){name}(?![A-Za-z])"),
                markup);
        }
    }

    private sealed class AnonymousAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }

    private sealed class SchoolsOn : IFeatureFlags
    {
        private static readonly FeatureFlagSnapshot Snap = new(false, false, SchoolsEnabled: true);

        public ValueTask<FeatureFlagSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Snap);

        public ValueTask<bool> IsEnabledAsync(PlatformFeature feature, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(Snap.IsEnabled(feature));

        public void Invalidate()
        {
        }
    }

    private sealed class PortalHandler : HttpMessageHandler
    {
        internal static readonly string GroupJson = JsonSerializer.Serialize(
            new TeacherGroupInsightsDto(
                true,
                6,
                [],
                [
                    new NamedCountDto("Achievement", 3),
                    new NamedCountDto("Connection", 2),
                    new NamedCountDto("Stability", 1)
                ],
                [],
                [
                    new NamedCountDto("Informal", 3),
                    new NamedCountDto("PeopleFirst", 2),
                    new NamedCountDto("Innovation", 1)
                ],
                [],
                [],
                PupilQuestionSet.Vo),
            JobsyApiClient.ApiJson);

        internal static readonly string OverviewJson = JsonSerializer.Serialize(
            new TeacherClassOverviewDto(
                ClassId,
                "2B",
                SchoolLevel.Havo,
                2,
                "2026-2027",
                10,
                6,
                2,
                2,
                60,
                null,
                TestWindowState.NotOpen,
                null,
                true,
                null,
                [],
                new TeacherGroupInsightsDto(
                    true,
                    6,
                    [],
                    [
                        new NamedCountDto("Achievement", 3),
                        new NamedCountDto("Stability", 2),
                        new NamedCountDto("Connection", 1)
                    ],
                    [],
                    [new NamedCountDto("Informal", 2)],
                    [],
                    [],
                    PupilQuestionSet.Vo,
                    UndecidedDreamJobCount: 4),
                PupilQuestionSet.Vo),
            JobsyApiClient.ApiJson);

        internal static readonly string ClassesJson = JsonSerializer.Serialize(
            new List<SchoolPortalClassListItemDto>
            {
                new(
                    ClassId,
                    "2B",
                    SchoolLevel.Havo,
                    2,
                    2026,
                    "2026-2027",
                    [],
                    10,
                    6,
                    true,
                    TestWindowState.Open,
                    null,
                    PupilQuestionSet.Vo)
            },
            JobsyApiClient.ApiJson);

        internal static readonly string ResultsJson = JsonSerializer.Serialize(
            new SchoolPortalResultsDto(
                ClassId,
                "2B",
                true,
                new ClassResultsAggregate(
                    10,
                    6,
                    60,
                    true,
                    [new NamedCount("R", 4)],
                    [
                        new NamedCount("Achievement", 3),
                        new NamedCount("Connection", 2),
                        new NamedCount("Stability", 1)
                    ],
                    [],
                    6),
                [
                    new SchoolPortalPerCodeResultDto(
                        Guid.Parse("22222222-2222-2222-2222-222222222222"),
                        "K7Q-M2P",
                        PupilCodeStatus.Completed,
                        "RS",
                        "Autonomy",
                        null)
                ],
                PupilQuestionSet.Vo),
            JobsyApiClient.ApiJson);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            var json = path.Contains("/results", StringComparison.Ordinal) ? ResultsJson
                : path.Contains("/overview", StringComparison.Ordinal) ? OverviewJson
                : path.Contains("/group", StringComparison.Ordinal) ? GroupJson
                : path.Contains("/classes", StringComparison.Ordinal) ? ClassesJson
                : "[]";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }
}
