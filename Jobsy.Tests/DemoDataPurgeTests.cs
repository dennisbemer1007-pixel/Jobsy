using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class DemoDataPurgeTests
{
    [Fact]
    public async Task Purge_removes_all_companies_vacancies_and_non_admin_users()
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

        var pricingBefore = await db.TokenPricings.CountAsync();
        var aboutBefore = await db.AboutPageSettings.CountAsync();
        Assert.True(await db.Companies.CountAsync() > 1);
        Assert.True(await db.Vacancies.AnyAsync());
        Assert.True(await db.Users.CountAsync() > 1);
        Assert.True(await db.LocalAuthCredentials.AnyAsync(c => c.Email == DemoDataPurge.KeptAdminEmail));

        var result = await DemoDataPurge.PurgeAsync(db, NullLogger.Instance);

        Assert.True(result.Companies >= 4);
        Assert.True(result.Vacancies >= 1);
        Assert.True(result.Users >= 1);

        Assert.Equal(0, await db.Companies.CountAsync());
        Assert.Equal(0, await db.Vacancies.CountAsync());
        Assert.Equal(0, await db.Applications.CountAsync());
        Assert.False(await db.Users.AnyAsync(u => u.Email != DemoDataPurge.KeptAdminEmail));
        Assert.Equal(DemoDataPurge.KeptAdminEmail, (await db.Users.SingleAsync()).Email);
        Assert.Equal(UserRole.Admin, (await db.Users.SingleAsync()).Role);
        Assert.Equal(1, await db.LocalAuthCredentials.CountAsync());
        Assert.Equal(DemoDataPurge.KeptAdminEmail, (await db.LocalAuthCredentials.SingleAsync()).Email);
        Assert.Equal(pricingBefore, await db.TokenPricings.CountAsync());
        Assert.Equal(aboutBefore, await db.AboutPageSettings.CountAsync());
        Assert.True(await db.PlatformLogs.AnyAsync(l =>
            l.Category == "Seed" && l.Message == DemoDataPurge.Marker));
    }

    [Fact]
    public async Task Purge_is_idempotent_when_already_clean()
    {
        await using var db = CreateDb();
        db.Users.Add(new User
        {
            Id = Guid.Parse("ffffffff-1111-1111-1111-111111111111"),
            Email = DemoDataPurge.KeptAdminEmail,
            FullName = "Platform Admin",
            Role = UserRole.Admin,
            IsActive = true
        });
        await db.SaveChangesAsync();

        var first = await DemoDataPurge.PurgeAsync(db, NullLogger.Instance);
        var second = await DemoDataPurge.PurgeAsync(db, NullLogger.Instance);
        Assert.Equal(0, first.Companies);
        Assert.Equal(0, second.Companies);
        Assert.Equal(0, first.Users);
        Assert.Equal(DemoDataPurge.KeptAdminEmail, (await db.Users.SingleAsync()).Email);
        Assert.Equal(1, await db.PlatformLogs.CountAsync(l => l.Message == DemoDataPurge.Marker));
    }

    [Theory]
    [InlineData("https://lobsy.nl", true)]
    [InlineData("https://www.lobsy.nl", true)]
    [InlineData("https://lobsy.nl/", true)]
    [InlineData("https://acceptatie.lobsy.nl", false)]
    [InlineData("https://lobsy-acc-web.onrender.com", false)]
    [InlineData("https://jobsy-web.onrender.com", false)]
    [InlineData("http://localhost:5201", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsLiveProductionSite_only_matches_apex_lobsy(string? url, bool expected)
        => Assert.Equal(expected, DemoDataPurge.IsLiveProductionSite(url));

    [Fact]
    public void ShouldRun_skips_when_already_marked_or_seed_enabled()
    {
        Assert.False(DemoDataPurge.ShouldRun(Config(("Seed:PurgeDemoData", "true")), alreadyMarked: true));
        Assert.False(DemoDataPurge.ShouldRun(
            Config(("Seed:Enabled", "true"), ("PublicWebBaseUrl", "https://lobsy.nl")),
            alreadyMarked: false));
    }

    [Fact]
    public void ShouldRun_on_live_lobsy_even_without_purge_flag()
    {
        Assert.True(DemoDataPurge.ShouldRun(
            Config(("Seed:Enabled", "false"), ("PublicWebBaseUrl", "https://lobsy.nl")),
            alreadyMarked: false));
        Assert.True(DemoDataPurge.ShouldRun(
            Config(("Seed:Enabled", "false"), ("Seed:PurgeDemoData", "true")),
            alreadyMarked: false));
        Assert.False(DemoDataPurge.ShouldRun(
            Config(("Seed:Enabled", "false"), ("PublicWebBaseUrl", "https://acceptatie.lobsy.nl")),
            alreadyMarked: false));
        Assert.False(DemoDataPurge.ShouldRun(Config(("Seed:Enabled", "false")), alreadyMarked: false));
    }

    [Fact]
    public void New_marker_is_not_the_previous_demo_purge_log()
        => Assert.NotEqual("Demo data purged", DemoDataPurge.Marker);

    private static IConfiguration Config(params (string Key, string Value)[] pairs)
    {
        var data = pairs.ToDictionary(p => p.Key, p => p.Value, StringComparer.OrdinalIgnoreCase);
        return new ConfigurationBuilder().AddInMemoryCollection(data!).Build();
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }
}
