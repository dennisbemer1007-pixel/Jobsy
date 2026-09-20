using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class LobsyCommercialSettingsTests
{
    [Fact]
    public async Task Update_persists_all_amounts_and_syncs_contact_unlock_spend_cost()
    {
        await using var db = CreateDb();
        var sut = new FlexCommercialService(db);

        var updated = await sut.UpdateAsync(new FlexCommercialSettingsUpdate(
            MarginPerHourEuro: 2.50m,
            BackofficePartnerName: "Partner X",
            DeepAnalysisPriceEuro: 4.95m,
            AgencyAnnualPriceEuro: 3500m,
            ContactUnlockCostTokens: 2m));

        Assert.Equal(2.50m, updated.MarginPerHourEuro);
        Assert.Equal("Partner X", updated.BackofficePartnerName);
        Assert.Equal(4.95m, updated.DeepAnalysisPriceEuro);
        Assert.Equal(3500m, updated.AgencyAnnualPriceEuro);
        Assert.Equal(2m, updated.ContactUnlockCostTokens);

        var spend = await db.TokenSpendCosts.SingleAsync(c => c.Reason == TokenSpendReason.ContactUnlock);
        Assert.Equal(2m, spend.CostTokens);
        Assert.True(spend.IsActive);

        var again = await sut.GetAsync();
        Assert.Equal(4.95m, again.DeepAnalysisPriceEuro);
    }

    [Fact]
    public async Task Activate_agency_subscription_snapshots_configured_annual_price()
    {
        await using var db = CreateDb();
        db.Companies.Add(new Company
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            Name = "Agency",
            KvkNumber = "12345678",
            Address = "Test",
            Location = new Core.ValueObjects.GeoPoint(52.0, 4.3)
        });
        await db.SaveChangesAsync();

        var sut = new FlexCommercialService(db);
        await sut.UpdateAsync(new FlexCommercialSettingsUpdate(
            2m, "Yellowstone", 2.99m, 4500m, 1m));

        var sub = await sut.ActivateAgencySubscriptionAsync(
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));

        Assert.Equal(4500m, sub.AnnualPriceEuro);
        Assert.Equal(4500m, (await db.AgencyAnnualSubscriptions.SingleAsync()).PriceEuro);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }
}
