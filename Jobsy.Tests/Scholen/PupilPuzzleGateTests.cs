using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Entities;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Scholen;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Scholen;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Jobsy.Tests.Scholen;

public sealed class PupilPuzzleGateTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private readonly RoleFunctionalWebAppFactory _factory;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public PupilPuzzleGateTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task After_fifteen_answers_progress_points_to_schelpenrij_until_completed()
    {
        await EnableSchoolsAsync();
        var seed = await SeedOpenClassAsync();
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var login = await client.PostAsJsonAsync("api/pupil/login", new PupilLoginRequest(
            seed.SchoolId, seed.ClassId, seed.PlainCode));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var bank = new PupilQuestionBank();
        for (var i = 0; i < 15; i++)
        {
            var save = await client.PutAsJsonAsync(
                $"api/pupil/progress/answers/{bank.AllItems[i].Id}",
                new PupilAnswerRequest(3));
            Assert.Equal(HttpStatusCode.OK, save.StatusCode);
        }

        var progress = await client.GetFromJsonAsync<PupilProgressStateDto>("api/pupil/progress", Json);
        Assert.NotNull(progress);
        Assert.Equal("puzzle", progress!.NextStep);
        Assert.Equal("schelpenrij", progress.NextPuzzleKey);

        var blocked = await client.PutAsJsonAsync(
            $"api/pupil/progress/answers/{bank.AllItems[15].Id}",
            new PupilAnswerRequest(4));
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);

        var done = await client.PutAsJsonAsync(
            "api/pupil/progress/puzzles/schelpenrij",
            new PupilPuzzleRequest("done"));
        Assert.Equal(HttpStatusCode.OK, done.StatusCode);

        var after = await client.GetFromJsonAsync<PupilProgressStateDto>("api/pupil/progress", Json);
        Assert.NotNull(after);
        Assert.Equal("question", after!.NextStep);
        Assert.Equal("9016", after.CurrentItemId);
    }

    private async Task EnableSchoolsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var row = await db.PlatformFeatureSettings.FirstOrDefaultAsync();
        if (row is null)
        {
            db.PlatformFeatureSettings.Add(new PlatformFeatureSettings { SchoolsEnabled = true });
        }
        else
        {
            row.SchoolsEnabled = true;
        }

        await db.SaveChangesAsync();
    }

    private async Task<(Guid SchoolId, Guid ClassId, string PlainCode)> SeedOpenClassAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var codes = scope.ServiceProvider.GetRequiredService<IPupilCodeService>();
        var schoolId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        db.Schools.Add(new School
        {
            Id = schoolId,
            Name = "Puzzle Test School",
            City = "Den Haag",
            AllowedEmailDomains = """["lobsy.nl"]""",
            IsActive = true,
            IsTestData = true,
            CreatedAtUtc = DateTime.UtcNow,
            ProcessorAgreementSignedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            ProcessorAgreementVersion = "test"
        });
        db.SchoolClasses.Add(new SchoolClass
        {
            Id = classId,
            SchoolId = schoolId,
            Name = "7A",
            Level = SchoolLevel.Groep78,
            QuestionSet = PupilQuestionSet.Groep78,
            Year = 7,
            SchoolYearStart = SchoolYear.Current(DateOnly.FromDateTime(DateTime.UtcNow)),
            PupilCount = 1,
            IsTestData = true,
            TestWindow = TestWindowState.Open,
            ParentalInfoConfirmedAtUtc = DateTime.UtcNow,
            ParentalInfoConfirmedByUserId = _factory.AdminId,
            ParentalInfoTextVersion = "1",
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var schoolClass = await db.SchoolClasses.SingleAsync(c => c.Id == classId);
        var generated = await codes.GenerateAsync(1, schoolClass, CancellationToken.None);
        var plain = codes.Unprotect(generated[0].CodeProtected)!;
        await db.SaveChangesAsync();
        return (schoolId, classId, plain);
    }
}
