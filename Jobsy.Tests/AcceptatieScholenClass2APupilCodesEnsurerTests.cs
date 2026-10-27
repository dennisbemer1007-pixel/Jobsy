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

public sealed class AcceptatieScholenClass2APupilCodesEnsurerTests
{
    [Fact]
    public async Task Ensure_generates_at_least_ten_codes_for_leraar_groep78_class_8B()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new JobsyDbContext(options);

        var schoolId = TestAccountsIds.School;
        var teacherId = Guid.Parse("bbbbbbbb-3333-3333-3333-333333333333");
        db.Schools.Add(new School
        {
            Id = schoolId,
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
            Email = AcceptatieScholenClass2APupilCodesEnsurer.LeraarEmail,
            FullName = "Demo Leraar",
            Role = UserRole.Teacher,
            SchoolId = schoolId,
            IsActive = true,
            AuthenticatorEnabled = true,
            SessionVersion = 0
        });
        db.SchoolClasses.Add(new SchoolClass
        {
            Id = TestAccountsIds.SchoolClassGroep78,
            SchoolId = schoolId,
            Name = DemoSchoolIds.ClassGroep78Name,
            Level = SchoolLevel.Groep78,
            QuestionSet = PupilQuestionSet.Groep78,
            Year = 8,
            SchoolYearStart = SchoolYear.Current(DateOnly.FromDateTime(DateTime.UtcNow)),
            PupilCount = AcceptatieScholenClass2APupilCodesEnsurer.MinimumPupilCodes,
            IsTestData = true,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Lobsy:DeploymentEnvironment"] = "Acceptatie",
                ["JobsyFeatures:SchoolsEnabled"] = "true",
                ["Scholen:CodeHmacKey"] = "test-hmac-key-for-scholen-unit-tests"
            })
            .Build();

        var dp = DataProtectionProvider.Create(Path.Combine(Path.GetTempPath(), "scholen-dp-" + Guid.NewGuid()));
        var codes = new PupilCodeService(
            db,
            dp,
            config,
            new FakeHostEnvironment());
        var applied = await AcceptatieScholenClass2APupilCodesEnsurer.TryEnsureAsync(
            db,
            codes,
            config,
            NullLogger.Instance);

        Assert.True(applied);
        var count = await db.PupilCodes.CountAsync(c => c.SchoolClassId == TestAccountsIds.SchoolClassGroep78);
        Assert.Equal(AcceptatieScholenClass2APupilCodesEnsurer.MinimumPupilCodes, count);
    }

    [Fact]
    public async Task Demo_college_creates_groep78_8B_when_only_vmbo_2A_exists()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new JobsyDbContext(options);
        var now = DateTime.UtcNow;
        var yearStart = SchoolYear.Current(DateOnly.FromDateTime(now));

        db.Schools.Add(new School
        {
            Id = DemoSchoolIds.SchoolId,
            Name = "Demo College Lobsy",
            City = "Naaldwijk",
            AllowedEmailDomains = """["jobsy.local"]""",
            IsActive = true,
            IsTestData = true,
            CreatedAtUtc = now,
            ProcessorAgreementSignedOn = DateOnly.FromDateTime(now),
            ProcessorAgreementVersion = "demo"
        });
        db.Users.Add(new User
        {
            Id = DemoSchoolIds.TeacherUserId,
            Email = AcceptatieScholenClass2APupilCodesEnsurer.LeraarEmail,
            FullName = "Demo Leraar",
            Role = UserRole.Teacher,
            SchoolId = DemoSchoolIds.SchoolId,
            IsActive = true,
            AuthenticatorEnabled = true,
            SessionVersion = 0
        });
        db.SchoolClasses.Add(new SchoolClass
        {
            Id = DemoSchoolIds.Class1AId,
            SchoolId = DemoSchoolIds.SchoolId,
            Name = "2A",
            Level = SchoolLevel.VmboGt,
            QuestionSet = PupilQuestionSet.Vo,
            Year = 2,
            SchoolYearStart = yearStart,
            PupilCount = 5,
            IsTestData = true,
            CreatedAtUtc = now
        });
        db.TeacherClassAssignments.Add(new TeacherClassAssignment
        {
            TeacherUserId = DemoSchoolIds.TeacherUserId,
            SchoolClassId = DemoSchoolIds.Class1AId,
            CreatedAtUtc = now
        });
        await db.SaveChangesAsync();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Lobsy:DeploymentEnvironment"] = "Acceptatie",
                ["JobsyFeatures:SchoolsEnabled"] = "true",
                ["Scholen:CodeHmacKey"] = "test-hmac-key-for-scholen-unit-tests"
            })
            .Build();

        var dp = DataProtectionProvider.Create(Path.Combine(Path.GetTempPath(), "scholen-dp-" + Guid.NewGuid()));
        var codes = new PupilCodeService(db, dp, config, new FakeHostEnvironment());
        Assert.True(await AcceptatieScholenClass2APupilCodesEnsurer.TryEnsureAsync(
            db, codes, config, NullLogger.Instance));

        var g78 = await db.SchoolClasses.SingleAsync(c => c.Id == DemoSchoolIds.ClassGroep78Id);
        Assert.Equal(DemoSchoolIds.ClassGroep78Name, g78.Name);
        Assert.Equal(SchoolLevel.Groep78, g78.Level);
        Assert.Equal(TestWindowState.Open, g78.TestWindow);
        Assert.Equal(AcceptatieScholenClass2APupilCodesEnsurer.MinimumPupilCodes,
            await db.PupilCodes.CountAsync(c => c.SchoolClassId == g78.Id));
        Assert.True(await db.TeacherClassAssignments.AnyAsync(
            a => a.TeacherUserId == DemoSchoolIds.TeacherUserId && a.SchoolClassId == g78.Id));
    }

    [Fact]
    public async Task Ensure_tops_up_to_ten_unused_when_some_codes_are_in_use()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new JobsyDbContext(options);
        var now = DateTime.UtcNow;
        var classId = DemoSchoolIds.ClassGroep78Id;

        db.Schools.Add(new School
        {
            Id = DemoSchoolIds.SchoolId,
            Name = "Demo College Lobsy",
            City = "Naaldwijk",
            AllowedEmailDomains = """["jobsy.local"]""",
            IsActive = true,
            IsTestData = true,
            CreatedAtUtc = now,
            ProcessorAgreementSignedOn = DateOnly.FromDateTime(now),
            ProcessorAgreementVersion = "demo"
        });
        db.Users.Add(new User
        {
            Id = DemoSchoolIds.TeacherUserId,
            Email = AcceptatieScholenClass2APupilCodesEnsurer.LeraarEmail,
            FullName = "Demo Leraar",
            Role = UserRole.Teacher,
            SchoolId = DemoSchoolIds.SchoolId,
            IsActive = true,
            AuthenticatorEnabled = true,
            SessionVersion = 0
        });
        db.SchoolClasses.Add(new SchoolClass
        {
            Id = classId,
            SchoolId = DemoSchoolIds.SchoolId,
            Name = DemoSchoolIds.ClassGroep78Name,
            Level = SchoolLevel.Groep78,
            QuestionSet = PupilQuestionSet.Groep78,
            Year = 8,
            SchoolYearStart = SchoolYear.Current(DateOnly.FromDateTime(now)),
            PupilCount = 7,
            TestWindow = TestWindowState.Open,
            IsTestData = true,
            CreatedAtUtc = now
        });
        for (var n = 1; n <= 7; n++)
        {
            var codeId = Guid.NewGuid();
            db.PupilCodes.Add(new PupilCode
            {
                Id = codeId,
                SchoolClassId = classId,
                Number = n,
                CodeLookupHash = $"hash-{n}",
                CodeProtected = "protected",
                Status = n <= 3 ? PupilCodeStatus.InProgress : PupilCodeStatus.NotStarted,
                CreatedAtUtc = now
            });
            if (n <= 3)
            {
                db.PupilProgresses.Add(new PupilProgress
                {
                    PupilCodeId = codeId,
                    CurrentIndex = 1,
                    AnswersJson = "{}",
                    StartedAtUtc = now,
                    UpdatedAtUtc = now
                });
            }
        }

        await db.SaveChangesAsync();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Lobsy:DeploymentEnvironment"] = "Acceptatie",
                ["JobsyFeatures:SchoolsEnabled"] = "true",
                ["Scholen:CodeHmacKey"] = "test-hmac-key-for-scholen-unit-tests"
            })
            .Build();

        var dp = DataProtectionProvider.Create(Path.Combine(Path.GetTempPath(), "scholen-dp-" + Guid.NewGuid()));
        var codes = new PupilCodeService(db, dp, config, new FakeHostEnvironment());
        Assert.True(await AcceptatieScholenClass2APupilCodesEnsurer.TryEnsureAsync(
            db, codes, config, NullLogger.Instance));

        var unused = await AcceptatieScholenClass2APupilCodesEnsurer.CountUnusedCodesAsync(
            db, classId, CancellationToken.None);
        Assert.Equal(AcceptatieScholenClass2APupilCodesEnsurer.MinimumPupilCodes, unused);
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
