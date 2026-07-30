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

    private static JobsyDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<JobsyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new JobsyDbContext(options);
    }
}
