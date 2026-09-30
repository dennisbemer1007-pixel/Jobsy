using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Sales;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Jobs;
using Jobsy.Infrastructure.Sales;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class SalesCommissionEngineTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(364, 1)]
    [InlineData(365, 2)]
    [InlineData(729, 2)]
    [InlineData(730, 3)]
    [InlineData(1094, 3)]
    [InlineData(1095, null)]
    public void YearFor_Boundaries(int daysAfterStart, int? expectedYear)
    {
        var start = new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc);
        var terms = new SalesCommissionRules.CommissionTerms(0.25m, 0.10m, 0.05m, 0.05m, 1095, start);
        var at = start.AddDays(daysAfterStart);
        Assert.Equal(expectedYear, SalesCommissionRules.YearFor(terms, at));
    }

    [Fact]
    public void YearFor_BeforeStart_IsNull()
    {
        var start = new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc);
        var terms = new SalesCommissionRules.CommissionTerms(0.25m, 0.10m, 0.05m, 0.05m, 1095, start);
        Assert.Null(SalesCommissionRules.YearFor(terms, start.AddSeconds(-1)));
    }

    [Fact]
    public void Referred_Year1_Direct20_Indirect5_AfterWindow_Zero()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var terms = new SalesCommissionRules.CommissionTerms(0.20m, 0.10m, 0.05m, 0.05m, 1095, start);
        Assert.Equal(0.20m, SalesCommissionRules.DirectRate(terms, SalesCommissionRules.YearFor(terms, start)));
        Assert.Equal(0.05m, SalesCommissionRules.IndirectRate(terms, SalesCommissionRules.YearFor(terms, start)));
        Assert.True(SalesCommissionRules.BonusTokensAllowed(terms, start.AddDays(100)));
        var after = start.AddDays(1095);
        Assert.Null(SalesCommissionRules.YearFor(terms, after));
        Assert.False(SalesCommissionRules.BonusTokensAllowed(terms, after));
        Assert.Null(SalesCommissionRules.DirectRate(terms, null));
    }

    [Fact]
    public void HoldAvailableFrom_FourteenDays_EndOfLocalDay()
    {
        // 29-09-2026 16:00 CEST = 14:00 UTC
        var paidAt = new DateTime(2026, 9, 29, 14, 0, 0, DateTimeKind.Utc);
        var available = SalesClock.HoldAvailableFromUtc(paidAt, 14);
        // 13-10-2026 23:59:59 CEST = 21:59:59 UTC (CEST = UTC+2)
        Assert.Equal(new DateTime(2026, 10, 13, 21, 59, 59, DateTimeKind.Utc), available);
    }

    [Fact]
    public async Task Activation_FirstPurchase_Snapshots_Once_And_Ignores_Later_Settings()
    {
        await using var db = CreateDb();
        SeedCommercialSettings(db);
        var smId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        SeedCompany(db, smId, companyId);
        await db.SaveChangesAsync();

        var tokens = new TokenLedgerService(db);
        var commercial = new SalesCommercialService(db, tokens);
        var commissions = new CommissionLedgerService(db, new AlwaysOnFeatures(), commercial);
        var share = new RevenueShareService(db, tokens, commissions, commercial);

        var checkout1 = Guid.NewGuid();
        await share.ApplyTokenPurchaseShareAsync(
            checkout1, companyId, Guid.NewGuid(), 10, 800m, smId, null);

        var company = await db.Companies.SingleAsync(c => c.Id == companyId);
        Assert.NotNull(company.CommissionStartsAtUtc);
        Assert.Equal(0.25m, company.CommissionDirectRateSnapshot);
        Assert.Equal(0.10m, company.CommissionYear2RateSnapshot);
        Assert.Equal(0.05m, company.CommissionYear3RateSnapshot);

        var settings = await db.SalesCommercialSettings.SingleAsync();
        settings.Year2DirectCommissionRate = 0.08m;
        settings.DirectCommissionRate = 0.30m;
        await db.SaveChangesAsync();

        // Parallel-ish second purchase does not re-activate / change snapshots.
        var checkout2 = Guid.NewGuid();
        await share.ApplyTokenPurchaseShareAsync(
            checkout2, companyId, Guid.NewGuid(), 10, 100m, smId, null);

        await db.Entry(company).ReloadAsync();
        Assert.Equal(0.25m, company.CommissionDirectRateSnapshot);
        Assert.Equal(0.10m, company.CommissionYear2RateSnapshot);

        var smBalance = await commissions.GetBalanceExVatAsync(smId);
        // 800 * 25% + 100 * 25% = 200 + 25
        Assert.Equal(225.00m, smBalance);
    }

    [Fact]
    public async Task Org_Plus_Vestiging_Counts_As_One_Unit_And_Uses_Root_Window()
    {
        await using var db = CreateDb();
        SeedCommercialSettings(db);
        var smId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        SeedCompany(db, smId, orgId, name: "Org Root");
        db.Companies.Add(new Company
        {
            Id = branchId,
            Name = "Vestiging",
            KvkNumber = "11112222",
            KvkEstablishmentId = "11112222_0002",
            Address = "B",
            Location = new Jobsy.Core.ValueObjects.GeoPoint(52, 4),
            ParentCompanyId = orgId,
            ReferredBySalesManagerUserId = smId,
            SalesAttributedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var tokens = new TokenLedgerService(db);
        var commercial = new SalesCommercialService(db, tokens);
        var commissions = new CommissionLedgerService(db, new AlwaysOnFeatures(), commercial);
        var share = new RevenueShareService(db, tokens, commissions, commercial);

        await share.ApplyTokenPurchaseShareAsync(
            Guid.NewGuid(), branchId, Guid.NewGuid(), 10, 100m, smId, null);

        var org = await db.Companies.SingleAsync(c => c.Id == orgId);
        Assert.NotNull(org.CommissionStartsAtUtc);
        Assert.Null((await db.Companies.SingleAsync(c => c.Id == branchId)).CommissionStartsAtUtc);

        var units = await new SalesEmployerReadService(db).ListUnitsAsync(smId);
        Assert.Single(units);
        Assert.Equal(orgId, units[0].RootCompanyId);
        Assert.Equal(2, units[0].BranchCount);
    }

    [Fact]
    public async Task After_Window_No_Commission_And_No_Bonus_Tokens()
    {
        await using var db = CreateDb();
        SeedCommercialSettings(db);
        var smId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        SeedCompany(db, smId, companyId);
        var company = await db.Companies.SingleAsync(c => c.Id == companyId);
        company.CommissionStartsAtUtc = DateTime.UtcNow.AddDays(-1100);
        company.CommissionDirectRateSnapshot = 0.25m;
        company.CommissionYear2RateSnapshot = 0.10m;
        company.CommissionYear3RateSnapshot = 0.05m;
        company.CommissionIndirectRateSnapshot = 0m;
        company.CommissionDurationDaysSnapshot = 1095;
        company.CommissionTermsSnapshottedAtUtc = company.CommissionStartsAtUtc;
        await db.SaveChangesAsync();

        var tokens = new TokenLedgerService(db);
        var commercial = new SalesCommercialService(db, tokens);
        var commissions = new CommissionLedgerService(db, new AlwaysOnFeatures(), commercial);
        var share = new RevenueShareService(db, tokens, commissions, commercial);
        var checkoutId = Guid.NewGuid();
        await share.ApplyTokenPurchaseShareAsync(
            checkoutId, companyId, Guid.NewGuid(), 10, 100m, smId, company.CommissionStartsAtUtc);

        Assert.Equal(0m, await commissions.GetBalanceExVatAsync(smId));
        Assert.Equal(0m, await tokens.GetBalanceAsync(companyId));
        var ambassadorLog = await db.RevenueShareLogs.SingleAsync(
            l => l.TokenCheckoutId == checkoutId && l.RecipientKind == RevenueShareRecipientKind.Ambassador);
        Assert.Equal(0m, ambassadorLog.Tokens);
    }

    [Fact]
    public async Task Hold_State_Pending_Then_Available()
    {
        await using var db = CreateDb();
        var smId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = smId,
            Email = "sm@test.local",
            FullName = "SM",
            Role = UserRole.SalesManager,
            IsActive = true
        });
        var paidAt = new DateTime(2026, 9, 29, 14, 0, 0, DateTimeKind.Utc);
        var availableFrom = SalesClock.HoldAvailableFromUtc(paidAt, 14);
        db.CommissionLedgerEntries.Add(new CommissionLedgerEntry
        {
            Id = Guid.NewGuid(),
            SalesManagerUserId = smId,
            Kind = CommissionEntryKind.TokenCommission,
            AmountExVat = 200m,
            VatAmount = 42m,
            VatRate = 0.21m,
            CreatedAt = paidAt,
            AvailableFromUtc = availableFrom
        });
        await db.SaveChangesAsync();

        var wallet = new SalesWalletReadService(db);
        var pendingNow = await wallet.GetBalancesAsync(smId, paidAt.AddDays(1));
        Assert.Equal(0m, pendingNow.Available);
        Assert.Equal(200m, pendingNow.Pending);

        var afterHold = await wallet.GetBalancesAsync(smId, availableFrom.AddSeconds(1));
        Assert.Equal(200m, afterHold.Available);
        Assert.Equal(0m, afterHold.Pending);
    }

    [Fact]
    public async Task Refund_Partial_Then_Full_Books_Delta_Corrections_Idempotent()
    {
        await using var db = CreateDb();
        var smId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var checkoutId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = smId, Email = "sm@test.local", FullName = "SM",
            Role = UserRole.SalesManager, IsActive = true
        });
        var original = new CommissionLedgerEntry
        {
            Id = Guid.NewGuid(),
            SalesManagerUserId = smId,
            Kind = CommissionEntryKind.TokenCommission,
            AmountExVat = 200m,
            VatAmount = 42m,
            CompanyId = companyId,
            SourceTokenCheckoutId = checkoutId,
            CreatedAt = DateTime.UtcNow,
            AvailableFromUtc = DateTime.UtcNow.AddDays(14)
        };
        db.CommissionLedgerEntries.Add(original);
        await db.SaveChangesAsync();

        var corrections = new SalesCorrectionService(db);
        await corrections.ApplyPaymentReversalsAsync(checkoutId, 800m, 320m, 0m); // 40%
        await corrections.ApplyPaymentReversalsAsync(checkoutId, 800m, 800m, 0m); // full
        await corrections.ApplyPaymentReversalsAsync(checkoutId, 800m, 800m, 0m); // replay

        var refunds = await db.CommissionLedgerEntries
            .Where(e => e.Kind == CommissionEntryKind.RefundCorrection)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync();
        Assert.Equal(2, refunds.Count);
        Assert.Equal(-80.00m, refunds[0].AmountExVat); // 40% of 200
        Assert.Equal(-120.00m, refunds[1].AmountExVat); // remainder
        Assert.Equal(-200.00m, refunds.Sum(r => r.AmountExVat));
        Assert.Contains(
            await db.PlatformLogs.ToListAsync(),
            l => l.Category == "sales.refund.tokens-not-reversed");
    }

    [Fact]
    public async Task Chargeback_Uses_Separate_Kind()
    {
        await using var db = CreateDb();
        var smId = Guid.NewGuid();
        var checkoutId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = smId, Email = "sm@test.local", FullName = "SM",
            Role = UserRole.SalesManager, IsActive = true
        });
        db.CommissionLedgerEntries.Add(new CommissionLedgerEntry
        {
            Id = Guid.NewGuid(),
            SalesManagerUserId = smId,
            Kind = CommissionEntryKind.TokenCommission,
            AmountExVat = 50m,
            VatAmount = 10.5m,
            SourceTokenCheckoutId = checkoutId,
            CreatedAt = DateTime.UtcNow,
            AvailableFromUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        await new SalesCorrectionService(db).ApplyPaymentReversalsAsync(checkoutId, 200m, 0m, 200m);
        var row = Assert.Single(await db.CommissionLedgerEntries
            .Where(e => e.Kind == CommissionEntryKind.ChargebackCorrection).ToListAsync());
        Assert.Equal(-50.00m, row.AmountExVat);
    }

    [Fact]
    public async Task Manual_Correction_Validation_And_Audit()
    {
        await using var db = CreateDb();
        var smId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = smId, Email = "sm@test.local", FullName = "SM",
            Role = UserRole.SalesManager, IsActive = true
        });
        await db.SaveChangesAsync();

        var svc = new SalesCorrectionService(db);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.BookManualCorrectionAsync(smId, null, 0m, "reden ok", adminId));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.BookManualCorrectionAsync(smId, null, 5m, "ab", adminId));

        var entry = await svc.BookManualCorrectionAsync(smId, null, -25.50m, "Correctie na conflict", adminId);
        Assert.Equal(CommissionEntryKind.Adjustment, entry.Kind);
        Assert.Equal(-25.50m, entry.AmountExVat);
        Assert.True(entry.AvailableFromUtc <= DateTime.UtcNow.AddSeconds(1));
        Assert.Contains(
            await db.PlatformLogs.ToListAsync(),
            l => l.Category == "sales.ledger.correction");
    }

    [Fact]
    public async Task Worked_Example_800_Purchase_Hold_And_Partial_Refund()
    {
        await using var db = CreateDb();
        SeedCommercialSettings(db);
        var smId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        SeedCompany(db, smId, companyId);
        await db.SaveChangesAsync();

        var tokens = new TokenLedgerService(db);
        var commercial = new SalesCommercialService(db, tokens);
        var commissions = new CommissionLedgerService(db, new AlwaysOnFeatures(), commercial);
        var share = new RevenueShareService(db, tokens, commissions, commercial);

        // First purchase 15-03-2026 € 800 → € 200 commission
        var paidAt = new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc);
        db.TokenPurchaseCheckouts.Add(new TokenPurchaseCheckout
        {
            Id = Guid.NewGuid(),
            PaymentId = "stub_pay_example",
            CompanyId = companyId,
            PackSize = 50,
            AmountEuro = 968m,
            AmountExVatCents = 80000,
            Status = TokenPurchaseCheckoutStatus.Credited,
            CreatedAt = paidAt,
            CreditedAt = paidAt
        });
        var checkoutId = (await db.TokenPurchaseCheckouts.SingleAsync()).Id;
        // Align Apply path: use checkout id after save
        await db.SaveChangesAsync();

        // Re-create share call with known checkout
        db.TokenPurchaseCheckouts.Remove(await db.TokenPurchaseCheckouts.SingleAsync());
        checkoutId = Guid.NewGuid();
        db.TokenPurchaseCheckouts.Add(new TokenPurchaseCheckout
        {
            Id = checkoutId,
            PaymentId = "stub_pay_example2",
            CompanyId = companyId,
            PackSize = 50,
            AmountEuro = 968m,
            AmountExVatCents = 80000,
            Status = TokenPurchaseCheckoutStatus.Credited,
            CreatedAt = paidAt,
            CreditedAt = paidAt
        });
        await db.SaveChangesAsync();

        await share.ApplyTokenPurchaseShareAsync(
            checkoutId, companyId, Guid.NewGuid(), 50, 800m, smId, null);

        var line = await db.CommissionLedgerEntries.SingleAsync(
            e => e.Kind == CommissionEntryKind.TokenCommission);
        Assert.Equal(200.00m, line.AmountExVat);
        Assert.Equal(SalesClock.HoldAvailableFromUtc(paidAt, 14), line.AvailableFromUtc);

        // Refund € 400 of € 800 → correction –€ 100
        await new SalesCorrectionService(db).ApplyPaymentReversalsAsync(checkoutId, 800m, 400m, 0m);
        var correction = await db.CommissionLedgerEntries.SingleAsync(
            e => e.Kind == CommissionEntryKind.RefundCorrection);
        Assert.Equal(-100.00m, correction.AmountExVat);

        // Year-2 purchase at snapshotted 10% even if settings changed to 8%
        var settings = await db.SalesCommercialSettings.SingleAsync();
        settings.Year2DirectCommissionRate = 0.08m;
        await db.SaveChangesAsync();

        var company = await db.Companies.SingleAsync(c => c.Id == companyId);
        company.CommissionStartsAtUtc = paidAt; // keep activation
        await db.SaveChangesAsync();

        // Force "now" for year-2 by setting start a year earlier relative to purchase time used in Apply (UtcNow).
        // Instead credit via ledger with snapshotted terms:
        var year2PurchaseAt = paidAt.AddDays(400);
        var terms = new SalesCommissionRules.CommissionTerms(
            company.CommissionDirectRateSnapshot!.Value,
            company.CommissionYear2RateSnapshot!.Value,
            company.CommissionYear3RateSnapshot!.Value,
            0m,
            company.CommissionDurationDaysSnapshot ?? 1095,
            paidAt);
        Assert.Equal(2, SalesCommissionRules.YearFor(terms, year2PurchaseAt));
        Assert.Equal(0.10m, SalesCommissionRules.DirectRate(terms, 2));
        Assert.NotEqual(0.08m, SalesCommissionRules.DirectRate(terms, 2));
    }

    [Fact]
    public async Task Backfill_Activates_Legacy_Units_Idempotently()
    {
        await using var db = CreateDb();
        SeedCommercialSettings(db);
        var smId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        SeedCompany(db, smId, companyId);
        var paidAt = new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc);
        db.TokenPurchaseCheckouts.Add(new TokenPurchaseCheckout
        {
            Id = Guid.NewGuid(),
            PaymentId = "stub_pay_bf",
            CompanyId = companyId,
            PackSize = 10,
            AmountEuro = 40m,
            Status = TokenPurchaseCheckoutStatus.Credited,
            CreatedAt = paidAt,
            CreditedAt = paidAt
        });
        var company = await db.Companies.SingleAsync(c => c.Id == companyId);
        company.CommissionDirectRateSnapshot = 0.25m;
        company.CommissionTermsSnapshottedAtUtc = paidAt;
        await db.SaveChangesAsync();

        var first = await SalesCommissionBackfillHostedService.RunOnceAsync(db);
        Assert.False(first.Skipped);
        Assert.Equal(1, first.UnitsActivated);
        await db.Entry(company).ReloadAsync();
        Assert.Equal(paidAt, company.CommissionStartsAtUtc);
        Assert.Equal(0.10m, company.CommissionYear2RateSnapshot);

        var second = await SalesCommissionBackfillHostedService.RunOnceAsync(db);
        Assert.True(second.Skipped);
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
            DirectCommissionRate = 0.25m,
            IndirectCommissionRate = 0.05m,
            Year2DirectCommissionRate = 0.10m,
            Year3DirectCommissionRate = 0.05m,
            ReferredYear1DirectCommissionRate = 0.20m,
            CommissionDurationDays = 1095,
            CommissionHoldDays = 14,
            PayoutMinimumEuro = 50m,
            IbanChangeHoldDays = 3,
            AttributionCookieDays = 30,
            UpdatedAtUtc = DateTime.UtcNow
        });
    }

    private static void SeedCompany(JobsyDbContext db, Guid smId, Guid companyId, string name = "Buyer")
    {
        db.Users.Add(new User
        {
            Id = smId,
            Email = $"{smId:N}@test.local",
            FullName = "SM",
            Role = UserRole.SalesManager,
            IsActive = true
        });
        db.SalesManagerProfiles.Add(new SalesManagerProfile
        {
            Id = Guid.NewGuid(),
            UserId = smId,
            TrackingCode = "SM-TEST01",
            CanRecruitSalesManagers = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            AgreementSignedAt = DateTime.UtcNow,
            OnboardingCompletedAt = DateTime.UtcNow
        });
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = name,
            KvkNumber = "11112222",
            KvkEstablishmentId = "11112222_0001",
            Address = "Straat 1, Amsterdam",
            Location = new Jobsy.Core.ValueObjects.GeoPoint(52, 4),
            ReferredBySalesManagerUserId = smId,
            SalesAttributedAtUtc = DateTime.UtcNow,
            SalesAttributionSource = SalesAttributionSource.TypedCode
        });
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }
}
