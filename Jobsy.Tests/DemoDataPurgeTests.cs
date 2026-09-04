using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class DemoDataPurgeTests
{
    [Fact]
    public async Task Purge_removes_seeder_rows_and_keeps_real_registrations()
    {
        await using var db = CreateDb();
        await JobsyDbSeederHarness.SeedFreshAsync(db);

        var realCompanyId = Guid.Parse("9a1b2c3d-4e5f-6071-8293-a4b5c6d7e8f9");
        db.Companies.Add(new Company
        {
            Id = realCompanyId,
            Name = "Echte Bakkerij BV",
            KvkNumber = "99887766",
            KvkEstablishmentId = "99887766_0001",
            Address = "Voorstraat 1, Delft",
            Type = CompanyType.Employer,
            Location = new GeoPoint(52.0116, 4.3571)
        });
        db.Users.Add(new User
        {
            Id = Guid.Parse("0f1e2d3c-4b5a-6978-8796-a5b4c3d2e1f0"),
            Email = "owner@bakkerij-voorbeeld.nl",
            FullName = "Echte Ondernemer",
            Role = UserRole.BranchManager,
            CompanyId = realCompanyId,
            IsActive = true
        });
        await db.SaveChangesAsync();

        Assert.True(await db.Companies.CountAsync() > 1);
        Assert.True(await db.Vacancies.AnyAsync());
        Assert.True(await db.Users.AnyAsync(u => u.Email.EndsWith("@jobsy.local")));

        var result = await DemoDataPurge.PurgeAsync(db, NullLogger.Instance);

        Assert.True(result.Companies >= 4);
        Assert.True(result.Vacancies >= 1);
        Assert.True(result.Users >= 1);

        Assert.False(await db.Companies.AnyAsync(c => DemoDataPurge.IsDemoCompanyId(c.Id)));
        Assert.False(await db.Vacancies.AnyAsync(v => DemoDataPurge.IsDemoVacancyId(v.Id)));
        Assert.False(await db.Users.AnyAsync(u => DemoDataPurge.IsDemoUserEmail(u.Email)));
        Assert.Equal(1, await db.Companies.CountAsync());
        Assert.Equal("Echte Bakkerij BV", (await db.Companies.SingleAsync()).Name);
        Assert.Equal("owner@bakkerij-voorbeeld.nl", (await db.Users.SingleAsync()).Email);
        Assert.True(await db.PlatformLogs.AnyAsync(l => l.Message == DemoDataPurge.Marker));
    }

    [Fact]
    public async Task Purge_is_idempotent_when_already_clean()
    {
        await using var db = CreateDb();
        var first = await DemoDataPurge.PurgeAsync(db, NullLogger.Instance);
        var second = await DemoDataPurge.PurgeAsync(db, NullLogger.Instance);
        Assert.Equal(0, first.Companies);
        Assert.Equal(0, second.Companies);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }
}
