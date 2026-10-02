using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Entities;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Scholen;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Scholen;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests.Scholen;

public class SchoolRetentionAndAggregatesTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private readonly RoleFunctionalWebAppFactory _factory;

    public SchoolRetentionAndAggregatesTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Snapshotter_class_under_5_no_class_row_but_merges_into_school_when_total_ge_5()
    {
        await using var db = CreateDb();
        var schoolId = Guid.NewGuid();
        db.Schools.Add(MakeSchool(schoolId, "A"));
        var c1 = MakeClass(schoolId, "2A", 2026, 4);
        var c2 = MakeClass(schoolId, "2B", 2026, 4);
        db.SchoolClasses.AddRange(c1, c2);
        await db.SaveChangesAsync();
        await SeedCodesAndResultsAsync(db, c1, completed: 4, dreamSparse: true);
        await SeedCodesAndResultsAsync(db, c2, completed: 4, dreamSparse: false);

        var snap = new SchoolAggregateSnapshotter(db, TimeProvider.System);
        var written = await snap.SnapshotSchoolYearAsync(schoolId, 2026);

        Assert.Equal(0, await db.SchoolClassAggregates.CountAsync(a => a.SchoolId == schoolId));
        var year = await db.SchoolYearAggregates.SingleAsync(a => a.SchoolId == schoolId && a.SchoolYearStart == 2026);
        Assert.Equal(8, year.CompletedCount);
        Assert.True(written >= 1);

        // Dream jobs with count < 2 → Overig in class-level path; school merge collapses again.
        Assert.DoesNotContain("PupilCode", year.DreamJobCountsJson, StringComparison.OrdinalIgnoreCase);
        AssertNoCodeIdsInAggregate(year);
    }

    [Fact]
    public async Task Snapshotter_class_with_5_plus_writes_class_row_and_is_idempotent()
    {
        await using var db = CreateDb();
        var schoolId = Guid.NewGuid();
        db.Schools.Add(MakeSchool(schoolId, "B"));
        var c = MakeClass(schoolId, "3A", 2026, 6);
        db.SchoolClasses.Add(c);
        await db.SaveChangesAsync();
        await SeedCodesAndResultsAsync(db, c, completed: 6, dreamSparse: true);

        var snap = new SchoolAggregateSnapshotter(db, TimeProvider.System);
        var first = await snap.SnapshotSchoolYearAsync(schoolId, 2026);
        var second = await snap.SnapshotSchoolYearAsync(schoolId, 2026);
        Assert.Equal(1, await db.SchoolClassAggregates.CountAsync(a => a.SchoolId == schoolId));
        Assert.Equal(first, second);

        var classAgg = await db.SchoolClassAggregates.SingleAsync(a => a.SchoolId == schoolId);
        Assert.Equal("3A", classAgg.ClassLabel);
        Assert.Contains("Overig", classAgg.DreamJobCountsJson, StringComparison.Ordinal);
        AssertNoCodeIdsInAggregate(classAgg);
        Assert.DoesNotContain("code", classAgg.RiasecTop3CountsJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Retention_deletes_ended_year_keeps_current_and_aggregates_FK_free()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2027, 8, 1, 10, 0, 0, TimeSpan.Zero));
        await using var db = CreateDb();
        var features = new StubFeatures(7, 31);
        var schoolId = Guid.NewGuid();
        db.Schools.Add(MakeSchool(schoolId, "C"));
        var oldClass = MakeClass(schoolId, "1A", 2026, 5);
        var newClass = MakeClass(schoolId, "1B", 2027, 5);
        db.SchoolClasses.AddRange(oldClass, newClass);
        await db.SaveChangesAsync();
        await SeedCodesAndResultsAsync(db, oldClass, completed: 5);
        await SeedCodesAndResultsAsync(db, newClass, completed: 5);

        var snap = new SchoolAggregateSnapshotter(db, clock);
        var retention = new SchoolRetentionService(
            db, snap, features, NullLogger<SchoolRetentionService>.Instance, clock);

        // With SchoolsEnabled = false the job must still run.
        Assert.False(features.SchoolsEnabled);
        var result = await retention.RunAsync();
        Assert.Equal("ok", result.Outcome);
        Assert.Equal(1, result.ClassesDeleted);
        Assert.False(await db.SchoolClasses.AnyAsync(c => c.Id == oldClass.Id));
        Assert.True(await db.SchoolClasses.AnyAsync(c => c.Id == newClass.Id));
        Assert.True(await db.SchoolYearAggregates.AnyAsync(a => a.SchoolId == schoolId && a.SchoolYearStart == 2026));
        Assert.True(await db.SchoolRetentionRuns.AnyAsync());
    }

    [Fact]
    public async Task Retention_custom_cutoff_30_june_works()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2027, 7, 1, 10, 0, 0, TimeSpan.Zero));
        await using var db = CreateDb();
        var features = new StubFeatures(6, 30);
        var schoolId = Guid.NewGuid();
        db.Schools.Add(MakeSchool(schoolId, "D"));
        var oldClass = MakeClass(schoolId, "2A", 2026, 5);
        db.SchoolClasses.Add(oldClass);
        await db.SaveChangesAsync();
        await SeedCodesAndResultsAsync(db, oldClass, completed: 5);

        var snap = new SchoolAggregateSnapshotter(db, clock);
        var retention = new SchoolRetentionService(
            db, snap, features, NullLogger<SchoolRetentionService>.Instance, clock);
        var result = await retention.RunAsync();
        Assert.Equal(1, result.ClassesDeleted);
        Assert.Equal(new DateOnly(2027, 6, 30), result.CutoffDate);
    }

    [Fact]
    public async Task Retention_dry_run_deletes_nothing()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2027, 8, 1, 10, 0, 0, TimeSpan.Zero));
        await using var db = CreateDb();
        var features = new StubFeatures(7, 31);
        var schoolId = Guid.NewGuid();
        db.Schools.Add(MakeSchool(schoolId, "E"));
        var oldClass = MakeClass(schoolId, "2A", 2026, 5);
        db.SchoolClasses.Add(oldClass);
        await db.SaveChangesAsync();
        await SeedCodesAndResultsAsync(db, oldClass, completed: 5);

        var snap = new SchoolAggregateSnapshotter(db, clock);
        var retention = new SchoolRetentionService(
            db, snap, features, NullLogger<SchoolRetentionService>.Instance, clock);
        var dry = await retention.DryRunAsync();
        Assert.Equal(1, dry.ClassesWouldDelete);
        Assert.True(await db.SchoolClasses.AnyAsync(c => c.Id == oldClass.Id));
        Assert.Equal(5, await db.PupilResults.CountAsync());
    }

    [Fact]
    public async Task Early_delete_school_keeps_aggregates_and_writes_audit()
    {
        await using var db = CreateDb();
        var schoolId = Guid.NewGuid();
        db.Schools.Add(MakeSchool(schoolId, "Verwijder College"));
        var c = MakeClass(schoolId, "2B", 2026, 5);
        db.SchoolClasses.Add(c);
        var staffId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = staffId,
            Email = "sa@verwijder.nl",
            FullName = "Admin",
            Role = UserRole.SchoolAdmin,
            SchoolId = schoolId,
            IsActive = true
        });
        await db.SaveChangesAsync();
        await SeedCodesAndResultsAsync(db, c, completed: 5);

        var snap = new SchoolAggregateSnapshotter(db, TimeProvider.System);
        var retention = new SchoolRetentionService(
            db, snap, new StubFeatures(7, 31), NullLogger<SchoolRetentionService>.Instance);
        var result = await retention.DeleteSchoolNowAsync(schoolId, "Verwijder College");
        Assert.Equal(1, result.ClassesDeleted);
        Assert.False(await db.SchoolClasses.AnyAsync(x => x.SchoolId == schoolId));
        Assert.False(await db.Users.AnyAsync(u => u.Id == staffId));
        Assert.False(await db.Schools.Where(s => s.Id == schoolId).Select(s => s.IsActive).SingleAsync());
        Assert.True(await db.SchoolYearAggregates.AnyAsync(a => a.SchoolId == schoolId));
        Assert.True(await db.PlatformLogs.AnyAsync(l =>
            l.Category == "Scholen.Audit" && l.Message.Contains("school.delete", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Reporting_reads_aggregates_only_masks_lt5_and_csv_header()
    {
        await using var db = CreateDb();
        var schoolId = Guid.NewGuid();
        db.Schools.Add(MakeSchool(schoolId, "R"));
        db.SchoolClassAggregates.Add(new SchoolClassAggregate
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            SchoolYearStart = 2026,
            ClassLabel = "2B",
            Level = SchoolLevel.Havo,
            Year = 2,
            PupilCount = 10,
            StartedCount = 8,
            CompletedCount = 6,
            RiasecTop3CountsJson = """{"S":4,"A":3,"E":2}""",
            TopValueCountsJson = """{"Helpen":4}""",
            TopCultureCountsJson = """{"Samen":3}""",
            CompetenceBandCountsJson = """{"Midden":6}""",
            DreamJobCountsJson = """{"arts":3,"Overig":2}""",
            SnapshotAtUtc = DateTime.UtcNow.Date
        });
        db.SchoolYearAggregates.Add(new SchoolYearAggregate
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            SchoolYearStart = 2026,
            PupilCount = 10,
            StartedCount = 8,
            CompletedCount = 6,
            RiasecTop3CountsJson = """{"S":4,"A":3,"E":2}""",
            TopValueCountsJson = """{"Helpen":4}""",
            TopCultureCountsJson = """{"Samen":3}""",
            CompetenceBandCountsJson = """{"Midden":6}""",
            DreamJobCountsJson = """{"arts":3,"Overig":2}""",
            SnapshotAtUtc = DateTime.UtcNow.Date
        });
        // Live pupil tables left empty on purpose — report must not need them.
        await db.SaveChangesAsync();

        var reporting = new SchoolReportingService(db, new StubFeatures(7, 31));
        var view = await reporting.GetReportAsync(new SchoolReportFilterDto(2026, schoolId, null, null));
        Assert.Equal(6, view.CompletedCount);
        Assert.DoesNotContain(view.RiasecTop3, r => r.Masked);

        var masked = await reporting.GetReportAsync(new SchoolReportFilterDto(2026, schoolId, SchoolLevel.Vwo, 6));
        Assert.Null(masked.CompletedCount);

        var (bytes, _) = await reporting.ExportCsvAsync(new SchoolReportFilterDto(2026, schoolId, null, null));
        var text = System.Text.Encoding.UTF8.GetString(bytes);
        Assert.Contains(SchoolReportingService.CsvHeader, text, StringComparison.Ordinal);
    }

    [Fact]
    public void Reporting_service_source_does_not_query_pupil_tables()
    {
        var path = Path.Combine(
            FindRepoRoot(),
            "Jobsy.Infrastructure",
            "Scholen",
            "SchoolReportingService.cs");
        var src = File.ReadAllText(path);
        Assert.DoesNotContain("PupilResults", src, StringComparison.Ordinal);
        Assert.DoesNotContain("PupilCodes", src, StringComparison.Ordinal);
        Assert.DoesNotContain("PupilProgress", src, StringComparison.Ordinal);
        Assert.Contains("SchoolClassAggregates", src, StringComparison.Ordinal);
        Assert.Contains("SchoolYearAggregates", src, StringComparison.Ordinal);
    }

    [Fact]
    public void Aggregate_entities_have_no_fk_to_classes_or_codes()
    {
        foreach (var type in new[] { typeof(SchoolClassAggregate), typeof(SchoolYearAggregate) })
        {
            var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            Assert.DoesNotContain(props, p => p.Name.Contains("ClassId", StringComparison.OrdinalIgnoreCase)
                                              && p.Name != "ClassLabel");
            Assert.DoesNotContain(props, p => p.Name.Contains("Code", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(props, p => p.Name.Contains("Pupil", StringComparison.OrdinalIgnoreCase)
                                              && p.Name is not ("PupilCount"));
        }
    }

    [Fact]
    public async Task Admin_report_endpoints_forbid_school_roles()
    {
        // Trigger RoleFunctionalWebAppFactory seed before inserting school staff
        // (EnsureSeeded skips when Users.Any()).
        using (_factory.CreateClient()) { }

        await EnableSchoolsAsync(true);
        var (schoolAdminId, _, _) = await SeedSchoolStaffAsync();
        using var schoolClient = JobsyTestAuth.CreateAuthenticatedClient(_factory, schoolAdminId);
        Assert.Equal(HttpStatusCode.Forbidden, (await schoolClient.GetAsync("api/admin/schools/rapportage")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await schoolClient.GetAsync("api/admin/schools/retention")).StatusCode);

        using var admin = JobsyTestAuth.CreateAuthenticatedClient(_factory, _factory.AdminId);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("api/admin/schools/rapportage")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync("api/admin/schools/retention/dry-run", null)).StatusCode);
    }

    [Fact]
    public async Task School_year_delete_requires_school_admin_and_confirm()
    {
        using (_factory.CreateClient()) { }

        await EnableSchoolsAsync(true);
        var (schoolAdminId, schoolId, classId) = await SeedSchoolStaffAsync(withClass: true, codeCount: 5);
        await SeedCompletedResultsAsync(classId, 5);

        using var client = JobsyTestAuth.CreateAuthenticatedClient(_factory, schoolAdminId);
        var bad = await client.PostAsJsonAsync(
            "api/school/privacy/delete-year",
            new DeleteSchoolYearRequest("nee"));
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);

        var ok = await client.PostAsJsonAsync(
            "api/school/privacy/delete-year",
            new DeleteSchoolYearRequest(SchoolRetentionService.ConfirmDeleteYearPhrase));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        Assert.False(await db.SchoolClasses.AnyAsync(c => c.Id == classId));
        Assert.True(await db.SchoolYearAggregates.AnyAsync(a => a.SchoolId == schoolId));
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("sch-ret-" + Guid.NewGuid())
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

    private static SchoolClass MakeClass(Guid schoolId, string name, int yearStart, int pupilCount) => new()
    {
        Id = Guid.NewGuid(),
        SchoolId = schoolId,
        Name = name,
        Level = SchoolLevel.Havo,
        Year = 2,
        SchoolYearStart = yearStart,
        PupilCount = pupilCount,
        CreatedAtUtc = DateTime.UtcNow
    };

    private static async Task SeedCodesAndResultsAsync(
        JobsyDbContext db,
        SchoolClass schoolClass,
        int completed,
        bool dreamSparse = false)
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
                var dream = dreamSparse
                    ? $"job-{i}" // each unique → Overig
                    : "arts";
                db.PupilResults.Add(new PupilResult
                {
                    PupilCodeId = codeId,
                    SchoolClassId = schoolClass.Id,
                    CompletedAtUtc = DateTime.UtcNow,
                    HollandCode = "SAE",
                    TopValue = "Helpen",
                    TopCulture = "Samen",
                    DreamJobKey = dream,
                    CompetenceScoresJson = """{"a":3,"b":3}""",
                    RiasecScoresJson = """{"S":3,"A":2,"E":1}""",
                    ValuesScoresJson = "{}",
                    CultureScoresJson = "{}",
                    ScoringVersion = "t",
                    StoryTemplateVersion = "t",
                    StoryKeysJson = "[]"
                });
            }
        }

        await db.SaveChangesAsync();
    }

    private static void AssertNoCodeIdsInAggregate(object agg)
    {
        var json = string.Join('|', agg.GetType().GetProperties()
            .Where(p => p.PropertyType == typeof(string))
            .Select(p => p.GetValue(agg)?.ToString() ?? ""));
        Assert.DoesNotContain("PupilCodeId", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CodeLookup", json, StringComparison.OrdinalIgnoreCase);
        // Guids of form with many hex chars that look like code ids should not appear as property names.
        Assert.DoesNotContain("\"codeId\"", json, StringComparison.OrdinalIgnoreCase);
    }

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

        throw new InvalidOperationException("Repo root not found");
    }

    private async Task EnableSchoolsAsync(bool enabled)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var row = await db.PlatformFeatureSettings.FirstOrDefaultAsync();
        if (row is null)
        {
            row = new PlatformFeatureSettings
            {
                Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                SchoolsEnabled = enabled
            };
            db.PlatformFeatureSettings.Add(row);
        }
        else
        {
            row.SchoolsEnabled = enabled;
        }

        await db.SaveChangesAsync();
    }

    private async Task<(Guid AdminId, Guid SchoolId, Guid ClassId)> SeedSchoolStaffAsync(
        bool withClass = false,
        int codeCount = 28)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var schoolId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        db.Schools.Add(MakeSchool(schoolId, "Voorbeeld College"));
        db.Users.Add(new User
        {
            Id = adminId,
            Email = $"sa-{adminId:N}@voorbeeldcollege.nl",
            FullName = "J. Visser",
            Role = UserRole.SchoolAdmin,
            SchoolId = schoolId,
            IsActive = true,
            AuthenticatorEnabled = true
        });
        var classId = Guid.Empty;
        if (withClass)
        {
            classId = Guid.NewGuid();
            var schoolClass = MakeClass(schoolId, "2B", SchoolYear.Current(DateOnly.FromDateTime(DateTime.UtcNow)), codeCount);
            schoolClass.Id = classId;
            db.SchoolClasses.Add(schoolClass);
            await db.SaveChangesAsync();
            var codes = scope.ServiceProvider.GetRequiredService<IPupilCodeService>();
            await codes.GenerateAsync(codeCount, schoolClass);
        }
        else
        {
            await db.SaveChangesAsync();
        }

        return (adminId, schoolId, classId);
    }

    private async Task SeedCompletedResultsAsync(Guid classId, int count)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var codes = await db.PupilCodes.Where(c => c.SchoolClassId == classId).Take(count).ToListAsync();
        foreach (var c in codes)
        {
            c.Status = PupilCodeStatus.Completed;
            db.PupilResults.Add(new PupilResult
            {
                PupilCodeId = c.Id,
                SchoolClassId = classId,
                CompletedAtUtc = DateTime.UtcNow,
                HollandCode = "SAE",
                TopValue = "Helpen",
                DreamJobKey = "arts",
                CompetenceScoresJson = "{}",
                RiasecScoresJson = """{"S":3}""",
                ValuesScoresJson = "{}",
                CultureScoresJson = "{}",
                ScoringVersion = "t",
                StoryTemplateVersion = "t",
                StoryKeysJson = "[]"
            });
        }

        await db.SaveChangesAsync();
    }

    private sealed class StubFeatures(int month, int day) : IPlatformFeatureService
    {
        public bool SchoolsEnabled { get; set; }

        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(
                VacancyContentModerationEnabled: false,
                AuthenticatorEnabled: false,
                PublicWebBaseUrl: "",
                UpdatedAtUtc: null,
                InactiveCompanyDays: 120,
                SessionInactivityTimeoutMinutes: 30,
                FreePublishUntil: null,
                MinimumSessionVersion: 0,
                SupportAccessNotifyAdmins: false,
                SupportAccessNotifySubject: false,
                SchoolsEnabled: SchoolsEnabled,
                SchoolPerCodeResultsEnabled: true,
                SchoolRetentionCutoffMonth: month,
                SchoolRetentionCutoffDay: day));

        public Task<PlatformFeatureSnapshot> UpdateAsync(
            PlatformFeatureUpdate update,
            CancellationToken cancellationToken = default)
            => GetAsync(cancellationToken);
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
