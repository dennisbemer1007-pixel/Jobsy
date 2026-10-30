using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jobsy.Tests;

public sealed class DemoVacancyTitleDedupeTests
{
    [Fact]
    public async Task EnsureBakkerijBranding_archives_duplicate_vitrine_and_hulp_titles()
    {
        var westland = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var supermarket = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new JobsyDbContext(options);

        foreach (var companyId in new[] { westland, supermarket })
        {
            db.Companies.Add(new Company
            {
                Id = companyId,
                Name = "Demo",
                KvkNumber = "12345678",
                KvkEstablishmentId = "12345678_0001",
                Type = CompanyType.Employer,
                Location = new GeoPoint(52.07, 4.31),
                VerificationStatus = CompanyVerificationStatus.Verified,
                VerificationMethod = CompanyVerificationMethod.AdminCreated,
                VerifiedAtUtc = DateTime.UtcNow,
                VerificationUpdatedAtUtc = DateTime.UtcNow
            });
        }

        for (var i = 0; i < 3; i++)
        {
            db.Vacancies.Add(new Vacancy
            {
                Id = Guid.NewGuid(),
                CompanyId = westland,
                Title = "Vakantiewerk vitrine",
                Description = "x",
                HourlyWage = 14m,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(1),
                Status = VacancyStatus.Active,
                Location = new GeoPoint(52.07, 4.31),
                PublishedAtUtc = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();

        await DemoVacancyTitleDedupe.ArchiveDuplicateTitlesAsync(
            db,
            [westland, supermarket],
            NullLogger.Instance);

        Assert.Equal(1, await db.Vacancies.CountAsync(v =>
            v.Title == "Vakantiewerk vitrine" && v.Status == VacancyStatus.Active));
        Assert.Equal(2, await db.Vacancies.CountAsync(v =>
            v.Title == "Vakantiewerk vitrine" && v.Status == VacancyStatus.Archived));
    }

    [Fact]
    public async Task ArchiveDuplicateTitles_archives_extra_active_rows_for_any_title()
    {
        var westland = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var db = new JobsyDbContext(options);
        db.Companies.Add(new Company
        {
            Id = westland,
            Name = "Demo",
            KvkNumber = "12345678",
            KvkEstablishmentId = "12345678_0001",
            Type = CompanyType.Employer,
            Location = new GeoPoint(52.07, 4.31),
            VerificationStatus = CompanyVerificationStatus.Verified,
            VerificationMethod = CompanyVerificationMethod.AdminCreated,
            VerifiedAtUtc = DateTime.UtcNow,
            VerificationUpdatedAtUtc = DateTime.UtcNow
        });
        for (var i = 0; i < 4; i++)
        {
            db.Vacancies.Add(new Vacancy
            {
                Id = Guid.NewGuid(),
                CompanyId = westland,
                Title = "Medewerker bakkerij",
                Description = "x",
                HourlyWage = 14m,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(1),
                Status = VacancyStatus.Active,
                Location = new GeoPoint(52.07, 4.31),
                PublishedAtUtc = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
        await DemoVacancyTitleDedupe.ArchiveDuplicateTitlesAsync(db, [westland], NullLogger.Instance);
        Assert.Equal(1, await db.Vacancies.CountAsync(v =>
            v.Title == "Medewerker bakkerij" && v.Status == VacancyStatus.Active));
        Assert.Equal(3, await db.Vacancies.CountAsync(v =>
            v.Title == "Medewerker bakkerij" && v.Status == VacancyStatus.Archived));
    }
}
