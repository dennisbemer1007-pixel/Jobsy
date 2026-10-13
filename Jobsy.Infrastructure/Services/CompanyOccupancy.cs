using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Helpers for "already on Lobsy" occupancy (07.6 / 07.7): a company is managed when it has
/// active employer users other than intermediaries.
/// </summary>
public static class CompanyOccupancy
{
    public static readonly UserRole[] ManagingEmployerRoles =
    [
        UserRole.BranchManager,
        UserRole.RegionalManager,
        UserRole.EnterpriseManager
    ];

    public static async Task<HashSet<Guid>> LoadManagedCompanyIdsAsync(
        JobsyDbContext db,
        CancellationToken cancellationToken = default)
    {
        var fromPrimary = await db.Users.AsNoTracking()
            .Where(u => u.IsActive && ManagingEmployerRoles.Contains(u.Role) && u.CompanyId != null)
            .Select(u => u.CompanyId!.Value)
            .ToListAsync(cancellationToken);

        var fromMembership = await (
            from u in db.Users.AsNoTracking()
            where u.IsActive && ManagingEmployerRoles.Contains(u.Role)
            from m in u.CompanyMemberships
            select m.CompanyId).ToListAsync(cancellationToken);

        return fromPrimary.Concat(fromMembership).ToHashSet();
    }

    public static async Task<bool> HasActiveManagingEmployersAsync(
        JobsyDbContext db,
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var ids = await ExpandCompanyTreeAsync(db, companyId, cancellationToken);
        var primaryHit = await db.Users.AsNoTracking()
            .AnyAsync(
                u => u.IsActive
                     && ManagingEmployerRoles.Contains(u.Role)
                     && u.CompanyId != null
                     && ids.Contains(u.CompanyId.Value),
                cancellationToken);
        if (primaryHit)
        {
            return true;
        }

        return await db.Users.AsNoTracking()
            .Where(u => u.IsActive && ManagingEmployerRoles.Contains(u.Role))
            .AnyAsync(u => u.CompanyMemberships.Any(m => ids.Contains(m.CompanyId)), cancellationToken);
    }

    public static async Task<HashSet<Guid>> ExpandCompanyTreeAsync(
        JobsyDbContext db,
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var company = await db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company is null)
        {
            return [];
        }

        var rootId = company.ParentCompanyId ?? company.Id;
        var ids = await db.Companies.AsNoTracking()
            .Where(c => c.Id == rootId || c.ParentCompanyId == rootId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);
        return ids.ToHashSet();
    }

    public static bool IsManagingEmployer(UserRole role)
        => ManagingEmployerRoles.Contains(role);

    public static bool IsRevocableOnTakeover(UserRole role)
        => JobsyRoles.IsEmployer(role) && role != UserRole.Intermediary;
}
