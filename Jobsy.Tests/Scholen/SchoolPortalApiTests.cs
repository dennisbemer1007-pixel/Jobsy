using System.Net;
using System.Net.Http.Json;
using System.Text;
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

public class SchoolPortalApiTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private readonly RoleFunctionalWebAppFactory _factory;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public SchoolPortalApiTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Walkthrough_create_class_codes_parents_window_and_codelist()
    {
        await EnableSchoolsAsync(true, perCode: true);
        var (schoolAdminId, schoolId, _) = await SeedSchoolStaffAsync();

        using var client = JobsyTestAuth.CreateAuthenticatedClient(_factory, schoolAdminId);

        var create = await client.PostAsJsonAsync("api/school/classes", new CreateSchoolClassRequest(
            "2B", SchoolLevel.Havo, 2, 28, null, null));
        if (create.StatusCode != HttpStatusCode.Created)
        {
            var err = await create.Content.ReadAsStringAsync();
            Assert.Fail($"{create.StatusCode}: {err}");
        }
        var detail = await create.Content.ReadFromJsonAsync<SchoolPortalClassDetailDto>(Json);
        Assert.NotNull(detail);
        Assert.Equal(28, detail!.Codes.Count);
        Assert.All(detail.Codes, c => Assert.Matches(@"^[A-Z0-9]{3}-[A-Z0-9]{3}$", c.DisplayCode));

        var csv = await client.GetAsync($"api/school/classes/{detail.Id}/codelist.csv");
        Assert.Equal(HttpStatusCode.OK, csv.StatusCode);
        Assert.Equal("no-store", csv.Headers.CacheControl?.ToString());
        var csvBytes = await csv.Content.ReadAsByteArrayAsync();
        var csvText = Encoding.UTF8.GetString(csvBytes).TrimStart('\uFEFF');
        var lines = csvText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Equal(SchoolCodeListCsv.Header, lines[0]);
        Assert.Equal(29, lines.Length);
        Assert.All(lines.Skip(1), line =>
        {
            var cols = line.Split(';');
            Assert.Equal(3, cols.Length);
            Assert.True(string.IsNullOrEmpty(cols[2]));
        });

        var pdf = await client.GetAsync($"api/school/classes/{detail.Id}/codelist.pdf");
        Assert.Equal(HttpStatusCode.OK, pdf.StatusCode);
        var pdfBytes = await pdf.Content.ReadAsByteArrayAsync();
        Assert.True(pdfBytes.Length > 200);

        // Window blocked without parental confirmation
        var openMissing = await client.PostAsJsonAsync(
            $"api/school/classes/{detail.Id}/test-window",
            new TestWindowRequest("open", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14))));
        Assert.Equal(HttpStatusCode.Conflict, openMissing.StatusCode);
        var openBody = await openMissing.Content.ReadAsStringAsync();
        Assert.Contains("parental_info_missing", openBody, StringComparison.Ordinal);

        var confirm = await client.PostAsJsonAsync(
            $"api/school/classes/{detail.Id}/parental-confirmation",
            new ParentalConfirmationRequest(true));
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);

        var open = await client.PostAsJsonAsync(
            $"api/school/classes/{detail.Id}/test-window",
            new TestWindowRequest("open", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14))));
        Assert.Equal(HttpStatusCode.OK, open.StatusCode);
        var opened = await open.Content.ReadFromJsonAsync<SchoolPortalClassDetailDto>(Json);
        Assert.Equal(TestWindowState.Open, opened!.TestWindow);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        Assert.True(await db.PersonalDataAccessLogs.AnyAsync(l =>
            l.Resource == "school.codelist" && (l.Action == "csv" || l.Action == "pdf")));
        _ = schoolId;
    }

    [Fact]
    public async Task Foreign_school_class_returns_404_and_teacher_forbidden_on_admin_actions()
    {
        await EnableSchoolsAsync(true, perCode: true);
        var (adminA, schoolA, _) = await SeedSchoolStaffAsync("A College", "a.nl");
        var (adminB, schoolB, classB) = await SeedSchoolStaffAsync("B College", "b.nl", withClass: true);
        var teacherA = await SeedTeacherAsync(schoolA);

        using var clientA = JobsyTestAuth.CreateAuthenticatedClient(_factory, adminA);
        var foreign = await clientA.GetAsync($"api/school/classes/{classB}");
        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);

        using var teacherClient = JobsyTestAuth.CreateAuthenticatedClient(_factory, teacherA);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await teacherClient.PostAsJsonAsync("api/school/classes", new CreateSchoolClassRequest(
                "9Z", SchoolLevel.Havo, 1, 5, null, null))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await teacherClient.PostAsJsonAsync("api/school/teachers", new InviteTeacherRequest(
                "X", "x@a.nl", []))).StatusCode);

        using var candidate = JobsyTestAuth.CreateAuthenticatedClient(_factory, _factory.CandidateId);
        var candidateStatus = (await candidate.GetAsync("api/school/classes")).StatusCode;
        Assert.True(
            candidateStatus is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized,
            $"Expected 403/401, got {candidateStatus}");

        using var employer = JobsyTestAuth.CreateAuthenticatedClient(_factory, _factory.EmployerId);
        var employerStatus = (await employer.GetAsync("api/school/classes")).StatusCode;
        Assert.True(
            employerStatus is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized,
            $"Expected 403/401, got {employerStatus}");
        _ = adminB;
        _ = schoolB;
    }

    [Fact]
    public async Task Results_per_code_gated_by_setting_server_side()
    {
        await EnableSchoolsAsync(true, perCode: true);
        var (adminId, schoolId, classId) = await SeedSchoolStaffAsync(withClass: true, codeCount: 6);
        await SeedCompletedResultsAsync(classId, count: 5);

        using var client = JobsyTestAuth.CreateAuthenticatedClient(_factory, adminId);
        var on = await client.GetFromJsonAsync<SchoolPortalResultsDto>(
            $"api/school/classes/{classId}/results", Json);
        Assert.NotNull(on);
        Assert.True(on!.PerCodeEnabled);
        Assert.NotNull(on.PerCode);
        Assert.True(on.Totals.TotalsVisible);

        await EnableSchoolsAsync(true, perCode: false);
        var off = await client.GetFromJsonAsync<SchoolPortalResultsDto>(
            $"api/school/classes/{classId}/results", Json);
        Assert.NotNull(off);
        Assert.False(off!.PerCodeEnabled);
        Assert.Null(off.PerCode);
        _ = schoolId;
    }

    [Fact]
    public async Task Add_codes_capped_at_40_and_delete_code_removes_result()
    {
        await EnableSchoolsAsync(true, perCode: true);
        var (adminId, _, classId) = await SeedSchoolStaffAsync(withClass: true, codeCount: 39);
        using var client = JobsyTestAuth.CreateAuthenticatedClient(_factory, adminId);

        var ok = await client.PostAsJsonAsync($"api/school/classes/{classId}/codes", new AddCodesRequest(1));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        var over = await client.PostAsJsonAsync($"api/school/classes/{classId}/codes", new AddCodesRequest(1));
        Assert.Equal(HttpStatusCode.BadRequest, over.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var code = await db.PupilCodes.FirstAsync(c => c.SchoolClassId == classId);
        db.PupilResults.Add(new PupilResult
        {
            PupilCodeId = code.Id,
            SchoolClassId = classId,
            CompletedAtUtc = DateTime.UtcNow,
            CompetenceScoresJson = "{}",
            RiasecScoresJson = "{}",
            ValuesScoresJson = "{}",
            CultureScoresJson = "{}",
            ScoringVersion = "t",
            StoryTemplateVersion = "t",
            StoryKeysJson = "[]"
        });
        await db.SaveChangesAsync();

        var del = await client.DeleteAsync($"api/school/classes/{classId}/codes/{code.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
        Assert.False(await db.PupilResults.AnyAsync(r => r.PupilCodeId == code.Id));
        Assert.False(await db.PupilCodes.AnyAsync(c => c.Id == code.Id));
    }

    [Fact]
    public async Task Teacher_invite_domain_check_and_no_role_field()
    {
        await EnableSchoolsAsync(true, perCode: true);
        var (adminId, schoolId, classId) = await SeedSchoolStaffAsync(withClass: true, codeCount: 5);
        using var client = JobsyTestAuth.CreateAuthenticatedClient(_factory, adminId);

        var bad = await client.PostAsJsonAsync("api/school/teachers", new InviteTeacherRequest(
            "T. Test", "t@other.nl", [classId]));
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        Assert.Contains("email_domain_not_allowed", await bad.Content.ReadAsStringAsync(), StringComparison.Ordinal);

        var school = await GetSchoolDomainAsync(schoolId);
        var ok = await client.PostAsJsonAsync("api/school/teachers", new InviteTeacherRequest(
            "T. Test", $"t@{school}", [classId]));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var dto = await ok.Content.ReadFromJsonAsync<SchoolStaffInviteResultDto>(Json);
        Assert.Equal("Teacher", dto!.Role);
    }

    [Fact]
    public async Task Create_groep78_year_8_sets_question_set()
    {
        await EnableSchoolsAsync(true, perCode: true);
        var (adminId, _, _) = await SeedSchoolStaffAsync();
        using var client = JobsyTestAuth.CreateAuthenticatedClient(_factory, adminId);

        var create = await client.PostAsJsonAsync("api/school/classes", new CreateSchoolClassRequest(
            "8A", SchoolLevel.Groep78, 8, 5, null, null));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var detail = await create.Content.ReadFromJsonAsync<SchoolPortalClassDetailDto>(Json);
        Assert.NotNull(detail);
        Assert.Equal(PupilQuestionSet.Groep78, detail!.QuestionSet);
        Assert.Equal(SchoolLevel.Groep78, detail.Level);
        Assert.Equal(8, detail.Year);
        Assert.False(detail.LevelLocked);
    }

    [Fact]
    public async Task Create_groep78_year_3_returns_400()
    {
        await EnableSchoolsAsync(true, perCode: true);
        var (adminId, _, _) = await SeedSchoolStaffAsync();
        using var client = JobsyTestAuth.CreateAuthenticatedClient(_factory, adminId);

        var create = await client.PostAsJsonAsync("api/school/classes", new CreateSchoolClassRequest(
            "X3", SchoolLevel.Groep78, 3, 5, null, null));
        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
        var body = await create.Content.ReadAsStringAsync();
        Assert.Contains("Kies groep 7 of groep 8", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Create_havo_sets_vo_question_set()
    {
        await EnableSchoolsAsync(true, perCode: true);
        var (adminId, _, _) = await SeedSchoolStaffAsync();
        using var client = JobsyTestAuth.CreateAuthenticatedClient(_factory, adminId);

        var create = await client.PostAsJsonAsync("api/school/classes", new CreateSchoolClassRequest(
            "2H", SchoolLevel.Havo, 2, 5, null, null));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var detail = await create.Content.ReadFromJsonAsync<SchoolPortalClassDetailDto>(Json);
        Assert.Equal(PupilQuestionSet.Vo, detail!.QuestionSet);
    }

    [Fact]
    public async Task Update_havo_to_vwo_with_started_codes_succeeds()
    {
        await EnableSchoolsAsync(true, perCode: true);
        var (adminId, _, classId) = await SeedSchoolStaffAsync(withClass: true, codeCount: 3);
        await MarkFirstCodeInProgressAsync(classId);
        using var client = JobsyTestAuth.CreateAuthenticatedClient(_factory, adminId);

        var update = await client.PutAsJsonAsync($"api/school/classes/{classId}", new UpdateSchoolClassRequest(
            "2B", SchoolLevel.Vwo, 2, null));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var detail = await update.Content.ReadFromJsonAsync<SchoolPortalClassDetailDto>(Json);
        Assert.Equal(SchoolLevel.Vwo, detail!.Level);
        Assert.Equal(PupilQuestionSet.Vo, detail.QuestionSet);
        Assert.True(detail.LevelLocked);
    }

    [Fact]
    public async Task Update_havo_to_groep78_with_started_code_returns_409_level_locked()
    {
        await EnableSchoolsAsync(true, perCode: true);
        var (adminId, _, classId) = await SeedSchoolStaffAsync(withClass: true, codeCount: 3);
        await MarkFirstCodeInProgressAsync(classId);
        using var client = JobsyTestAuth.CreateAuthenticatedClient(_factory, adminId);

        var update = await client.PutAsJsonAsync($"api/school/classes/{classId}", new UpdateSchoolClassRequest(
            "2B", SchoolLevel.Groep78, 7, null));
        Assert.Equal(HttpStatusCode.Conflict, update.StatusCode);
        var body = await update.Content.ReadAsStringAsync();
        Assert.Contains("level_locked", body, StringComparison.Ordinal);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var entity = await db.SchoolClasses.AsNoTracking().SingleAsync(c => c.Id == classId);
        Assert.Equal(SchoolLevel.Havo, entity.Level);
        Assert.Equal(PupilQuestionSet.Vo, entity.QuestionSet);
    }

    [Fact]
    public async Task Update_havo_to_groep78_with_only_not_started_codes_succeeds()
    {
        await EnableSchoolsAsync(true, perCode: true);
        var (adminId, _, classId) = await SeedSchoolStaffAsync(withClass: true, codeCount: 3);
        using var client = JobsyTestAuth.CreateAuthenticatedClient(_factory, adminId);

        var update = await client.PutAsJsonAsync($"api/school/classes/{classId}", new UpdateSchoolClassRequest(
            "7B", SchoolLevel.Groep78, 7, null));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var detail = await update.Content.ReadFromJsonAsync<SchoolPortalClassDetailDto>(Json);
        Assert.Equal(SchoolLevel.Groep78, detail!.Level);
        Assert.Equal(PupilQuestionSet.Groep78, detail.QuestionSet);
        Assert.False(detail.LevelLocked);
    }

    [Fact]
    public async Task Update_groep78_year_7_to_8_with_started_codes_succeeds()
    {
        await EnableSchoolsAsync(true, perCode: true);
        var (adminId, _, _) = await SeedSchoolStaffAsync();
        using var client = JobsyTestAuth.CreateAuthenticatedClient(_factory, adminId);
        var create = await client.PostAsJsonAsync("api/school/classes", new CreateSchoolClassRequest(
            "7C", SchoolLevel.Groep78, 7, 3, null, null));
        var detail = await create.Content.ReadFromJsonAsync<SchoolPortalClassDetailDto>(Json);
        Assert.NotNull(detail);
        await MarkFirstCodeInProgressAsync(detail!.Id);

        var update = await client.PutAsJsonAsync($"api/school/classes/{detail.Id}", new UpdateSchoolClassRequest(
            "7C", SchoolLevel.Groep78, 8, null));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await update.Content.ReadFromJsonAsync<SchoolPortalClassDetailDto>(Json);
        Assert.Equal(8, updated!.Year);
        Assert.Equal(PupilQuestionSet.Groep78, updated.QuestionSet);
    }

    [Fact]
    public async Task Teacher_cannot_create_or_update_class()
    {
        await EnableSchoolsAsync(true, perCode: true);
        var (_, schoolId, classId) = await SeedSchoolStaffAsync(withClass: true, codeCount: 2);
        var teacherId = await SeedTeacherAsync(schoolId);
        using var client = JobsyTestAuth.CreateAuthenticatedClient(_factory, teacherId);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PostAsJsonAsync("api/school/classes", new CreateSchoolClassRequest(
                "9Z", SchoolLevel.Havo, 1, 5, null, null))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PutAsJsonAsync($"api/school/classes/{classId}", new UpdateSchoolClassRequest(
                "2B", SchoolLevel.Vwo, 2, null))).StatusCode);
    }

    [Fact]
    public async Task Test_window_auto_close_on_read()
    {
        await EnableSchoolsAsync(true, perCode: true);
        var (adminId, _, classId) = await SeedSchoolStaffAsync(withClass: true, codeCount: 5);
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            var c = await db.SchoolClasses.FirstAsync(x => x.Id == classId);
            c.ParentalInfoConfirmedAtUtc = DateTime.UtcNow;
            c.ParentalInfoTextVersion = ParentalInfoTexts.CurrentVersion;
            c.TestWindow = TestWindowState.Open;
            c.TestWindowClosesOn = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2));
            var school = await db.Schools.FirstAsync(s => s.Id == c.SchoolId);
            school.ProcessorAgreementSignedOn = DateOnly.FromDateTime(DateTime.UtcNow);
            school.ProcessorAgreementVersion = "1.0";
            await db.SaveChangesAsync();
        }

        using var client = JobsyTestAuth.CreateAuthenticatedClient(_factory, adminId);
        var detail = await client.GetFromJsonAsync<SchoolPortalClassDetailDto>(
            $"api/school/classes/{classId}", Json);
        Assert.Equal(TestWindowState.Closed, detail!.TestWindow);
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

    private async Task<(Guid AdminId, Guid SchoolId, Guid ClassId)> SeedSchoolStaffAsync(
        string name = "Voorbeeld College",
        string domain = "voorbeeldcollege.nl",
        bool withClass = false,
        int codeCount = 28)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var schoolId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        db.Schools.Add(new School
        {
            Id = schoolId,
            Name = name,
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

        var classId = Guid.Empty;
        if (withClass)
        {
            classId = Guid.NewGuid();
            var schoolClass = new SchoolClass
            {
                Id = classId,
                SchoolId = schoolId,
                Name = "2B",
                Level = SchoolLevel.Havo,
                QuestionSet = PupilQuestionSet.Vo,
                Year = 2,
                SchoolYearStart = SchoolYear.Current(DateOnly.FromDateTime(DateTime.UtcNow)),
                PupilCount = codeCount,
                CreatedAtUtc = DateTime.UtcNow
            };
            db.SchoolClasses.Add(schoolClass);
            await db.SaveChangesAsync();

            var codes = scope.ServiceProvider.GetRequiredService<Infrastructure.Scholen.IPupilCodeService>();
            await codes.GenerateAsync(codeCount, schoolClass);
        }
        else
        {
            await db.SaveChangesAsync();
        }

        return (adminId, schoolId, classId);
    }

    private async Task<Guid> SeedTeacherAsync(Guid schoolId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var id = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = id,
            Email = $"t-{id:N}@school.nl",
            FullName = "L. Bakker",
            Role = UserRole.Teacher,
            SchoolId = schoolId,
            IsActive = true
        });
        await db.SaveChangesAsync();
        return id;
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
                RiasecScoresJson = """{"S":3,"A":2,"E":1}""",
                ValuesScoresJson = "{}",
                CultureScoresJson = "{}",
                ScoringVersion = "t",
                StoryTemplateVersion = "t",
                StoryKeysJson = "[]"
            });
        }

        await db.SaveChangesAsync();
    }

    private async Task<string> GetSchoolDomainAsync(Guid schoolId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var school = await db.Schools.AsNoTracking().FirstAsync(s => s.Id == schoolId);
        return JsonSerializer.Deserialize<List<string>>(school.AllowedEmailDomains)!.First();
    }

    private async Task MarkFirstCodeInProgressAsync(Guid classId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var code = await db.PupilCodes.FirstAsync(c => c.SchoolClassId == classId);
        code.Status = PupilCodeStatus.InProgress;
        await db.SaveChangesAsync();
    }
}
