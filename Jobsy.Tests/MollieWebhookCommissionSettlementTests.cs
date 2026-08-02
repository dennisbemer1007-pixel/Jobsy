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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Jobsy.Tests;

/// <summary>
/// End-to-end: Mollie paid checkout fulfillment → real-time direct (15%) + upline (3%)
/// commission ledger credits → salesmanager dashboard balance, with 1-year window enforcement.
/// </summary>
public class MollieWebhookCommissionSettlementTests
{
    [Fact]
    public async Task Webhook_fulfillment_credits_direct_and_upline_commission_ex_vat_on_dashboard()
    {
        await using var db = CreateDb();
        SeedCommercialSettings(db);
        SeedPlatformCompany(db);

        var parentSmId = Guid.NewGuid();
        var directSmId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var started = DateTime.UtcNow.AddMonths(-3);
        SeedHierarchy(db, parentSmId, directSmId, companyId, started);

        var (ex, vat, total) = TokenVatPricing.SplitInclVatEuros(40.00m); // 10-token pack
        var purchaseExVat = TokenVatPricing.FromCents(ex);
        var checkoutId = Guid.NewGuid();
        db.TokenPurchaseCheckouts.Add(new TokenPurchaseCheckout
        {
            Id = checkoutId,
            PaymentId = "stub_pay_commission_ok",
            CompanyId = companyId,
            PackSize = 10,
            AmountEuro = 40.00m,
            AmountExVatCents = ex,
            VatAmountCents = vat,
            TotalAmountCents = total,
            PaymentMethod = MolliePaymentMethods.Ideal,
            Status = TokenPurchaseCheckoutStatus.Paid,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var fulfillment = CreateFulfillment(db);
        // Same entry point as POST /api/webhooks/mollie after Mollie reports paid.
        var result = await fulfillment.TryFulfillPaidCheckoutAsync(checkoutId, allowDevStubMarkPaid: false);
        Assert.NotNull(result);
        Assert.False(result!.AlreadyFulfilled);

        var expectedDirect = SalesCommissionRules.ShareEuro(
            purchaseExVat, SalesCommissionRules.DefaultDirectCommissionRate);
        var expectedIndirect = SalesCommissionRules.ShareEuro(
            purchaseExVat, SalesCommissionRules.DefaultIndirectCommissionRate);

        var commissions = new CommissionLedgerService(db);
        Assert.Equal(expectedDirect, await commissions.GetBalanceExVatAsync(directSmId));
        Assert.Equal(expectedIndirect, await commissions.GetBalanceExVatAsync(parentSmId));

        // Dashboard balance is the ledger sum — immediately visible to the salesmanager.
        var dashboard = new SalesManagerDashboardService(db, commissions);
        var directDash = await dashboard.GetDashboardAsync(directSmId);
        var parentDash = await dashboard.GetDashboardAsync(parentSmId);
        Assert.NotNull(directDash);
        Assert.NotNull(parentDash);
        Assert.Equal(expectedDirect, directDash!.BalanceExVat);
        Assert.Equal(expectedIndirect, parentDash!.BalanceExVat);

        Assert.Contains(
            directDash.RecentLedger,
            e => e.Kind == nameof(CommissionEntryKind.TokenCommission) && e.AmountExVat == expectedDirect);
        Assert.Contains(
            parentDash.RecentLedger,
            e => e.Kind == nameof(CommissionEntryKind.IndirectTokenCommission)
                 && e.AmountExVat == expectedIndirect);

        // Idempotent: second webhook must not double-credit.
        var again = await fulfillment.TryFulfillPaidCheckoutAsync(checkoutId);
        Assert.NotNull(again);
        Assert.True(again!.AlreadyFulfilled);
        Assert.Equal(expectedDirect, await commissions.GetBalanceExVatAsync(directSmId));
        Assert.Equal(expectedIndirect, await commissions.GetBalanceExVatAsync(parentSmId));
    }

    [Fact]
    public async Task Webhook_fulfillment_skips_sm_commission_after_one_year_window()
    {
        await using var db = CreateDb();
        SeedCommercialSettings(db);
        SeedPlatformCompany(db);

        var parentSmId = Guid.NewGuid();
        var directSmId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var started = DateTime.UtcNow.AddYears(-1).AddDays(-2);
        SeedHierarchy(db, parentSmId, directSmId, companyId, started);

        var (ex, vat, total) = TokenVatPricing.SplitInclVatEuros(40.00m);
        var checkoutId = Guid.NewGuid();
        db.TokenPurchaseCheckouts.Add(new TokenPurchaseCheckout
        {
            Id = checkoutId,
            PaymentId = "stub_pay_commission_expired",
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

        var fulfillment = CreateFulfillment(db);
        var result = await fulfillment.TryFulfillPaidCheckoutAsync(checkoutId);
        Assert.NotNull(result);

        var commissions = new CommissionLedgerService(db);
        Assert.Equal(0m, await commissions.GetBalanceExVatAsync(directSmId));
        Assert.Equal(0m, await commissions.GetBalanceExVatAsync(parentSmId));

        // Ambassador token share still applies after the SM window (platform loyalty).
        var companyBalance = await new TokenLedgerService(db).GetBalanceAsync(companyId);
        Assert.Equal(10m + SalesCommissionRules.AmbassadorTokens(10), companyBalance);
    }

    [Fact]
    public async Task Already_fulfilled_checkout_retries_missed_commission_settlement()
    {
        await using var db = CreateDb();
        SeedCommercialSettings(db);
        SeedPlatformCompany(db);

        var parentSmId = Guid.NewGuid();
        var directSmId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        SeedHierarchy(db, parentSmId, directSmId, companyId, DateTime.UtcNow.AddMonths(-1));

        var (ex, vat, total) = TokenVatPricing.SplitInclVatEuros(40.00m);
        var purchaseExVat = TokenVatPricing.FromCents(ex);
        var checkoutId = Guid.NewGuid();
        var txId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();

        db.TokenTransactions.Add(new TokenTransaction
        {
            Id = txId,
            CompanyId = companyId,
            Kind = TokenTransactionKind.Purchase,
            Reason = TokenSpendReason.None,
            Amount = 10m,
            OldBalance = 0m,
            NewBalance = 10m,
            AmountExVatCents = ex,
            VatAmountCents = vat,
            TotalAmountCents = total,
            TokenPurchaseCheckoutId = checkoutId,
            CreatedAt = DateTime.UtcNow
        });
        db.TokenPurchaseInvoices.Add(new TokenPurchaseInvoice
        {
            Id = invoiceId,
            InvoiceNumber = "LOB-TK-TEST-0001",
            TokenPurchaseCheckoutId = checkoutId,
            TokenTransactionId = txId,
            CompanyId = companyId,
            MolliePaymentId = "stub_pay_retry",
            PackSize = 10,
            AmountExVatCents = ex,
            VatAmountCents = vat,
            TotalAmountCents = total,
            CompanyName = "Buyer",
            IssuedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });
        db.TokenPurchaseCheckouts.Add(new TokenPurchaseCheckout
        {
            Id = checkoutId,
            PaymentId = "stub_pay_retry",
            CompanyId = companyId,
            PackSize = 10,
            AmountEuro = 40.00m,
            AmountExVatCents = ex,
            VatAmountCents = vat,
            TotalAmountCents = total,
            Status = TokenPurchaseCheckoutStatus.Credited,
            CreditedAt = DateTime.UtcNow,
            TokenTransactionId = txId,
            TokenPurchaseInvoiceId = invoiceId,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // Simulate first pass that credited tokens/invoice but never wrote commissions.
        Assert.Empty(await db.CommissionLedgerEntries.ToListAsync());

        var fulfillment = CreateFulfillment(db);
        var result = await fulfillment.TryFulfillPaidCheckoutAsync(checkoutId);
        Assert.NotNull(result);
        Assert.True(result!.AlreadyFulfilled);

        var expectedDirect = SalesCommissionRules.ShareEuro(
            purchaseExVat, SalesCommissionRules.DefaultDirectCommissionRate);
        var expectedIndirect = SalesCommissionRules.ShareEuro(
            purchaseExVat, SalesCommissionRules.DefaultIndirectCommissionRate);

        var commissions = new CommissionLedgerService(db);
        Assert.Equal(expectedDirect, await commissions.GetBalanceExVatAsync(directSmId));
        Assert.Equal(expectedIndirect, await commissions.GetBalanceExVatAsync(parentSmId));
    }

    private static void SeedHierarchy(
        JobsyDbContext db,
        Guid parentSmId,
        Guid directSmId,
        Guid companyId,
        DateTime firstYearStartedAt)
    {
        var now = DateTime.UtcNow;
        db.Users.AddRange(
            new User
            {
                Id = parentSmId,
                Email = "upline@test.local",
                FullName = "Upline SM",
                Role = UserRole.SalesManager,
                IsActive = true
            },
            new User
            {
                Id = directSmId,
                Email = "direct@test.local",
                FullName = "Direct SM",
                Role = UserRole.SalesManager,
                IsActive = true
            });
        db.SalesManagerProfiles.AddRange(
            new SalesManagerProfile
            {
                Id = Guid.NewGuid(),
                UserId = parentSmId,
                TrackingCode = "SM-UPLINE",
                CanRecruitSalesManagers = true,
                AgreementSignedAt = now,
                OnboardingCompletedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            },
            new SalesManagerProfile
            {
                Id = Guid.NewGuid(),
                UserId = directSmId,
                TrackingCode = "SM-DIRECT",
                CanRecruitSalesManagers = false,
                ReferredBySalesManagerUserId = parentSmId,
                AgreementSignedAt = now,
                OnboardingCompletedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            });
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Buyer Co",
            KvkNumber = "87654321",
            KvkEstablishmentId = "87654321_0001",
            Address = "Westland",
            Location = new GeoPoint(52, 4),
            Type = CompanyType.Employer,
            ReferredBySalesManagerUserId = directSmId,
            CommissionIndirectSalesManagerUserId = parentSmId,
            CommissionDirectRateSnapshot = SalesCommissionRules.DefaultDirectCommissionRate,
            CommissionIndirectRateSnapshot = SalesCommissionRules.DefaultIndirectCommissionRate,
            CommissionDurationDaysSnapshot = SalesCommissionRules.DefaultCommissionDurationDays,
            CommissionTermsSnapshottedAtUtc = firstYearStartedAt,
            FirstYearStartedAt = firstYearStartedAt
        });
    }

    private static void SeedCommercialSettings(JobsyDbContext db)
    {
        db.SalesCommercialSettings.Add(new SalesCommercialSettings
        {
            Id = SalesCommercialService.SingletonId,
            BaseTokenValueEuro = 25m,
            HighlightCarouselTokens = 2m,
            HighlightPulseTokens = 1m,
            HighlightCarouselDays = 7,
            StartHighlightBonusTokens = 2m,
            DirectCommissionRate = SalesCommissionRules.DefaultDirectCommissionRate,
            IndirectCommissionRate = SalesCommissionRules.DefaultIndirectCommissionRate,
            CommissionDurationDays = SalesCommissionRules.DefaultCommissionDurationDays,
            UpdatedAtUtc = DateTime.UtcNow
        });
    }

    private static void SeedPlatformCompany(JobsyDbContext db)
    {
        db.PlatformCompanySettings.Add(new PlatformCompanySettings
        {
            Id = PlatformCompanySettingsService.SingletonId,
            CompanyName = "Lobsy",
            VatBufferIban = "NL91KNAB0417164300"
        });
    }

    private static TokenPurchaseFulfillmentService CreateFulfillment(JobsyDbContext db)
    {
        var companySettings = new PlatformCompanySettingsService(db);
        var tokens = new TokenLedgerService(db);
        var commissions = new CommissionLedgerService(db);
        var commercial = new SalesCommercialService(db, tokens);
        var revenueShare = new RevenueShareService(db, tokens, commissions, commercial);
        var features = new PlatformFeatureService(
            db,
            Options.Create(new JobsyFeatureOptions()),
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PublicWebBaseUrl"] = "http://localhost:5201"
            }).Build());

        return new TokenPurchaseFulfillmentService(
            db,
            tokens,
            new MolliePaymentStub(db, features, NullLogger<MolliePaymentStub>.Instance),
            new TokenPurchaseInvoiceService(db, companySettings),
            new VatBufferTransferService(db, companySettings, NullLogger<VatBufferTransferService>.Instance),
            revenueShare,
            new NoopPendingActions(),
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

    private sealed class NoopPendingActions : IPendingTokenActionService
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
            => Task.FromResult(new PendingTokenAction { Id = Guid.NewGuid() });

        public Task<PendingTokenActionExecutionResult?> TryExecuteForCheckoutAsync(
            Guid checkoutId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<PendingTokenActionExecutionResult?>(null);
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = "/tmp";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
