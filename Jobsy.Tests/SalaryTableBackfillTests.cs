using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Infrastructure.Data;
using Jobsy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Tests;

public class SalaryTableBackfillTests
{
    [Fact]
    public async Task FillEmptySalaryTables_copies_wml_age_rates()
    {
        await using var db = CreateDb();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.MinimumWageRates.AddRange(
            new MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 15, HourlyRate = 4.22m, Label = "15", EffectiveFrom = today },
            new MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 21, HourlyRate = 14.06m, Label = "21+", EffectiveFrom = today });

        var companyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Lege Tabel BV",
            KvkNumber = "87654321",
            Address = "Test 1",
            Type = CompanyType.Employer,
            Location = new Jobsy.Core.ValueObjects.GeoPoint(52, 4)
        });

        var emptyTableId = Guid.NewGuid();
        db.CompanySalaryTables.Add(new CompanySalaryTable
        {
            Id = emptyTableId,
            CompanyId = companyId,
            Name = "Lege CAO",
            IsActive = true,
            IsSystemWml = false
        });
        await db.SaveChangesAsync();

        await WmlSalaryTableService.FillEmptySalaryTablesAsync(db);

        var rates = await db.CompanySalaryRates
            .Where(r => r.SalaryTableId == emptyTableId)
            .OrderBy(r => r.AgeYears)
            .ToListAsync();
        Assert.Equal(2, rates.Count);
        Assert.Equal(4.22m, rates[0].HourlyRate);
        Assert.Equal(14.06m, rates[1].HourlyRate);
    }

    [Fact]
    public async Task FillEmptySalaryTables_adds_missing_age_bands_without_overwriting()
    {
        await using var db = CreateDb();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.MinimumWageRates.AddRange(
            new MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 15, HourlyRate = 4.22m, Label = "15", EffectiveFrom = today },
            new MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 16, HourlyRate = 4.85m, Label = "16", EffectiveFrom = today },
            new MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 17, HourlyRate = 5.55m, Label = "17", EffectiveFrom = today },
            new MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 18, HourlyRate = 7.03m, Label = "18", EffectiveFrom = today },
            new MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 19, HourlyRate = 8.44m, Label = "19", EffectiveFrom = today },
            new MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 20, HourlyRate = 11.25m, Label = "20", EffectiveFrom = today },
            new MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 21, HourlyRate = 14.06m, Label = "21+", EffectiveFrom = today });

        var companyId = Guid.NewGuid();
        db.Companies.Add(new Company
        {
            Id = companyId,
            Name = "Incomplete CAO BV",
            KvkNumber = "99887766",
            Address = "Test 3",
            Type = CompanyType.Employer,
            Location = new Jobsy.Core.ValueObjects.GeoPoint(52.2, 4.2)
        });

        var tableId = Guid.NewGuid();
        db.CompanySalaryTables.Add(new CompanySalaryTable
        {
            Id = tableId,
            CompanyId = companyId,
            Name = "CAO alleen 21+",
            IsActive = true,
            IsSystemWml = false
        });
        db.CompanySalaryRates.Add(new CompanySalaryRate
        {
            Id = Guid.NewGuid(),
            SalaryTableId = tableId,
            AgeYears = 21,
            HourlyRate = 16.50m,
            Label = "21+"
        });
        await db.SaveChangesAsync();

        await WmlSalaryTableService.FillEmptySalaryTablesAsync(db);

        var rates = await db.CompanySalaryRates
            .Where(r => r.SalaryTableId == tableId)
            .OrderBy(r => r.AgeYears)
            .ToListAsync();
        Assert.Equal(7, rates.Count);
        Assert.Equal(4.22m, rates.Single(r => r.AgeYears == 15).HourlyRate);
        Assert.Equal(16.50m, rates.Single(r => r.AgeYears == 21).HourlyRate);
    }

    [Fact]
    public async Task EnsureForAll_creates_wml_with_rates_for_new_org()
    {
        await using var db = CreateDb();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.MinimumWageRates.Add(new MinimumWageRate
        {
            Id = Guid.NewGuid(),
            AgeYears = 21,
            HourlyRate = 14.06m,
            Label = "21+",
            EffectiveFrom = today
        });
        db.Companies.Add(new Company
        {
            Id = Guid.NewGuid(),
            Name = "Nieuwe Org",
            KvkNumber = "11223344",
            Address = "Test 2",
            Type = CompanyType.Employer,
            Location = new Jobsy.Core.ValueObjects.GeoPoint(52.1, 4.1)
        });
        await db.SaveChangesAsync();

        await WmlSalaryTableService.EnsureForAllCompaniesAsync(db);

        var table = await db.CompanySalaryTables
            .Include(t => t.Rates)
            .SingleAsync(t => t.IsSystemWml);
        Assert.NotEmpty(table.Rates);
    }

    [Fact]
    public async Task AssignMissingVacancySalaryTables_links_null_vacancies_to_org_wml()
    {
        await using var db = CreateDb();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.MinimumWageRates.AddRange(
            new MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 15, HourlyRate = 4.22m, Label = "15", EffectiveFrom = today },
            new MinimumWageRate { Id = Guid.NewGuid(), AgeYears = 21, HourlyRate = 14.06m, Label = "21+", EffectiveFrom = today });

        var orgId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        db.Companies.AddRange(
            new Company
            {
                Id = orgId,
                Name = "Org BV",
                KvkNumber = "55667788",
                Address = "Org 1",
                Type = CompanyType.Employer,
                Location = new Jobsy.Core.ValueObjects.GeoPoint(52, 4)
            },
            new Company
            {
                Id = branchId,
                Name = "Vestiging",
                KvkNumber = "55667788",
                Address = "Vestiging 1",
                Type = CompanyType.Employer,
                ParentCompanyId = orgId,
                Location = new Jobsy.Core.ValueObjects.GeoPoint(52.01, 4.01)
            });

        var withTableId = Guid.NewGuid();
        var withoutTableId = Guid.NewGuid();
        db.Vacancies.AddRange(
            new Vacancy
            {
                Id = withTableId,
                Title = "Heeft tabel",
                Description = "x",
                HourlyWage = 14m,
                StartDate = today,
                EndDate = today.AddMonths(1),
                Status = VacancyStatus.Active,
                CompanyId = orgId,
                Location = new Jobsy.Core.ValueObjects.GeoPoint(52, 4),
                RequiredTransport = TransportMode.Bike,
                WorkTypes = WorkType.Winkel,
                SalaryTableId = null
            },
            new Vacancy
            {
                Id = withoutTableId,
                Title = "Geen tabel",
                Description = "y",
                HourlyWage = 13m,
                StartDate = today,
                EndDate = today.AddMonths(1),
                Status = VacancyStatus.Active,
                CompanyId = branchId,
                Location = new Jobsy.Core.ValueObjects.GeoPoint(52.01, 4.01),
                RequiredTransport = TransportMode.Bike,
                WorkTypes = WorkType.Horeca,
                SalaryTableId = null
            });
        await db.SaveChangesAsync();

        await WmlSalaryTableService.EnsureForAllCompaniesAsync(db);

        var wmlId = await db.CompanySalaryTables
            .Where(t => t.IsSystemWml && t.CompanyId == orgId)
            .Select(t => t.Id)
            .SingleAsync();
        var vacancies = await db.Vacancies.OrderBy(v => v.Title).ToListAsync();
        Assert.All(vacancies, v => Assert.Equal(wmlId, v.SalaryTableId));
        Assert.True(await db.CompanySalaryRates.AnyAsync(r => r.SalaryTableId == wmlId));
    }

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }
}
