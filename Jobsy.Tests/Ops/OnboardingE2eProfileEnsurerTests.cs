using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Ops;
using Jobsy.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests.Ops;

public sealed class OnboardingE2eProfileEnsurerTests
{
    [Fact]
    public async Task EnsureAsync_normalizes_user_linked_by_login_credential()
    {
        var legacyUserId = Guid.Parse("bbbbbbbb-9999-9999-9999-999999999999");
        await using var db = CreateDb();
        db.Users.Add(new User
        {
            Id = legacyUserId,
            Email = "other@jobsy.local",
            FullName = "Legacy Utrecht",
            Role = UserRole.Candidate,
            OpenForWork = false,
            HomeLocation = new GeoPoint(52.089, 5.110),
            PreferencesJson = """{"homeAddress":"Stationsplein 2, 3511ED Utrecht","maxTravelMinutes":60}""",
            IsActive = true
        });
        db.LocalAuthCredentials.Add(new LocalAuthCredential
        {
            Id = Guid.NewGuid(),
            UserId = legacyUserId,
            Email = OnboardingE2eProfileEnsurer.LoginEmail,
            PasswordHash = JobsyPasswordHasher.Hash(DemoUsersSeeder.DemoPassword)
        });
        await db.SaveChangesAsync();

        await OnboardingE2eProfileEnsurer.EnsureAsync(db, NullLogger.Instance);
        await db.SaveChangesAsync();

        var reloaded = await db.Users.AsNoTracking().SingleAsync(u => u.Id == legacyUserId);
        Assert.Equal(OnboardingE2eProfileEnsurer.DisplayFullName, reloaded.FullName);
        Assert.Equal(OnboardingE2eProfileEnsurer.HomeLocation.Latitude, reloaded.HomeLocation!.Latitude, 4);
        Assert.Contains("Kasweg 12", reloaded.PreferencesJson ?? "", StringComparison.Ordinal);
        Assert.True(reloaded.OpenForWork);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }
}
