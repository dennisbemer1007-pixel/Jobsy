using Jobsy.Core.Enums;
using Jobsy.Core.Reports.TestReflection;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace Jobsy.Tests;

public sealed class TestReflectionScoreLoaderSmokeTests
{
    [Fact]
    public async Task LoadFull_competence_for_seeded_kandidaat_when_db_available()
    {
        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__JobsyDb");
        if (string.IsNullOrWhiteSpace(conn))
        {
            return;
        }

        try
        {
            var options = new DbContextOptionsBuilder<JobsyDbContext>()
                .UseNpgsql(conn, n => n.UseNetTopologySuite()).Options;
            await using var db = new JobsyDbContext(options);
            if (!await db.Database.CanConnectAsync())
            {
                return;
            }

            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == "kandidaat@jobsy.local");
            if (user is null)
            {
                return;
            }

            var row = await db.CandidateCompetencies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == user.Id);
            if (row is null)
            {
                return;
            }

            var version = AssessmentTestVersions.Normalize(row.QuickScanVersion);
            var depth = Math.Max(
                QuickScanAssessmentBridge.ParseAnswers(AssessmentKind.Competence, row.AnswersJson, version).Count,
                row.QuickScanHighestAnswered);
            if (TestDepthRules.Reached(AssessmentKind.Competence, depth, version) != TestDepthLevel.Full)
            {
                return;
            }

            var snap = await TestReflectionScoreLoader.LoadFullAsync(db, user.Id, AssessmentKind.Competence, default);
            Assert.NotNull(snap);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            // Unit-test job sets ConnectionStrings__JobsyDb before migrate/seed; skip until schema exists.
        }
    }
}
