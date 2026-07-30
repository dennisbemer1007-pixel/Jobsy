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
            new FakeCommissions(),
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

    private sealed class FakeCommissions : ICommissionLedgerService
    {
        public Task<decimal> GetBalanceExVatAsync(Guid salesManagerUserId, CancellationToken cancellationToken = default)
            => Task.FromResult(0m);

        public Task<decimal> GetUninvoicedBalanceExVatAsync(Guid salesManagerUserId, CancellationToken cancellationToken = default)
            => Task.FromResult(0m);

        public Task<IReadOnlyList<CommissionLedgerEntry>> ListEntriesAsync(
            Guid salesManagerUserId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CommissionLedgerEntry>>([]);

        public Task<CommissionLedgerEntry?> TryCreditFounderBonusAsync(
            Guid salesManagerUserId,
            Guid companyId,
            string paymentId,
            int? firstYearSlot,
            CancellationToken cancellationToken = default)
            => Task.FromResult<CommissionLedgerEntry?>(null);

        public Task<CommissionLedgerEntry?> TryCreditTokenCommissionAsync(
            Guid salesManagerUserId,
            Guid companyId,
            Guid tokenCheckoutId,
            decimal purchaseAmountEuro,
            DateTime? firstYearStartedAt,
            CancellationToken cancellationToken = default)
            => Task.FromResult<CommissionLedgerEntry?>(null);

        public Task AttachEntriesToInvoiceAsync(
            Guid invoiceId,
            IReadOnlyList<Guid> entryIds,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<CommissionLedgerEntry> RecordPayoutAsync(
            Guid salesManagerUserId,
            Guid invoiceId,
            decimal amountExVat,
            decimal vatAmount,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
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
