using System.Net;
using System.Net.Http.Json;
using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Entities;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.MboHbo;
using Jobsy.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Jobsy.Infrastructure.Scholen;
using Jobsy.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests.Scholen;

public sealed class PupilMboHboPdfApiTests : IClassFixture<RoleFunctionalWebAppFactory>
{
    private readonly RoleFunctionalWebAppFactory _factory;
    public PupilMboHboPdfApiTests(RoleFunctionalWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Completed_mbo_pupil_gets_pdf_bytes_from_api()
    {
        await EnableSchoolsAsync(true);
        var (schoolId, classId, plainCode, protectedCode) = await SeedCompleteMboCodeAsync();
        using var client = _factory.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = true });

        var login = await client.PostAsJsonAsync(
            "api/pupil/login",
            new PupilLoginRequest(schoolId, classId, plainCode));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.True(login.Headers.TryGetValues("Set-Cookie", out _));

        var pdf = await client.GetAsync("leerling/pdf");
        Assert.Equal(HttpStatusCode.OK, pdf.StatusCode);
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
        var bytes = await pdf.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 500);
        Assert.Equal((byte)'%', bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        _ = protectedCode;
    }

    private async Task<(Guid SchoolId, Guid ClassId, string Plain, string Protected)> SeedCompleteMboCodeAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var codes = scope.ServiceProvider.GetRequiredService<IPupilCodeService>();
        var schoolId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        db.Schools.Add(new School
        {
            Id = schoolId,
            Name = "PDF Test School",
            City = "Utrecht",
            AllowedEmailDomains = "[\"pdf.test\"]",
            IsActive = true,
            ProcessorAgreementSignedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            ProcessorAgreementVersion = "1",
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = _factory.AdminId
        });
        db.SchoolClasses.Add(new SchoolClass
        {
            Id = classId,
            SchoolId = schoolId,
            Name = "M2PDF",
            Level = SchoolLevel.Mbo2,
            Year = 2,
            QuestionSet = PupilQuestionSet.MboHbo,
            SchoolYearStart = SchoolYear.Current(DateOnly.FromDateTime(DateTime.UtcNow)),
            PupilCount = 1,
            TestWindow = TestWindowState.Open,
            ParentalInfoConfirmedAtUtc = DateTime.UtcNow,
            ParentalInfoConfirmedByUserId = _factory.AdminId,
            ParentalInfoTextVersion = "1",
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var cls = await db.SchoolClasses.SingleAsync(c => c.Id == classId);
        var generated = await codes.GenerateAsync(1, cls);
        var code = generated[0];
        var plain = codes.Unprotect(code.CodeProtected)!;

        var state = new PupilMboHboProgressState();
        var now = DateTime.UtcNow.ToString("O");
        foreach (var block in PupilMboHboCatalog.AllBlocks)
        {
            state.BlocksCompleted[PupilMboHboProgressState.BlockKey(block)] = now;
        }

        db.PupilProgresses.Add(new PupilProgress
        {
            PupilCodeId = code.Id,
            MboHboProgressJson = state.ToJson(),
            StartedAtUtc = DateTime.UtcNow.AddHours(-1),
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return (schoolId, classId, plain, code.CodeProtected);
    }

    private async Task EnableSchoolsAsync(bool enabled)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<JobsyDbContext>();
        var row = await db.PlatformFeatureSettings.FirstOrDefaultAsync();
        if (row is null)
        {
            db.PlatformFeatureSettings.Add(new PlatformFeatureSettings
            {
                Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                SchoolsEnabled = enabled
            });
        }
        else
        {
            row.SchoolsEnabled = enabled;
        }

        await db.SaveChangesAsync();
    }
}
