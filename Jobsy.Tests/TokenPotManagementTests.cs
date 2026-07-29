using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class TokenPotManagementTests
{
    [Fact]
    public async Task TokensManagedByEnterprise_persists_on_vestiging()
    {
        await using var db = CreateDb();
        var orgId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        db.Companies.AddRange(
            new Company
            {
                Id = orgId,
                Name = "Org Pot",
                KvkNumber = "11223344",
                Address = "HQ",
                Location = new GeoPoint(52, 4),
                Type = CompanyType.Employer
            },
            new Company
            {
                Id = branchId,
                Name = "Vestiging A",
                KvkNumber = "11223344",
                Address = "Branch",
                Location = new GeoPoint(52.1, 4.1),
                Type = CompanyType.Employer,
                ParentCompanyId = orgId,
                TokensManagedByEnterprise = true
            });
        await db.SaveChangesAsync();

        var loaded = await db.Companies.AsNoTracking().SingleAsync(c => c.Id == branchId);
        Assert.True(loaded.TokensManagedByEnterprise);
        Assert.Equal(orgId, loaded.ParentCompanyId);

        var pot = await db.Companies.AsNoTracking().SingleAsync(c => c.Id == orgId);
        Assert.False(pot.TokensManagedByEnterprise);
        Assert.Null(pot.ParentCompanyId);
    }

    [Fact]
    public async Task Demo_children_default_to_enterprise_token_management_when_seeded()
    {
        await using var db = CreateDb();
        var supermarketId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var westlandId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var cafeId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        db.Companies.AddRange(
            new Company
            {
                Id = supermarketId,
                Name = "Supermarkt De Fred",
                KvkNumber = "11223344",
                Address = "HQ",
                Location = new GeoPoint(52, 4),
                Type = CompanyType.Employer
            },
            new Company
            {
                Id = westlandId,
                Name = "Westland",
                KvkNumber = "12345678",
                Address = "W",
                Location = new GeoPoint(52, 4),
                Type = CompanyType.Employer,
                ParentCompanyId = supermarketId,
                TokensManagedByEnterprise = true
            },
            new Company
            {
                Id = cafeId,
                Name = "Café",
                KvkNumber = "87654321",
                Address = "C",
                Location = new GeoPoint(52, 4),
                Type = CompanyType.Employer,
                ParentCompanyId = supermarketId,
                TokensManagedByEnterprise = true
            });
        await db.SaveChangesAsync();

        Assert.All(
            await db.Companies.Where(c => c.ParentCompanyId == supermarketId).ToListAsync(),
            c => Assert.True(c.TokensManagedByEnterprise));
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }
}
