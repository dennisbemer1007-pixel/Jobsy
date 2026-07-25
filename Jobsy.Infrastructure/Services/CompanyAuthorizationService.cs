using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Core.Exceptions;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public class CompanyAuthorizationService : ICompanyAuthorizationService
{
    private readonly JobsyDbContext _db;

    public CompanyAuthorizationService(JobsyDbContext db)
    {
        _db = db;
    }

    public bool IsAdmin(ClaimsPrincipal user) =>
        RoleClaimMatching.HasRole(user, JobsyRoles.Admin);

    public bool IsEmployer(ClaimsPrincipal user) =>
        RoleClaimMatching.HasAnyRole(user, JobsyRoles.EmployerRoles);

    public bool IsCandidate(ClaimsPrincipal user) =>
        RoleClaimMatching.HasRole(user, JobsyRoles.Candidate);

    public UserRole? GetPrimaryRole(ClaimsPrincipal user)
    {
        foreach (UserRole role in Enum.GetValues<UserRole>().Reverse())
        {
            if (RoleClaimMatching.HasRole(user, role.ToString()))
            {
                return role;
            }
        }

        return null;
    }

    public async Task<IReadOnlyCollection<Guid>?> GetAccessibleCompanyIdsAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (IsAdmin(user))
        {
            return null; // all companies
        }

        if (!IsEmployer(user))
        {
            return Array.Empty<Guid>();
        }

        // DB is the source of truth — never trust client-supplied company claim IDs alone.
        var dbIds = await ResolveCompanyIdsFromDatabaseAsync(user, cancellationToken);
        if (dbIds.Count == 0)
        {
            return Array.Empty<Guid>();
        }

        var claimed = ReadCompanyIdsFromClaims(user);
        if (claimed.Count == 0)
        {
            return dbIds;
        }

        // Claims may only narrow access to a subset of DB memberships.
        var narrowed = claimed.Where(dbIds.Contains).ToHashSet();

        // Re-expand children after claim intersection so new vestigingen under an
        // accessible parent remain reachable without re-login.
        if (GetPrimaryRole(user) == UserRole.EnterpriseManager && narrowed.Count > 0)
        {
            await ExpandChildCompanyIdsAsync(narrowed, cancellationToken);
        }

        return narrowed.ToList();
    }

    public async Task<bool> CanAccessCompanyAsync(
        ClaimsPrincipal user,
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var accessible = await GetAccessibleCompanyIdsAsync(user, cancellationToken);
        return accessible is null || accessible.Contains(companyId);
    }

    public async Task EnsureCanAccessCompanyAsync(
        ClaimsPrincipal user,
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        if (!await CanAccessCompanyAsync(user, companyId, cancellationToken))
        {
            throw new ForbiddenCompanyAccessException(companyId);
        }
    }

    private async Task<List<Guid>> ResolveCompanyIdsFromDatabaseAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var email = user.FindFirst(ClaimTypes.Email)?.Value
                    ?? user.FindFirst("preferred_username")?.Value
                    ?? user.Identity?.Name;

        if (string.IsNullOrWhiteSpace(email))
        {
            return [];
        }

        var dbUser = await _db.Users
            .AsNoTracking()
            .Include(u => u.CompanyMemberships)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (dbUser is null || !dbUser.IsActive)
        {
            return [];
        }

        // Branch managers are strictly limited to their primary company.
        if (dbUser.Role == UserRole.BranchManager)
        {
            return dbUser.CompanyId.HasValue ? [dbUser.CompanyId.Value] : [];
        }

        var ids = new HashSet<Guid>();
        if (dbUser.CompanyId.HasValue)
        {
            ids.Add(dbUser.CompanyId.Value);
        }

        foreach (var membership in dbUser.CompanyMemberships)
        {
            ids.Add(membership.CompanyId);
        }

        // Enterprise managers see child vestigingen under parent companies they access.
        if (dbUser.Role == UserRole.EnterpriseManager && ids.Count > 0)
        {
            await ExpandChildCompanyIdsAsync(ids, cancellationToken);
        }

        return ids.ToList();
    }

    private async Task ExpandChildCompanyIdsAsync(HashSet<Guid> ids, CancellationToken cancellationToken)
    {
        var frontier = ids.ToList();
        while (frontier.Count > 0)
        {
            var children = await _db.Companies.AsNoTracking()
                .Where(c => c.ParentCompanyId != null && frontier.Contains(c.ParentCompanyId.Value))
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

            frontier = [];
            foreach (var childId in children)
            {
                if (ids.Add(childId))
                {
                    frontier.Add(childId);
                }
            }
        }
    }

    private static List<Guid> ReadCompanyIdsFromClaims(ClaimsPrincipal user)
    {
        var ids = new HashSet<Guid>();

        foreach (var claim in user.FindAll(JobsyClaimTypes.CompanyId))
        {
            if (Guid.TryParse(claim.Value, out var id))
            {
                ids.Add(id);
            }
        }

        foreach (var claim in user.FindAll(JobsyClaimTypes.CompanyIds))
        {
            foreach (var part in claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (Guid.TryParse(part, out var id))
                {
                    ids.Add(id);
                }
            }
        }

        return ids.ToList();
    }
}
