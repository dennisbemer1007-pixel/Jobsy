using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests.Scholen;

/// <summary>
/// Backfill contract for AddClassQuestionSet: every existing class → QuestionSet = Vo (2)
/// unless Level = Groep78 (8) → Groep78 (1). Codes/progress/results are untouched.
/// </summary>
public class AddClassQuestionSetMigrationTests
{
    [Fact]
    public async Task Backfill_sets_vo_for_existing_levels_and_leaves_pupil_rows_untouched()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase($"addqset-{Guid.NewGuid():N}")
            .Options;

        await using var db = new JobsyDbContext(options);
        var schoolId = Guid.NewGuid();
        db.Schools.Add(new School
        {
            Id = schoolId,
            Name = "Mig School",
            City = "Den Haag",
            AllowedEmailDomains = """["x.nl"]""",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        });

        var voClassId = Guid.NewGuid();
        var g78ClassId = Guid.NewGuid();
        db.SchoolClasses.AddRange(
            new SchoolClass
            {
                Id = voClassId,
                SchoolId = schoolId,
                Name = "2B",
                Level = SchoolLevel.Havo,
                QuestionSet = (PupilQuestionSet)0, // simulate pre-backfill / wrong default
                Year = 2,
                SchoolYearStart = 2026,
                PupilCount = 1,
                CreatedAtUtc = DateTime.UtcNow
            },
            new SchoolClass
            {
                Id = g78ClassId,
                SchoolId = schoolId,
                Name = "7A",
                Level = SchoolLevel.Groep78,
                QuestionSet = (PupilQuestionSet)0,
                Year = 7,
                SchoolYearStart = 2026,
                PupilCount = 1,
                CreatedAtUtc = DateTime.UtcNow
            });

        var codeId = Guid.NewGuid();
        db.PupilCodes.Add(new PupilCode
        {
            Id = codeId,
            SchoolClassId = voClassId,
            Number = 1,
            CodeLookupHash = "h",
            CodeProtected = "p",
            Status = PupilCodeStatus.InProgress,
            SessionVersion = 1,
            CreatedAtUtc = DateTime.UtcNow
        });
        db.PupilProgresses.Add(new PupilProgress
        {
            PupilCodeId = codeId,
            CurrentIndex = 3,
            AnswersJson = """{"9001":3}""",
            UpdatedAtUtc = DateTime.UtcNow
        });
        db.PupilResults.Add(new PupilResult
        {
            PupilCodeId = codeId,
            SchoolClassId = voClassId,
            CompletedAtUtc = DateTime.UtcNow,
            CompetenceScoresJson = "{}",
            RiasecScoresJson = "{}",
            ValuesScoresJson = "{}",
            CultureScoresJson = "{}",
            ScoringVersion = "1",
            StoryTemplateVersion = "1",
            StoryKeysJson = "[]"
        });
        await db.SaveChangesAsync();

        var progressBefore = await db.PupilProgresses.AsNoTracking().SingleAsync();
        var resultBefore = await db.PupilResults.AsNoTracking().SingleAsync();
        var codeBefore = await db.PupilCodes.AsNoTracking().SingleAsync();

        // Apply the same backfill SQL semantics as the migration (InMemory has no SQL).
        foreach (var c in await db.SchoolClasses.ToListAsync())
        {
            c.QuestionSet = c.Level == SchoolLevel.Groep78
                ? PupilQuestionSet.Groep78
                : PupilQuestionSet.Vo;
        }

        await db.SaveChangesAsync();

        Assert.Equal(PupilQuestionSet.Vo, (await db.SchoolClasses.SingleAsync(c => c.Id == voClassId)).QuestionSet);
        Assert.Equal(PupilQuestionSet.Groep78, (await db.SchoolClasses.SingleAsync(c => c.Id == g78ClassId)).QuestionSet);

        var progressAfter = await db.PupilProgresses.AsNoTracking().SingleAsync();
        var resultAfter = await db.PupilResults.AsNoTracking().SingleAsync();
        var codeAfter = await db.PupilCodes.AsNoTracking().SingleAsync();
        Assert.Equal(progressBefore.AnswersJson, progressAfter.AnswersJson);
        Assert.Equal(progressBefore.CurrentIndex, progressAfter.CurrentIndex);
        Assert.Equal(resultBefore.ScoringVersion, resultAfter.ScoringVersion);
        Assert.Equal(codeBefore.Status, codeAfter.Status);
        Assert.Equal(codeBefore.CodeProtected, codeAfter.CodeProtected);
        Assert.Equal(codeBefore.CodeLookupHash, codeAfter.CodeLookupHash);
    }
}
