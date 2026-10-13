using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Entities;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Scholen;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Jobsy.Tests.Scholen;

/// <summary>
/// Hotfix: never leave a pupil Completed without PupilResult; self-heal on read.
/// </summary>
public class PupilResultSelfHealTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private readonly RoleFunctionalWebAppFactory _factory;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public PupilResultSelfHealTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Builder_fails_once_then_progress_self_heals_and_result_is_reachable()
    {
        await EnableSchoolsAsync(true);
        var failOnce = new FailCountResultBuilder(failTimes: 1);
        var logs = new CollectingLoggerProvider();
        using var factory = CreateFactory(failOnce, logs);
        var seed = await SeedOpenClassAsync(factory, withTeacher: false);
        using var client = await LoginPupilAsync(factory, seed);

        var bank = new PupilQuestionBank();
        PupilAnswerResponse? lastBody = null;
        for (var i = 0; i < 30; i++)
        {
            var r = await client.PutAsJsonAsync(
                $"api/pupil/progress/answers/{bank.AllItems[i].Id}",
                new PupilAnswerRequest(5));
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        }

        var chips = await client.PutAsJsonAsync("api/pupil/progress/chips", new PupilChipsRequest(
            ["tekenen", "dieren"],
            ["voor-de-klas"],
            null,
            null));
        Assert.Equal(HttpStatusCode.OK, chips.StatusCode);

        for (var i = 30; i < 60; i++)
        {
            var r = await client.PutAsJsonAsync(
                $"api/pupil/progress/answers/{bank.AllItems[i].Id}",
                new PupilAnswerRequest(3));
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            if (i == 59)
            {
                lastBody = await r.Content.ReadFromJsonAsync<PupilAnswerResponse>(Json);
            }
        }

        Assert.NotNull(lastBody);
        Assert.False(lastBody!.Completed);
        Assert.True(lastBody.ResultPending);

        var progress = await client.GetFromJsonAsync<PupilProgressStateDto>("api/pupil/progress", Json);
        Assert.NotNull(progress);
        Assert.True(progress!.Completed);

        var resultResponse = await client.GetAsync("api/pupil/result");
        Assert.Equal(HttpStatusCode.OK, resultResponse.StatusCode);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            var count = await db.PupilResults.CountAsync(r => r.PupilCodeId == seed.CodeId);
            Assert.Equal(1, count);
            var code = await db.PupilCodes.AsNoTracking().FirstAsync(c => c.Id == seed.CodeId);
            Assert.Equal(PupilCodeStatus.Completed, code.Status);
        }

        var warning = logs.Entries
            .Where(e => e.Level == LogLevel.Warning)
            .Select(e => e.Message)
            .FirstOrDefault(m => m.Contains("Pupil result builder failed", StringComparison.Ordinal));
        Assert.NotNull(warning);
        Assert.Contains(seed.CodeId.ToString("D"), warning!, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(seed.PlainCode, warning!, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AnswersJson", warning!, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"9001\"", warning!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Builder_keeps_failing_stays_in_progress_and_teacher_sees_result_pending()
    {
        await EnableSchoolsAsync(true);
        var alwaysFail = new FailCountResultBuilder(failTimes: int.MaxValue);
        var logs = new CollectingLoggerProvider();
        using var factory = CreateFactory(alwaysFail, logs);
        var seed = await SeedOpenClassAsync(factory, withTeacher: true);
        using var client = await LoginPupilAsync(factory, seed);

        var bank = new PupilQuestionBank();
        for (var i = 0; i < 30; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync(
                $"api/pupil/progress/answers/{bank.AllItems[i].Id}",
                new PupilAnswerRequest(5))).StatusCode);
        }

        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("api/pupil/progress/chips", new PupilChipsRequest(
            ["tekenen", "dieren"],
            ["voor-de-klas"],
            null,
            null))).StatusCode);

        PupilAnswerResponse? lastBody = null;
        for (var i = 30; i < 60; i++)
        {
            var r = await client.PutAsJsonAsync(
                $"api/pupil/progress/answers/{bank.AllItems[i].Id}",
                new PupilAnswerRequest(3));
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            if (i == 59)
            {
                lastBody = await r.Content.ReadFromJsonAsync<PupilAnswerResponse>(Json);
            }
        }

        Assert.NotNull(lastBody);
        Assert.False(lastBody!.Completed);
        Assert.True(lastBody.ResultPending);

        var progress = await client.GetFromJsonAsync<PupilProgressStateDto>("api/pupil/progress", Json);
        Assert.NotNull(progress);
        Assert.False(progress!.Completed);
        Assert.Equal(PupilCodeStatus.InProgress, progress.Status);

        var stuckResult = await client.GetAsync("api/pupil/result");
        Assert.Equal(HttpStatusCode.Conflict, stuckResult.StatusCode);
        var err = await stuckResult.Content.ReadFromJsonAsync<PupilErrorDto>(Json);
        Assert.Equal("not_completed", err!.Error);

        // Not a dead end: progress still works and answers are kept.
        var again = await client.GetAsync("api/pupil/progress");
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
            Assert.Equal(0, await db.PupilResults.CountAsync(r => r.PupilCodeId == seed.CodeId));
            var code = await db.PupilCodes.AsNoTracking().FirstAsync(c => c.Id == seed.CodeId);
            Assert.Equal(PupilCodeStatus.InProgress, code.Status);
        }

        using var teacher = JobsyTestAuth.CreateAuthenticatedClient(factory, seed.TeacherId!.Value);
        var detail = await teacher.GetFromJsonAsync<TeacherCodeDetailDto>(
            $"api/teacher/classes/{seed.ClassId:D}/codes/{seed.CodeId:D}", Json);
        Assert.NotNull(detail);
        Assert.True(detail!.ResultPending);
        Assert.Null(detail.Story);
    }

    private WebApplicationFactory<Jobsy.Api.ApiAssemblyMarker> CreateFactory(
        FailCountResultBuilder failState,
        CollectingLoggerProvider logs)
    {
        // Ensure shared fixture seed ran once.
        _ = _factory.CreateClient();

        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<ILoggerProvider>(logs);
                services.RemoveAll<IPupilResultBuilder>();
                services.AddScoped<IPupilResultBuilder>(sp =>
                {
                    var real = ActivatorUtilities.CreateInstance<PupilResultBuilder>(sp);
                    return new ThrowingThenRealResultBuilder(failState, real);
                });
            });
        });
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
        WebApplicationFactory<Jobsy.Api.ApiAssemblyMarker> factory,
        bool withTeacher)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var codes = scope.ServiceProvider.GetRequiredService<IPupilCodeService>();

        var school = new School
        {
            Id = Guid.NewGuid(),
            Name = "SelfHeal College " + Guid.NewGuid().ToString("N")[..6],
            City = "Naaldwijk",
            AllowedEmailDomains = "[\"selfheal.nl\"]",
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
            Name = "8A",
            Level = SchoolLevel.Groep78,
            Year = 8,
            QuestionSet = PupilQuestionSet.Groep78,
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
                Email = $"leraar-{teacherId:N}@selfheal.nl",
                FullName = "Leraar SelfHeal",
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

    private static async Task<HttpClient> LoginPupilAsync(
        WebApplicationFactory<Jobsy.Api.ApiAssemblyMarker> factory,
        SeedInfo seed)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var login = await client.PostAsJsonAsync("api/pupil/login", new PupilLoginRequest(
            seed.SchoolId, seed.ClassId, seed.PlainCode));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return client;
    }

    private sealed record SeedInfo(
        Guid SchoolId,
        Guid ClassId,
        Guid CodeId,
        string PlainCode,
        Guid? TeacherId);

    private sealed class FailCountResultBuilder
    {
        private int _remaining;
        public FailCountResultBuilder(int failTimes) => _remaining = failTimes;
        public bool ShouldFail()
        {
            if (_remaining <= 0)
            {
                return false;
            }

            _remaining--;
            return true;
        }
    }

    private sealed class ThrowingThenRealResultBuilder : IPupilResultBuilder
    {
        private readonly FailCountResultBuilder _state;
        private readonly IPupilResultBuilder _inner;

        public ThrowingThenRealResultBuilder(FailCountResultBuilder state, IPupilResultBuilder inner)
        {
            _state = state;
            _inner = inner;
        }

        public Task BuildAsync(Guid pupilCodeId, CancellationToken cancellationToken = default)
        {
            if (_state.ShouldFail())
            {
                throw new InvalidOperationException("Simulated pupil result builder failure.");
            }

            return _inner.BuildAsync(pupilCodeId, cancellationToken);
        }
    }

    private sealed class CollectingLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentBag<string> _entries = new();
        public IReadOnlyList<(LogLevel Level, string Message)> Entries =>
            _entries.Select(e =>
            {
                var parts = e.Split('\u001f', 2);
                return (Enum.Parse<LogLevel>(parts[0]), parts[1]);
            }).ToList();

        public ILogger CreateLogger(string categoryName) => new CollectingLogger(_entries);
        public void Dispose() { }

        private sealed class CollectingLogger(ConcurrentBag<string> entries) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
                => entries.Add($"{logLevel}\u001f{formatter(state, exception)}");
        }
    }
}
