using System.Net;
using System.Text;
using System.Text.Json;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public class MolliePaymentServiceTests
{
    [Fact]
    public async Task Without_api_key_in_development_falls_back_to_stub()
    {
        await using var db = CreateDb();
        var companyId = await SeedCompanyAsync(db);
        var sut = CreateSut(db, handler: null, isDevelopment: true, apiKey: null);

        var result = await sut.CreateTokenPurchaseCheckoutAsync(companyId, 10);

        Assert.True(result.IsStub);
        Assert.StartsWith("stub_pay_", result.PaymentId);
    }

    [Fact]
    public async Task Without_api_key_outside_development_throws()
    {
        await using var db = CreateDb();
        var companyId = await SeedCompanyAsync(db);
        var sut = CreateSut(db, handler: null, isDevelopment: false, apiKey: null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.CreateTokenPurchaseCheckoutAsync(companyId, 10));
        Assert.Contains("Mollie API-key", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task With_api_key_creates_mollie_payment_and_persists_session()
    {
        await using var db = CreateDb();
        var companyId = await SeedCompanyAsync(db);
        var handler = new StubMollieHandler(request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.EndsWith("/payments", request.RequestUri!.AbsoluteUri, StringComparison.Ordinal);
            Assert.Equal("Bearer test_abc123", request.Headers.Authorization?.ToString());
            return JsonResponse(new
            {
                id = "tr_testpayment1",
                status = "open",
                _links = new
                {
                    checkout = new { href = "https://www.mollie.com/checkout/test" }
                }
            });
        });

        var sut = CreateSut(db, handler, isDevelopment: false, apiKey: "test_abc123");
        var result = await sut.CreateTokenPurchaseCheckoutAsync(companyId, 10);

        Assert.False(result.IsStub);
        Assert.Equal("tr_testpayment1", result.PaymentId);
        Assert.Equal("https://www.mollie.com/checkout/test", result.CheckoutUrl);
        Assert.Equal(40.00m, result.AmountEuro);

        var session = await db.TokenPurchaseCheckouts.SingleAsync();
        Assert.Equal("tr_testpayment1", session.PaymentId);
        Assert.Equal(companyId, session.CompanyId);
        Assert.Equal(TokenPurchaseCheckoutStatus.Pending, session.Status);
    }

    [Fact]
    public async Task GetPaymentStatus_marks_session_paid_when_mollie_says_paid()
    {
        await using var db = CreateDb();
        var companyId = await SeedCompanyAsync(db);
        db.TokenPurchaseCheckouts.Add(new TokenPurchaseCheckout
        {
            Id = Guid.NewGuid(),
            PaymentId = "tr_paid1",
            CompanyId = companyId,
            PackSize = 10,
            AmountEuro = 40m,
            Status = TokenPurchaseCheckoutStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var handler = new StubMollieHandler(_ => JsonResponse(new
        {
            id = "tr_paid1",
            status = "paid"
        }));

        var sut = CreateSut(db, handler, isDevelopment: false, apiKey: "test_abc123");
        var status = await sut.GetPaymentStatusAsync("tr_paid1");

        Assert.True(status.IsPaid);
        Assert.Equal("paid", status.Status);
        var session = await db.TokenPurchaseCheckouts.SingleAsync();
        Assert.Equal(TokenPurchaseCheckoutStatus.Paid, session.Status);
    }

    private static MolliePaymentService CreateSut(
        JobsyDbContext db,
        HttpMessageHandler? handler,
        bool isDevelopment,
        string? apiKey)
    {
        var credentials = new IntegrationCredentialService(db, new PassthroughSecretProtector());
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            credentials.UpsertAsync(
                IntegrationKey.Mollie,
                new IntegrationCredentialUpdate(ApiKey: apiKey)).GetAwaiter().GetResult();
        }

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PublicWebBaseUrl"] = "http://localhost:5201",
                ["PublicApiBaseUrl"] = "https://api.example.test"
            })
            .Build();

        var features = new PlatformFeatureService(
            db,
            Options.Create(new JobsyFeatureOptions()),
            config);

        var stub = new MolliePaymentStub(db, features, NullLogger<MolliePaymentStub>.Instance);
        var http = new NamedHttpClientFactory(handler ?? new StubMollieHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        return new MolliePaymentService(
            db,
            credentials,
            features,
            http,
            config,
            new FakeHostEnvironment(isDevelopment),
            stub,
            NullLogger<MolliePaymentService>.Instance);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private static async Task<Guid> SeedCompanyAsync(JobsyDbContext db)
    {
        var companyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Buyer",
            KvkNumber = "12345678",
            Address = "Test",
            Location = new GeoPoint(52, 4),
            Type = CompanyType.Employer
        });
        db.TokenPricings.Add(new TokenPricing
        {
            Id = Guid.NewGuid(),
            PackSize = 10,
            PriceEuro = 40.00m,
            IsActive = true
        });
        await db.SaveChangesAsync();
        return companyId;
    }

    private static HttpResponseMessage JsonResponse(object body)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json")
        };
    }

    private sealed class StubMollieHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubMollieHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
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

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public FakeHostEnvironment(bool isDevelopment)
            => EnvironmentName = isDevelopment ? Environments.Development : Environments.Production;

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = "/tmp";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
