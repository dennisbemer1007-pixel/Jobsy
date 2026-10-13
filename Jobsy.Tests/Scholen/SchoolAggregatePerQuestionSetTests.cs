using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Scholen;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests.Scholen;

public class SchoolAggregatePerQuestionSetTests
{
    [Fact]
    public async Task Snapshot_writes_separate_year_and_platform_rows_per_test()
    {
        await using var db = CreateDb();
        var schoolId = Guid.NewGuid();
        db.Schools.Add(MakeSchool(schoolId, "Mix"));
        var g78 = MakeClass(schoolId, "G8A", SchoolLevel.Groep78, 8, PupilQuestionSet.Groep78, pupilCount: 6);
        var vo = MakeClass(schoolId, "2B", SchoolLevel.Havo, 2, PupilQuestionSet.Vo, pupilCount: 6);
        db.SchoolClasses.AddRange(g78, vo);
        await SeedCompletedAsync(db, g78, 6, "g78-1");
        await SeedCompletedAsync(db, vo, 6, "1");

        var written = await new SchoolAggregateSnapshotter(db).SnapshotSchoolYearAsync(schoolId, 2026);
        Assert.True(written >= 4);

        var yearRows = await db.SchoolYearAggregates.AsNoTracking()
            .Where(a => a.SchoolId == schoolId && a.SchoolYearStart == 2026)
            .ToListAsync();
        Assert.Equal(2, yearRows.Count);
        Assert.Contains(yearRows, r => r.QuestionSet == PupilQuestionSet.Groep78 && r.CompletedCount == 6);
        Assert.Contains(yearRows, r => r.QuestionSet == PupilQuestionSet.Vo && r.CompletedCount == 6);

        var platform = await db.SchoolYearAggregates.AsNoTracking()
            .Where(a => a.SchoolId == null && a.SchoolYearStart == 2026)
            .ToListAsync();
        Assert.Equal(2, platform.Count);
        Assert.DoesNotContain(platform, r => r.CompletedCount == 12);
    }

    [Fact]
    public async Task Snapshot_applies_k_gate_per_test()
    {
        await using var db = CreateDb();
        var schoolId = Guid.NewGuid();
        db.Schools.Add(MakeSchool(schoolId, "K"));
        var g78 = MakeClass(schoolId, "G8A", SchoolLevel.Groep78, 8, PupilQuestionSet.Groep78, pupilCount: 6);
        var vo = MakeClass(schoolId, "2B", SchoolLevel.Havo, 2, PupilQuestionSet.Vo, pupilCount: 3);
        db.SchoolClasses.AddRange(g78, vo);
        await SeedCompletedAsync(db, g78, 6, "g78-1");
        await SeedCompletedAsync(db, vo, 3, "1");

        await new SchoolAggregateSnapshotter(db).SnapshotSchoolYearAsync(schoolId, 2026);

        var yearRows = await db.SchoolYearAggregates.AsNoTracking()
            .Where(a => a.SchoolId == schoolId && a.SchoolYearStart == 2026)
            .ToListAsync();
        Assert.Single(yearRows);
        Assert.Equal(PupilQuestionSet.Groep78, yearRows[0].QuestionSet);
        Assert.Equal(6, yearRows[0].CompletedCount);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("agg-" + Guid.NewGuid())
            .Options;
        return new JobsyDbContext(options);
    }

    private static School MakeSchool(Guid id, string name) => new()
    {
        Id = id,
        Name = name,
        City = "X",
        AllowedEmailDomains = "[\"x.nl\"]",
        IsActive = true,
        ProcessorAgreementSignedOn = new DateOnly(2026, 1, 1),
        ProcessorAgreementVersion = "1",
        CreatedAtUtc = DateTime.UtcNow,
        CreatedByUserId = Guid.NewGuid()
    };

    private static SchoolClass MakeClass(
        Guid schoolId,
        string name,
        SchoolLevel level,
        int year,
        PupilQuestionSet set,
        int pupilCount) => new()
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            Name = name,
            Level = level,
            Year = year,
            QuestionSet = set,
            SchoolYearStart = 2026,
            PupilCount = pupilCount,
            TestWindow = TestWindowState.Open,
            CreatedAtUtc = DateTime.UtcNow
        };

    private static async Task SeedCompletedAsync(
        JobsyDbContext db,
        SchoolClass schoolClass,
        int completed,
        string scoringVersion)
    {
        for (var i = 1; i <= schoolClass.PupilCount; i++)
        {
            var codeId = Guid.NewGuid();
            db.PupilCodes.Add(new PupilCode
            {
                Id = codeId,
                SchoolClassId = schoolClass.Id,
                Number = i,
                CodeLookupHash = $"h{codeId:N}",
                CodeProtected = "p",
                Status = i <= completed ? PupilCodeStatus.Completed : PupilCodeStatus.NotStarted,
                SessionVersion = 1,
                CreatedAtUtc = DateTime.UtcNow
            });
            if (i <= completed)
            {
                db.PupilResults.Add(new PupilResult
                {
                    PupilCodeId = codeId,
                    SchoolClassId = schoolClass.Id,
                    CompletedAtUtc = DateTime.UtcNow,
                    HollandCode = "SAE",
                    TopValue = "Helpen",
                    TopCulture = "Samen",
                    DreamJobKey = "arts",
                    CompetenceScoresJson = """{"a":3,"b":3}""",
                    RiasecScoresJson = """{"S":3,"A":2,"E":1}""",
                    ValuesScoresJson = "{}",
                    CultureScoresJson = "{}",
                    ScoringVersion = scoringVersion,
                    StoryTemplateVersion = "1",
                    StoryKeysJson = "[]"
                });
            }
        }

        await db.SaveChangesAsync();
    }
}
