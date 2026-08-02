using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class RevenueShareServiceTests
{
    [Fact]
    public async Task ApplyTokenPurchaseShare_Credits_Ambassador_Sm_And_Logs_All_Three()
    {
        await using var db = CreateDb();
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
        var share = new RevenueShareService(db, tokens, commissions);

        var checkoutId = Guid.NewGuid();
        var purchaseTxId = Guid.NewGuid();
        await share.ApplyTokenPurchaseShareAsync(
            checkoutId,
            companyId,
            purchaseTxId,
            packSize: 10,
            purchaseAmountEuro: 100m,
            smId,
            DateTime.UtcNow.AddMonths(-1));

        // Idempotent second call
        await share.ApplyTokenPurchaseShareAsync(
            checkoutId,
            companyId,
            purchaseTxId,
            packSize: 10,
            purchaseAmountEuro: 100m,
            smId,
            DateTime.UtcNow.AddMonths(-1));

        var logs = await db.RevenueShareLogs.Where(l => l.TokenCheckoutId == checkoutId).ToListAsync();
        Assert.Equal(3, logs.Count);
        Assert.Contains(logs, l => l.RecipientKind == RevenueShareRecipientKind.Ambassador && l.Percentage == 15m && l.Tokens == 1.5m);
        Assert.Contains(logs, l => l.RecipientKind == RevenueShareRecipientKind.SalesManager && l.AmountEuro == 5m);
        Assert.Contains(logs, l => l.RecipientKind == RevenueShareRecipientKind.Platform && l.AmountEuro == 80m);

        var balance = await tokens.GetBalanceAsync(companyId);
        Assert.Equal(1.5m, balance);

        var smBalance = await commissions.GetBalanceExVatAsync(smId);
        Assert.Equal(5.00m, smBalance);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }
}
