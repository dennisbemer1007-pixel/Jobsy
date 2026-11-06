using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Jobsy.Tests;

public class CompanyValueCardsTests
{
    [Fact]
    public void Exactly_three_cards_or_none_is_valid()
    {
        Assert.True(CompanyValueCards.IsValidSelection(null));
        Assert.True(CompanyValueCards.IsValidSelection([]));
        Assert.True(CompanyValueCards.IsValidSelection(["zorg", "vakmanschap", "betekenis"]));
        Assert.False(CompanyValueCards.IsValidSelection(["zorg", "vakmanschap"]));
        Assert.False(CompanyValueCards.IsValidSelection(["zorg", "vakmanschap", "betekenis", "eerlijk"]));
        Assert.False(CompanyValueCards.IsValidSelection(["unknown", "zorg", "groei"]));
    }

    [Fact]
    public void Driver_with_two_cards_is_90_one_is_75_zero_is_40()
    {
        // Connection has zorg + teamgevoel; Impact has betekenis; others 0.
        var scores = CompanyValueCards.Score(["zorg", "teamgevoel", "betekenis"]);
        Assert.Equal(90, scores.Connection);
        Assert.Equal(75, scores.Impact);
        Assert.Equal(40, scores.Autonomy);
        Assert.Equal(40, scores.Achievement);
        Assert.Equal(40, scores.Stability);
    }

    [Fact]
    public async Task Vestiging_falls_back_to_organisation_values()
    {
        await using var db = CreateDb();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var org = SeedCompany(db, "Org");
        var vestiging = SeedCompany(db, "Vestiging", parentId: org.Id);
        db.CompanyValuesProfiles.Add(new CompanyValuesProfile
        {
            Id = Guid.NewGuid(),
            CompanyId = org.Id,
            CardIdsJson = CompanyValueCards.Serialize(["zorg", "vakmanschap", "betekenis"]),
            AutonomyPercent = 40,
            ConnectionPercent = 75,
            AchievementPercent = 40,
            StabilityPercent = 75,
            ImpactPercent = 75,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var lookup = new CompanyCultureLookup(db, cache);
        var result = await lookup.GetForCompaniesAsync([vestiging.Id]);
        Assert.NotNull(result[vestiging.Id].Values);
        Assert.Equal(75, result[vestiging.Id].Values!.Connection);
        Assert.Null(result[vestiging.Id].Culture);
    }

    private static Company SeedCompany(JobsyDbContext db, string name, Guid? parentId = null)
    {
        var c = new Company
        {
            Id = Guid.NewGuid(),
            Name = name,
            KvkNumber = "12345678",
            Address = "A 1",
            Location = new GeoPoint(52, 5),
            Type = CompanyType.Employer,
            ParentCompanyId = parentId,
            VerificationStatus = CompanyVerificationStatus.Unverified,
            VerificationMethod = CompanyVerificationMethod.None,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        };
        db.Companies.Add(c);
        return c;
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("company-values-" + Guid.NewGuid())
            .Options;
        return new JobsyDbContext(options);
    }
}
