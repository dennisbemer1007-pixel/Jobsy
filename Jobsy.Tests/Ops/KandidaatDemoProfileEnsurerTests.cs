using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Ops;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests.Ops;

public sealed class KandidaatDemoProfileEnsurerTests
{
    [Fact]
    public async Task EnsureAsync_renames_legacy_display_name_and_application_copy()
    {
        await using var db = CreateDb();
        var user = new User
        {
            Id = KandidaatDemoProfileEnsurer.PrimaryDemoCandidateId,
            Email = "sanne@jobsy.local",
            FullName = "Oude Demo Naam",
            FirstName = "Oude",
            LastName = "Demo Naam",
            Role = UserRole.Candidate,
            IsActive = true,
        };
        db.Users.Add(user);
        db.Applications.Add(new Application
        {
            Id = Guid.NewGuid(),
            CandidateUserId = user.Id,
            CandidateName = "Oude Demo Naam",
            VacancyId = Guid.NewGuid(),
            Status = ApplicationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        await KandidaatDemoProfileEnsurer.EnsureAsync(db, NullLogger.Instance);

        var reloaded = await db.Users.SingleAsync(u => u.Id == user.Id);
        Assert.Equal(KandidaatDemoProfileEnsurer.DemoFullName, reloaded.FullName);
        Assert.Equal(KandidaatDemoProfileEnsurer.DemoFirstName, reloaded.FirstName);
        Assert.Equal(KandidaatDemoProfileEnsurer.DemoLastName, reloaded.LastName);

        var app = await db.Applications.SingleAsync(a => a.CandidateUserId == user.Id);
        Assert.Equal(KandidaatDemoProfileEnsurer.DemoFullName, app.CandidateName);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }
}
