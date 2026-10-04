using System.Net;
using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Entities;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Scholen;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Scholen;
using Microsoft.AspNetCore.Antiforgery;
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

        using var web = new PupilWebFactory(_api.CreateClient());
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
        private readonly HttpClient _api;

        public PupilWebFactory(HttpClient api) => _api = api;

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

        public ForwardFactory(HttpClient api) => _handler = new ForwardHandler(api);

        public HttpClient CreateClient(string name)
            => new(_handler, disposeHandler: false);

        public void Dispose() => _handler.Dispose();
    }

    private sealed class ForwardHandler : HttpMessageHandler
    {
        private readonly HttpClient _api;

        public ForwardHandler(HttpClient api) => _api = api;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.PathAndQuery ?? "/";
            using var clone = new HttpRequestMessage(request.Method, path);
            if (request.Content is not null)
            {
                var bytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
                clone.Content = new ByteArrayContent(bytes);
                if (request.Content.Headers.ContentType is not null)
                {
                    clone.Content.Headers.ContentType = request.Content.Headers.ContentType;
                }
            }

            return await _api.SendAsync(clone, cancellationToken);
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
