using Jobsy.Core.Entities;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Ensures every employer company has a default "WML" salary table that mirrors
/// the current platform minimum-wage rates (updated every half year).
/// </summary>
public static class WmlSalaryTableService
{
    public const string TableName = "WML";

    public static async Task EnsureForCompanyAsync(
        JobsyDbContext db,
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var rates = await LoadCurrentWmlRatesAsync(db, cancellationToken);
        if (rates.Count == 0)
        {
            return;
        }

        var table = await db.CompanySalaryTables
            .FirstOrDefaultAsync(
                t => t.CompanyId == companyId
                     && t.Name == TableName,
                cancellationToken);

        if (table is null)
        {
            table = new CompanySalaryTable
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Name = TableName,
                IsActive = true
            };
            db.CompanySalaryTables.Add(table);
        }
        else
        {
            table.IsActive = true;
            await db.CompanySalaryRates
                .Where(r => r.SalaryTableId == table.Id)
                .ExecuteDeleteAsync(cancellationToken);
        }

        foreach (var rate in rates)
        {
            table.Rates.Add(new CompanySalaryRate
            {
                Id = Guid.NewGuid(),
                SalaryTableId = table.Id,
                AgeYears = rate.AgeYears,
                HourlyRate = rate.HourlyRate,
                Label = string.IsNullOrWhiteSpace(rate.Label)
                    ? (rate.AgeYears >= 21 ? "21+" : rate.AgeYears.ToString())
                    : rate.Label
            });
        }
    }

    public static async Task EnsureForAllCompaniesAsync(
        JobsyDbContext db,
        CancellationToken cancellationToken = default)
    {
        var companyIds = await db.Companies
            .AsNoTracking()
            .Where(c => c.KvkEstablishmentId != null || c.ParentCompanyId == null)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        foreach (var companyId in companyIds)
        {
            await EnsureForCompanyAsync(db, companyId, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public static async Task SyncAllWmlTablesAsync(
        JobsyDbContext db,
        CancellationToken cancellationToken = default)
    {
        var rates = await LoadCurrentWmlRatesAsync(db, cancellationToken);
        if (rates.Count == 0)
        {
            return;
        }

        var tables = await db.CompanySalaryTables
            .Where(t => t.Name == TableName)
            .ToListAsync(cancellationToken);

        foreach (var table in tables)
        {
            table.IsActive = true;
            await db.CompanySalaryRates
                .Where(r => r.SalaryTableId == table.Id)
                .ExecuteDeleteAsync(cancellationToken);

            foreach (var rate in rates)
            {
                table.Rates.Add(new CompanySalaryRate
                {
                    Id = Guid.NewGuid(),
                    SalaryTableId = table.Id,
                    AgeYears = rate.AgeYears,
                    HourlyRate = rate.HourlyRate,
                    Label = string.IsNullOrWhiteSpace(rate.Label)
                        ? (rate.AgeYears >= 21 ? "21+" : rate.AgeYears.ToString())
                        : rate.Label
                });
            }
        }

        // Also create WML tables for companies that somehow missed them.
        var withTable = tables.Select(t => t.CompanyId).ToHashSet();
        var missing = await db.Companies
            .AsNoTracking()
            .Where(c => !withTable.Contains(c.Id))
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        foreach (var companyId in missing)
        {
            await EnsureForCompanyAsync(db, companyId, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<List<MinimumWageRate>> LoadCurrentWmlRatesAsync(
        JobsyDbContext db,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var all = await db.MinimumWageRates
            .AsNoTracking()
            .Where(r => r.EffectiveFrom <= today)
            .ToListAsync(cancellationToken);

        return all
            .GroupBy(r => r.AgeYears)
            .Select(g => g.OrderByDescending(r => r.EffectiveFrom).First())
            .OrderBy(r => r.AgeYears)
            .ToList();
    }
}
