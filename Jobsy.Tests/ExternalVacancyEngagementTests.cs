using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services.CandidateExternalVacancies;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class ExternalVacancyEngagementTests
{
    [Theory]
    [InlineData(CandidateExternalVacancyStatus.Saved, true)]
    [InlineData(CandidateExternalVacancyStatus.Applied, true)]
    [InlineData(CandidateExternalVacancyStatus.Closed, false)]
    [InlineData(CandidateExternalVacancyStatus.RemovedFromSaved, false)]
    public void IsOnSavedList_matches_status(CandidateExternalVacancyStatus status, bool expected)
    {
        Assert.Equal(expected, CandidateExternalVacancyService.IsOnSavedList(status));
    }

    [Fact]
    public async Task ListAsync_excludes_closed_and_removed_from_saved()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.CandidateExternalVacancies.AddRange(
            Row(userId, CandidateExternalVacancyStatus.Saved, "A"),
            Row(userId, CandidateExternalVacancyStatus.Applied, "B"),
            Row(userId, CandidateExternalVacancyStatus.Closed, "C"),
            Row(userId, CandidateExternalVacancyStatus.RemovedFromSaved, "D"));
        await db.SaveChangesAsync();

        var listed = await db.CandidateExternalVacancies.AsNoTracking()
            .Where(v => v.CandidateUserId == userId && CandidateExternalVacancyService.IsOnSavedList(v.Status))
            .Select(v => v.Title)
            .ToListAsync();

        Assert.Equal(["A", "B"], listed);
    }

    private static CandidateExternalVacancy Row(Guid userId, CandidateExternalVacancyStatus status, string title)
        => new()
        {
            Id = Guid.NewGuid(),
            CandidateUserId = userId,
            SourceUrl = "https://example.nl/v",
            SourceHost = "example.nl",
            Title = title,
            CompanyName = "Co",
            Place = "Pl",
            Status = status,
            SavedAtUtc = DateTime.UtcNow
        };

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }
}
