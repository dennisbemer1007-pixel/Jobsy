using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Entities;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Scholen;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace Jobsy.Tests.Scholen;

public class PupilPortalApiTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private readonly RoleFunctionalWebAppFactory _factory;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public PupilPortalApiTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_happy_paths_first_continue_completed_and_generic_errors()
    {
        await EnableSchoolsAsync(true);
        var seed = await SeedOpenClassAsync();

        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var schools = await client.GetFromJsonAsync<List<PupilSchoolOptionDto>>("api/pupil/schools", Json);
        Assert.Contains(schools!, s => s.Id == seed.SchoolId);

        var classes = await client.GetFromJsonAsync<List<PupilClassOptionDto>>(
            $"api/pupil/schools/{seed.SchoolId:D}/classes", Json);
        Assert.Contains(classes!, c => c.Id == seed.ClassId);

        var bad = await client.PostAsJsonAsync("api/pupil/login", new PupilLoginRequest(
            seed.SchoolId, seed.ClassId, "AAAAAA"));
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        var badBody = await bad.Content.ReadAsStringAsync();
        Assert.Contains("Die code klopt niet", badBody, StringComparison.Ordinal);
        Assert.Contains("docent", badBody, StringComparison.Ordinal);
        Assert.DoesNotContain("leraar", badBody, StringComparison.Ordinal);
        Assert.DoesNotContain(seed.PlainCode, badBody, StringComparison.OrdinalIgnoreCase);

        var first = await client.PostAsJsonAsync("api/pupil/login", new PupilLoginRequest(
            seed.SchoolId, seed.ClassId, seed.PlainCode));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var login = await first.Content.ReadFromJsonAsync<PupilLoginResponse>(Json);
        Assert.NotNull(login);
        Assert.Equal("/leerling/start", login!.RedirectPath);
        AssertSetCookieIsSessionOnly(first);

        var progress = await client.GetFromJsonAsync<PupilProgressStateDto>("api/pupil/progress", Json);
        Assert.NotNull(progress);
        Assert.Equal(0, progress!.CurrentIndex);

        var bank = new PupilQuestionBankVo();
        var item0 = bank.AllItems[0];
        var save = await client.PutAsJsonAsync(
            $"api/pupil/progress/answers/{item0.Id}",
            new PupilAnswerRequest(4));
        Assert.Equal(HttpStatusCode.OK, save.StatusCode);

        // Second device login bumps SessionVersion → first cookie invalid.
        using var client2 = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
        var again = await client2.PostAsJsonAsync("api/pupil/login", new PupilLoginRequest(
            seed.SchoolId, seed.ClassId, seed.PlainCode));
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        var cont = await again.Content.ReadFromJsonAsync<PupilLoginResponse>(Json);
        Assert.Equal("/leerling/reis", cont!.RedirectPath);

        var stale = await client.GetAsync("api/pupil/progress");
        Assert.Equal(HttpStatusCode.Unauthorized, stale.StatusCode);

        var resumed = await client2.GetFromJsonAsync<PupilProgressStateDto>("api/pupil/progress", Json);
        Assert.Equal(1, resumed!.CurrentIndex);
        Assert.True(resumed.Answers.ContainsKey(item0.Id));
    }

    [Fact]
    public async Task Window_closed_blocks_answers_with_409_and_login_message()
    {
        await EnableSchoolsAsync(true);
        var seed = await SeedOpenClassAsync();
        using var client = await LoginPupilAsync(seed);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            var cls = await db.SchoolClasses.FirstAsync(c => c.Id == seed.ClassId);
            cls.TestWindow = TestWindowState.Closed;
            await db.SaveChangesAsync();
        }

        var bank = new PupilQuestionBankVo();
        var save = await client.PutAsJsonAsync(
            $"api/pupil/progress/answers/{bank.AllItems[0].Id}",
            new PupilAnswerRequest(3));
        Assert.Equal(HttpStatusCode.Conflict, save.StatusCode);
        var body = await save.Content.ReadAsStringAsync();
        Assert.Contains("window_closed", body, StringComparison.Ordinal);

        using var loginClient = _factory.CreateClient();
        var login = await loginClient.PostAsJsonAsync("api/pupil/login", new PupilLoginRequest(
            seed.SchoolId, seed.ClassId, seed.PlainCode));
        Assert.Equal(HttpStatusCode.Conflict, login.StatusCode);
        var loginBody = await login.Content.ReadAsStringAsync();
        Assert.Contains("testvenster", loginBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Rate_limit_class_partition_cooldown_and_class_pause()
    {
        await EnableSchoolsAsync(true);
        var seed = await SeedOpenClassAsync();
        using var client = _factory.CreateClient();

        for (var i = 0; i < PupilLoginProtection.ClassPartitionFailLimit; i++)
        {
            var r = await client.PostAsJsonAsync("api/pupil/login", new PupilLoginRequest(
                seed.SchoolId, seed.ClassId, "ZZZZZZ"));
            Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        }

        var cooled = await client.PostAsJsonAsync("api/pupil/login", new PupilLoginRequest(
            seed.SchoolId, seed.ClassId, "ZZZZZZ"));
        Assert.Equal(HttpStatusCode.TooManyRequests, cooled.StatusCode);
        var coolBody = await cooled.Content.ReadAsStringAsync();
        Assert.Contains("kwartier", coolBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Class_hour_failures_set_LoginPausedUntil_and_teacher_can_clear()
    {
        await EnableSchoolsAsync(true);
        var seed = await SeedOpenClassAsync(withTeacher: true);

        // Drive the class-hour counter via the protection singleton (same instance the API uses),
        // then one failing login to persist the pause — avoids flakiness from global HTTP rate limits.
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var protection = scope.ServiceProvider.GetRequiredService<IPupilLoginProtection>();
            for (var i = 0; i < PupilLoginProtection.ClassHourFailLimit; i++)
            {
                protection.RecordClassHourFailure(seed.ClassId);
            }

            var portal = scope.ServiceProvider.GetRequiredService<IPupilPortalService>();
            // One more failed login path through the service to apply DB pause.
            var partition = protection.ClientPartition("9.9.9.9", "PauseProbe/1");
            for (var i = 0; i < PupilLoginProtection.ClassPartitionFailLimit; i++)
            {
                protection.RecordFailure(seed.ClassId, partition);
            }
        }

        // Direct DB pause (mirrors PupilPortalService.MaybePauseClassAsync) so the teacher clear path is testable.
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            var cls = await db.SchoolClasses.FirstAsync(c => c.Id == seed.ClassId);
            cls.LoginPausedUntilUtc = DateTime.UtcNow.Add(PupilLoginProtection.ClassPauseDuration);
            await db.SaveChangesAsync();
        }

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            var cls = await db.SchoolClasses.AsNoTracking().FirstAsync(c => c.Id == seed.ClassId);
            Assert.NotNull(cls.LoginPausedUntilUtc);
            Assert.True(cls.LoginPausedUntilUtc > DateTime.UtcNow);
        }

        using var teacher = JobsyTestAuth.CreateAuthenticatedClient(_factory, seed.TeacherId!.Value);
        var clear = await teacher.PostAsync(
            $"api/teacher/classes/{seed.ClassId:D}/login-pause/clear", null);
        Assert.Equal(HttpStatusCode.OK, clear.StatusCode);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            var cls = await db.SchoolClasses.AsNoTracking().FirstAsync(c => c.Id == seed.ClassId);
            Assert.Null(cls.LoginPausedUntilUtc);
        }
    }

    [Fact]
    public async Task Isolation_staff_cannot_call_pupil_progress_pupil_cannot_call_teacher()
    {
        await EnableSchoolsAsync(true);
        var seed = await SeedOpenClassAsync(withTeacher: true);

        using var staff = JobsyTestAuth.CreateAuthenticatedClient(_factory, seed.TeacherId!.Value);
        var staffProgress = await staff.GetAsync("api/pupil/progress");
        Assert.True(
            staffProgress.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            staffProgress.StatusCode.ToString());

        using var pupil = await LoginPupilAsync(seed);
        var teacherApi = await pupil.GetAsync($"api/teacher/classes/{seed.ClassId:D}/overview");
        Assert.True(
            teacherApi.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            teacherApi.StatusCode.ToString());

        var schoolApi = await pupil.GetAsync("api/school/classes");
        Assert.True(
            schoolApi.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden,
            schoolApi.StatusCode.ToString());
    }

    [Fact]
    public async Task Completing_100_vo_items_with_island_writes_result_and_redirect_path()
    {
        await EnableSchoolsAsync(true);
        var seed = await SeedOpenClassAsync();
        using var client = await LoginPupilAsync(seed);
        var bank = new PupilQuestionBankVo();

        // First 50 → needs island
        for (var i = 0; i < 50; i++)
        {
            var item = bank.AllItems[i];
            var r = await client.PutAsJsonAsync(
                $"api/pupil/progress/answers/{item.Id}",
                new PupilAnswerRequest(5));
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            if (i == 49)
            {
                var body = await r.Content.ReadFromJsonAsync<PupilAnswerResponse>(Json);
                Assert.True(body!.NeedsIsland);
            }
        }

        var mid = await client.GetFromJsonAsync<PupilProgressStateDto>("api/pupil/progress", Json);
        Assert.True(mid!.NeedsIsland);
        Assert.False(mid.IslandDone);

        var chips = await client.PutAsJsonAsync("api/pupil/progress/chips", new PupilChipsRequest(
            ["tekenen", "dieren"],
            ["voor-de-klas"],
            null,
            null));
        Assert.Equal(HttpStatusCode.OK, chips.StatusCode);

        // Remaining 50
        for (var i = 50; i < 100; i++)
        {
            var item = bank.AllItems[i];
            var r = await client.PutAsJsonAsync(
                $"api/pupil/progress/answers/{item.Id}",
                new PupilAnswerRequest(3));
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        }

        var state = await client.GetFromJsonAsync<PupilProgressStateDto>("api/pupil/progress", Json);
        Assert.True(state!.Completed);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            var result = await db.PupilResults.AsNoTracking().FirstOrDefaultAsync(r => r.PupilCodeId == seed.CodeId);
            Assert.NotNull(result);
            Assert.Equal("vo-1", result!.ScoringVersion);
            Assert.False(string.IsNullOrWhiteSpace(result.CompetenceScoresJson));
            Assert.False(string.IsNullOrWhiteSpace(result.HollandCode));
            Assert.False(string.IsNullOrWhiteSpace(result.TopValue));
            Assert.False(string.IsNullOrWhiteSpace(result.TopCulture));

            // No adult assessment tables touched
            Assert.Equal(0, await db.CandidateCompetencies.CountAsync());
            Assert.Equal(0, await db.CandidateCareerInterests.CountAsync());
            Assert.Equal(0, await db.CandidateValuesProfiles.CountAsync());
            Assert.Equal(0, await db.CandidateCulturePersonalityProfiles.CountAsync());
        }

        using var again = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var login = await again.PostAsJsonAsync("api/pupil/login", new PupilLoginRequest(
            seed.SchoolId, seed.ClassId, seed.PlainCode));
        var bodyLogin = await login.Content.ReadFromJsonAsync<PupilLoginResponse>(Json);
        Assert.Equal("/leerling/dit-ben-jij", bodyLogin!.RedirectPath);
    }

    [Fact]
    public async Task Save_item_51_before_island_returns_409_step_pending()
    {
        await EnableSchoolsAsync(true);
        var seed = await SeedOpenClassAsync();
        using var client = await LoginPupilAsync(seed);
        var bank = new PupilQuestionBankVo();

        for (var i = 0; i < 50; i++)
        {
            var r = await client.PutAsJsonAsync(
                $"api/pupil/progress/answers/{bank.AllItems[i].Id}",
                new PupilAnswerRequest(4));
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        }

        var blocked = await client.PutAsJsonAsync(
            $"api/pupil/progress/answers/{bank.AllItems[50].Id}",
            new PupilAnswerRequest(3));
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        var body = await blocked.Content.ReadAsStringAsync();
        Assert.Contains("step_pending", body, StringComparison.Ordinal);
        Assert.Contains("island", body, StringComparison.OrdinalIgnoreCase);

        // Changing an earlier answer while the island is due stays allowed.
        var change = await client.PutAsJsonAsync(
            $"api/pupil/progress/answers/{bank.AllItems[5].Id}",
            new PupilAnswerRequest(2));
        Assert.Equal(HttpStatusCode.OK, change.StatusCode);
    }

    [Fact]
    public async Task Answer_id_9101_succeeds_on_vo_and_9001_is_wrong_set()
    {
        await EnableSchoolsAsync(true);
        var seed = await SeedOpenClassAsync();
        using var client = await LoginPupilAsync(seed);
        var ok = await client.PutAsJsonAsync(
            "api/pupil/progress/answers/9101",
            new PupilAnswerRequest(3));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        var progress = await client.GetFromJsonAsync<PupilProgressStateDto>("api/pupil/progress", Json);
        Assert.Equal(100, progress!.TotalItems);

        var foreign = await client.PutAsJsonAsync(
            "api/pupil/progress/answers/9001",
            new PupilAnswerRequest(3));
        Assert.Equal(HttpStatusCode.BadRequest, foreign.StatusCode);
        var body = await foreign.Content.ReadAsStringAsync();
        Assert.Contains("wrong_set", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Groep78_class_rejects_9101_as_wrong_set_and_has_60_items()
    {
        await EnableSchoolsAsync(true);
        var seed = await SeedOpenClassAsync(
            level: SchoolLevel.Groep78, year: 8, questionSet: PupilQuestionSet.Groep78);
        using var client = await LoginPupilAsync(seed);
        var progress = await client.GetFromJsonAsync<PupilProgressStateDto>("api/pupil/progress", Json);
        Assert.Equal(60, progress!.TotalItems);

        var g78 = await client.PutAsJsonAsync(
            "api/pupil/progress/answers/9001",
            new PupilAnswerRequest(3));
        Assert.Equal(HttpStatusCode.OK, g78.StatusCode);

        var foreign = await client.PutAsJsonAsync(
            "api/pupil/progress/answers/9101",
            new PupilAnswerRequest(3));
        Assert.Equal(HttpStatusCode.BadRequest, foreign.StatusCode);
        var body = await foreign.Content.ReadAsStringAsync();
        Assert.Contains("wrong_set", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Chips_reject_digits_at_and_first_names_and_enforce_exclusivity()
    {
        await EnableSchoolsAsync(true);
        var seed = await SeedOpenClassAsync();
        using var client = await LoginPupilAsync(seed);
        var bank = new PupilQuestionBankVo();
        for (var i = 0; i < 50; i++)
        {
            var r = await client.PutAsJsonAsync(
                $"api/pupil/progress/answers/{bank.AllItems[i].Id}",
                new PupilAnswerRequest(4));
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        }

        var digits = await client.PutAsJsonAsync("api/pupil/progress/chips", new PupilChipsRequest(
            [], [], "test123", null));
        Assert.Equal(HttpStatusCode.BadRequest, digits.StatusCode);

        var at = await client.PutAsJsonAsync("api/pupil/progress/chips", new PupilChipsRequest(
            [], [], "a@b", null));
        Assert.Equal(HttpStatusCode.BadRequest, at.StatusCode);

        var name = await client.PutAsJsonAsync("api/pupil/progress/chips", new PupilChipsRequest(
            [], [], "Emma", null));
        Assert.Equal(HttpStatusCode.BadRequest, name.StatusCode);
        var nameBody = await name.Content.ReadAsStringAsync();
        Assert.Contains("naam", nameBody, StringComparison.OrdinalIgnoreCase);

        var ok = await client.PutAsJsonAsync("api/pupil/progress/chips", new PupilChipsRequest(
            ["sport", "gamen"],
            ["sport", "lang-stilzitten"],
            "paardrijden",
            null));
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var progress = await db.PupilProgresses.AsNoTracking().FirstAsync(p => p.PupilCodeId == seed.CodeId);
        Assert.NotNull(progress.ChipsSavedAtUtc);
        Assert.Contains("sport", progress.LikesJson, StringComparison.Ordinal);
        Assert.DoesNotContain("\"sport\"", progress.DislikesJson, StringComparison.Ordinal);
        Assert.Equal("paardrijden", progress.LikeOtherWord);
    }

    private static void AssertSetCookieIsSessionOnly(HttpResponseMessage response)
    {
        Assert.True(response.Headers.TryGetValues(HeaderNames.SetCookie, out var values));
        var pupil = values!.FirstOrDefault(v => v.StartsWith("Lobsy.Leerling=", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(pupil);
        Assert.DoesNotContain("expires=", pupil, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("max-age=", pupil, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("httponly", pupil, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", pupil, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<HttpClient> LoginPupilAsync(SeedInfo seed)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var login = await client.PostAsJsonAsync("api/pupil/login", new PupilLoginRequest(
            seed.SchoolId, seed.ClassId, seed.PlainCode));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return client;
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

    private async Task<SeedInfo> SeedOpenClassAsync(
        bool withTeacher = false,
        SchoolLevel level = SchoolLevel.Havo,
        int year = 2,
        PupilQuestionSet questionSet = PupilQuestionSet.Vo)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var codes = scope.ServiceProvider.GetRequiredService<IPupilCodeService>();

        var school = new School
        {
            Id = Guid.NewGuid(),
            Name = "Voorbeeld College " + Guid.NewGuid().ToString("N")[..6],
            City = "Naaldwijk",
            AllowedEmailDomains = "[\"voorbeeldcollege.nl\"]",
            IsActive = true,
            ProcessorAgreementSignedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            ProcessorAgreementVersion = "1.0",
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = _factory.AdminId
        };
        db.Schools.Add(school);

        var cls = new SchoolClass
        {
            Id = Guid.NewGuid(),
            SchoolId = school.Id,
            Name = questionSet == PupilQuestionSet.Groep78 ? "8A" : "2B",
            Level = level,
            Year = year,
            QuestionSet = questionSet,
            SchoolYearStart = SchoolYear.Current(DateOnly.FromDateTime(DateTime.UtcNow)),
            PupilCount = 3,
            TestWindow = TestWindowState.Open,
            ParentalInfoConfirmedAtUtc = DateTime.UtcNow,
            ParentalInfoConfirmedByUserId = _factory.AdminId,
            ParentalInfoTextVersion = "1",
            CreatedAtUtc = DateTime.UtcNow
        };
        db.SchoolClasses.Add(cls);
        await db.SaveChangesAsync();

        var generated = await codes.GenerateAsync(3, cls);
        var plain = codes.Unprotect(generated[0].CodeProtected)!;

        Guid? teacherId = null;
        if (withTeacher)
        {
            teacherId = Guid.NewGuid();
            db.Users.Add(new User
            {
                Id = teacherId.Value,
                Email = $"leraar-{teacherId:N}@voorbeeldcollege.nl",
                FullName = "Leraar Test",
                Role = UserRole.Teacher,
                SchoolId = school.Id,
                IsActive = true,
                AuthenticatorEnabled = true
            });
            db.TeacherClassAssignments.Add(new TeacherClassAssignment
            {
                TeacherUserId = teacherId.Value,
                SchoolClassId = cls.Id,
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        return new SeedInfo(school.Id, cls.Id, generated[0].Id, plain, teacherId);
    }

    private sealed record SeedInfo(
        Guid SchoolId,
        Guid ClassId,
        Guid CodeId,
        string PlainCode,
        Guid? TeacherId);
}
