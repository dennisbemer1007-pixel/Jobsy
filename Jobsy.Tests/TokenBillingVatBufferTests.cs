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

public class TokenBillingVatBufferTests
{
    [Fact]
    public void TokenVatPricing_splits_incl_vat_into_reconciling_cents()
    {
        var (ex, vat, total) = TokenVatPricing.SplitInclVatEuros(5.00m);
        Assert.Equal(500, total);
        Assert.Equal(ex + vat, total);
        Assert.Equal(413, ex); // round(500/1.21)
        Assert.Equal(87, vat);
    }

    [Fact]
    public async Task GrantGoodwill_credits_tokens_at_zero_euro_value()
    {
        await using var db = CreateDb();
        var companyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Test BV",
            KvkNumber = "123",
            Address = "Straat 1",
            Location = new GeoPoint(51.9, 4.2)
        });
        await db.SaveChangesAsync();

        var ledger = new TokenLedgerService(db);
        var entry = await ledger.GrantGoodwillAsync(companyId, 3m, "Compensatie storing #42");

        Assert.Equal(TokenTransactionKind.Goodwill, entry.Kind);
        Assert.Equal(3m, entry.Amount);
        Assert.Equal(0, entry.AmountExVatCents);
        Assert.Equal(0, entry.VatAmountCents);
        Assert.Equal(0, entry.TotalAmountCents);
        Assert.Equal("Compensatie storing #42", entry.Note);
        Assert.Equal(3m, await ledger.GetBalanceAsync(companyId));
    }

    [Fact]
    public async Task GrantGoodwill_requires_reason()
    {
        await using var db = CreateDb();
        var companyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Test BV",
            KvkNumber = "123",
            Address = "Straat 1",
            Location = new GeoPoint(51.9, 4.2)
        });
        await db.SaveChangesAsync();

        var ledger = new TokenLedgerService(db);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            ledger.GrantGoodwillAsync(companyId, 1m, "  "));
    }

    [Fact]
    public async Task Fulfillment_creates_invoice_and_queues_vat_buffer_with_invoice_number()
    {
        await using var db = CreateDb();
        var companyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Betaal BV",
            KvkNumber = "999",
            Address = "Laan 2",
            Location = new GeoPoint(51.9, 4.2)
        });
        db.PlatformCompanySettings.Add(new PlatformCompanySettings
        {
            Id = PlatformCompanySettingsService.SingletonId,
            CompanyName = "Lobsy",
            VatBufferIban = "NL91KNAB0417164300"
        });

        var (ex, vat, total) = TokenVatPricing.SplitInclVatEuros(40.00m);
        var checkoutId = Guid.NewGuid();
        db.TokenPurchaseCheckouts.Add(new TokenPurchaseCheckout
        {
            Id = checkoutId,
            PaymentId = "stub_pay_testfulfill",
            CompanyId = companyId,
            PackSize = 10,
            AmountEuro = 40.00m,
            AmountExVatCents = ex,
            VatAmountCents = vat,
            TotalAmountCents = total,
            Status = TokenPurchaseCheckoutStatus.Paid,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var companySettings = new PlatformCompanySettingsService(db);
        var invoices = new TokenPurchaseInvoiceService(db, companySettings);
        var vatBuffer = new VatBufferTransferService(db, companySettings, NullLogger<VatBufferTransferService>.Instance);
        var payments = new MolliePaymentStub(db, new FakeFeatures(), NullLogger<MolliePaymentStub>.Instance);
        var revenueShare = new FakeRevenueShare();
        var ledger = new TokenLedgerService(db);
        var env = new FakeHostEnvironment { EnvironmentName = Environments.Development };

        var fulfillment = new TokenPurchaseFulfillmentService(
            db, ledger, payments, invoices, vatBuffer, revenueShare, env,
            NullLogger<TokenPurchaseFulfillmentService>.Instance);

        var result = await fulfillment.TryFulfillPaidCheckoutAsync(checkoutId);
        Assert.NotNull(result);
        Assert.False(result!.AlreadyFulfilled);
        Assert.StartsWith("LOB-TK-", result.InvoiceNumber);
        Assert.Equal(10m, result.NewBalance);

        var invoice = await db.TokenPurchaseInvoices.SingleAsync();
        Assert.Equal(result.InvoiceNumber, invoice.InvoiceNumber);
        Assert.Equal(ex, invoice.AmountExVatCents);
        Assert.Equal(vat, invoice.VatAmountCents);
        Assert.Equal(total, invoice.TotalAmountCents);

        var transfer = await db.VatBufferTransfers.SingleAsync();
        Assert.Equal(invoice.InvoiceNumber, transfer.InvoiceNumber);
        Assert.Equal(vat, transfer.AmountCents);
        Assert.Equal("NL91KNAB0417164300", transfer.DestinationIban);
        Assert.Equal(VatBufferTransferStatus.Pending, transfer.Status);

        var processed = await vatBuffer.ProcessPendingAsync();
        Assert.Equal(1, processed);
        await db.Entry(transfer).ReloadAsync();
        Assert.Equal(VatBufferTransferStatus.Logged, transfer.Status);
        Assert.Contains(invoice.InvoiceNumber, transfer.Note!);
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
            decimal purchaseAmountEuro,
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
