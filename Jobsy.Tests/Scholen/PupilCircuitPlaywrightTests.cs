using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.QuestionSets;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Scholen;
using Jobsy.Web.Auth;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright;

namespace Jobsy.Tests.Scholen;

/// <summary>
/// Real browser against Kestrel. HttpClient never starts a Blazor circuit, so it
/// missed the jump to /login. This test waits until the circuit is up.
/// </summary>
[Collection("PlaywrightSmoke")]
public sealed class PupilCircuitPlaywrightTests : IClassFixture<RoleFunctionalWebAppFactory>, IAsyncLifetime
{
    private readonly RoleFunctionalWebAppFactory _api;
    private PupilKestrelFactory? _web;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public PupilCircuitPlaywrightTests(RoleFunctionalWebAppFactory api) => _api = api;

    public async ValueTask InitializeAsync()
    {
        var exit = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        Assert.Equal(0, exit);
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
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

    [Fact(Timeout = 300_000)]
    public async Task Pupil_stays_on_start_after_the_circuit_and_reaches_the_pdf()
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        var seed = await SeedOpenClassAsync();
        var baseUrl = EnsureWeb();
        Assert.StartsWith("http://127.0.0.1:", baseUrl, StringComparison.Ordinal);

        var page = await _browser!.NewPageAsync();
        page.SetDefaultTimeout(90_000);
        page.SetDefaultNavigationTimeout(90_000);
        await page.Context.ClearCookiesAsync();
        var loginUrl = $"{baseUrl}/leerling?schoolId={seed.SchoolId:D}&classId={seed.ClassId:D}";

        await LoginAsync(page, loginUrl, seed.PlainCode, seed.ClassId);
        await page.WaitForURLAsync("**/leerling/start**", DomReady);
        await page.WaitForFunctionAsync("() => typeof window.Blazor !== 'undefined'");
        await page.WaitForTimeoutAsync(3_000);
        Assert.Contains("/leerling/start", page.Url, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/login", page.Url, StringComparison.OrdinalIgnoreCase);

        await page.GetByRole(AriaRole.Link, new() { Name = "Beginnen" }).ClickAsync();
        await page.WaitForURLAsync("**/leerling/reis**", DomReady);
        await page.GetByRole(AriaRole.Radio, new() { Name = "Soms" }).WaitForAsync();
        await AnswerOneAsync(page);
        Assert.DoesNotContain("/login", page.Url, StringComparison.OrdinalIgnoreCase);

        await page.GetByRole(AriaRole.Button, new() { Name = "Pauze" }).ClickAsync();
        await page.WaitForURLAsync("**/leerling/stop**", DomReady);
        Assert.Contains("done=1", page.Url, StringComparison.Ordinal);
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Goed gedaan!" })).ToBeVisibleAsync();

        await LoginAsync(page, loginUrl, seed.PlainCode, seed.ClassId);
        await page.WaitForURLAsync("**/leerling/reis**", DomReady);
        await page.WaitForFunctionAsync("() => typeof window.Blazor !== 'undefined'");
        await page.WaitForTimeoutAsync(3_000);
        Assert.Contains("/leerling/reis", page.Url, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/login", page.Url, StringComparison.OrdinalIgnoreCase);
        await Assertions.Expect(page.Locator(".ll-question__meta")).ToContainTextAsync("Vraag 2");

        for (var step = 0; step < 80 && !page.Url.Contains("/leerling/dit-ben-jij", StringComparison.OrdinalIgnoreCase)
             && !page.Url.Contains("/leerling/droombaan", StringComparison.OrdinalIgnoreCase); step++)
        {
            Assert.DoesNotContain("/login", page.Url, StringComparison.OrdinalIgnoreCase);
            if (page.Url.Contains("/leerling/eiland", StringComparison.OrdinalIgnoreCase))
            {
                var likes = page.Locator("fieldset.ll-chips").Nth(0);
                var dislikes = page.Locator("fieldset.ll-chips").Nth(1);
                var sport = likes.GetByRole(AriaRole.Button, new() { Name = "Sport", Exact = true });
                var rekenen = dislikes.GetByRole(AriaRole.Button, new() { Name = "Rekenen", Exact = true });
                await sport.ClickAsync();
                await Assertions.Expect(sport).ToHaveClassAsync(new Regex(@"\bis-on\b"));
                await rekenen.ClickAsync();
                await Assertions.Expect(rekenen).ToHaveClassAsync(new Regex(@"\bis-on\b"));
                await page.GetByRole(AriaRole.Button, new() { Name = "Klaar, verder!" }).ClickAsync();
                await page.WaitForFunctionAsync(
                    "() => !location.pathname.includes('/leerling/eiland')");
                continue;
            }

            await AnswerOneAsync(page);
        }

        Assert.True(
            page.Url.Contains("/leerling/dit-ben-jij", StringComparison.OrdinalIgnoreCase)
            || page.Url.Contains("/leerling/droombaan", StringComparison.OrdinalIgnoreCase),
            "Journey did not reach the story. URL: " + page.Url);

        // Results.File sends Content-Disposition: attachment, so a document
        // navigation starts a download. The browser context request uses the
        // same pupil cookies the circuit just used.
        var pdf = await page.Context.APIRequest.GetAsync(baseUrl + "/leerling/pdf");
        Assert.Equal(200, pdf.Status);
        Assert.True(
            pdf.Headers.TryGetValue("content-type", out var contentType)
            && contentType.Contains("application/pdf", StringComparison.OrdinalIgnoreCase),
            "PDF response was " + pdf.Status + " " + pdf.Url);
        Assert.DoesNotContain("/login", pdf.Url, StringComparison.OrdinalIgnoreCase);
        var bytes = await pdf.BodyAsync();
        Assert.True(bytes.Length > 4 && bytes[0] == (byte)'%' && bytes[1] == (byte)'P');
    }

    [Fact(Timeout = 300_000)]
    public async Task Question_card_is_painted_above_the_scene_at_1366_and_390()
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        var baseUrl = EnsureWeb();
        foreach (var (width, height) in new[] { (1366, 900), (390, 844) })
        {
            var pupil = await SeedOpenClassAsync();
            var loginUrl = $"{baseUrl}/leerling?schoolId={pupil.SchoolId:D}&classId={pupil.ClassId:D}";
            var page = await _browser!.NewPageAsync(new BrowserNewPageOptions
            {
                ViewportSize = new ViewportSize { Width = width, Height = height }
            });
            page.SetDefaultTimeout(90_000);
            page.SetDefaultNavigationTimeout(90_000);
            await page.Context.ClearCookiesAsync();
            await LoginAsync(page, loginUrl, pupil.PlainCode, pupil.ClassId);
            await page.WaitForURLAsync("**/leerling/start**", DomReady);
            if (width == 1366)
            {
                await page.GotoAsync($"{baseUrl}/leerling", new()
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded,
                    Timeout = 90_000
                });
                await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Je bent al ingelogd" }))
                    .ToBeVisibleAsync();
                await Assertions.Expect(page.GetByText("ingelogd als medewerker")).ToHaveCountAsync(0);
                await page.GetByRole(AriaRole.Link, new() { Name = "Ga verder" }).ClickAsync();
                await page.WaitForURLAsync("**/leerling/start**", DomReady);
            }

            await page.GetByRole(AriaRole.Link, new() { Name = "Beginnen" }).ClickAsync();
            await page.WaitForURLAsync("**/leerling/reis**", DomReady);
            await page.Locator(".ll-card").WaitForAsync();
            var hit = await page.EvaluateAsync<string>(
                """
                () => {
                  const card = document.querySelector('.ll-card');
                  if (!card) return 'missing-card';
                  const restored = [];
                  document.querySelectorAll('.ll-scene').forEach((el) => {
                    restored.push([el, el.style.pointerEvents]);
                    el.style.pointerEvents = 'auto';
                  });
                  const r = card.getBoundingClientRect();
                  const x = r.left + r.width / 2;
                  const y = r.top + r.height / 2;
                  const stack = document.elementsFromPoint(x, y);
                  restored.forEach(([el, pe]) => { el.style.pointerEvents = pe; });
                  const top = stack[0];
                  if (!top) return 'none';
                  if (top.closest('.ll-scene')) return 'scene';
                  if (top.closest('.ll-card')) return 'card';
                  return 'other';
                }
                """);
            Assert.Equal("card", hit);
            await page.CloseAsync();
        }
    }

    [Fact(Timeout = 300_000)]
    public async Task Teacher_group_view_loads_with_five_finished_pupils()
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        var seed = await SeedFiveFinishedGroep78Async();
        var baseUrl = EnsureWeb();
        var page = await _browser!.NewPageAsync();
        page.SetDefaultTimeout(90_000);
        page.SetDefaultNavigationTimeout(90_000);
        await page.Context.ClearCookiesAsync();
        var signIn = $"{baseUrl}/__test/staff?user={seed.TeacherId:D}&school={seed.SchoolId:D}&role=Teacher&return=/leraar/klas/{seed.ClassId:D}/groep";
        await page.GotoAsync(signIn, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
        await page.WaitForURLAsync("**/leraar/klas/**/groep**", DomReady);
        await page.WaitForFunctionAsync("() => typeof window.Blazor !== 'undefined'");
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Interesses in de klas" })).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByText("De klasgegevens konden niet geladen worden")).ToHaveCountAsync(0);
        await page.CloseAsync();
    }

    private string EnsureWeb()
    {
        if (_web is not null)
        {
            return _web.ServerAddress.TrimEnd('/');
        }

        // Disposing a Kestrel host mid-suite can stop the next host
        // (background-service cancellation). One host for the class avoids that.
        InvalidOperationException? last = null;
        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                _web?.Dispose();
                _web = new PupilKestrelFactory(_api.Server.CreateHandler());
                _ = _web.CreateClient();
                return _web.ServerAddress.TrimEnd('/');
            }
            catch (InvalidOperationException ex)
            {
                last = ex;
                _web?.Dispose();
                _web = null;
            }
        }

        throw last ?? new InvalidOperationException("Pupil web host did not start.");
    }

    private static readonly PageWaitForURLOptions DomReady = new()
    {
        WaitUntil = WaitUntilState.DOMContentLoaded,
        Timeout = 90_000
    };

    private static async Task LoginAsync(IPage page, string loginUrl, string code, Guid classId)
    {
        var classValue = classId.ToString("D");
        Exception? last = null;
        for (var attempt = 0; attempt < 2; attempt++)
        {
            await page.GotoAsync(loginUrl, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            // The class list comes from the API. Under suite load that call can fail
            // once and leave only the placeholder, which posts as an unknown class.
            try
            {
                await page.WaitForFunctionAsync(
                    """
                    id => {
                      const select = document.querySelector('select[name=classId]');
                      return !!select && [...select.options].some(o => o.value === id);
                    }
                    """,
                    classValue,
                    new() { Timeout = 20_000 });
            }
            catch (TimeoutException ex) when (attempt == 0)
            {
                last = ex;
                continue;
            }

            // One turn: Blazor must not swap the form between selecting the class and POST.
            // Under suite load the option can vanish in that gap. Retry the whole login once.
            try
            {
                await page.EvaluateAsync(
                    """
                    ([id, code]) => {
                      const select = document.querySelector('select[name=classId]');
                      const input = document.querySelector('input[name=code]');
                      if (!select || !input) throw new Error('login form missing');
                      const option = [...select.options].find(o => o.value === id);
                      if (!option) throw new Error('class option missing');
                      option.selected = true;
                      input.value = code;
                      const form = input.closest('form');
                      if (typeof form.requestSubmit === 'function') form.requestSubmit();
                      else form.submit();
                    }
                    """,
                    new[] { classValue, code });
            }
            catch (PlaywrightException ex) when (attempt == 0
                && (ex.Message.Contains("class option missing", StringComparison.Ordinal)
                    || ex.Message.Contains("login form missing", StringComparison.Ordinal)))
            {
                last = ex;
                continue;
            }
            try
            {
                await page.WaitForURLAsync(
                    url => url.Contains("/leerling/start", StringComparison.OrdinalIgnoreCase)
                        || url.Contains("/leerling/reis", StringComparison.OrdinalIgnoreCase)
                        || url.Contains("/leerling/dit-ben-jij", StringComparison.OrdinalIgnoreCase),
                    new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 45_000 });
                return;
            }
            catch (TimeoutException ex) when (attempt == 0)
            {
                last = ex;
            }
        }

        var body = await page.Locator("body").InnerTextAsync(new() { Timeout = 5_000 });
        var snippet = body.Length <= 600 ? body : body[..600];
        throw new TimeoutException($"Login stayed on {page.Url}. Body: {snippet}", last);
    }

    private static async Task AnswerOneAsync(IPage page)
    {
        var meta = page.Locator(".ll-question__meta");
        await meta.WaitForAsync();
        var before = await meta.InnerTextAsync();
        await page.GetByRole(AriaRole.Radio, new() { Name = "Soms" }).ClickAsync();
        await page.WaitForFunctionAsync(
            """
            prev => {
                const meta = document.querySelector('.ll-question__meta');
                const path = location.pathname;
                return (meta && meta.innerText !== prev) || !path.includes('/leerling/reis');
            }
            """,
            before);
    }

    private async Task EnableSchoolsAsync()
    {
        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var row = await db.PlatformFeatureSettings.FirstOrDefaultAsync();
        if (row is null)
        {
            db.PlatformFeatureSettings.Add(new PlatformFeatureSettings
            {
                Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                SchoolsEnabled = true
            });
        }
        else
        {
            row.SchoolsEnabled = true;
        }

        await db.SaveChangesAsync();
    }

    private async Task<Seed> SeedOpenClassAsync()
    {
        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var codes = scope.ServiceProvider.GetRequiredService<IPupilCodeService>();
        var school = new School
        {
            Id = Guid.NewGuid(),
            Name = "Circuit " + Guid.NewGuid().ToString("N")[..6],
            City = "Naaldwijk",
            AllowedEmailDomains = "[\"voorbeeldcollege.nl\"]",
            IsActive = true,
            ProcessorAgreementSignedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            ProcessorAgreementVersion = "1.0",
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = _api.AdminId
        };
        var cls = new SchoolClass
        {
            Id = Guid.NewGuid(),
            SchoolId = school.Id,
            Name = "7A",
            Level = SchoolLevel.Groep78,
            Year = 7,
            QuestionSet = PupilQuestionSet.Groep78,
            SchoolYearStart = SchoolYear.Current(DateOnly.FromDateTime(DateTime.UtcNow)),
            PupilCount = 1,
            TestWindow = TestWindowState.Open,
            ParentalInfoConfirmedAtUtc = DateTime.UtcNow,
            ParentalInfoConfirmedByUserId = _api.AdminId,
            ParentalInfoTextVersion = "1",
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Schools.Add(school);
        db.SchoolClasses.Add(cls);
        await db.SaveChangesAsync();
        var generated = await codes.GenerateAsync(1, cls);
        return new Seed(school.Id, cls.Id, codes.Unprotect(generated[0].CodeProtected)!);
    }

    private async Task<TeacherSeed> SeedFiveFinishedGroep78Async()
    {
        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var schoolId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var domain = $"p{schoolId:N}"[..12] + ".nl";
        var school = new School
        {
            Id = schoolId,
            Name = "Vijf " + Guid.NewGuid().ToString("N")[..4],
            City = "Naaldwijk",
            AllowedEmailDomains = System.Text.Json.JsonSerializer.Serialize(new[] { domain }),
            IsActive = true,
            ProcessorAgreementSignedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            ProcessorAgreementVersion = "1.0",
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = _api.AdminId
        };
        var cls = new SchoolClass
        {
            Id = Guid.NewGuid(),
            SchoolId = school.Id,
            Name = "7A",
            Level = SchoolLevel.Groep78,
            Year = 7,
            QuestionSet = PupilQuestionSet.Groep78,
            SchoolYearStart = SchoolYear.Current(DateOnly.FromDateTime(DateTime.UtcNow)),
            PupilCount = 5,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Schools.Add(school);
        db.SchoolClasses.Add(cls);
        db.Users.Add(new User
        {
            Id = teacherId,
            Email = $"t-{teacherId:N}@{domain}",
            FullName = "R. Jansen",
            Role = UserRole.Teacher,
            SchoolId = schoolId,
            IsActive = true,
            AuthenticatorEnabled = true,
            SessionVersion = 0
        });
        db.TeacherClassAssignments.Add(new TeacherClassAssignment
        {
            TeacherUserId = teacherId,
            SchoolClassId = cls.Id,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var codeService = scope.ServiceProvider.GetRequiredService<IPupilCodeService>();
        var generated = await codeService.GenerateAsync(5, cls);
        var registry = scope.ServiceProvider.GetRequiredService<IPupilQuestionSetRegistry>();
        var bank = registry.Get(PupilQuestionSet.Groep78).Bank;
        var answers = bank.AllItems.ToDictionary(i => i.Id, _ => 4);
        var answersJson = System.Text.Json.JsonSerializer.Serialize(answers);
        var now = DateTime.UtcNow;
        foreach (var code in generated)
        {
            db.PupilProgresses.Add(new PupilProgress
            {
                PupilCodeId = code.Id,
                AnswersJson = answersJson,
                CurrentIndex = bank.AllItems.Count,
                LikesJson = """["dieren"]""",
                DislikesJson = """["lang-stilzitten"]""",
                StartedAtUtc = now.AddMinutes(-20),
                UpdatedAtUtc = now,
                CompletedAtUtc = now
            });
        }

        await db.SaveChangesAsync();
        var builder = scope.ServiceProvider.GetRequiredService<IPupilResultBuilder>();
        foreach (var code in generated)
        {
            await builder.BuildAsync(code.Id);
        }

        return new TeacherSeed(schoolId, cls.Id, teacherId);
    }

    private sealed record Seed(Guid SchoolId, Guid ClassId, string PlainCode);

    private sealed record TeacherSeed(Guid SchoolId, Guid ClassId, Guid TeacherId);

    private sealed class PupilKestrelFactory : WebApplicationFactory<Jobsy.Web.WebAssemblyMarker>
    {
        private readonly HttpMessageHandler _api;
        private IHost? _kestrel;

        public PupilKestrelFactory(HttpMessageHandler api) => _api = api;

        public string ServerAddress { get; private set; } = "";

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
                services.AddSingleton<IStartupFilter, StaffTestSignInFilter>();
                services.RemoveAll<IFeatureFlags>();
                services.AddSingleton<IFeatureFlags>(new SchoolsOnFlags());
                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton<IHttpClientFactory>(new ForwardFactory(_api));
                services.RemoveAll<JobsyApiClient>();
                services.AddScoped(sp =>
                {
                    var auth = new JobsyApiAuthHandler(
                        sp.GetRequiredService<IHttpContextAccessor>(),
                        sp.GetRequiredService<AuthenticationStateProvider>(),
                        sp,
                        sp.GetRequiredService<IConfiguration>(),
                        sp.GetRequiredService<JobsyAccessTokenIssuer>())
                    {
                        InnerHandler = new ForwardHandler(_api)
                    };
                    var http = new HttpClient(auth, disposeHandler: false)
                    {
                        BaseAddress = new Uri("http://api.test/")
                    };
                    return new JobsyApiClient(http);
                });
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

    private sealed class StaffTestSignInFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            => app =>
            {
                app.Use(async (ctx, nxt) =>
                {
                    if (!ctx.Request.Path.Equals("/__test/staff", StringComparison.OrdinalIgnoreCase))
                    {
                        await nxt();
                        return;
                    }

                    var user = ctx.Request.Query["user"].ToString();
                    var school = ctx.Request.Query["school"].ToString();
                    var role = ctx.Request.Query["role"].ToString();
                    if (string.IsNullOrWhiteSpace(role))
                    {
                        role = "Teacher";
                    }

                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.NameIdentifier, user),
                        new(ClaimTypes.Role, role),
                        new(JobsyClaimTypes.SchoolId, school),
                        new(JobsyClaimTypes.SessionVersion, "0"),
                        new(JobsyClaimTypes.MfaVerified, "1")
                    };
                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await ctx.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));
                    var ret = ctx.Request.Query["return"].ToString();
                    if (string.IsNullOrWhiteSpace(ret) || !ret.StartsWith('/') || ret.StartsWith("//", StringComparison.Ordinal))
                    {
                        ret = "/";
                    }

                    ctx.Response.Redirect(ret);
                });
                next(app);
            };
    }

    private sealed class SchoolsOnFlags : IFeatureFlags
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

    private sealed class ForwardFactory : IHttpClientFactory, IDisposable
    {
        private readonly HttpMessageHandler _handler;

        public ForwardFactory(HttpMessageHandler api) => _handler = new ForwardHandler(api);

        public HttpClient CreateClient(string name)
            => new(_handler, disposeHandler: false);

        public void Dispose() => _handler.Dispose();
    }

    private sealed class ForwardHandler : HttpMessageHandler
    {
        private readonly HttpMessageInvoker _api;

        public ForwardHandler(HttpMessageHandler api)
            => _api = new HttpMessageInvoker(api, disposeHandler: false);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.PathAndQuery ?? "/";
            if (!path.StartsWith('/'))
            {
                path = "/" + path;
            }

            using var clone = new HttpRequestMessage(request.Method, new Uri("http://localhost" + path));
            foreach (var header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            if (request.Content is not null)
            {
                var bytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
                clone.Content = new ByteArrayContent(bytes);
                foreach (var header in request.Content.Headers)
                {
                    clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            return await _api.SendAsync(clone, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _api.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
