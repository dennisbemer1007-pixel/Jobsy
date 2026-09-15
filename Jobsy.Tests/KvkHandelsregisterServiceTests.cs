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
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public class KvkHandelsregisterServiceTests
{
    [Fact]
    public async Task Without_api_key_uses_stub_catalog()
    {
        await using var db = CreateDb();
        var sut = CreateSut(db, handler: null, apiKey: null);

        var company = await sut.GetByKvkNumberAsync("11223344");

        Assert.NotNull(company);
        Assert.Equal("Supermarkt De Fred B.V.", company!.Name);
    }

    [Fact]
    public async Task With_api_key_calls_basisprofiel_and_vestigingen()
    {
        await using var db = CreateDb();
        db.Companies.Add(new Company
        {
            Id = Guid.NewGuid(),
            Name = "Existing HQ",
            KvkNumber = "69599084",
            KvkEstablishmentId = CompanyPublicPaths.BuildEstablishmentId("69599084", "000038509658"),
            Address = "Old",
            Location = new GeoPoint(52, 4),
            Type = CompanyType.Employer
        });
        await db.SaveChangesAsync();

        var handler = new RecordingHandler(request =>
        {
            Assert.Equal("test-kvk-key", request.Headers.GetValues("apikey").Single());
            var path = request.RequestUri!.AbsolutePath;
            if (path.Contains("/basisprofielen/69599084/vestigingen", StringComparison.Ordinal))
            {
                return JsonResponse(new
                {
                    kvkNummer = "69599084",
                    vestigingen = new[]
                    {
                        new
                        {
                            vestigingsnummer = "000038509658",
                            eersteHandelsnaam = "Test NV HQ",
                            indHoofdvestiging = "Ja",
                            volledigAdres = "Nevelgaarde 20, 3436ZZ Nieuwegein"
                        },
                        new
                        {
                            vestigingsnummer = "000038509659",
                            eersteHandelsnaam = "Test NV Depot",
                            indHoofdvestiging = "Nee",
                            volledigAdres = "Industrieweg 1, 3436ZZ Nieuwegein"
                        }
                    }
                });
            }

            if (path.Contains("/basisprofielen/69599084", StringComparison.Ordinal))
            {
                return JsonResponse(new
                {
                    kvkNummer = "69599084",
                    naam = "Test NV",
                    statutaireNaam = "Test Naamloze Vennootschap",
                    sbiActiviteiten = new[]
                    {
                        new { sbiCode = "6201", indHoofdactiviteit = "Ja" },
                        new { sbiCode = "7820", indHoofdactiviteit = "Nee" }
                    },
                    _embedded = new
                    {
                        hoofdvestiging = new
                        {
                            vestigingsnummer = "000038509658",
                            eersteHandelsnaam = "Test NV HQ",
                            adressen = new[]
                            {
                                new
                                {
                                    type = "bezoekadres",
                                    volledigAdres = "Nevelgaarde 20, 3436ZZ Nieuwegein",
                                    geoData = new { gpsLatitude = 52.029, gpsLongitude = 5.090 }
                                }
                            }
                        }
                    }
                });
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var sut = CreateSut(db, handler, apiKey: "test-kvk-key");
        var lookup = await sut.LookupEstablishmentsAsync("69599084");

        Assert.Equal(KvkLookupStatus.Ok, lookup.Status);
        Assert.Equal(2, lookup.Establishments.Count);
        var hq = lookup.Establishments.Single(e => e.EstablishmentNumber == "000038509658");
        Assert.True(hq.IsInUse);
        Assert.Equal("69599084_000038509658", hq.KvkEstablishmentId);
        Assert.Equal(52.029, hq.Latitude, 3);
        Assert.Contains(hq.EffectiveSbiCodes, c => c.StartsWith("78", StringComparison.Ordinal));

        var depot = lookup.Establishments.Single(e => e.EstablishmentNumber == "000038509659");
        Assert.False(depot.IsInUse);
        Assert.Equal("Test NV Depot", depot.Name);

        var company = await sut.GetByKvkNumberAsync("6959-9084");
        Assert.NotNull(company);
        Assert.Equal("Test Naamloze Vennootschap", company!.Name);
        Assert.Equal("6201", company.EffectiveSbiCodes[0]);
    }

    [Fact]
    public async Task Live_404_is_not_found_not_stub()
    {
        await using var db = CreateDb();
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var sut = CreateSut(db, handler, apiKey: "test-kvk-key");

        var lookup = await sut.LookupEstablishmentsAsync("11223344");

        Assert.Equal(KvkLookupStatus.NotFound, lookup.Status);
        Assert.Empty(lookup.Establishments);
    }

    [Fact]
    public async Task Live_401_is_unavailable()
    {
        await using var db = CreateDb();
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var sut = CreateSut(db, handler, apiKey: "test-kvk-key");

        var lookup = await sut.LookupEstablishmentsAsync("69599084");

        Assert.Equal(KvkLookupStatus.Unavailable, lookup.Status);
        Assert.Contains("API-key", lookup.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Empty_base_url_uses_production_host()
    {
        await using var db = CreateDb();
        Uri? seen = null;
        var handler = new RecordingHandler(request =>
        {
            seen = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var sut = CreateSut(db, handler, apiKey: "test-kvk-key", baseUrl: null);
        await sut.LookupEstablishmentsAsync("69599084");

        Assert.NotNull(seen);
        Assert.StartsWith("https://api.kvk.nl/api/v1/basisprofielen/", seen!.AbsoluteUri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TestConnection_ok_on_404()
    {
        await using var db = CreateDb();
        var handler = new RecordingHandler(request =>
        {
            Assert.Contains("/v2/zoeken", request.RequestUri!.AbsoluteUri, StringComparison.Ordinal);
            Assert.Equal("test-kvk-key", request.Headers.GetValues("apikey").Single());
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
        var sut = CreateSut(db, handler, apiKey: "test-kvk-key");

        var (ok, message) = await sut.TestConnectionAsync();

        Assert.True(ok);
        Assert.Contains("Live lookup is actief", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Env_api_key_enables_live_client()
    {
        await using var db = CreateDb();
        var called = false;
        var handler = new RecordingHandler(_ =>
        {
            called = true;
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
        var credentials = new IntegrationCredentialService(
            db,
            new PassthroughSecretProtector(),
            Options.Create(new MailOptions()),
            Options.Create(new KvkOptions { ApiKey = "env-kvk-key" }));
        var sut = CreateSut(db, handler, credentials);

        var lookup = await sut.LookupEstablishmentsAsync("12345678");

        Assert.True(called);
        Assert.Equal(KvkLookupStatus.NotFound, lookup.Status);
    }

    private static KvkHandelsregisterService CreateSut(
        JobsyDbContext db,
        HttpMessageHandler? handler,
        string? apiKey,
        string? baseUrl = null)
    {
        var credentials = new IntegrationCredentialService(db, new PassthroughSecretProtector());
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            credentials.UpsertAsync(
                IntegrationKey.Kvk,
                new IntegrationCredentialUpdate(ApiKey: apiKey, BaseUrl: baseUrl)).GetAwaiter().GetResult();
        }

        return CreateSut(db, handler, credentials);
    }

    private static KvkHandelsregisterService CreateSut(
        JobsyDbContext db,
        HttpMessageHandler? handler,
        IIntegrationCredentialService credentials)
    {
        var http = new NamedHttpClientFactory(handler ?? new RecordingHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError)));
        return new KvkHandelsregisterService(
            db,
            credentials,
            http,
            new KvkServiceStub(db),
            NullLogger<KvkHandelsregisterService>.Instance);
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
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
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
