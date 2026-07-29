using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Options;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

public class StubServicesTests
{
    [Fact]
    public async Task Mollie_stub_creates_checkout_for_known_pack()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new JobsyDbContext(options);
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

        var sut = new MolliePaymentStub(db, NullLogger<MolliePaymentStub>.Instance);
        var result = await sut.CreateTokenPurchaseCheckoutAsync(companyId, 10);
        Assert.True(result.IsStub);
        Assert.Equal(10, result.PackSize);
        Assert.Equal(40.00m, result.AmountEuro);
        Assert.StartsWith("stub_pay_", result.PaymentId);

        var session = await db.TokenPurchaseCheckouts.SingleAsync(c => c.PaymentId == result.PaymentId);
        Assert.Equal(companyId, session.CompanyId);
        Assert.Equal(10, session.PackSize);
        Assert.Equal(TokenPurchaseCheckoutStatus.Pending, session.Status);
    }

    [Fact]
    public async Task Mollie_stub_unknown_payment_is_not_paid()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new JobsyDbContext(options);
        var sut = new MolliePaymentStub(db, NullLogger<MolliePaymentStub>.Instance);

        var status = await sut.GetPaymentStatusAsync("forged_pay_id");
        Assert.False(status.IsPaid);
    }

    [Fact]
    public async Task Integration_health_returns_all_keys()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new JobsyDbContext(options);
        var credentials = new IntegrationCredentialService(db, new PassthroughSecretProtector());
        var http = new FakeHttpClientFactory();
        var sut = new IntegrationHealthStub(
            credentials,
            http,
            new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance),
            Options.Create(new OpenAiOptions()),
            NullLogger<IntegrationHealthStub>.Instance);

        var all = await sut.GetAllAsync();
        Assert.Equal(Enum.GetValues<IntegrationKey>().Length, all.Count);
        Assert.Contains(all, x => x.Key == IntegrationKey.OpenAI && !x.IsActive);
        Assert.Contains(all, x => x.Key == IntegrationKey.Mollie);
    }

    [Fact]
    public async Task OpenAi_credential_can_be_saved_and_masked()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new JobsyDbContext(options);
        var sut = new IntegrationCredentialService(db, new PassthroughSecretProtector());

        var saved = await sut.UpsertAsync(
            IntegrationKey.OpenAI,
            new IntegrationCredentialUpdate(
                ApiKey: "sk-test-abcdefghijklmnop",
                Model: "gpt-4o-mini"));

        Assert.True(saved.HasApiKey);
        Assert.Equal("sk-••••••••mnop", saved.ApiKeyMasked);
        Assert.Equal("sk-test-abcdefghijklmnop", await sut.GetRawApiKeyAsync(IntegrationKey.OpenAI));
    }

    [Fact]
    public async Task Platform_features_can_disable_moderation()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new JobsyDbContext(options);
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
        var sut = new PlatformFeatureService(
            db,
            Options.Create(new JobsyFeatureOptions { VacancyContentModerationEnabled = true }),
            config);

        var updated = await sut.UpdateAsync(new PlatformFeatureUpdate(
            VacancyContentModerationEnabled: false,
            AuthenticatorEnabled: false,
            ExposeRegistrationActivationLinks: false,
            PublicWebBaseUrl: "http://localhost:5201"));

        Assert.False(updated.VacancyContentModerationEnabled);
        var loaded = await sut.GetAsync();
        Assert.False(loaded.VacancyContentModerationEnabled);
    }

    private sealed class FakeHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}
