using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Jobsy.Core.Authorization;
using Jobsy.Core.Entities;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Scholen;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Scholen;
using Jobsy.Web.Auth;
using Jobsy.Web.Services;
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

    [Fact(Timeout = 180_000)]
    public async Task Pupil_stays_on_start_after_the_circuit_and_reaches_the_pdf()
    {
        _ = TestContext.Current.CancellationToken;
        await EnableSchoolsAsync();
        var seed = await SeedOpenClassAsync();
        _web = new PupilKestrelFactory(_api.Server.CreateHandler());
        _ = _web.CreateClient();
        var baseUrl = _web.ServerAddress.TrimEnd('/');
        Assert.StartsWith("http://127.0.0.1:", baseUrl, StringComparison.Ordinal);

        var page = await _browser!.NewPageAsync();
        page.SetDefaultTimeout(20_000);
        var loginUrl = $"{baseUrl}/leerling?schoolId={seed.SchoolId:D}&classId={seed.ClassId:D}";

        await LoginAsync(page, loginUrl, seed.PlainCode, seed.ClassId);
        await page.WaitForURLAsync("**/leerling/start**");
        await page.WaitForFunctionAsync("() => typeof window.Blazor !== 'undefined'");
        await page.WaitForTimeoutAsync(3_000);
        Assert.Contains("/leerling/start", page.Url, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/login", page.Url, StringComparison.OrdinalIgnoreCase);

        await page.GetByRole(AriaRole.Link, new() { Name = "Beginnen" }).ClickAsync();
        await page.WaitForURLAsync("**/leerling/reis**");
        await page.GetByRole(AriaRole.Radio, new() { Name = "Soms" }).WaitForAsync();
        await AnswerOneAsync(page);
        Assert.DoesNotContain("/login", page.Url, StringComparison.OrdinalIgnoreCase);

        await page.GetByRole(AriaRole.Button, new() { Name = "Pauze" }).ClickAsync();
        await page.WaitForURLAsync("**/leerling/stop**");
        Assert.Contains("done=1", page.Url, StringComparison.Ordinal);
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Goed gedaan!" })).ToBeVisibleAsync();

        await LoginAsync(page, loginUrl, seed.PlainCode, seed.ClassId);
        await page.WaitForURLAsync("**/leerling/reis**");
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

    private static async Task LoginAsync(IPage page, string loginUrl, string code, Guid classId)
    {
        await page.GotoAsync(loginUrl);
        await page.Locator("select[name=classId]").SelectOptionAsync(classId.ToString("D"));
        await page.Locator("input[name=code]").FillAsync(code);
        await page.Locator("button.ll-login__submit").ClickAsync();
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

    private sealed record Seed(Guid SchoolId, Guid ClassId, string PlainCode);

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
