using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.PupilPassport;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Scholen;
using Jobsy.Infrastructure.Scholen.Passport;
using Jobsy.Web.Localization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Jobsy.Tests.Scholen;

public sealed class ParentPassportServiceTests
{
    [Fact]
    public async Task OpenSession_uses_CreatedAtUtc_for_thirty_day_window()
    {
        await using var scope = CreateScope(out var db, out var service, out var codes);
        var schoolClass = await SeedClassAsync(db);
        var created = await codes.GenerateAsync(1, schoolClass);
        var code = created[0];
        code.Status = PupilCodeStatus.Completed;
        code.Result = MinimalResult(code, schoolClass.Id);
        code.CreatedAtUtc = DateTime.UtcNow.AddDays(-31);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var plain = codes.Unprotect(code.CodeProtected)!;
        var (ok, _, status) = await service.OpenSessionAsync(PupilCodeFormat.Display(plain));
        Assert.Null(ok);
        Assert.Equal(404, status);
    }

    [Fact]
    public async Task OpenSession_accepts_completed_code_within_window()
    {
        await using var scope = CreateScope(out var db, out var service, out var codes);
        var schoolClass = await SeedClassAsync(db);
        var created = await codes.GenerateAsync(1, schoolClass);
        var code = created[0];
        code.Status = PupilCodeStatus.Completed;
        code.Result = MinimalResult(code, schoolClass.Id);
        code.CreatedAtUtc = DateTime.UtcNow.AddDays(-5);
        await db.SaveChangesAsync();

        var plain = codes.Unprotect(code.CodeProtected)!;
        var (ok, _, status) = await service.OpenSessionAsync(PupilCodeFormat.Display(plain));
        Assert.Equal(200, status);
        Assert.NotNull(ok);
        Assert.Equal(code.Id, ok!.PupilCodeId);
    }

    [Fact]
    public void Ouder_copy_is_nl_en_without_em_dash_or_exclamation()
    {
        var nl = new Dictionary<string, string>(StringComparer.Ordinal);
        var en = new Dictionary<string, string>(StringComparer.Ordinal);
        UiStringsOuder.MergeNlEn(nl, en);
        foreach (var value in nl.Values.Concat(en.Values))
        {
            Assert.DoesNotContain('—', value);
            Assert.DoesNotContain('!', value);
            Assert.DoesNotContain('%', value);
        }
    }

    private static ServiceProvider CreateScope(
        out JobsyDbContext db,
        out ParentPassportService service,
        out PupilCodeService codes)
    {
        var services = new ServiceCollection();
        var context = CreateDb();
        db = context;
        var codeService = CreateCodeService(context);
        services.AddSingleton(context);
        services.AddSingleton(codeService);
        services.AddSingleton<IPupilStoryRenderer, PupilStoryRenderer>();
        services.AddSingleton<PupilPassportPdfModelBuilder>();
        services.AddSingleton<IPupilPassportPdfService, PupilPassportPdfService>();
        services.AddSingleton<IPlatformFeatureService, FixedFeatures>();
        services.AddSingleton(_ => new ParentPassportService(
            context,
            codeService,
            _.GetRequiredService<PupilPassportPdfModelBuilder>(),
            _.GetRequiredService<IPupilPassportPdfService>(),
            _.GetRequiredService<IPlatformFeatureService>(),
            TimeProvider.System));
        var sp = services.BuildServiceProvider();
        service = sp.GetRequiredService<ParentPassportService>();
        codes = codeService;
        return sp;
    }

    private static PupilResult MinimalResult(PupilCode code, Guid classId)
        => new()
        {
            PupilCodeId = code.Id,
            SchoolClassId = classId,
            CompletedAtUtc = DateTime.UtcNow,
            ScoringVersion = "test",
            StoryTemplateVersion = "test"
        };

    private static PupilCodeService CreateCodeService(JobsyDbContext db)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Scholen:CodeHmacKey"] = "test-hmac-key-for-scholen-unit-tests"
            })
            .Build();
        var env = new FakeHostEnvironment();
        var dp = DataProtectionProvider.Create(Path.Combine(Path.GetTempPath(), "scholen-dp-" + Guid.NewGuid()));
        return new PupilCodeService(db, dp, config, env);
    }

    private static async Task<SchoolClass> SeedClassAsync(JobsyDbContext db)
    {
        var school = new School
        {
            Id = Guid.NewGuid(),
            Name = "T",
            City = "C",
            AllowedEmailDomains = "[]",
            IsActive = true,
            ProcessorAgreementSignedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            CreatedAtUtc = DateTime.UtcNow
        };
        var sc = new SchoolClass
        {
            Id = Guid.NewGuid(),
            SchoolId = school.Id,
            School = school,
            Name = "1A",
            Year = 1,
            SchoolYearStart = 2026,
            PupilCount = 40,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Schools.Add(school);
        db.SchoolClasses.Add(sc);
        await db.SaveChangesAsync();
        return sc;
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("ParentPassport-" + Guid.NewGuid())
            .Options;
        return new JobsyDbContext(options);
    }

    private sealed class FixedFeatures : IPlatformFeatureService
    {
        public Task<PlatformFeatureSnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformFeatureSnapshot(
                false,
                false,
                "https://lobsy.nl",
                DateTime.UtcNow,
                SchoolsEnabled: true,
                PupilPassportV2Enabled: true));

        public Task<PlatformFeatureSnapshot> UpdateAsync(
            PlatformFeatureUpdate update,
            CancellationToken cancellationToken = default)
            => GetAsync(cancellationToken);
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
