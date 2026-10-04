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
using Jobsy.Web.Components.Pages.Leraar;
using Jobsy.Web.Components.Pages.Leerling;
using Jobsy.Web.Components.Pages.School;
using Jobsy.Web.Localization;
using Jobsy.Web.Scholen;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

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

public class PupilFirstLoadBunitTests : BunitContext
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
    }

    [Fact]
    public async Task Island_first_load_failure_shows_offline_not_a_lost_session()
    {
        await PrepareAsync();
        _handler.Throw = true;
        var cut = Render<LeerlingEiland>();
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Even geen verbinding", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Probeer opnieuw", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Je sessie is verlopen", cut.Markup, StringComparison.Ordinal);
            Assert.True(_handler.Calls >= 2);
        }, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task Island_401_shows_a_lost_session()
    {
        await PrepareAsync();
        _handler.Status = HttpStatusCode.Unauthorized;
        var cut = Render<LeerlingEiland>();
        Assert.Contains("Je sessie is verlopen", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Even geen verbinding", cut.Markup, StringComparison.Ordinal);
        Assert.Equal(1, _handler.Calls);
    }

    [Fact]
    public async Task Journey_result_and_dream_job_share_the_offline_first_load()
    {
        await PrepareAsync();
        _handler.Throw = true;

        var journey = Render<LeerlingReis>();
        journey.WaitForAssertion(() =>
        {
            Assert.Contains("Even geen verbinding", journey.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Je sessie is verlopen", journey.Markup, StringComparison.Ordinal);
        }, TimeSpan.FromSeconds(10));

        var result = Render<LeerlingDitBenJij>();
        result.WaitForAssertion(() =>
        {
            Assert.Contains("Even geen verbinding", result.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Je sessie is verlopen", result.Markup, StringComparison.Ordinal);
        }, TimeSpan.FromSeconds(10));

        var dream = Render<LeerlingDroombaan>();
        dream.WaitForAssertion(() =>
        {
            Assert.Contains("Even geen verbinding", dream.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("Je sessie is verlopen", dream.Markup, StringComparison.Ordinal);
        }, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task Result_and_dream_job_401_show_a_lost_session()
    {
        await PrepareAsync();
        _handler.Status = HttpStatusCode.Unauthorized;

        var result = Render<LeerlingDitBenJij>();
        Assert.Contains("Je sessie is verlopen", result.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Even geen verbinding", result.Markup, StringComparison.Ordinal);

        var dream = Render<LeerlingDroombaan>();
        Assert.Contains("Je sessie is verlopen", dream.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Even geen verbinding", dream.Markup, StringComparison.Ordinal);
    }

    private async Task PrepareAsync()
    {
        var culture = Services.GetRequiredService<CultureState>();
        await culture.SetLanguageAsync("nl");
        var nav = Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        nav.NavigateTo("http://localhost/leerling/eiland");
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

public class LoggedInPupilIntroBunitTests : BunitContext
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
    public async Task Already_logged_in_hides_the_pick_a_school_intro()
    {
        var culture = Services.GetRequiredService<CultureState>();
        await culture.SetLanguageAsync("nl");
        var nav = Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        nav.NavigateTo("http://localhost/leerling");

        var cut = Render<LeerlingLogin>();
        Assert.Contains("Je bent al ingelogd", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Kies je school en je klas", cut.Markup, StringComparison.Ordinal);
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

public class TeacherSchoolLabelBunitTests : BunitContext
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
        var nav = Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();

        nav.NavigateTo($"http://localhost/leraar/klas/{ClassId:D}/groep");
        var group = Render<LeraarGroup>(p => p.Add(x => x.ClassId, ClassId));
        AssertNoRawEnums(group.Markup);
        Assert.Contains("Informeel en open", group.Markup, StringComparison.Ordinal);
        Assert.Contains("Iets goed afmaken", group.Markup, StringComparison.Ordinal);

        nav.NavigateTo($"http://localhost/leraar/klas/{ClassId:D}");
        var overview = Render<LeraarKlasOverview>(p => p.Add(x => x.ClassId, ClassId));
        AssertNoRawEnums(overview.Markup);
        Assert.Contains("Rust en duidelijkheid", overview.Markup, StringComparison.Ordinal);
        Assert.Contains("Weet ik nog niet: 4", overview.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("sch-chip-list\"></div>", overview.Markup, StringComparison.Ordinal);

        nav.NavigateTo("http://localhost/school/resultaten");
        var school = Render<SchoolResults>();
        AssertNoRawEnums(school.Markup);
        Assert.Contains("Iets goed afmaken", school.Markup, StringComparison.Ordinal);
        Assert.Contains("Zelf kiezen", school.Markup, StringComparison.Ordinal);
        Assert.Contains("Weet ik nog niet: 6", school.Markup, StringComparison.Ordinal);
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
        private static readonly string GroupJson = JsonSerializer.Serialize(
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

        private static readonly string OverviewJson = JsonSerializer.Serialize(
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

        private static readonly string ClassesJson = JsonSerializer.Serialize(
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

        private static readonly string ResultsJson = JsonSerializer.Serialize(
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
