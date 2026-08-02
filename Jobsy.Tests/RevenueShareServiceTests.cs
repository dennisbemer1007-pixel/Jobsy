using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class RevenueShareServiceTests
{
    [Fact]
    public async Task ApplyTokenPurchaseShare_Credits_Ambassador_Sm_And_Logs_Split()
    {
        await using var db = CreateDb();
        SeedCommercialSettings(db);
        var smId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = smId,
            Email = "sm@test.local",
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
            Name = "Buyer",
            KvkNumber = "11112222",
            KvkEstablishmentId = "11112222_0001",
            Address = "A",
            Location = new GeoPoint(52, 4),
            ReferredBySalesManagerUserId = smId,
            FirstYearStartedAt = DateTime.UtcNow.AddMonths(-1)
        });
        await db.SaveChangesAsync();

        var tokens = new TokenLedgerService(db);
        var commissions = new CommissionLedgerService(db);
        var share = new RevenueShareService(db, tokens, commissions, new SalesCommercialService(db, tokens));

        var checkoutId = Guid.NewGuid();
        var purchaseTxId = Guid.NewGuid();
        await share.ApplyTokenPurchaseShareAsync(
            checkoutId,
            companyId,
            purchaseTxId,
            packSize: 10,
            purchaseAmountExVatEuro: 100m,
            smId,
            DateTime.UtcNow.AddMonths(-1));

        // Idempotent second call
        await share.ApplyTokenPurchaseShareAsync(
            checkoutId,
            companyId,
            purchaseTxId,
            packSize: 10,
            purchaseAmountExVatEuro: 100m,
            smId,
            DateTime.UtcNow.AddMonths(-1));

        var logs = await db.RevenueShareLogs.Where(l => l.TokenCheckoutId == checkoutId).ToListAsync();
        Assert.Equal(3, logs.Count);
        Assert.Contains(logs, l => l.RecipientKind == RevenueShareRecipientKind.Ambassador && l.Percentage == 15m && l.Tokens == 1.5m);
        Assert.Contains(logs, l => l.RecipientKind == RevenueShareRecipientKind.SalesManager && l.AmountEuro == 15m);
        Assert.Contains(logs, l => l.RecipientKind == RevenueShareRecipientKind.Platform && l.AmountEuro == 70m);

        var balance = await tokens.GetBalanceAsync(companyId);
        Assert.Equal(1.5m, balance);

        var smBalance = await commissions.GetBalanceExVatAsync(smId);
        Assert.Equal(15.00m, smBalance);
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

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }
}
