using System.Net;
using System.Text;
using System.Text.Json;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public class KvkSearchTests
{
    [Fact]
    public async Task Eight_digits_search_uses_kvkNummer_query()
    {
        await using var db = CreateDb();
        Uri? seen = null;
        var handler = new RecordingHandler(request =>
        {
            seen = request.RequestUri;
            return JsonResponse(new { totaal = 0, resultaten = Array.Empty<object>() });
        });
        var sut = CreateSut(db, handler, apiKey: "test-kvk-key");

        await sut.SearchAsync(new KvkSearchQuery("90.12 3456"));

        Assert.NotNull(seen);
        Assert.Contains("kvkNummer=90123456", seen!.Query, StringComparison.Ordinal);
        Assert.DoesNotContain("naam=", seen.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Short_name_throws_validation()
    {
        await using var db = CreateDb();
        var sut = CreateSut(db, handler: null, apiKey: "test-kvk-key");

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.SearchAsync(new KvkSearchQuery("gr")));
        Assert.Contains("3", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Name_and_place_query_contains_naam_and_plaats()
    {
        await using var db = CreateDb();
        Uri? seen = null;
        var handler = new RecordingHandler(request =>
        {
            seen = request.RequestUri;
            return JsonResponse(LoadFixture("zoeken-groen-en-zorg.json"));
        });
        var sut = CreateSut(db, handler, apiKey: "test-kvk-key");

        var result = await sut.SearchAsync(new KvkSearchQuery("groen en zorg", "Utrecht"));

        Assert.NotNull(seen);
        Assert.Contains("naam=groen%20en%20zorg", seen!.Query, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("plaats=Utrecht", seen.Query, StringComparison.Ordinal);
        Assert.Equal(KvkLookupStatus.Ok, result.Status);
        Assert.Contains(result.Hits, h => h.KvkNumber == "90123456" && h.VestigingCount == 3);
    }

    [Fact]
    public async Task Groups_hits_per_kvk_with_vestiging_count()
    {
        await using var db = CreateDb();
        var handler = new RecordingHandler(_ =>
            JsonResponse(LoadFixture("zoeken-groen-en-zorg.json")));
        var sut = CreateSut(db, handler, apiKey: "test-kvk-key");

        var result = await sut.SearchAsync(new KvkSearchQuery("groen en zorg"));

        Assert.Equal(4, result.Hits.Count);
        Assert.Equal(4, result.Hits.Select(h => h.KvkNumber).Distinct().Count());
        Assert.Equal(3, result.Hits.Single(h => h.KvkNumber == "90123456").VestigingCount);
        Assert.Equal(2, result.Hits.Single(h => h.KvkNumber == "66554433").VestigingCount);
    }

    [Fact]
    public async Task IsOnLobsy_true_only_with_active_member()
    {
        await using var db = CreateDb();
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "owner@example.com",
            FullName = "Owner",
            Role = UserRole.EnterpriseManager,
            IsActive = true
        });
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Stichting",
            KvkNumber = "81234567",
            KvkEstablishmentId = "81234567_0001",
            Address = "x",
            Location = new GeoPoint(52, 5),
            Type = CompanyType.Employer,
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.Backfill,
            VerifiedAtUtc = DateTime.UtcNow,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        });
        db.UserCompanies.Add(new UserCompany { UserId = userId, CompanyId = companyId });
        await db.SaveChangesAsync();

        var handler = new RecordingHandler(_ =>
            JsonResponse(LoadFixture("zoeken-groen-en-zorg.json")));
        var sut = CreateSut(db, handler, apiKey: "test-kvk-key");

        var result = await sut.SearchAsync(new KvkSearchQuery("groen en zorg"));

        Assert.True(result.Hits.Single(h => h.KvkNumber == "81234567").IsOnLobsy);
        Assert.False(result.Hits.Single(h => h.KvkNumber == "90123456").IsOnLobsy);
    }

    [Fact]
    public async Task Search_cache_hit_skips_second_http_call()
    {
        await using var db = CreateDb();
        var calls = 0;
        var handler = new RecordingHandler(_ =>
        {
            calls++;
            return JsonResponse(LoadFixture("zoeken-groen-en-zorg.json"));
        });
        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = CreateSut(db, handler, apiKey: "test-kvk-key", cache: cache);

        await sut.SearchAsync(new KvkSearchQuery("groen en zorg", "Utrecht"));
        await sut.SearchAsync(new KvkSearchQuery("groen en zorg", "Utrecht"));

        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Search_5xx_is_unavailable()
    {
        await using var db = CreateDb();
        var handler = new RecordingHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var sut = CreateSut(db, handler, apiKey: "test-kvk-key");

        var result = await sut.SearchAsync(new KvkSearchQuery("groen en zorg"));

        Assert.Equal(KvkLookupStatus.Unavailable, result.Status);
    }

    [Fact]
    public async Task Profile_parses_websites_legal_form_and_addresses()
    {
        await using var db = CreateDb();
        var handler = new RecordingHandler(request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path.Contains("/vestigingen", StringComparison.Ordinal))
            {
                return JsonResponse(LoadFixture("vestigingen-90123456.json"));
            }

            if (path.Contains("/basisprofielen/90123456", StringComparison.Ordinal))
            {
                return JsonResponse(LoadFixture("basisprofiel-90123456.json"));
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
        var sut = CreateSut(db, handler, apiKey: "test-kvk-key");

        var profile = await sut.GetProfileAsync("90123456");

        Assert.Equal(KvkLookupStatus.Ok, profile.Status);
        Assert.Equal("Besloten Vennootschap", profile.LegalForm);
        Assert.Contains("groenenzorg.nl", profile.Websites);
        Assert.Equal(3, profile.Establishments.Count);
        var hq = profile.Establishments.Single(e => e.EstablishmentNumber == "000045678901");
        Assert.NotNull(hq.VisitingAddress);
        Assert.Equal("Utrecht", hq.VisitingAddress!.Place);
        Assert.NotNull(hq.PostalAddress);
        Assert.Contains("3500AA", hq.PostalAddress!.FormattedLine, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Profile_uses_exactly_two_kvk_calls_then_cache()
    {
        await using var db = CreateDb();
        var calls = 0;
        var handler = new RecordingHandler(request =>
        {
            calls++;
            var path = request.RequestUri!.AbsolutePath;
            if (path.Contains("/vestigingen", StringComparison.Ordinal))
            {
                return JsonResponse(LoadFixture("vestigingen-90123456.json"));
            }

            return JsonResponse(LoadFixture("basisprofiel-90123456.json"));
        });
        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = CreateSut(db, handler, apiKey: "test-kvk-key", cache: cache);

        await sut.GetProfileAsync("90123456");
        Assert.Equal(2, calls);

        await sut.GetProfileAsync("90123456");
        Assert.Equal(2, calls);
    }

    private static object LoadFixture(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "kvk", fileName);
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<JsonElement>(json);
    }

    private static KvkHandelsregisterService CreateSut(
        JobsyDbContext db,
        HttpMessageHandler? handler,
        string? apiKey,
        IMemoryCache? cache = null)
    {
        var credentials = new IntegrationCredentialService(db, new PassthroughSecretProtector());
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            credentials.UpsertAsync(
                IntegrationKey.Kvk,
                new IntegrationCredentialUpdate(ApiKey: apiKey)).GetAwaiter().GetResult();
        }

        var http = new NamedHttpClientFactory(handler ?? new RecordingHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError)));
        return new KvkHandelsregisterService(
            db,
            credentials,
            http,
            new KvkServiceStub(db),
            NullLogger<KvkHandelsregisterService>.Instance,
            cache);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private static HttpResponseMessage JsonResponse(object body)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                body is JsonElement el ? el.GetRawText() : JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json")
        };

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
            => _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(_responder(request));
    }

    private sealed class NamedHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public NamedHttpClientFactory(HttpMessageHandler handler) => _handler = handler;

        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false)
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
    }
}

public class SbiWorkTypeMapTests
{
    [Theory]
    [InlineData("5610", WorkTypeLabels.Horeca)]
    [InlineData("55", WorkTypeLabels.Horeca)]
    [InlineData("4711", WorkTypeLabels.Winkel)]
    [InlineData("4941", WorkTypeLabels.Logistiek)]
    [InlineData("01.13", WorkTypeLabels.Tuinbouw)]
    [InlineData("0113", WorkTypeLabels.Tuinbouw)]
    [InlineData("8130", WorkTypeLabels.Tuinbouw)]
    [InlineData("88101", WorkTypeLabels.Zorg)]
    [InlineData("6201", WorkTypeLabels.Kantoor)]
    [InlineData("4120", WorkTypeLabels.Bouw)]
    [InlineData("81210", WorkTypeLabels.Schoonmaak)]
    [InlineData("2511", WorkTypeLabels.Productie)]
    [InlineData("7820", null)]
    [InlineData("9999", null)]
    public void MapOne_matches_prefix_rules(string code, string? expected)
        => Assert.Equal(expected, SbiWorkTypeMap.MapOne(code));

    [Fact]
    public void Map_fixture_company_codes()
    {
        Assert.Equal([WorkTypeLabels.Zorg], SbiWorkTypeMap.Map(["88101", "88102"]));
        Assert.Equal([WorkTypeLabels.Schoonmaak], SbiWorkTypeMap.Map(["81210"]));
    }

    [Fact]
    public void Map_preserves_sbi_order_and_caps_at_four()
    {
        var mapped = SbiWorkTypeMap.Map(["5610", "4711", "4941", "88101", "4120", "6201"]);
        Assert.Equal(
            [WorkTypeLabels.Horeca, WorkTypeLabels.Winkel, WorkTypeLabels.Logistiek, WorkTypeLabels.Zorg],
            mapped);
    }
}

public class KvkWebsiteDomainTests
{
    [Theory]
    [InlineData("https://www.groenenzorg.nl/home", "groenenzorg.nl")]
    [InlineData("http://GROENENZORG.NL", "groenenzorg.nl")]
    [InlineData("www.example.com/path", "example.com")]
    public void Normalize_strips_scheme_www_and_path(string raw, string expected)
        => Assert.Equal(expected, KvkWebsiteDomain.Normalize(raw));
}
