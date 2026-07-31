using Jobsy.Core.Entities;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Ensures every employer organization has one system "Wettelijk Minimumloon" salary table
/// that mirrors the current platform minimum-wage rates (updated every half year).
/// Vestigingen reuse the org table; they do not get their own WML copy.
/// </summary>
public static class WmlSalaryTableService
{
    public const string TableName = "Wettelijk Minimumloon";
    public const string LegacyTableName = "WML";

    public static async Task EnsureForCompanyAsync(
        JobsyDbContext db,
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var organizationId = await ResolveOrganizationIdAsync(db, companyId, cancellationToken);
        if (organizationId is null)
        {
            return;
        }

        var rates = await LoadCurrentWmlRatesAsync(db, cancellationToken);
        if (rates.Count == 0)
        {
            return;
        }

        var table = await db.CompanySalaryTables
            .Include(t => t.Rates)
            .FirstOrDefaultAsync(
                t => t.CompanyId == organizationId.Value && t.IsSystemWml,
                cancellationToken);

        // Legacy: table named WML/Wettelijk Minimumloon without the flag yet.
        table ??= await db.CompanySalaryTables
            .Include(t => t.Rates)
            .FirstOrDefaultAsync(
                t => t.CompanyId == organizationId.Value
                     && (t.Name == TableName || t.Name == LegacyTableName),
                cancellationToken);

        if (table is null)
        {
            table = new CompanySalaryTable
            {
                Id = Guid.NewGuid(),
                CompanyId = organizationId.Value,
                Name = TableName,
                IsActive = true,
                IsSystemWml = true
            };
            db.CompanySalaryTables.Add(table);

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

            return;
        }

        // Existing table: keep rates stable on startup (semi-annual sync updates them).
        table.IsActive = true;
        table.IsSystemWml = true;
        table.Name = TableName;
        if (table.CompanyId != organizationId.Value)
        {
            table.CompanyId = organizationId.Value;
        }

        if (table.Rates.Count == 0)
        {
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
    }

    public static async Task EnsureForAllCompaniesAsync(
        JobsyDbContext db,
        CancellationToken cancellationToken = default)
    {
        var organizationIds = await db.Companies
            .AsNoTracking()
            .Where(c => c.ParentCompanyId == null)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        foreach (var organizationId in organizationIds)
        {
            try
            {
                await EnsureForCompanyAsync(db, organizationId, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Startup seed can race with consolidation; next boot is idempotent.
                db.ChangeTracker.Clear();
            }

            db.ChangeTracker.Clear();
        }

        await FillEmptySalaryTablesAsync(db, cancellationToken);
        await AssignMissingVacancySalaryTablesAsync(db, cancellationToken);
    }

    /// <summary>
    /// Links every vacancy without a salary table to its organization's active system WML table
    /// (must already have rates). Idempotent; skips vacancies that already have a table.
    /// </summary>
    public static async Task AssignMissingVacancySalaryTablesAsync(
        JobsyDbContext db,
        CancellationToken cancellationToken = default)
    {
        var missing = await db.Vacancies
            .Include(v => v.Company)
            .Where(v => v.SalaryTableId == null)
            .ToListAsync(cancellationToken);
        if (missing.Count == 0)
        {
            return;
        }

        var orgIds = missing
            .Select(v => v.Company?.ParentCompanyId ?? v.CompanyId)
            .Distinct()
            .ToList();

        foreach (var orgId in orgIds)
        {
            await EnsureForCompanyAsync(db, orgId, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);

        var wmlByOrg = await db.CompanySalaryTables
            .AsNoTracking()
            .Where(t => t.IsSystemWml && t.IsActive && orgIds.Contains(t.CompanyId))
            .Where(t => t.Rates.Any())
            .Select(t => new { t.CompanyId, t.Id })
            .ToListAsync(cancellationToken);

        var tableByOrg = wmlByOrg
            .GroupBy(t => t.CompanyId)
            .ToDictionary(g => g.Key, g => g.OrderBy(t => t.Id).First().Id);

        var assigned = 0;
        foreach (var vacancy in missing)
        {
            var orgId = vacancy.Company?.ParentCompanyId ?? vacancy.CompanyId;
            if (!tableByOrg.TryGetValue(orgId, out var tableId))
            {
                continue;
            }

            vacancy.SalaryTableId = tableId;
            assigned++;
        }

        if (assigned > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Copies current WML age rates into any active salary table that has zero rates.
    /// When <paramref name="organizationId"/> is set, only tables for that org (and its branches) are filled.
    /// </summary>
    public static async Task FillEmptySalaryTablesAsync(
        JobsyDbContext db,
        CancellationToken cancellationToken = default,
        Guid? organizationId = null)
    {
        var rates = await LoadCurrentWmlRatesAsync(db, cancellationToken);
        if (rates.Count == 0)
        {
            return;
        }

        var query = db.CompanySalaryTables
            .Include(t => t.Rates)
            .Where(t => t.IsActive && !t.Rates.Any());

        if (organizationId is Guid orgId)
        {
            var branchIds = await db.Companies
                .AsNoTracking()
                .Where(c => c.Id == orgId || c.ParentCompanyId == orgId)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);
            query = query.Where(t => branchIds.Contains(t.CompanyId));
        }

        var emptyTables = await query.ToListAsync(cancellationToken);

        if (emptyTables.Count == 0)
        {
            return;
        }

        foreach (var table in emptyTables)
        {
            foreach (var rate in rates)
            {
                db.CompanySalaryRates.Add(new CompanySalaryRate
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
            .Include(t => t.Rates)
            .Where(t => t.IsSystemWml || t.Name == TableName || t.Name == LegacyTableName)
            .ToListAsync(cancellationToken);

        foreach (var table in tables)
        {
            var organizationId = await ResolveOrganizationIdAsync(db, table.CompanyId, cancellationToken);
            if (organizationId is null)
            {
                continue;
            }

            table.IsActive = true;
            table.IsSystemWml = true;
            table.Name = TableName;
            table.CompanyId = organizationId.Value;

            if (table.Rates.Count > 0)
            {
                db.CompanySalaryRates.RemoveRange(table.Rates);
                table.Rates.Clear();
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

        // Deduplicate: keep one system WML per organization.
        var duplicates = tables
            .GroupBy(t => t.CompanyId)
            .Where(g => g.Count() > 1)
            .ToList();
        foreach (var group in duplicates)
        {
            var keep = group.OrderBy(t => t.Id).First();
            foreach (var extra in group.Where(t => t.Id != keep.Id))
            {
                var vacancies = await db.Vacancies
                    .Where(v => v.SalaryTableId == extra.Id)
                    .ToListAsync(cancellationToken);
                foreach (var vacancy in vacancies)
                {
                    vacancy.SalaryTableId = keep.Id;
                }

                db.CompanySalaryTables.Remove(extra);
            }
        }

        var withTable = tables
            .Where(t => db.Entry(t).State != EntityState.Deleted)
            .Select(t => t.CompanyId)
            .ToHashSet();
        var missing = await db.Companies
            .AsNoTracking()
            .Where(c => c.ParentCompanyId == null && !withTable.Contains(c.Id))
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        foreach (var organizationId in missing)
        {
            await EnsureForCompanyAsync(db, organizationId, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public static async Task<Guid?> ResolveOrganizationIdAsync(
        JobsyDbContext db,
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var company = await db.Companies
            .AsNoTracking()
            .Where(c => c.Id == companyId)
            .Select(c => new { c.Id, c.ParentCompanyId })
            .FirstOrDefaultAsync(cancellationToken);
        if (company is null)
        {
            return null;
        }

        return company.ParentCompanyId ?? company.Id;
    }

    /// <summary>
    /// Returns true when a vestiging may use the given organization salary table.
    /// System WML is always allowed for any vestiging under the same organization.
    /// </summary>
    public static bool IsAllowedForBranch(CompanySalaryTable table, Guid branchCompanyId, Guid organizationId)
    {
        if (!table.IsActive || table.CompanyId != organizationId)
        {
            return false;
        }

        if (table.IsSystemWml)
        {
            return true;
        }

        return table.AllowedBranches.Any(b => b.CompanyId == branchCompanyId);
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
