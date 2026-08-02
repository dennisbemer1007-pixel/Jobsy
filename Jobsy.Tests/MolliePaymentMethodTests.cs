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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public class MolliePaymentMethodTests
{
    [Theory]
    [InlineData("ideal")]
    [InlineData("creditcard")]
    [InlineData("IDEAL")]
    [InlineData("CreditCard")]
    public void MolliePaymentMethods_accepts_primary_methods(string method)
    {
        Assert.True(MolliePaymentMethods.IsSupported(method));
        Assert.NotNull(MolliePaymentMethods.NormalizeOrNull(method));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("paypal")]
    [InlineData("bancontact")]
    public void MolliePaymentMethods_rejects_unsupported(string? method)
    {
        Assert.False(MolliePaymentMethods.IsSupported(method));
        Assert.Null(MolliePaymentMethods.NormalizeOrNull(method));
    }

    [Fact]
    public async Task Live_checkout_pins_creditcard_method_and_sets_webhook()
    {
        await using var db = CreateDb();
        var companyId = await SeedCompanyAsync(db, preferred: null);
        string? postedJson = null;
        var handler = new StubMollieHandler(async request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            postedJson = await request.Content!.ReadAsStringAsync();
            return JsonResponse(new
            {
                id = "tr_cc1",
                status = "open",
                method = "creditcard",
                _links = new { checkout = new { href = "https://www.mollie.com/checkout/cc" } }
            });
        });

        var sut = CreateSut(db, handler, apiKey: "test_key");
        var result = await sut.CreateTokenPurchaseCheckoutAsync(companyId, 10, MolliePaymentMethods.CreditCard);

        Assert.False(result.IsStub);
        Assert.Equal(MolliePaymentMethods.CreditCard, result.PaymentMethod);
        Assert.Contains("\"method\":\"creditcard\"", postedJson, StringComparison.Ordinal);
        Assert.Contains("webhookUrl", postedJson, StringComparison.Ordinal);
        Assert.Contains("/api/webhooks/mollie", postedJson, StringComparison.Ordinal);

        var session = await db.TokenPurchaseCheckouts.SingleAsync();
        Assert.Equal(MolliePaymentMethods.CreditCard, session.PaymentMethod);
    }

    [Fact]
    public async Task Live_checkout_uses_company_preferred_method_when_request_omits_it()
    {
        await using var db = CreateDb();
        var companyId = await SeedCompanyAsync(db, preferred: MolliePaymentMethods.CreditCard);
        string? postedJson = null;
        var handler = new StubMollieHandler(async request =>
        {
            postedJson = await request.Content!.ReadAsStringAsync();
            return JsonResponse(new
            {
                id = "tr_pref1",
                status = "open",
                method = "creditcard",
                _links = new { checkout = new { href = "https://www.mollie.com/checkout/pref" } }
            });
        });

        var sut = CreateSut(db, handler, apiKey: "test_key");
        var result = await sut.CreateTokenPurchaseCheckoutAsync(companyId, 10);

        Assert.Equal(MolliePaymentMethods.CreditCard, result.PaymentMethod);
        Assert.Contains("\"method\":\"creditcard\"", postedJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Live_checkout_without_preference_offers_primary_methods_array()
    {
        await using var db = CreateDb();
        var companyId = await SeedCompanyAsync(db, preferred: null);
        string? postedJson = null;
        var handler = new StubMollieHandler(async request =>
        {
            postedJson = await request.Content!.ReadAsStringAsync();
            return JsonResponse(new
            {
                id = "tr_any1",
                status = "open",
                _links = new { checkout = new { href = "https://www.mollie.com/checkout/any" } }
            });
        });

        var sut = CreateSut(db, handler, apiKey: "test_key");
        await sut.CreateTokenPurchaseCheckoutAsync(companyId, 10);

        using var doc = JsonDocument.Parse(postedJson!);
        var method = doc.RootElement.GetProperty("method");
        Assert.Equal(JsonValueKind.Array, method.ValueKind);
        var values = method.EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Contains(MolliePaymentMethods.Ideal, values);
        Assert.Contains(MolliePaymentMethods.CreditCard, values);
    }

    [Fact]
    public async Task Stub_checkout_persists_creditcard_method()
    {
        await using var db = CreateDb();
        var companyId = await SeedCompanyAsync(db, preferred: null);
        var features = new PlatformFeatureService(
            db,
            Options.Create(new JobsyFeatureOptions()),
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PublicWebBaseUrl"] = "http://localhost:5201"
            }).Build());
        var stub = new MolliePaymentStub(db, features, NullLogger<MolliePaymentStub>.Instance);

        var result = await stub.CreateTokenPurchaseCheckoutAsync(
            companyId, 5, MolliePaymentMethods.CreditCard);

        Assert.True(result.IsStub);
        Assert.Equal(MolliePaymentMethods.CreditCard, result.PaymentMethod);
        Assert.Contains("method=creditcard", result.CheckoutUrl, StringComparison.Ordinal);
        var session = await db.TokenPurchaseCheckouts.SingleAsync();
        Assert.Equal(MolliePaymentMethods.CreditCard, session.PaymentMethod);
    }

    [Fact]
    public async Task GetPaymentStatus_persists_method_from_mollie_when_paid()
    {
        await using var db = CreateDb();
        var companyId = await SeedCompanyAsync(db, preferred: null);
        db.TokenPurchaseCheckouts.Add(new TokenPurchaseCheckout
        {
            Id = Guid.NewGuid(),
            PaymentId = "tr_paid_cc",
            CompanyId = companyId,
            PackSize = 10,
            AmountEuro = 40m,
            PaymentMethod = MolliePaymentMethods.Ideal,
            Status = TokenPurchaseCheckoutStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var handler = new StubMollieHandler(_ => JsonResponse(new
        {
            id = "tr_paid_cc",
            status = "paid",
            method = "creditcard"
        }));

        var sut = CreateSut(db, handler, apiKey: "test_key");
        var status = await sut.GetPaymentStatusAsync("tr_paid_cc");

        Assert.True(status.IsPaid);
        Assert.Equal(MolliePaymentMethods.CreditCard, status.Method);
        var session = await db.TokenPurchaseCheckouts.SingleAsync();
        Assert.Equal(TokenPurchaseCheckoutStatus.Paid, session.Status);
        Assert.Equal(MolliePaymentMethods.CreditCard, session.PaymentMethod);
    }

    private static MolliePaymentService CreateSut(
        JobsyDbContext db,
        HttpMessageHandler handler,
        string apiKey)
    {
        var credentials = new IntegrationCredentialService(db, new PassthroughSecretProtector());
        credentials.UpsertAsync(
            IntegrationKey.Mollie,
            new IntegrationCredentialUpdate(ApiKey: apiKey)).GetAwaiter().GetResult();

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
        var http = new NamedHttpClientFactory(handler);

        return new MolliePaymentService(
            db,
            credentials,
            features,
            http,
            config,
            new FakeHostEnvironment(),
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

    private static async Task<Guid> SeedCompanyAsync(JobsyDbContext db, string? preferred)
    {
        var companyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Buyer",
            KvkNumber = "12345678",
            Address = "Test",
            Location = new GeoPoint(52, 4),
            Type = CompanyType.Employer,
            PreferredPaymentMethod = preferred
        });
        db.TokenPricings.Add(new TokenPricing
        {
            Id = Guid.NewGuid(),
            PackSize = 10,
            PriceEuro = 40.00m,
            IsActive = true
        });
        db.TokenPricings.Add(new TokenPricing
        {
            Id = Guid.NewGuid(),
            PackSize = 5,
            PriceEuro = 22.50m,
            IsActive = true
        });
        await db.SaveChangesAsync();
        return companyId;
    }

    private static HttpResponseMessage JsonResponse(object body)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };

    private sealed class StubMollieHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _responder;

        public StubMollieHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responder)
            => _responder = responder;

        public StubMollieHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
            : this(request => Task.FromResult(responder(request)))
        {
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => _responder(request);
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
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = "/tmp";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
