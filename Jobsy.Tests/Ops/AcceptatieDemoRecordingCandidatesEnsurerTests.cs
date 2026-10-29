using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Ops;
using Jobsy.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests.Ops;

public sealed class AcceptatieDemoRecordingCandidatesEnsurerTests
{
    [Fact]
    public async Task EnsureAsync_creates_three_sam_de_wit_accounts_with_demo_password_and_no_onboarding_row()
    {
        await using var db = CreateDb();

        await AcceptatieDemoRecordingCandidatesEnsurer.EnsureAsync(db, NullLogger.Instance);
        await db.SaveChangesAsync();

        foreach (var email in AcceptatieDemoRecordingCandidatesEnsurer.AllEmails)
        {
            var normalized = email.ToLowerInvariant();
            var user = await db.Users.AsNoTracking().SingleAsync(u => u.Email == normalized);
            Assert.Equal(KandidaatDemoProfileEnsurer.DemoFullName, user.FullName);
            Assert.Equal(KandidaatDemoProfileEnsurer.DemoFirstName, user.FirstName);
            Assert.Equal(KandidaatDemoProfileEnsurer.DemoLastName, user.LastName);
            Assert.True(user.IsTestAccount);
            Assert.False(user.OpenForWork);
            Assert.Null(user.PreferencesJson);
            Assert.Null(user.CandidateHowToCompletedAt);

            var credential = await db.LocalAuthCredentials.AsNoTracking()
                .SingleAsync(c => c.Email == normalized);
            Assert.True(JobsyPasswordHasher.Verify(DemoUsersSeeder.DemoPassword, credential.PasswordHash));

            Assert.False(await db.CandidateOnboardings.AnyAsync(o => o.UserId == user.Id));
        }
    }

    [Fact]
    public async Task EnsureAsync_resets_completed_onboarding_back_to_fresh_state()
    {
        await using var db = CreateDb();
        var userId = AcceptatieDemoRecordingCandidatesEnsurer.Demo1UserId;
        db.Users.Add(new User
        {
            Id = userId,
            Email = AcceptatieDemoRecordingCandidatesEnsurer.Demo1Email,
            FullName = "Was klaar",
            Role = UserRole.Candidate,
            IsActive = true,
            OpenForWork = true,
            PreferencesJson = """{"maxTravelMinutes":30,"preferredTransport":"Fiets","educations":["MBO"]}""",
            CandidateHowToCompletedAt = DateTime.UtcNow,
        });
        db.CandidateOnboardings.Add(new CandidateOnboarding
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CurrentStep = 10,
            CompletedAtUtc = DateTime.UtcNow,
            StartedAtUtc = DateTime.UtcNow.AddDays(-1),
            UpdatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        await AcceptatieDemoRecordingCandidatesEnsurer.EnsureAsync(db, NullLogger.Instance);
        await db.SaveChangesAsync();

        var reloaded = await db.Users.AsNoTracking().SingleAsync(u => u.Id == userId);
        Assert.Equal(KandidaatDemoProfileEnsurer.DemoFullName, reloaded.FullName);
        Assert.False(reloaded.OpenForWork);
        Assert.Null(reloaded.CandidateHowToCompletedAt);
        Assert.False(await db.CandidateOnboardings.AnyAsync(o => o.UserId == userId));
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }
}
