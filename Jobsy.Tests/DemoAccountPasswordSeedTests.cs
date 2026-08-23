using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public class DemoAccountPasswordSeedTests
{
    [Fact]
    public async Task Seed_creates_local_passwords_for_all_demo_emails()
    {
        await using var db = CreateDb();
        await DemoUsersSeeder.SeedUsersAsync(db, NullLogger.Instance);

        var emails = new[]
        {
            "kandidaat@jobsy.local",
            "kandidaat.denhaag@jobsy.local",
            "kandidaat.ver@jobsy.local",
            "ondernemer@jobsy.local",
            "regio@jobsy.local",
            "enterprise@jobsy.local",
            "intermediair@jobsy.local",
            "admin@jobsy.local",
            "sales@jobsy.local",
            "ambassadeur@jobsy.local"
        };

        foreach (var email in emails)
        {
            var credential = await db.LocalAuthCredentials.SingleOrDefaultAsync(c => c.Email == email);
            Assert.NotNull(credential);
            Assert.True(
                JobsyPasswordHasher.Verify(DemoUsersSeeder.DemoPassword, credential.PasswordHash),
                $"{email} should accept {DemoUsersSeeder.DemoPassword}");
        }
    }

    [Fact]
    public async Task Seed_resets_demo_password_if_it_was_changed()
    {
        await using var db = CreateDb();
        await DemoUsersSeeder.SeedUsersAsync(db, NullLogger.Instance);

        var credential = await db.LocalAuthCredentials.SingleAsync(c => c.Email == "kandidaat@jobsy.local");
        credential.PasswordHash = JobsyPasswordHasher.Hash("ChangedPass1!");
        await db.SaveChangesAsync();

        await DemoUsersSeeder.SeedUsersAsync(db, NullLogger.Instance);

        var restored = await db.LocalAuthCredentials.SingleAsync(c => c.Email == "kandidaat@jobsy.local");
        Assert.True(JobsyPasswordHasher.Verify(DemoUsersSeeder.DemoPassword, restored.PasswordHash));
        Assert.False(JobsyPasswordHasher.Verify("ChangedPass1!", restored.PasswordHash));
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }
}
