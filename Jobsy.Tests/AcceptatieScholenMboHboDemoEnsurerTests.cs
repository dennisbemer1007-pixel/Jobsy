using Jobsy.Core.Entities;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Ops;
using Jobsy.Infrastructure.Scholen;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public sealed class AcceptatieScholenMboHboDemoEnsurerTests
{
    [Fact]
    public async Task Ensure_creates_mbo_and_hbo_classes_on_demo_college_for_test_leraar()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new JobsyDbContext(options);
        var teacherId = Guid.Parse("cccccccc-3333-3333-3333-333333333333");
        var testSchoolId = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111");

        db.Schools.Add(new School
        {
            Id = DemoSchoolIds.SchoolId,
            Name = "Demo College Lobsy",
            City = "Naaldwijk",
            AllowedEmailDomains = """["jobsy.local","lobsy.nl"]""",
            IsActive = true,
            IsTestData = true,
            CreatedAtUtc = DateTime.UtcNow,
            ProcessorAgreementSignedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            ProcessorAgreementVersion = "test"
        });
        db.Schools.Add(new School
        {
            Id = testSchoolId,
            Name = "Testschool",
            City = "Den Haag",
            AllowedEmailDomains = """["lobsy.nl"]""",
            IsActive = true,
            IsTestData = true,
            CreatedAtUtc = DateTime.UtcNow,
            ProcessorAgreementSignedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            ProcessorAgreementVersion = "test"
        });
        db.Users.Add(new User
        {
            Id = teacherId,
            Email = AcceptatieScholenMboHboDemoEnsurer.TestLeraarEmail,
            FullName = "Test Leraar",
            Role = UserRole.Teacher,
            SchoolId = testSchoolId,
            IsActive = true,
            AuthenticatorEnabled = true,
            SessionVersion = 0
        });
        await db.SaveChangesAsync();

        var config = AcceptatieConfig();
        var codes = CreatePupilCodeService(db, config);
        Assert.True(await AcceptatieScholenMboHboDemoEnsurer.TryEnsureAsync(
            db, codes, config, NullLogger.Instance));

        var mbo = await db.SchoolClasses.SingleAsync(c => c.Id == DemoSchoolIds.ClassMbo2Id);
        Assert.Equal(SchoolLevel.Mbo2, mbo.Level);
        Assert.Equal(PupilQuestionSet.MboHbo, mbo.QuestionSet);
        Assert.Equal(TestWindowState.Open, mbo.TestWindow);

        var hbo = await db.SchoolClasses.SingleAsync(c => c.Id == DemoSchoolIds.ClassHboId);
        Assert.Equal(SchoolLevel.Hbo, hbo.Level);
        Assert.Equal(PupilQuestionSet.MboHbo, hbo.QuestionSet);

        Assert.True(await db.TeacherClassAssignments.AnyAsync(
            a => a.TeacherUserId == teacherId && a.SchoolClassId == mbo.Id));
        Assert.True(await db.TeacherClassAssignments.AnyAsync(
            a => a.TeacherUserId == teacherId && a.SchoolClassId == hbo.Id));

        Assert.Equal(
            AcceptatieScholenMboHboDemoEnsurer.MinimumPupilCodes,
            await db.PupilCodes.CountAsync(c => c.SchoolClassId == mbo.Id));
        Assert.Equal(
            AcceptatieScholenMboHboDemoEnsurer.MinimumPupilCodes,
            await db.PupilCodes.CountAsync(c => c.SchoolClassId == hbo.Id));
    }

    [Fact]
    public async Task Ensure_assigns_mbo_hbo_to_demo_leraar_jobsy_local()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new JobsyDbContext(options);
        var demoTeacherId = DemoSchoolIds.TeacherUserId;

        db.Schools.Add(new School
        {
            Id = DemoSchoolIds.SchoolId,
            Name = "Demo College Lobsy",
            City = "Naaldwijk",
            AllowedEmailDomains = """["jobsy.local"]""",
            IsActive = true,
            IsTestData = true,
            CreatedAtUtc = DateTime.UtcNow,
            ProcessorAgreementSignedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            ProcessorAgreementVersion = "test"
        });
        db.Users.Add(new User
        {
            Id = demoTeacherId,
            Email = AcceptatieScholenMboHboDemoEnsurer.DemoLeraarEmail,
            FullName = "Demo Leraar",
            Role = UserRole.Teacher,
            SchoolId = DemoSchoolIds.SchoolId,
            IsActive = true,
            AuthenticatorEnabled = true,
            SessionVersion = 0
        });
        await db.SaveChangesAsync();

        var config = AcceptatieConfig();
        var codes = CreatePupilCodeService(db, config);
        Assert.True(await AcceptatieScholenMboHboDemoEnsurer.TryEnsureAsync(
            db, codes, config, NullLogger.Instance));

        var mbo = await db.SchoolClasses.SingleAsync(c => c.Id == DemoSchoolIds.ClassMbo2Id);
        var hbo = await db.SchoolClasses.SingleAsync(c => c.Id == DemoSchoolIds.ClassHboId);
        Assert.True(await db.TeacherClassAssignments.AnyAsync(
            a => a.TeacherUserId == demoTeacherId && a.SchoolClassId == mbo.Id));
        Assert.True(await db.TeacherClassAssignments.AnyAsync(
            a => a.TeacherUserId == demoTeacherId && a.SchoolClassId == hbo.Id));
    }

    [Fact]
    public async Task Ensure_assigns_both_teachers_when_present()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new JobsyDbContext(options);
        var testTeacherId = Guid.Parse("cccccccc-3333-3333-3333-333333333333");
        var testSchoolId = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111");

        db.Schools.Add(new School
        {
            Id = DemoSchoolIds.SchoolId,
            Name = "Demo College Lobsy",
            City = "Naaldwijk",
            AllowedEmailDomains = """["jobsy.local","lobsy.nl"]""",
            IsActive = true,
            IsTestData = true,
            CreatedAtUtc = DateTime.UtcNow,
            ProcessorAgreementSignedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            ProcessorAgreementVersion = "test"
        });
        db.Schools.Add(new School
        {
            Id = testSchoolId,
            Name = "Testschool",
            City = "Den Haag",
            AllowedEmailDomains = """["lobsy.nl"]""",
            IsActive = true,
            IsTestData = true,
            CreatedAtUtc = DateTime.UtcNow,
            ProcessorAgreementSignedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            ProcessorAgreementVersion = "test"
        });
        db.Users.Add(new User
        {
            Id = DemoSchoolIds.TeacherUserId,
            Email = AcceptatieScholenMboHboDemoEnsurer.DemoLeraarEmail,
            FullName = "Demo Leraar",
            Role = UserRole.Teacher,
            SchoolId = DemoSchoolIds.SchoolId,
            IsActive = true,
            AuthenticatorEnabled = true,
            SessionVersion = 0
        });
        db.Users.Add(new User
        {
            Id = testTeacherId,
            Email = AcceptatieScholenMboHboDemoEnsurer.TestLeraarEmail,
            FullName = "Test Leraar",
            Role = UserRole.Teacher,
            SchoolId = testSchoolId,
            IsActive = true,
            AuthenticatorEnabled = true,
            SessionVersion = 0
        });
        await db.SaveChangesAsync();

        var config = AcceptatieConfig();
        var codes = CreatePupilCodeService(db, config);
        await AcceptatieScholenMboHboDemoEnsurer.TryEnsureAsync(db, codes, config, NullLogger.Instance);

        var mbo = DemoSchoolIds.ClassMbo2Id;
        var hbo = DemoSchoolIds.ClassHboId;
        Assert.Equal(2, await db.TeacherClassAssignments.CountAsync(a => a.SchoolClassId == mbo));
        Assert.Equal(2, await db.TeacherClassAssignments.CountAsync(a => a.SchoolClassId == hbo));
    }

    [Fact]
    public async Task Skips_when_demo_school_is_not_test_data()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new JobsyDbContext(options);
        db.Schools.Add(new School
        {
            Id = DemoSchoolIds.SchoolId,
            Name = "Demo College",
            City = "Naaldwijk",
            AllowedEmailDomains = """["lobsy.nl"]""",
            IsActive = true,
            IsTestData = false,
            CreatedAtUtc = DateTime.UtcNow,
            ProcessorAgreementSignedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            ProcessorAgreementVersion = "test"
        });
        await db.SaveChangesAsync();

        var config = AcceptatieConfig();
        var codes = CreatePupilCodeService(db, config);
        Assert.False(await AcceptatieScholenMboHboDemoEnsurer.TryEnsureAsync(
            db, codes, config, NullLogger.Instance));
        Assert.False(await db.SchoolClasses.AnyAsync());
    }

    private static IConfiguration AcceptatieConfig()
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Lobsy:DeploymentEnvironment"] = "Acceptatie",
                ["JobsyFeatures:SchoolsEnabled"] = "true",
                ["Scholen:CodeHmacKey"] = "test-hmac-key-for-scholen-unit-tests"
            })
            .Build();

    private static PupilCodeService CreatePupilCodeService(JobsyDbContext db, IConfiguration config)
    {
        var dp = DataProtectionProvider.Create(Path.Combine(Path.GetTempPath(), "scholen-dp-" + Guid.NewGuid()));
        return new PupilCodeService(db, dp, config, new FakeHostEnvironment());
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } =
            new NullFileProvider();
    }
}
