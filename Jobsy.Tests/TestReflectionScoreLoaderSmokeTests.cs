using Jobsy.Core.Enums;
using Jobsy.Core.Reports.TestReflection;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jobsy.Tests;

public sealed class TestReflectionScoreLoaderSmokeTests
{
    [Fact]
    public async Task LoadFull_competence_for_seeded_kandidaat_when_db_available()
    {
        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__JobsyDb");
        if (string.IsNullOrWhiteSpace(conn)) return;
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseNpgsql(conn, n => n.UseNetTopologySuite()).Options;
        await using var db = new JobsyDbContext(options);
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == "kandidaat@jobsy.local");
        if (user is null) return;
        var snap = await TestReflectionScoreLoader.LoadFullAsync(db, user.Id, AssessmentKind.Competence, default);
        Assert.NotNull(snap);
    }
}
