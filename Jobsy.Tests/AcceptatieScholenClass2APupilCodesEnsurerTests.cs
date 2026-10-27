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
    public async Task Ensure_generates_at_least_ten_codes_for_leraar_class_2A()
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
            Name = "2A",
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

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Jobsy.Tests";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
