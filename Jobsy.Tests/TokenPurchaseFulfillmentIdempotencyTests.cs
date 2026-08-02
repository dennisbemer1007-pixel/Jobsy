using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class TokenPurchaseFulfillmentIdempotencyTests
{
    [Fact]
    public async Task Repair_after_credit_without_invoice_does_not_double_credit()
    {
        await using var db = CreateDb();
        var companyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Co",
            KvkNumber = "1",
            Address = "a",
            Location = new GeoPoint(51.9, 4.2)
        });
        db.PlatformCompanySettings.Add(new PlatformCompanySettings
        {
            Id = PlatformCompanySettingsService.SingletonId,
            CompanyName = "Lobsy",
            VatBufferIban = "NL91KNAB0417164300"
        });

        var (ex, vat, total) = TokenVatPricing.SplitInclVatEuros(40m);
        var checkoutId = Guid.NewGuid();
        db.TokenPurchaseCheckouts.Add(new TokenPurchaseCheckout
        {
            Id = checkoutId,
            PaymentId = "stub_pay_idem",
            CompanyId = companyId,
            PackSize = 10,
            AmountEuro = 40m,
            AmountExVatCents = ex,
            VatAmountCents = vat,
            TotalAmountCents = total,
            Status = TokenPurchaseCheckoutStatus.Paid,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var fulfillment = CreateFulfillment(db);
        var first = await fulfillment.TryFulfillPaidCheckoutAsync(checkoutId, allowDevStubMarkPaid: true);
        Assert.NotNull(first);
        Assert.Equal(10m, first!.NewBalance);

        // Simulate stuck state: credited with tx but invoice link cleared.
        var session = await db.TokenPurchaseCheckouts.FirstAsync(c => c.Id == checkoutId);
        var invoiceId = session.TokenPurchaseInvoiceId;
        session.TokenPurchaseInvoiceId = null;
        await db.SaveChangesAsync();

        // Detach invoice link from checkout only; invoice row still exists for CreateForCheckout idempotency.
        var second = await fulfillment.TryFulfillPaidCheckoutAsync(checkoutId, allowDevStubMarkPaid: true);
        Assert.NotNull(second);
        Assert.True(second!.AlreadyFulfilled);
        Assert.Equal(10m, await db.TokenTransactions.Where(t => t.CompanyId == companyId).SumAsync(t => t.Amount));
        Assert.Equal(1, await db.TokenPurchaseInvoices.CountAsync(i => i.TokenPurchaseCheckoutId == checkoutId));
        Assert.NotNull(invoiceId);
    }

    private static TokenPurchaseFulfillmentService CreateFulfillment(JobsyDbContext db)
    {
        var companySettings = new PlatformCompanySettingsService(db);
        return new TokenPurchaseFulfillmentService(
            db,
            new TokenLedgerService(db),
            new MolliePaymentStub(db, new FakeFeatures(), NullLogger<MolliePaymentStub>.Instance),
            new TokenPurchaseInvoiceService(db, companySettings),
            new VatBufferTransferService(db, companySettings, NullLogger<VatBufferTransferService>.Instance),
            new FakeRevenueShare(),
            new FakePendingActions(),
            new FakeHostEnvironment(),
            NullLogger<TokenPurchaseFulfillmentService>.Instance);
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

    private sealed class FakePendingActions : IPendingTokenActionService
    {
        public Task<PendingTokenAction> AttachAsync(
            Guid checkoutId,
            Guid spendCompanyId,
            Guid vacancyId,
            PendingTokenActionKind actionKind,
            bool optionHighlight,
            bool optionPushBom,
            bool optionExtend,
            decimal requiredTokens,
            Guid? actorUserId,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<PendingTokenActionExecutionResult?> TryExecuteForCheckoutAsync(
            Guid checkoutId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<PendingTokenActionExecutionResult?>(null);
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
