using Jobsy.Api.Models;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Admin;

/// <summary>
/// Builds admin user aggregates without loading every user row into memory.
/// Week buckets still project only <c>TermsAcceptedAt</c> so the Sunday-week algorithm
/// stays identical on InMemory and Npgsql.
/// </summary>
public static class AdminUsersAggregatesQuery
{
    public static async Task<AdminUsersAggregateDto> QueryAsync(
        JobsyDbContext db,
        CancellationToken cancellationToken = default)
    {
        var byRoleRows = await db.Users.AsNoTracking()
            .GroupBy(u => u.Role)
            .Select(g => new { Role = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var byRole = byRoleRows.ToDictionary(r => r.Role.ToString(), r => r.Count);

        var activeCount = await db.Users.AsNoTracking()
            .CountAsync(u => u.IsActive, cancellationToken);
        var inactiveCount = await db.Users.AsNoTracking()
            .CountAsync(u => !u.IsActive, cancellationToken);

        // Project only the columns needed, then group in-memory so InMemory + Npgsql stay aligned.
        var companyRows = await db.Users.AsNoTracking()
            .Where(u => u.CompanyId != null)
            .Select(u => new { u.CompanyId, Name = u.Company != null ? u.Company.Name : "—" })
            .ToListAsync(cancellationToken);
        var topCompanies = companyRows
            .GroupBy(u => new { u.CompanyId, u.Name })
            .Select(g => new AdminUsersCompanyCountDto(g.Key.CompanyId, g.Key.Name, g.Count()))
            .OrderByDescending(x => x.Count)
            .Take(20)
            .ToList();

        var stamps = await db.Users.AsNoTracking()
            .Select(u => u.TermsAcceptedAt)
            .ToListAsync(cancellationToken);
        var byWeek = BuildWeekBuckets(stamps);

        return new AdminUsersAggregateDto(byRole, topCompanies, activeCount, inactiveCount, byWeek);
    }

    /// <summary>Legacy in-memory algorithm kept for parity tests.</summary>
    public static AdminUsersAggregateDto FromLoadedRows(
        IReadOnlyList<(Jobsy.Core.Enums.UserRole Role, Guid? CompanyId, string? CompanyName, bool IsActive, DateTime? TermsAcceptedAt)> rows)
    {
        var byRole = rows
            .GroupBy(u => u.Role.ToString())
            .ToDictionary(g => g.Key, g => g.Count());
        var activeCount = rows.Count(u => u.IsActive);
        var inactiveCount = rows.Count - activeCount;
        var topCompanies = rows
            .Where(u => u.CompanyId != null)
            .GroupBy(u => new { u.CompanyId, Name = u.CompanyName ?? "—" })
            .Select(g => new AdminUsersCompanyCountDto(g.Key.CompanyId, g.Key.Name, g.Count()))
            .OrderByDescending(x => x.Count)
            .Take(20)
            .ToList();
        var byWeek = BuildWeekBuckets(rows.Select(u => u.TermsAcceptedAt).ToList());
        return new AdminUsersAggregateDto(byRole, topCompanies, activeCount, inactiveCount, byWeek);
    }

    public static IReadOnlyList<AdminUsersWeekBucketDto> BuildWeekBuckets(IReadOnlyList<DateTime?> stamps)
    {
        return stamps
            .Select(t =>
            {
                var stamp = t ?? DateTime.UnixEpoch;
                var weekStart = stamp.Date.AddDays(-(int)stamp.DayOfWeek);
                return weekStart;
            })
            .GroupBy(d => d)
            .OrderByDescending(g => g.Key)
            .Take(12)
            .Select(g => new AdminUsersWeekBucketDto(g.Key.ToString("yyyy-MM-dd"), g.Count()))
            .ToList();
    }
}
