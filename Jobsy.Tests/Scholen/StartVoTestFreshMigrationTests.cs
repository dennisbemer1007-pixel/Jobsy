using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests.Scholen;

public class StartVoTestFreshMigrationTests
{
    [Fact]
    public void Migration_sql_is_data_only_idempotent_and_down_is_noop()
    {
        var path = Path.Combine(
            FindRepoRoot(),
            "Jobsy.Infrastructure",
            "Data",
            "Migrations",
            "20261003062714_StartVoTestFresh.cs");
        Assert.True(File.Exists(path), path);
        var src = File.ReadAllText(path);
        Assert.Contains("Cut-over to two separate tests", src, StringComparison.Ordinal);
        Assert.Contains("DELETE FROM \"PupilResults\"", src, StringComparison.Ordinal);
        Assert.Contains("DELETE FROM \"PupilProgresses\"", src, StringComparison.Ordinal);
        Assert.Contains("SET \"Status\" = 0", src, StringComparison.Ordinal);
        Assert.Contains("SessionVersion\" = c.\"SessionVersion\" + 1", src, StringComparison.Ordinal);
        Assert.Contains("DELETE FROM \"SchoolClassAggregates\" WHERE \"QuestionSet\" = 2", src, StringComparison.Ordinal);
        Assert.Contains("DELETE FROM \"SchoolYearAggregates\" WHERE \"QuestionSet\" = 2", src, StringComparison.Ordinal);
        Assert.Contains("QuestionSet\" = 2", src, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateTable", src, StringComparison.Ordinal);
        Assert.DoesNotContain("AddColumn", src, StringComparison.Ordinal);
        Assert.Contains("No-op: deleted legacy VO answers cannot be restored", src, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Apply_twice_resets_vo_and_leaves_g78_byte_identical()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase($"vo-fresh-{Guid.NewGuid():N}")
            .Options;
        await using var db = new JobsyDbContext(options);

        var schoolId = Guid.NewGuid();
        db.Schools.Add(new School
        {
            Id = schoolId,
            Name = "Cutover",
            City = "X",
            AllowedEmailDomains = """["x.nl"]""",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        });

        var voClass = MakeClass(schoolId, "2B", SchoolLevel.Havo, 2, PupilQuestionSet.Vo);
        var g78Class = MakeClass(schoolId, "8A", SchoolLevel.Groep78, 8, PupilQuestionSet.Groep78);
        db.SchoolClasses.AddRange(voClass, g78Class);

        var voNotStarted = MakeCode(voClass.Id, 1, PupilCodeStatus.NotStarted, session: 3);
        var voProgress = MakeCode(voClass.Id, 2, PupilCodeStatus.InProgress, session: 4);
        var voDone = MakeCode(voClass.Id, 3, PupilCodeStatus.Completed, session: 5);
        var g78Progress = MakeCode(g78Class.Id, 1, PupilCodeStatus.InProgress, session: 7);
        db.PupilCodes.AddRange(voNotStarted, voProgress, voDone, g78Progress);

        db.PupilProgresses.AddRange(
            MakeProgress(voProgress.Id, """{"9001":3}""", dream: "kok"),
            MakeProgress(voDone.Id, """{"9001":5}""", dream: "leraar"),
            MakeProgress(g78Progress.Id, """{"9001":4}""", dream: "dierenarts"));
        db.PupilResults.AddRange(
            MakeResult(voDone.Id, voClass.Id, "1"),
            MakeResult(g78Progress.Id, g78Class.Id, "g78-1"));

        db.SchoolClassAggregates.AddRange(
            MakeClassAgg(schoolId, PupilQuestionSet.Vo),
            MakeClassAgg(schoolId, PupilQuestionSet.Groep78));
        db.SchoolYearAggregates.AddRange(
            MakeYearAgg(schoolId, PupilQuestionSet.Vo),
            MakeYearAgg(schoolId, PupilQuestionSet.Groep78),
            MakeYearAgg(null, PupilQuestionSet.Vo));
        await db.SaveChangesAsync();

        var g78CodeBefore = await db.PupilCodes.AsNoTracking().SingleAsync(c => c.Id == g78Progress.Id);
        var g78ProgressBefore = await db.PupilProgresses.AsNoTracking().SingleAsync(p => p.PupilCodeId == g78Progress.Id);
        var g78ResultBefore = await db.PupilResults.AsNoTracking().SingleAsync(r => r.PupilCodeId == g78Progress.Id);
        var voHash = voProgress.CodeLookupHash;
        var voProtected = voProgress.CodeProtected;
        var voNumber = voProgress.Number;

        await ApplyAsync(db);
        await ApplyAsync(db);

        var voCodes = await db.PupilCodes.AsNoTracking().Where(c => c.SchoolClassId == voClass.Id).ToListAsync();
        Assert.All(voCodes, c =>
        {
            Assert.Equal(PupilCodeStatus.NotStarted, c.Status);
        });
        Assert.Empty(await db.PupilProgresses.Where(p => voCodes.Select(c => c.Id).Contains(p.PupilCodeId)).ToListAsync());
        Assert.Empty(await db.PupilResults.Where(r => voCodes.Select(c => c.Id).Contains(r.PupilCodeId)).ToListAsync());
        Assert.Equal(3, voCodes.Single(c => c.Id == voNotStarted.Id).SessionVersion);
        Assert.Equal(5, voCodes.Single(c => c.Id == voProgress.Id).SessionVersion);
        Assert.Equal(6, voCodes.Single(c => c.Id == voDone.Id).SessionVersion);
        Assert.Equal(voHash, voCodes.Single(c => c.Id == voProgress.Id).CodeLookupHash);
        Assert.Equal(voProtected, voCodes.Single(c => c.Id == voProgress.Id).CodeProtected);
        Assert.Equal(voNumber, voCodes.Single(c => c.Id == voProgress.Id).Number);

        Assert.Equal(0, await db.PupilResults.CountAsync(r => r.ScoringVersion == "1"));
        Assert.Empty(await db.SchoolClassAggregates.Where(a => a.QuestionSet == PupilQuestionSet.Vo).ToListAsync());
        Assert.Empty(await db.SchoolYearAggregates.Where(a => a.QuestionSet == PupilQuestionSet.Vo).ToListAsync());

        var g78CodeAfter = await db.PupilCodes.AsNoTracking().SingleAsync(c => c.Id == g78Progress.Id);
        var g78ProgressAfter = await db.PupilProgresses.AsNoTracking().SingleAsync(p => p.PupilCodeId == g78Progress.Id);
        var g78ResultAfter = await db.PupilResults.AsNoTracking().SingleAsync(r => r.PupilCodeId == g78Progress.Id);
        Assert.Equal(g78CodeBefore.Status, g78CodeAfter.Status);
        Assert.Equal(g78CodeBefore.SessionVersion, g78CodeAfter.SessionVersion);
        Assert.Equal(g78CodeBefore.CodeLookupHash, g78CodeAfter.CodeLookupHash);
        Assert.Equal(g78ProgressBefore.AnswersJson, g78ProgressAfter.AnswersJson);
        Assert.Equal(g78ProgressBefore.DreamJobKey, g78ProgressAfter.DreamJobKey);
        Assert.Equal(g78ResultBefore.ScoringVersion, g78ResultAfter.ScoringVersion);
        Assert.Equal(1, await db.SchoolClassAggregates.CountAsync(a => a.QuestionSet == PupilQuestionSet.Groep78));
        Assert.Equal(1, await db.SchoolYearAggregates.CountAsync(a => a.QuestionSet == PupilQuestionSet.Groep78));
    }

    private static async Task ApplyAsync(JobsyDbContext db)
    {
        var voClassIds = await db.SchoolClasses
            .Where(c => c.QuestionSet == PupilQuestionSet.Vo)
            .Select(c => c.Id)
            .ToListAsync();
        var voCodes = await db.PupilCodes
            .Include(c => c.Progress)
            .Include(c => c.Result)
            .Where(c => voClassIds.Contains(c.SchoolClassId))
            .ToListAsync();
        var bumpIds = voCodes
            .Where(c => c.Status != PupilCodeStatus.NotStarted || c.Progress is not null || c.Result is not null)
            .Select(c => c.Id)
            .ToHashSet();

        foreach (var code in voCodes.Where(c => bumpIds.Contains(c.Id)))
        {
            code.Status = PupilCodeStatus.NotStarted;
            code.SessionVersion++;
        }

        var voCodeIds = voCodes.Select(c => c.Id).ToList();
        db.PupilResults.RemoveRange(db.PupilResults.Where(r => voCodeIds.Contains(r.PupilCodeId)));
        db.PupilProgresses.RemoveRange(db.PupilProgresses.Where(p => voCodeIds.Contains(p.PupilCodeId)));
        db.SchoolClassAggregates.RemoveRange(
            db.SchoolClassAggregates.Where(a => a.QuestionSet == PupilQuestionSet.Vo));
        db.SchoolYearAggregates.RemoveRange(
            db.SchoolYearAggregates.Where(a => a.QuestionSet == PupilQuestionSet.Vo));
        await db.SaveChangesAsync();
    }

    private static SchoolClass MakeClass(
        Guid schoolId, string name, SchoolLevel level, int year, PupilQuestionSet set) => new()
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            Name = name,
            Level = level,
            Year = year,
            QuestionSet = set,
            SchoolYearStart = 2026,
            PupilCount = 3,
            CreatedAtUtc = DateTime.UtcNow
        };

    private static PupilCode MakeCode(Guid classId, int number, PupilCodeStatus status, int session) => new()
    {
        Id = Guid.NewGuid(),
        SchoolClassId = classId,
        Number = number,
        CodeLookupHash = $"h-{classId:N}-{number}",
        CodeProtected = $"p-{number}",
        Status = status,
        SessionVersion = session,
        CreatedAtUtc = DateTime.UtcNow
    };

    private static PupilProgress MakeProgress(Guid codeId, string answers, string dream) => new()
    {
        PupilCodeId = codeId,
        CurrentIndex = 3,
        AnswersJson = answers,
        DreamJobKey = dream,
        LikesJson = """["tekenen"]""",
        UpdatedAtUtc = DateTime.UtcNow
    };

    private static PupilResult MakeResult(Guid codeId, Guid classId, string scoring) => new()
    {
        PupilCodeId = codeId,
        SchoolClassId = classId,
        CompletedAtUtc = DateTime.UtcNow,
        CompetenceScoresJson = "{}",
        RiasecScoresJson = "{}",
        ValuesScoresJson = "{}",
        CultureScoresJson = "{}",
        ScoringVersion = scoring,
        StoryTemplateVersion = "1",
        StoryKeysJson = "[]"
    };

    private static SchoolClassAggregate MakeClassAgg(Guid schoolId, PupilQuestionSet set) => new()
    {
        Id = Guid.NewGuid(),
        SchoolId = schoolId,
        SchoolYearStart = 2026,
        ClassLabel = set == PupilQuestionSet.Vo ? "2B" : "8A",
        Level = set == PupilQuestionSet.Vo ? SchoolLevel.Havo : SchoolLevel.Groep78,
        Year = set == PupilQuestionSet.Vo ? 2 : 8,
        QuestionSet = set,
        SnapshotAtUtc = DateTime.UtcNow
    };

    private static SchoolYearAggregate MakeYearAgg(Guid? schoolId, PupilQuestionSet set) => new()
    {
        Id = Guid.NewGuid(),
        SchoolId = schoolId,
        SchoolYearStart = 2026,
        QuestionSet = set,
        SnapshotAtUtc = DateTime.UtcNow
    };

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Jobsy.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Jobsy.sln not found");
    }
}
