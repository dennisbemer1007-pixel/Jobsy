using System.Security.Claims;

namespace Jobsy.Core.Scholen;

/// <summary>Single source of truth for §R school/teacher authorization checks.</summary>
public interface ISchoolScopeService
{
    Guid GetSchoolIdOrThrow(ClaimsPrincipal user);

    Task<bool> CanManageClassAsync(ClaimsPrincipal user, Guid classId, CancellationToken cancellationToken = default);

    /// <summary>TeacherClassAssignment exists; SchoolAdmin counts only if assigned (D2).</summary>
    Task<bool> CanTeachClassAsync(ClaimsPrincipal user, Guid classId, CancellationToken cancellationToken = default);

    Task<bool> CanSeeClassTotalsAsync(ClaimsPrincipal user, Guid classId, CancellationToken cancellationToken = default);

    /// <summary>Teacher-of-class, or SchoolAdmin when <c>SchoolPerCodeResultsEnabled</c>.</summary>
    Task<bool> CanSeePerCodeShortResultAsync(ClaimsPrincipal user, Guid classId, CancellationToken cancellationToken = default);

    /// <summary>Teacher-of-class only (story / likes / PDF).</summary>
    Task<bool> CanSeePerCodeDetailAsync(ClaimsPrincipal user, Guid classId, CancellationToken cancellationToken = default);
}
