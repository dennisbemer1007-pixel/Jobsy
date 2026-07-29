using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.ValueObjects;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class VacancyDraftCreationServiceTests
{
    [Fact]
    public async Task CreateDraft_from_csv_sets_source_and_stays_draft()
    {
        await using var db = CreateDb();
        var companyId = Guid.NewGuid();
        var tableId = Guid.NewGuid();
        SeedCompanyWithTable(db, companyId, tableId);
        await db.SaveChangesAsync();

        var sut = new VacancyDraftCreationService(db, new AlwaysOkSalary(), new AllowAllModeration());
        var result = await sut.CreateDraftAsync(
            new VacancyDraftInput(
                companyId,
                "CSV Kassière",
                "Omschrijving via CSV import",
                0,
                DateOnly.FromDateTime(DateTime.UtcNow),
                DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
                TransportMode.Bike,
                ["Winkel"],
                tableId),
            VacancySource.Csv);

        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.NotNull(result.Vacancy);
        Assert.Equal(VacancyStatus.Draft, result.Vacancy!.Status);
        Assert.Equal(VacancySource.Csv, result.Vacancy.CreatedVia);
        Assert.Equal(14.50m, result.Vacancy.HourlyWage);
    }

    [Fact]
    public async Task CreateDraft_rejects_empty_title_without_persisting()
    {
        await using var db = CreateDb();
        var companyId = Guid.NewGuid();
        var tableId = Guid.NewGuid();
        SeedCompanyWithTable(db, companyId, tableId);
        await db.SaveChangesAsync();

        var sut = new VacancyDraftCreationService(db, new AlwaysOkSalary(), new AllowAllModeration());
        var result = await sut.CreateDraftAsync(
            new VacancyDraftInput(
                companyId,
                "  ",
                "Omschrijving",
                0,
                DateOnly.FromDateTime(DateTime.UtcNow),
                DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
                TransportMode.Bike,
                ["Winkel"],
                tableId),
            VacancySource.Csv);

        Assert.False(result.Succeeded);
        Assert.Equal(0, await db.Vacancies.CountAsync());
    }

    [Fact]
    public async Task CreateDraft_accepts_base64_image()
    {
        await using var db = CreateDb();
        var companyId = Guid.NewGuid();
        var tableId = Guid.NewGuid();
        SeedCompanyWithTable(db, companyId, tableId);
        await db.SaveChangesAsync();

        var png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";
        var sut = new VacancyDraftCreationService(db, new AlwaysOkSalary(), new AllowAllModeration());
        var result = await sut.CreateDraftAsync(
            new VacancyDraftInput(
                companyId,
                "Met foto",
                "Omschrijving",
                0,
                DateOnly.FromDateTime(DateTime.UtcNow),
                DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
                TransportMode.Bike,
                ["Winkel"],
                tableId,
                ImageUrl: png),
            VacancySource.Api);

        Assert.True(result.Succeeded, result.ErrorMessage);
        Assert.StartsWith("data:image/png;base64,", result.Vacancy!.ImageUrl);
        Assert.Equal(VacancySource.Api, result.Vacancy.CreatedVia);
        Assert.Equal(VacancyStatus.Draft, result.Vacancy.Status);
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase("CsvDraft-" + Guid.NewGuid())
            .Options;
        return new JobsyDbContext(options);
    }

    private static void SeedCompanyWithTable(JobsyDbContext db, Guid companyId, Guid tableId)
    {
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Test Org",
            KvkNumber = "12345678",
            Address = "Straat 1",
            Location = new GeoPoint(52, 4),
            CsvBatchImportEnabled = true
        });

        var table = new CompanySalaryTable
        {
            Id = tableId,
            CompanyId = companyId,
            Name = "WML",
            IsActive = true,
            IsSystemWml = true
        };
        table.Rates.Add(new CompanySalaryRate
        {
            Id = Guid.NewGuid(),
            SalaryTableId = tableId,
            AgeYears = 21,
            HourlyRate = 14.50m
        });
        db.CompanySalaryTables.Add(table);
    }

    private sealed class AlwaysOkSalary : ISalaryService
    {
        public bool MeetsMinimumWage(decimal hourlyWage, int ageYears) => hourlyWage > 0;
        public decimal GetMinimumHourlyWage(int ageYears) => 14m;
    }

    private sealed class AllowAllModeration : IVacancyContentModerationService
    {
        public Task<VacancyContentModerationResult> CheckAsync(
            string title,
            string description,
            CancellationToken cancellationToken = default)
            => Task.FromResult(VacancyContentModerationResult.Allowed());
    }
}
