using System.Security.Claims;
using Jobsy.Core.Authorization;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Scholen;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Scholen;

public sealed class SchoolScopeService : ISchoolScopeService
{
    private readonly JobsyDbContext _db;
    private readonly IPlatformFeatureService _features;

    public SchoolScopeService(JobsyDbContext db, IPlatformFeatureService features)
    {
        _db = db;
        _features = features;
    }

    public Guid GetSchoolIdOrThrow(ClaimsPrincipal user)
    {
        var claim = user.FindFirst(JobsyClaimTypes.SchoolId)?.Value;
        if (Guid.TryParse(claim, out var fromClaim) && fromClaim != Guid.Empty)
        {
            return fromClaim;
        }

        var userId = GetUserId(user)
            ?? throw new UnauthorizedAccessException("Gebruiker niet gevonden.");

        var schoolId = _db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.SchoolId)
            .FirstOrDefault();

        if (schoolId is null || schoolId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Geen school gekoppeld aan dit account.");
        }

        return schoolId.Value;
    }

    public async Task<bool> CanManageClassAsync(
        ClaimsPrincipal user,
        Guid classId,
        CancellationToken cancellationToken = default)
    {
        if (!RoleClaimMatching.HasRole(user, JobsyRoles.SchoolAdmin))
        {
            return false;
        }

        var schoolId = GetSchoolIdOrThrow(user);
        return await _db.SchoolClasses.AsNoTracking()
            .AnyAsync(c => c.Id == classId && c.SchoolId == schoolId, cancellationToken);
    }

    public async Task<bool> CanTeachClassAsync(
        ClaimsPrincipal user,
        Guid classId,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return false;
        }

        // D2: SchoolAdmin counts only when assigned as teacher of that class.
        return await _db.TeacherClassAssignments.AsNoTracking()
            .AnyAsync(a => a.TeacherUserId == userId && a.SchoolClassId == classId, cancellationToken);
    }

    public async Task<bool> CanSeeClassTotalsAsync(
        ClaimsPrincipal user,
        Guid classId,
        CancellationToken cancellationToken = default)
    {
        if (await CanManageClassAsync(user, classId, cancellationToken))
        {
            return true;
        }

        return await CanTeachClassAsync(user, classId, cancellationToken);
    }

    public async Task<bool> CanSeePerCodeShortResultAsync(
        ClaimsPrincipal user,
        Guid classId,
        CancellationToken cancellationToken = default)
    {
        if (await CanTeachClassAsync(user, classId, cancellationToken))
        {
            return true;
        }

        if (!await CanManageClassAsync(user, classId, cancellationToken))
        {
            return false;
        }

        var snap = await _features.GetAsync(cancellationToken);
        return snap.SchoolPerCodeResultsEnabled;
    }

    public async Task<bool> CanSeePerCodeDetailAsync(
        ClaimsPrincipal user,
        Guid classId,
        CancellationToken cancellationToken = default)
        => await CanTeachClassAsync(user, classId, cancellationToken);

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var raw = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
