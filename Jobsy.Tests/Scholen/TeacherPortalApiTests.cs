using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Entities;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Tests.Scholen;

public class TeacherPortalApiTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private readonly RoleFunctionalWebAppFactory _factory;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public TeacherPortalApiTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Teacher_sees_only_assigned_classes_foreign_is_404()
    {
        await EnableSchoolsAsync(true, perCode: true);
        var (adminId, schoolId, class2B) = await SeedSchoolWithClassAsync("2B", codeCount: 8);
        var class3A = await SeedExtraClassAsync(schoolId, "3A", 5);
        var foreignClass = await SeedForeignClassAsync();
        var teacherId = await SeedTeacherAsync(schoolId, [class2B, class3A]);

        using var client = JobsyTestAuth.CreateAuthenticatedClient(_factory, teacherId);

        var mine = await client.GetFromJsonAsync<List<TeacherAssignedClassDto>>("api/teacher/classes", Json);
        Assert.NotNull(mine);
        Assert.Equal(2, mine!.Count);
        Assert.Contains(mine, c => c.ClassName == "2B");
        Assert.Contains(mine, c => c.ClassName == "3A");

        Assert.Equal(HttpStatusCode.OK,
            (await client.GetAsync($"api/teacher/classes/{class2B}/overview")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await client.GetAsync($"api/teacher/classes/{class3A}/codes")).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"api/teacher/classes/{foreignClass}/overview")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"api/teacher/classes/{foreignClass}/codes")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"api/teacher/classes/{foreignClass}/group")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"api/teacher/classes/{foreignClass}/dreamjobs")).StatusCode);

        // Unassigned class in same school
        var unassigned = await SeedExtraClassAsync(schoolId, "9Z", 3);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"api/teacher/classes/{unassigned}/overview")).StatusCode);

        _ = adminId;
    }

    [Fact]
    public async Task Code_of_other_class_returns_404_and_access_log_on_detail()
    {
        await EnableSchoolsAsync(true, perCode: true);
        var (_, schoolId, classA) = await SeedSchoolWithClassAsync("2B", codeCount: 6);
        var classB = await SeedExtraClassAsync(schoolId, "3A", 6);
        var teacherId = await SeedTeacherAsync(schoolId, [classA]);
        await SeedCompletedResultsAsync(classA, count: 1);
        await SeedCompletedResultsAsync(classB, count: 1);

        Guid codeA;
        Guid codeB;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            codeA = await db.PupilCodes.Where(c => c.SchoolClassId == classA && c.Status == PupilCodeStatus.Completed)
                .Select(c => c.Id).FirstAsync();
            codeB = await db.PupilCodes.Where(c => c.SchoolClassId == classB && c.Status == PupilCodeStatus.Completed)
                .Select(c => c.Id).FirstAsync();
        }

        using var client = JobsyTestAuth.CreateAuthenticatedClient(_factory, teacherId);
        Assert.Equal(HttpStatusCode.OK,
            (await client.GetAsync($"api/teacher/classes/{classA}/codes/{codeA}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"api/teacher/classes/{classA}/codes/{codeB}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync($"api/teacher/classes/{classB}/codes/{codeB}")).StatusCode);

        await using var scope2 = _factory.Services.CreateAsyncScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<JobsyDbContext>();
        Assert.True(await db2.PersonalDataAccessLogs.AnyAsync(l =>
            l.Resource == "school.pupil-code"
            && l.Action == "view"
            && l.SubjectPupilCodeId == codeA));
    }

    [Fact]
    public async Task SchoolAdmin_unassigned_404_assigned_ok_and_per_code_setting_ignored()
    {
        await EnableSchoolsAsync(true, perCode: false);
        var (adminId, schoolId, classId) = await SeedSchoolWithClassAsync("2B", codeCount: 6);
        await SeedCompletedResultsAsync(classId, count: 1);

        Guid codeId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            codeId = await db.PupilCodes.Where(c => c.SchoolClassId == classId && c.Status == PupilCodeStatus.Completed)
                .Select(c => c.Id).FirstAsync();
        }

        using var adminClient = JobsyTestAuth.CreateAuthenticatedClient(_factory, adminId);
        Assert.Equal(HttpStatusCode.NotFound,
            (await adminClient.GetAsync($"api/teacher/classes/{classId}/codes/{codeId}")).StatusCode);

        await AssignTeacherAsync(adminId, classId);
        Assert.Equal(HttpStatusCode.OK,
            (await adminClient.GetAsync($"api/teacher/classes/{classId}/codes/{codeId}")).StatusCode);

        // Setting off must not affect teacher/assigned SchoolAdmin detail
        var detail = await adminClient.GetFromJsonAsync<TeacherCodeDetailDto>(
            $"api/teacher/classes/{classId}/codes/{codeId}", Json);
        Assert.NotNull(detail);
        Assert.NotNull(detail!.Story);
        _ = schoolId;
    }

    [Fact]
    public async Task Group_k_anonymity_and_dream_jobs_overig()
    {
        await EnableSchoolsAsync(true, perCode: true);
        var (_, schoolId, classId) = await SeedSchoolWithClassAsync("2B", codeCount: 10);
        var teacherId = await SeedTeacherAsync(schoolId, [classId]);
        using var client = JobsyTestAuth.CreateAuthenticatedClient(_factory, teacherId);

        await SeedCompletedResultsAsync(classId, count: 4, dreamKeys: ["arts", "arts", "kok", "piloot"]);
        var hidden = await client.GetFromJsonAsync<TeacherGroupInsightsDto>(
            $"api/teacher/classes/{classId}/group", Json);
        Assert.False(hidden!.Visible);
        Assert.Empty(hidden.RiasecBars);

        await SeedCompletedResultsAsync(classId, count: 1, dreamKeys: ["arts"], skip: 4);
        var shown = await client.GetFromJsonAsync<TeacherGroupInsightsDto>(
            $"api/teacher/classes/{classId}/group", Json);
        Assert.True(shown!.Visible);
        Assert.NotEmpty(shown.RiasecBars);

        var dreams = await client.GetFromJsonAsync<TeacherDreamJobsDto>(
            $"api/teacher/classes/{classId}/dreamjobs", Json);
        Assert.True(dreams!.Visible);
        Assert.Contains(dreams.Jobs, d => d.Key == "arts" && d.Count >= 2);
        Assert.Contains(dreams.Jobs, d => d.Key == "Overig");
    }

    [Fact]
    public async Task Teacher_dto_never_contains_raw_answers()
    {
        var forbidden = new[]
        {
            "AnswersJson", "answers", "Answers", "rawAnswers", "Likert", "AnswerValues"
        };

        foreach (var type in new[]
                 {
                     typeof(TeacherClassOverviewDto),
                     typeof(TeacherCodeRowDto),
                     typeof(TeacherGroupInsightsDto),
                     typeof(TeacherDreamJobsDto),
                     typeof(TeacherCodeDetailDto),
                     typeof(PupilStoryViewDto),
                     typeof(DreamJobRouteStubDto),
                 })
        {
            var names = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(p => p.Name)
                .ToList();
            Assert.DoesNotContain(names, n => forbidden.Contains(n, StringComparer.OrdinalIgnoreCase));
            Assert.DoesNotContain(names, n => n.Contains("Answer", StringComparison.OrdinalIgnoreCase)
                                              && !n.Contains("Conversation", StringComparison.OrdinalIgnoreCase));
        }

        // Runtime JSON sample
        await EnableSchoolsAsync(true, perCode: true);
        var (_, schoolId, classId) = await SeedSchoolWithClassAsync("2B", codeCount: 6);
        var teacherId = await SeedTeacherAsync(schoolId, [classId]);
        await SeedCompletedResultsAsync(classId, count: 1);
        Guid codeId;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            codeId = await db.PupilCodes.Where(c => c.SchoolClassId == classId && c.Status == PupilCodeStatus.Completed)
                .Select(c => c.Id).FirstAsync();
            var progress = await db.PupilProgresses.FirstOrDefaultAsync(p => p.PupilCodeId == codeId);
            if (progress is null)
            {
                db.PupilProgresses.Add(new PupilProgress
                {
                    PupilCodeId = codeId,
                    AnswersJson = """{"q1":5,"q2":1}""",
                    CurrentIndex = 60,
                    LikesJson = """["tekenen"]""",
                    DislikesJson = "[]",
                    StartedAtUtc = DateTime.UtcNow.AddMinutes(-30),
                    UpdatedAtUtc = DateTime.UtcNow,
                    CompletedAtUtc = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }
        }

        using var client = JobsyTestAuth.CreateAuthenticatedClient(_factory, teacherId);
        var json = await (await client.GetAsync($"api/teacher/classes/{classId}/codes/{codeId}")).Content.ReadAsStringAsync();
        Assert.DoesNotContain("AnswersJson", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("answersJson", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"q1\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Parental_info_missing_blocks_teacher_open_window()
    {
        await EnableSchoolsAsync(true, perCode: true);
        var (_, schoolId, classId) = await SeedSchoolWithClassAsync("2B", codeCount: 5);
        var teacherId = await SeedTeacherAsync(schoolId, [classId]);
        using var client = JobsyTestAuth.CreateAuthenticatedClient(_factory, teacherId);

        var open = await client.PostAsJsonAsync(
            $"api/teacher/classes/{classId}/test-window",
            new TestWindowRequest("open", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))));
        Assert.Equal(HttpStatusCode.Conflict, open.StatusCode);
        Assert.Contains("parental_info_missing", await open.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    private async Task EnableSchoolsAsync(bool enabled, bool perCode)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var row = await db.PlatformFeatureSettings.FirstOrDefaultAsync();
        if (row is null)
        {
            row = new PlatformFeatureSettings
            {
                Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")
            };
            db.PlatformFeatureSettings.Add(row);
        }

        row.SchoolsEnabled = enabled;
        row.SchoolPerCodeResultsEnabled = perCode;
        await db.SaveChangesAsync();
    }

    private async Task<(Guid AdminId, Guid SchoolId, Guid ClassId)> SeedSchoolWithClassAsync(
        string className,
        int codeCount)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var schoolId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var domain = $"t{schoolId:N}.nl"[..20] + ".nl";
        db.Schools.Add(new School
        {
            Id = schoolId,
            Name = "Teacher College",
            City = "Naaldwijk",
            AllowedEmailDomains = JsonSerializer.Serialize(new[] { domain }),
            IsActive = true,
            ProcessorAgreementSignedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            ProcessorAgreementVersion = "1.0",
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = _factory.AdminId
        });
        db.Users.Add(new User
        {
            Id = adminId,
            Email = $"sa-{adminId:N}@{domain}",
            FullName = "J. Visser",
            Role = UserRole.SchoolAdmin,
            SchoolId = schoolId,
            IsActive = true,
            AuthenticatorEnabled = true
        });
        var classId = Guid.NewGuid();
        var schoolClass = new SchoolClass
        {
            Id = classId,
            SchoolId = schoolId,
            Name = className,
            Level = SchoolLevel.Havo,
            Year = 2,
            SchoolYearStart = SchoolYear.Current(DateOnly.FromDateTime(DateTime.UtcNow)),
            PupilCount = codeCount,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.SchoolClasses.Add(schoolClass);
        await db.SaveChangesAsync();
        var codes = scope.ServiceProvider.GetRequiredService<Infrastructure.Scholen.IPupilCodeService>();
        await codes.GenerateAsync(codeCount, schoolClass);
        return (adminId, schoolId, classId);
    }

    private async Task<Guid> SeedExtraClassAsync(Guid schoolId, string name, int codeCount)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var classId = Guid.NewGuid();
        var schoolClass = new SchoolClass
        {
            Id = classId,
            SchoolId = schoolId,
            Name = name,
            Level = SchoolLevel.Mavo,
            Year = 3,
            SchoolYearStart = SchoolYear.Current(DateOnly.FromDateTime(DateTime.UtcNow)),
            PupilCount = codeCount,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.SchoolClasses.Add(schoolClass);
        await db.SaveChangesAsync();
        var codes = scope.ServiceProvider.GetRequiredService<Infrastructure.Scholen.IPupilCodeService>();
        await codes.GenerateAsync(codeCount, schoolClass);
        return classId;
    }

    private async Task<Guid> SeedForeignClassAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var schoolId = Guid.NewGuid();
        db.Schools.Add(new School
        {
            Id = schoolId,
            Name = "Other",
            City = "X",
            AllowedEmailDomains = "[]",
            IsActive = true,
            ProcessorAgreementSignedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            ProcessorAgreementVersion = "1.0",
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = _factory.AdminId
        });
        var classId = Guid.NewGuid();
        db.SchoolClasses.Add(new SchoolClass
        {
            Id = classId,
            SchoolId = schoolId,
            Name = "FX",
            Level = SchoolLevel.Havo,
            Year = 1,
            SchoolYearStart = SchoolYear.Current(DateOnly.FromDateTime(DateTime.UtcNow)),
            PupilCount = 2,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return classId;
    }

    private async Task<Guid> SeedTeacherAsync(Guid schoolId, IReadOnlyList<Guid> classIds)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var id = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = id,
            Email = $"t-{id:N}@school.nl",
            FullName = "R. Jansen",
            Role = UserRole.Teacher,
            SchoolId = schoolId,
            IsActive = true,
            AuthenticatorEnabled = true
        });
        foreach (var classId in classIds)
        {
            db.TeacherClassAssignments.Add(new TeacherClassAssignment
            {
                TeacherUserId = id,
                SchoolClassId = classId,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
        return id;
    }

    private async Task AssignTeacherAsync(Guid userId, Guid classId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        db.TeacherClassAssignments.Add(new TeacherClassAssignment
        {
            TeacherUserId = userId,
            SchoolClassId = classId,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private async Task SeedCompletedResultsAsync(
        Guid classId,
        int count,
        IReadOnlyList<string>? dreamKeys = null,
        int skip = 0)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var codes = await db.PupilCodes.Where(c => c.SchoolClassId == classId)
            .OrderBy(c => c.Number)
            .Skip(skip)
            .Take(count)
            .ToListAsync();
        for (var i = 0; i < codes.Count; i++)
        {
            var c = codes[i];
            if (await db.PupilResults.AnyAsync(r => r.PupilCodeId == c.Id))
            {
                continue;
            }

            c.Status = PupilCodeStatus.Completed;
            var dream = dreamKeys is not null && i < dreamKeys.Count ? dreamKeys[i] : "arts";
            db.PupilResults.Add(new PupilResult
            {
                PupilCodeId = c.Id,
                SchoolClassId = classId,
                CompletedAtUtc = DateTime.UtcNow,
                HollandCode = "SAE",
                TopValue = "Helpen",
                TopCulture = "Klein team",
                DreamJobKey = dream,
                CompetenceScoresJson = """{"samenwerken":3.2,"communiceren":2.8}""",
                RiasecScoresJson = """{"S":3,"A":2,"E":1}""",
                ValuesScoresJson = "{}",
                CultureScoresJson = "{}",
                ScoringVersion = "t",
                StoryTemplateVersion = "t",
                StoryKeysJson = "[]"
            });
            db.PupilProgresses.Add(new PupilProgress
            {
                PupilCodeId = c.Id,
                AnswersJson = "{}",
                CurrentIndex = 60,
                LikesJson = """["tekenen"]""",
                DislikesJson = """["stilzitten"]""",
                LikeOtherWord = "fietsen",
                StartedAtUtc = DateTime.UtcNow.AddMinutes(-40),
                UpdatedAtUtc = DateTime.UtcNow,
                CompletedAtUtc = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
    }
}
