using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bunit;
using Jobsy.Core.Careers;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Web.Components.Leerling;
using Jobsy.Web.Components.Pages.Leerling;
using Jobsy.Web.Components.Pages.School;
using Jobsy.Web.Components.School;
using Jobsy.Web.Localization;
using Jobsy.Web.Scholen;
using Jobsy.Web.Seo;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Jobsy.Tests.Scholen;

public class SchoolRun8CopyTests : BunitContext
{
    public SchoolRun8CopyTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<AuthenticationStateProvider>(new AnonymousAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
    }

    [Fact]
    public async Task Dream_job_slugs_use_the_same_labels_as_the_teacher_view()
    {
        var culture = Services.GetRequiredService<CultureState>();
        await culture.SetLanguageAsync("nl");
        Assert.Equal("Weet ik nog niet", PupilDreamJobText.Label(culture, "weet-ik-nog-niet"));
        Assert.Equal("—", PupilDreamJobText.Label(culture, null));
        Assert.Equal("—", PupilDreamJobText.Label(culture, "  "));

        foreach (var job in DreamJobCatalog.All)
        {
            var label = PupilDreamJobText.Label(culture, job.Key);
            Assert.NotEqual(job.Key, label);
            Assert.DoesNotContain("weet-ik-nog-niet", label, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Equal("Brandweerman/-vrouw", PupilDreamJobText.Label(culture, "brandweer"));
    }

    [Fact]
    public void Firefighter_titles_use_one_slash_form()
    {
        var titles = DreamJobCatalog.All.Select(job => job.TitleNl)
            .Concat(CareerDreamCatalog.All.Select(entry => entry.Title));
        foreach (var title in titles)
        {
            Assert.DoesNotContain("Brandweerman-vrouw", title, StringComparison.Ordinal);
            Assert.DoesNotContain("Brandweerman / -vrouw", title, StringComparison.Ordinal);
        }

        Assert.Equal("Brandweerman/-vrouw", CareerDreamCatalog.FindByKey("brandweerman")!.Title);
        Assert.Equal("Brandweerman/-vrouw", DreamJobCatalog.All.Single(job => job.Key == "brandweer").TitleNl);
    }

    [Theory]
    [InlineData("nl", "Zo werkt Lobsy voor scholen · Lobsy", "Wie wij zijn · Lobsy")]
    [InlineData("en", "How Lobsy works for schools · Lobsy", "Who we are · Lobsy")]
    [InlineData("pl", "Tak działa Lobsy dla szkół · Lobsy", "Kim jesteśmy · Lobsy")]
    [InlineData("ro", "Așa funcționează Lobsy pentru școli · Lobsy", "Cine suntem · Lobsy")]
    [InlineData("ar", "هكذا يعمل Lobsy للمدارس · Lobsy", "من نكون · Lobsy")]
    public void Schools_and_about_document_titles_are_branded_without_a_trailing_period(
        string language,
        string schools,
        string about)
    {
        var schoolsTitle = PageSeoResolver.WithBrand(UiStrings.Get(PageSeoCatalog.Resolve("/scholen").TitleKey, language));
        var aboutTitle = PageSeoResolver.WithBrand(UiStrings.Get(PageSeoCatalog.Resolve("/wie-zijn-wij").TitleKey, language));
        Assert.Equal(schools, schoolsTitle);
        Assert.Equal(about, aboutTitle);
        Assert.False(schoolsTitle.EndsWith('.'));
        Assert.False(aboutTitle.EndsWith('.'));
        Assert.DoesNotContain("een loopbaangids in je eigen taal", schoolsTitle, StringComparison.Ordinal);
    }

    [Fact]
    public void NoBlazor_shell_gives_wie_zijn_wij_its_own_title()
    {
        var root = TestRepo.FindRoot();
        var app = File.ReadAllText(Path.Combine(root, "Jobsy.Web", "Components", "App.razor"));
        Assert.Contains("\"/wie-zijn-wij\"", app, StringComparison.Ordinal);
        Assert.Equal("Seo.AboutTitle", PageSeoCatalog.Resolve("/wie-zijn-wij").TitleKey);
        Assert.Equal("Seo.SchoolsTitle", PageSeoCatalog.Resolve("/scholen").TitleKey);
    }

    private sealed class AnonymousAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}

public class SchoolRun8ReadAloudTests : BunitContext
{
    private readonly ScriptedHandler _handler = new();

    public SchoolRun8ReadAloudTests()
    {
        JSInterop.Setup<ReadAloudProbe>("lobsyReadAloud.whenReady", _ => true)
            .SetResult(new ReadAloudProbe { Supported = true, Enabled = true });
        Services.AddLogging();
        Services.AddSingleton<AuthenticationStateProvider>(new AnonymousAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor { HttpContext = new DefaultHttpContext() });
        Services.AddSingleton<IAntiforgery>(new AlwaysValidAntiforgery());
        Services.AddSingleton<IPupilQuestionSetRegistry>(new PupilQuestionSetRegistry());
        Services.AddSingleton(new JobsyApiClient(new HttpClient(_handler) { BaseAddress = new Uri("http://api.test/") }));
    }

    [Fact]
    public async Task Each_leerling_page_renders_read_aloud_when_a_voice_is_available()
    {
        var culture = Services.GetRequiredService<CultureState>();
        await culture.SetLanguageAsync("nl");
        var nav = Services.GetRequiredService<NavigationManager>();

        nav.NavigateTo("http://localhost/leerling");
        await AssertSpoken(Render<LeerlingLogin>());

        nav.NavigateTo("http://localhost/leerling/start");
        await AssertSpoken(Render<LeerlingStart>());

        nav.NavigateTo("http://localhost/leerling/stop");
        await AssertSpoken(Render<LeerlingStop>());

        await AssertSpoken(Render<LeerlingVoPartBreak>());

        _handler.JsonByPath["/api/pupil/progress"] = Json(QuestionProgress());
        nav.NavigateTo("http://localhost/leerling/reis");
        var reis = Render<LeerlingReis>();
        await AssertSpoken(reis);
        var info = reis.Find(".ll-info-btn");
        Assert.Equal("Meer uitleg", info.GetAttribute("aria-label"));
        Assert.Equal("false", info.GetAttribute("aria-expanded"));
        var controls = info.GetAttribute("aria-controls");
        Assert.False(string.IsNullOrWhiteSpace(controls));
        Assert.NotNull(reis.Find($"#{controls}"));
        await info.ClickAsync();
        Assert.Equal("true", reis.Find(".ll-info-btn").GetAttribute("aria-expanded"));

        _handler.JsonByPath["/api/pupil/progress"] = Json(IslandProgress());
        nav.NavigateTo("http://localhost/leerling/eiland");
        await AssertSpoken(Render<LeerlingEiland>());

        _handler.JsonByPath["/api/pupil/result"] = Json(ResultPage());
        nav.NavigateTo("http://localhost/leerling/dit-ben-jij");
        await AssertSpoken(Render<LeerlingDitBenJij>());

        nav.NavigateTo("http://localhost/leerling/droombaan");
        await AssertSpoken(Render<LeerlingDroombaan>());
    }

    private static async Task AssertSpoken<TComponent>(IRenderedComponent<TComponent> cut)
        where TComponent : IComponent
    {
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-read-aloud]")), TimeSpan.FromSeconds(5));
        var buttons = cut.FindAll("[data-read-aloud]");
        Assert.Contains(buttons, button => button.GetAttribute("aria-label") == "Lees voor");
        await Task.CompletedTask;
    }

    private static PupilProgressStateDto QuestionProgress()
        => new(
            Guid.NewGuid(), "8A", "ABC-123", PupilCodeStatus.InProgress,
            0, 60, 0, 0, false,
            "9001", "koraalrif", new Dictionary<string, int>(),
            true, false, false, false, [], [], null, null,
            PupilQuestionSet.Groep78, "question");

    private static PupilProgressStateDto IslandProgress()
        => new(
            Guid.NewGuid(), "8A", "ABC-123", PupilCodeStatus.InProgress,
            15, 60, 15, 2, false,
            null, "schatgrot", new Dictionary<string, int>(),
            true, false, true, false, [], [], null, null,
            PupilQuestionSet.Groep78, "island");

    private static PupilResultPageDto ResultPage()
        => new(
            "8A",
            "ABC-123",
            "School",
            new PupilStoryViewDto(
                "Dit ben jij",
                "Jij helpt graag andere mensen.",
                [new PupilStoryTileDto("competence", "Zo ben jij", "Je let op anderen.")],
                ["Dierenarts", "Leraar"],
                []),
            [],
            [],
            null,
            null,
            60,
            PupilQuestionSet.Groep78);

    private static string Json<T>(T value)
        => JsonSerializer.Serialize(value, ApiJson);

    private static readonly JsonSerializerOptions ApiJson = CreateApiJson();

    private static JsonSerializerOptions CreateApiJson()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private sealed class ScriptedHandler : HttpMessageHandler
    {
        public Dictionary<string, string> JsonByPath { get; } = new(StringComparer.OrdinalIgnoreCase);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (!JsonByPath.TryGetValue(path, out var json))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
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

public class SchoolRun8Groep8Tests : BunitContext
{
    private readonly CaptureHandler _handler = new();

    public SchoolRun8Groep8Tests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        Services.AddSingleton<AuthenticationStateProvider>(new AnonymousAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<IFeatureFlags>(new SchoolsOn());
        Services.AddSingleton(new JobsyApiClient(new HttpClient(_handler) { BaseAddress = new Uri("http://api.test/") }));
    }

    [Fact]
    public async Task Stale_year_after_groep_8_does_not_fall_back_to_groep_7()
    {
        var culture = Services.GetRequiredService<CultureState>();
        await culture.SetLanguageAsync("nl");

        var level = SchoolLevel.Groep78;
        var year = 7;
        var cut = Render<SchoolClassForm>(ps => ps
            .Add(p => p.ClassName, "8A")
            .Add(p => p.Level, level)
            .Add(p => p.LevelChanged, EventCallback.Factory.Create<SchoolLevel>(this, v => level = v))
            .Add(p => p.Year, year)
            .Add(p => p.YearChanged, EventCallback.Factory.Create<int>(this, v => year = v))
            .Add(p => p.ShowPupilCount, true)
            .Add(p => p.PupilCount, 28));

        await cut.Find("input[value='8']").ChangeAsync(new ChangeEventArgs { Value = "8" });
        Assert.Equal(8, year);

        year = 7;
        cut.Render(ps => ps
            .Add(p => p.ClassName, "8A")
            .Add(p => p.Level, level)
            .Add(p => p.LevelChanged, EventCallback.Factory.Create<SchoolLevel>(this, v => level = v))
            .Add(p => p.Year, year)
            .Add(p => p.YearChanged, EventCallback.Factory.Create<int>(this, v => year = v))
            .Add(p => p.ShowPupilCount, true)
            .Add(p => p.PupilCount, 28));

        Assert.Equal(8, year);
        Assert.Contains("sch-seg__opt is-on", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("value=\"8\"", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Creating_a_class_with_groep_8_saves_groep_8()
    {
        var culture = Services.GetRequiredService<CultureState>();
        await culture.SetLanguageAsync("nl");
        Services.GetRequiredService<NavigationManager>().NavigateTo("http://localhost/school/klassen");

        var cut = Render<SchoolClasses>();
        cut.WaitForAssertion(() => Assert.Contains("Nieuwe klas", cut.Markup, StringComparison.Ordinal));
        await cut.FindAll("button").First(button => button.TextContent.Contains("Nieuwe klas", StringComparison.Ordinal)).ClickAsync();
        cut.WaitForAssertion(() => Assert.Contains("value=\"primary\"", cut.Markup, StringComparison.Ordinal));

        await cut.Find("input[value='primary']").ChangeAsync(new ChangeEventArgs { Value = "primary" });
        cut.WaitForAssertion(() => Assert.Contains("Groep 8", cut.Markup, StringComparison.Ordinal));
        await cut.Find("input[value='8']").ChangeAsync(new ChangeEventArgs { Value = "8" });
        await cut.Find("button[type=submit]").ClickAsync();

        Assert.False(string.IsNullOrWhiteSpace(_handler.PostedBody));
        using var doc = JsonDocument.Parse(_handler.PostedBody!);
        Assert.Equal(8, doc.RootElement.GetProperty("Year").GetInt32());
        Assert.Equal("Groep78", doc.RootElement.GetProperty("Level").GetString());
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

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public string? PostedBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            if (request.Method == HttpMethod.Post && path.Equals("/api/school/classes", StringComparison.OrdinalIgnoreCase))
            {
                PostedBody = request.Content is null
                    ? null
                    : await request.Content.ReadAsStringAsync(cancellationToken);
                var created = new SchoolPortalClassDetailDto(
                    Guid.NewGuid(), "8A", SchoolLevel.Groep78, 8, 2026, "2026-2027", 28,
                    [], false, null, null, null, TestWindowState.NotOpen, null, false, [],
                    PupilQuestionSet.Groep78, true);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(created, ApiJson), Encoding.UTF8, "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", Encoding.UTF8, "application/json")
            };
        }
    }

    private static readonly JsonSerializerOptions ApiJson = CreateApiJson();

    private static JsonSerializerOptions CreateApiJson()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private sealed class AnonymousAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}

public class SchoolRun8ResultsTests : BunitContext
{
    public SchoolRun8ResultsTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        Services.AddSingleton<AuthenticationStateProvider>(new AnonymousAuth());
        Services.AddSingleton(sp => new CultureState(
            sp.GetRequiredService<IJSRuntime>(),
            sp,
            sp.GetRequiredService<AuthenticationStateProvider>()));
        Services.AddSingleton<IFeatureFlags>(new SchoolsOn());
        Services.AddSingleton(new JobsyApiClient(new HttpClient(new ResultsHandler())
        {
            BaseAddress = new Uri("http://api.test/")
        }));
    }

    [Fact]
    public async Task Per_code_table_shows_dream_job_labels_instead_of_slugs()
    {
        var culture = Services.GetRequiredService<CultureState>();
        await culture.SetLanguageAsync("nl");
        Services.GetRequiredService<NavigationManager>().NavigateTo("http://localhost/school/resultaten");

        var cut = Render<SchoolResults>();
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Weet ik nog niet", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Brandweerman/-vrouw", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Dierenarts", cut.Markup, StringComparison.Ordinal);
            Assert.DoesNotContain("weet-ik-nog-niet", cut.Markup, StringComparison.OrdinalIgnoreCase);
        }, TimeSpan.FromSeconds(5));
    }

    private sealed class ResultsHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? "";
            string json;
            if (path.EndsWith("/results", StringComparison.OrdinalIgnoreCase))
            {
                var classId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
                var totals = new ClassResultsAggregate(
                    10, 6, 60, true,
                    [new NamedCount("A", 4)],
                    [new NamedCount("Autonomy", 3)],
                    [
                        new NamedCount("weet-ik-nog-niet", 2),
                        new NamedCount("brandweer", 3),
                        new NamedCount("dierenarts", 1)
                    ],
                    2);
                var results = new SchoolPortalResultsDto(
                    classId,
                    "8A",
                    true,
                    totals,
                    [
                        new SchoolPortalPerCodeResultDto(Guid.NewGuid(), "K7Q-M2P", PupilCodeStatus.Completed, "AI", "Autonomy", "weet-ik-nog-niet"),
                        new SchoolPortalPerCodeResultDto(Guid.NewGuid(), "K7Q-M3P", PupilCodeStatus.Completed, "R", "Connection", "brandweer")
                    ],
                    PupilQuestionSet.Groep78);
                json = JsonSerializer.Serialize(results, ApiJson);
            }
            else if (path.Equals("/api/school/classes", StringComparison.OrdinalIgnoreCase))
            {
                var row = new SchoolPortalClassListItemDto(
                    Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    "8A", SchoolLevel.Groep78, 8, 2026, "2026-2027", [], 10, 6, true,
                    TestWindowState.Open, null, PupilQuestionSet.Groep78);
                json = JsonSerializer.Serialize(new[] { row }, ApiJson);
            }
            else
            {
                json = "[]";
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }

    private static readonly JsonSerializerOptions ApiJson = CreateApiJson();

    private static JsonSerializerOptions CreateApiJson()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
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

    private sealed class AnonymousAuth : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
    }
}
