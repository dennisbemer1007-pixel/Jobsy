using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Ops;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Jobsy.Tests.TestAccounts;

public class TestAccountsCleanupTests
{
    private const string FakePassword = "fake-Password-For-Tests-1";

    [Fact]
    public async Task Execute_with_wrong_count_refuses()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new JobsyDbContext(options);
        var config = BuildConfig();
        var seeder = new TestAccountsSeedService(db, config);
        await seeder.SeedAsync(false, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Admin", "Candidate" });
        await db.SaveChangesAsync();

        var cleanup = new TestAccountsCleanupService(db);
        var (ok, error) = await cleanup.ExecuteAsync(expectUsers: 99);
        Assert.False(ok);
        Assert.Contains("expect-users", error, StringComparison.Ordinal);
        Assert.Equal(2, await db.Users.CountAsync(u => u.IsTestAccount));
    }

    [Fact]
    public async Task Execute_deletes_test_data_and_keeps_real_rows()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new JobsyDbContext(options);
        var realId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = realId,
            Email = "real@example.com",
            FullName = "Real User",
            Role = UserRole.Candidate,
            IsActive = true,
            IsTestAccount = false
        });
        await db.SaveChangesAsync();

        var seeder = new TestAccountsSeedService(db, BuildConfig());
        await seeder.SeedAsync(false, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Admin", "Candidate" });
        await db.SaveChangesAsync();
        var plan = await new TestAccountsCleanupService(db).PlanAsync();
        Assert.Equal(2, plan.TestUserCount);

        var (ok, error) = await new TestAccountsCleanupService(db).ExecuteAsync(plan.TestUserCount);
        Assert.True(ok, error);
        Assert.Equal(0, await db.Users.CountAsync(u => u.IsTestAccount));
        Assert.True(await db.Users.AnyAsync(u => u.Id == realId));
    }

    private static IConfiguration BuildConfig()
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TestAccounts:EmailDomain"] = "lobsy.nl",
                ["TestAccounts:Password:Admin"] = FakePassword,
                ["TestAccounts:Password:Candidate"] = FakePassword,
            })
            .Build();
}
