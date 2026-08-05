using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class PrepaidTokenCheckoutTests
{
    [Fact]
    public async Task Publish_without_tokens_and_no_pending_approval_returns_InsufficientTokens()
    {
        await using var db = CreateDb();
        var (_, vacancyId) = await SeedDraftVacancyAsync(db, tokenBalance: 0);
        SeedSpendCosts(db);
        await db.SaveChangesAsync();

        var vacancy = await db.Vacancies.Include(v => v.Company).SingleAsync(v => v.Id == vacancyId);
        var sut = CreateProducts(db);

        var result = await sut.PublishAsync(
            vacancy,
            new VacancyPublishOptions(),
            actorUserId: null,
            allowPendingApproval: false);

        Assert.False(result.Succeeded);
        Assert.True(result.InsufficientTokens);
        Assert.Equal(VacancyStatus.Draft, vacancy.Status);
        Assert.True(result.RequiredTokens >= 1m);
        Assert.Equal(0m, result.Balance);
        Assert.Contains("tokens", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Exact_match_pack_size_is_accepted_by_stub_checkout()
    {
        await using var db = CreateDb();
        var companyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Exact Co",
            KvkNumber = "11112222",
            Address = "Test",
            Location = new GeoPoint(52.0, 4.3)
        });
        await db.SaveChangesAsync();

        var stub = new MolliePaymentStub(db, new FakeFeatures(), NullLogger<MolliePaymentStub>.Instance);
        var result = await stub.CreateTokenPurchaseCheckoutAsync(companyId, packSize: 3);

        Assert.Equal(3, result.PackSize);
        Assert.Equal(15.00m, result.AmountEuro);
        Assert.NotEqual(Guid.Empty, result.CheckoutId);
        Assert.True(result.IsStub);
    }

    [Fact]
    public async Task Fulfillment_credits_tokens_and_executes_pending_publish()
    {
        await using var db = CreateDb();
        var (companyId, vacancyId) = await SeedDraftVacancyAsync(db, tokenBalance: 0);
        SeedSpendCosts(db);

        var checkoutId = Guid.NewGuid();
        var paymentId = $"stub_pay_{Guid.NewGuid():N}";
        db.TokenPurchaseCheckouts.Add(new TokenPurchaseCheckout
        {
            Id = checkoutId,
            PaymentId = paymentId,
            CompanyId = companyId,
            PackSize = 2,
            AmountEuro = 10.00m,
            AmountExVatCents = 826,
            VatAmountCents = 174,
            TotalAmountCents = 1000,
            Status = TokenPurchaseCheckoutStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        db.PendingTokenActions.Add(new PendingTokenAction
        {
            Id = Guid.NewGuid(),
            TokenPurchaseCheckoutId = checkoutId,
            CompanyId = companyId,
            VacancyId = vacancyId,
            ActionKind = PendingTokenActionKind.Publish,
            RequiredTokens = 1m,
            Status = PendingTokenActionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var companySettings = new PlatformCompanySettingsService(db);
        var products = CreateProducts(db);
        var pending = new PendingTokenActionService(
            db, products, new TokenLedgerService(db), NullLogger<PendingTokenActionService>.Instance);
        var fulfillment = new TokenPurchaseFulfillmentService(
            db,
            new TokenLedgerService(db),
            new MolliePaymentStub(db, new FakeFeatures(), NullLogger<MolliePaymentStub>.Instance),
            new TokenPurchaseInvoiceService(db, companySettings),
            new VatBufferTransferService(db, companySettings, NullLogger<VatBufferTransferService>.Instance),
            new FakeRevenueShare(),
            new CommissionLedgerService(db),
            pending,
            new FakeHostEnvironment(),
            NullLogger<TokenPurchaseFulfillmentService>.Instance);

        var result = await fulfillment.TryFulfillPaidCheckoutAsync(
            checkoutId,
            allowDevStubMarkPaid: true);

        Assert.NotNull(result);
        Assert.NotNull(result!.PendingAction);
        Assert.True(result.PendingAction!.Succeeded);
        Assert.Equal(PendingTokenActionKind.Publish, result.PendingAction.ActionKind);

        var vacancy = await db.Vacancies.SingleAsync(v => v.Id == vacancyId);
        Assert.Equal(VacancyStatus.Active, vacancy.Status);
        Assert.Equal(
            PendingTokenActionStatus.Executed,
            await db.PendingTokenActions.Where(a => a.TokenPurchaseCheckoutId == checkoutId)
                .Select(a => a.Status).SingleAsync());
        Assert.True(await db.TokenTransactions.AnyAsync(t => t.Kind == TokenTransactionKind.Spend));
    }

    [Fact]
    public async Task Highlight_without_tokens_returns_InsufficientTokens()
    {
        await using var db = CreateDb();
        var (_, vacancyId) = await SeedDraftVacancyAsync(db, tokenBalance: 0);
        SeedSpendCosts(db);
        var vacancy = await db.Vacancies.Include(v => v.Company).SingleAsync(v => v.Id == vacancyId);
        vacancy.Status = VacancyStatus.Active;
        await db.SaveChangesAsync();

        var result = await CreateProducts(db).HighlightAsync(vacancy, actorUserId: null);

        Assert.False(result.Succeeded);
        Assert.True(result.InsufficientTokens);
        Assert.Contains("tokens", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    private static IVacancyProductService CreateProducts(JobsyDbContext db)
    {
        db.PlatformFeatureSettings.Add(new PlatformFeatureSettings
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            FreePublishUntil = null,
            UpdatedAtUtc = DateTime.UtcNow
        });
        db.SaveChanges();

        var features = new PlatformFeatureService(
            db,
            Microsoft.Extensions.Options.Options.Create(new Jobsy.Core.Options.JobsyFeatureOptions()),
            new ConfigurationBuilder().Build());

        return new VacancyProductService(
            db,
            new TokenLedgerService(db),
            new SalesCommercialService(db, new TokenLedgerService(db)),
            new VacancyCategoryService(db),
            new PushNotificationServiceStub(db, NullLogger<PushNotificationServiceStub>.Instance),
            new EmailServiceStub(db, NullLogger<EmailServiceStub>.Instance),
            features,
            new MockRoutingService(),
            NullLogger<VacancyProductService>.Instance);
    }

    private static void SeedSpendCosts(JobsyDbContext db)
    {
        db.TokenSpendCosts.AddRange(
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.Publish, CostTokens = 1m, IsActive = true },
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.Highlight, CostTokens = VacancyProductRules.DefaultHighlightCostTokens, IsActive = true },
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.PushBom, CostTokens = 3m, IsActive = true },
            new TokenSpendCost { Id = Guid.NewGuid(), Reason = TokenSpendReason.Extend, CostTokens = 1m, IsActive = true });
    }

    private static async Task<(Guid CompanyId, Guid VacancyId)> SeedDraftVacancyAsync(
        JobsyDbContext db,
        decimal tokenBalance)
    {
        var companyId = Guid.NewGuid();
        var vacancyId = Guid.NewGuid();

        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Prepaid Co",
            KvkNumber = "12345678",
            Address = "Westland",
            Location = new GeoPoint(51.99, 4.22)
        });

        db.Vacancies.Add(new Vacancy
        {
            Id = vacancyId,
            CompanyId = companyId,
            Title = "Kasmedewerker",
            Description = "Demo",
            Status = VacancyStatus.Draft,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            Location = new GeoPoint(51.99, 4.22),
            RequiredTransport = TransportMode.Bike
        });

        if (tokenBalance > 0)
        {
            db.TokenTransactions.Add(new TokenTransaction
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Amount = tokenBalance,
                Kind = TokenTransactionKind.Grant,
                OldBalance = 0,
                NewBalance = tokenBalance,
                CreatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
        return (companyId, vacancyId);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class FakeFeatures : IPlatformFeatureService
    {
        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(true, false, false, "http://localhost:5201", DateTime.UtcNow, 120));

        public Task<PlatformFeatureSnapshot> UpdateAsync(PlatformFeatureUpdate update, CancellationToken cancellationToken = default)
            => GetAsync(cancellationToken);
    }

    private sealed class FakeRevenueShare : IRevenueShareService
    {
        public Task ApplyTokenPurchaseShareAsync(
            Guid tokenCheckoutId,
            Guid companyId,
            Guid? purchaseTokenTransactionId,
            int packSize,
            decimal purchaseAmountExVatEuro,
            Guid? salesManagerUserId,
            DateTime? firstYearStartedAt,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<RevenueShareLog>> ListForCompanyAsync(
            Guid companyId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<RevenueShareLog>>([]);
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = "/";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
