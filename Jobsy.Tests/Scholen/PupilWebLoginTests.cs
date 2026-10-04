using System.Net;
using System.Net.Http.Json;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Entities;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Scholen;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Scholen;
using Jobsy.Web.Auth;
using Jobsy.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Jobsy.Tests.Scholen;

public class PupilWebLoginTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private readonly RoleFunctionalWebAppFactory _api;

    public PupilWebLoginTests(RoleFunctionalWebAppFactory api) => _api = api;

    [Fact]
    public async Task Web_login_with_a_valid_code_sets_the_pupil_cookie()
    {
        await EnableSchoolsAsync();
        var seed = await SeedOpenClassAsync();

        using var web = new PupilWebFactory(_api.Server.CreateHandler());
        using var client = web.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["schoolId"] = seed.SchoolId.ToString("D"),
            ["classId"] = seed.ClassId.ToString("D"),
            ["code"] = seed.PlainCode
        });
        var response = await client.PostAsync("/leerling/login", body);
        var text = await response.Content.ReadAsStringAsync();
        Assert.True(
            response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found,
            $"expected redirect, got {(int)response.StatusCode} {text}");
        var location = response.Headers.Location?.ToString() ?? "";
        Assert.Contains("/leerling/start", location, StringComparison.Ordinal);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies!, c => c.StartsWith(PupilAuthDefaults.CookieName + "=", StringComparison.Ordinal));
        Assert.Contains(cookies!, c => c.StartsWith(PupilApiSessionCookie.Name + "=", StringComparison.Ordinal));
        var webTicket = CookieValue(cookies!, PupilAuthDefaults.CookieName);
        var apiTicket = CookieValue(cookies!, PupilApiSessionCookie.Name);
        Assert.False(string.IsNullOrWhiteSpace(webTicket));
        Assert.False(string.IsNullOrWhiteSpace(apiTicket));
        Assert.NotEqual(webTicket, apiTicket);
    }

    [Fact]
    public async Task Logged_in_pupil_loads_start_and_progress_across_separate_hosts()
    {
        await EnableSchoolsAsync();
        var seed = await SeedOpenClassAsync();
        var apiHandler = _api.Server.CreateHandler();
        using var web = new PupilWebFactory(apiHandler);
        using var client = HtmlClient(web);

        var login = await PostLoginAsync(client, seed);
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Contains("/leerling/start", login.Headers.Location?.ToString() ?? "", StringComparison.Ordinal);
        var apiTicket = CookieValue(login.Headers.GetValues("Set-Cookie"), PupilApiSessionCookie.Name);
        Assert.False(string.IsNullOrWhiteSpace(apiTicket));

        var start = await client.GetAsync("/leerling/start");
        var html = await start.Content.ReadAsStringAsync();
        Assert.True(start.StatusCode == HttpStatusCode.OK, $"start returned {(int)start.StatusCode} {html[..Math.Min(html.Length, 400)]}");
        Assert.DoesNotContain("/login", start.Headers.Location?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.Contains(PupilSessionDuration.Groep78, html, StringComparison.Ordinal);
        Assert.DoesNotContain("Inloggen bij Lobsy", html, StringComparison.Ordinal);

        using var api = new HttpClient(apiHandler, disposeHandler: false) { BaseAddress = new Uri("http://localhost/") };
        var progress = await GetProgressAsync(api, apiTicket!);
        Assert.Equal(HttpStatusCode.OK, progress.Status);
        Assert.False(progress.Body!.Completed);
        Assert.Equal(60, progress.Body.TotalItems);
    }

    [Fact]
    public async Task Pupil_can_pause_and_resume_the_same_question()
    {
        await EnableSchoolsAsync();
        var seed = await SeedOpenClassAsync();
        var apiHandler = _api.Server.CreateHandler();
        using var web = new PupilWebFactory(apiHandler);
        using var client = HtmlClient(web);
        using var api = new HttpClient(apiHandler, disposeHandler: false) { BaseAddress = new Uri("http://localhost/") };

        var login = await PostLoginAsync(client, seed);
        var apiTicket = CookieValue(login.Headers.GetValues("Set-Cookie"), PupilApiSessionCookie.Name)!;
        var first = await GetProgressAsync(api, apiTicket);
        Assert.Equal(HttpStatusCode.OK, first.Status);
        var itemId = first.Body!.CurrentItemId;
        Assert.False(string.IsNullOrWhiteSpace(itemId));

        var saved = await PutAnswerAsync(api, apiTicket, itemId!, 3);
        Assert.Equal(HttpStatusCode.OK, saved.StatusCode);

        var stop = await client.PostAsync("/leerling/pauze", new FormUrlEncodedContent([]));
        Assert.Equal(HttpStatusCode.Redirect, stop.StatusCode);
        Assert.Contains("/leerling/stop", stop.Headers.Location?.ToString() ?? "", StringComparison.Ordinal);

        var again = await PostLoginAsync(client, seed);
        Assert.Equal(HttpStatusCode.Redirect, again.StatusCode);
        Assert.Contains("/leerling/reis", again.Headers.Location?.ToString() ?? "", StringComparison.Ordinal);
        var resumedTicket = CookieValue(again.Headers.GetValues("Set-Cookie"), PupilApiSessionCookie.Name)!;
        var resumed = await GetProgressAsync(api, resumedTicket);
        Assert.Equal(HttpStatusCode.OK, resumed.Status);
        Assert.Equal(1, resumed.Body!.AnsweredCount);
        Assert.Equal(3, resumed.Body.Answers[itemId!]);

        var reis = await client.GetAsync("/leerling/reis");
        var reisHtml = await reis.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, reis.StatusCode);
        Assert.DoesNotContain("/login", reis.Headers.Location?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Inloggen bij Lobsy", reisHtml, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Pupil_cookie_challenge_with_a_leftover_api_ticket_shows_expired()
    {
        await EnableSchoolsAsync();
        var seed = await SeedOpenClassAsync();
        using var web = new PupilWebFactory(_api.Server.CreateHandler());
        using var client = web.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html");

        var login = await PostLoginAsync(client, seed);
        var apiTicket = CookieValue(login.Headers.GetValues("Set-Cookie"), PupilApiSessionCookie.Name);
        Assert.False(string.IsNullOrWhiteSpace(apiTicket));

        var stale = new HttpRequestMessage(HttpMethod.Get, "/leerling/start");
        stale.Headers.TryAddWithoutValidation("Cookie", PupilApiSessionCookie.Name + "=" + apiTicket);
        stale.Headers.Accept.ParseAdd("text/html");
        var expired = await client.SendAsync(stale);
        Assert.Equal(HttpStatusCode.Redirect, expired.StatusCode);
        Assert.Contains("error=expired", expired.Headers.Location?.ToString() ?? "", StringComparison.Ordinal);
        Assert.Equal("no-store", expired.Headers.CacheControl?.ToString());
        Assert.True(expired.Headers.TryGetValues("Set-Cookie", out var cleared));
        Assert.Contains(cleared!, c => c.StartsWith(PupilApiSessionCookie.Name + "=", StringComparison.Ordinal));

        var first = new HttpRequestMessage(HttpMethod.Get, "/leerling/start");
        first.Headers.Accept.ParseAdd("text/html");
        var fresh = await client.SendAsync(first);
        var location = fresh.Headers.Location?.ToString() ?? "";
        Assert.Equal(HttpStatusCode.Redirect, fresh.StatusCode);
        Assert.Contains("/leerling", location, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("error=expired", location, StringComparison.Ordinal);
        Assert.DoesNotContain("/login", location, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Logged_in_pupil_downloads_the_story_pdf()
    {
        await EnableSchoolsAsync();
        var seed = await SeedOpenClassAsync();
        var apiHandler = _api.Server.CreateHandler();
        using var web = new PupilWebFactory(apiHandler);
        using var client = HtmlClient(web);
        using var api = new HttpClient(apiHandler, disposeHandler: false) { BaseAddress = new Uri("http://localhost/") };

        var login = await PostLoginAsync(client, seed);
        var apiTicket = CookieValue(login.Headers.GetValues("Set-Cookie"), PupilApiSessionCookie.Name)!;
        await FinishJourneyAsync(api, apiTicket);

        var pdf = await client.GetAsync("/leerling/pdf");
        var bytes = await pdf.Content.ReadAsByteArrayAsync();
        Assert.True(pdf.StatusCode == HttpStatusCode.OK, $"pdf returned {(int)pdf.StatusCode}");
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
        Assert.True(bytes.Length > 4 && bytes[0] == (byte)'%' && bytes[1] == (byte)'P' && bytes[2] == (byte)'D' && bytes[3] == (byte)'F');
        Assert.DoesNotContain("/login", pdf.Headers.Location?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    private static HttpClient HtmlClient(PupilWebFactory web)
    {
        var client = web.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html");
        return client;
    }

    private static Task<HttpResponseMessage> PostLoginAsync(HttpClient client, Seed seed)
        => client.PostAsync("/leerling/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["schoolId"] = seed.SchoolId.ToString("D"),
            ["classId"] = seed.ClassId.ToString("D"),
            ["code"] = seed.PlainCode
        }));

    private static string? CookieValue(IEnumerable<string> setCookies, string name)
    {
        foreach (var header in setCookies)
        {
            var pair = header.Split(';', 2)[0].Trim();
            var eq = pair.IndexOf('=');
            if (eq <= 0)
            {
                continue;
            }

            if (string.Equals(pair[..eq].Trim(), name, StringComparison.Ordinal))
            {
                return pair[(eq + 1)..].Trim();
            }
        }

        return null;
    }

    private static async Task<(HttpStatusCode Status, PupilProgressStateDto? Body)> GetProgressAsync(
        HttpClient api,
        string apiTicket)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/pupil/progress");
        request.Headers.TryAddWithoutValidation("Cookie", PupilAuthDefaults.CookieName + "=" + apiTicket);
        var response = await api.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            return (response.StatusCode, null);
        }

        var body = await response.Content.ReadFromJsonAsync<PupilProgressStateDto>(JobsyApiClient.ApiJson);
        return (response.StatusCode, body);
    }

    private static async Task<HttpResponseMessage> PutAnswerAsync(
        HttpClient api,
        string apiTicket,
        string itemId,
        int value)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Put,
            "/api/pupil/progress/answers/" + Uri.EscapeDataString(itemId))
        {
            Content = JsonContent.Create(new PupilAnswerRequest(value), options: JobsyApiClient.ApiJson)
        };
        request.Headers.TryAddWithoutValidation("Cookie", PupilAuthDefaults.CookieName + "=" + apiTicket);
        return await api.SendAsync(request);
    }

    private static async Task FinishJourneyAsync(HttpClient api, string apiTicket)
    {
        for (var step = 0; step < 80; step++)
        {
            var progress = await GetProgressAsync(api, apiTicket);
            Assert.Equal(HttpStatusCode.OK, progress.Status);
            if (progress.Body!.Completed)
            {
                return;
            }

            if (progress.Body.NeedsIsland || progress.Body.NextStep == "island")
            {
                var chips = new HttpRequestMessage(HttpMethod.Put, "/api/pupil/progress/chips")
                {
                    Content = JsonContent.Create(
                        new PupilChipsRequest(["sport"], ["rekenen"], null, null),
                        options: JobsyApiClient.ApiJson)
                };
                chips.Headers.TryAddWithoutValidation("Cookie", PupilAuthDefaults.CookieName + "=" + apiTicket);
                var savedChips = await api.SendAsync(chips);
                Assert.Equal(HttpStatusCode.OK, savedChips.StatusCode);
                continue;
            }

            Assert.False(string.IsNullOrWhiteSpace(progress.Body.CurrentItemId));
            var saved = await PutAnswerAsync(api, apiTicket, progress.Body.CurrentItemId!, 3);
            Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        }

        Assert.Fail("Pupil journey did not finish.");
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
            Name = "Web Login " + Guid.NewGuid().ToString("N")[..6],
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

    private sealed class PupilWebFactory : WebApplicationFactory<Jobsy.Web.WebAssemblyMarker>
    {
        private readonly HttpMessageHandler _api;

        public PupilWebFactory(HttpMessageHandler api) => _api = api;

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
                services.RemoveAll<IAntiforgery>();
                services.AddSingleton<IAntiforgery>(new AlwaysValidAntiforgery());
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
            // The Web client calls http://api.test/. The test server only accepts localhost.
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

    private sealed class AlwaysValidAntiforgery : IAntiforgery
    {
        public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext)
            => new("req", "cookie", "form", "header");

        public AntiforgeryTokenSet GetTokens(HttpContext httpContext) => GetAndStoreTokens(httpContext);

        public Task<bool> IsRequestValidAsync(HttpContext httpContext) => Task.FromResult(true);

        public void ValidateRequest(HttpContext httpContext)
        {
            _ = httpContext;
        }

        public Task ValidateRequestAsync(HttpContext httpContext)
        {
            _ = httpContext;
            return Task.CompletedTask;
        }

        public void SetCookieTokenAndHeader(HttpContext httpContext)
        {
            _ = httpContext;
        }
    }
}
