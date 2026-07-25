using System.Security.Claims;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Interfaces;

/// <summary>
/// Enforces tenant/company data isolation for employer roles (Broken Access Control mitigation).
/// </summary>
public interface ICompanyAuthorizationService
{
    bool IsAdmin(ClaimsPrincipal user);
    bool IsEmployer(ClaimsPrincipal user);
    bool IsCandidate(ClaimsPrincipal user);
    UserRole? GetPrimaryRole(ClaimsPrincipal user);

    /// <summary>
    /// Company IDs the user may access. Admin returns null (= all). Candidate returns empty.
    /// </summary>
    Task<IReadOnlyCollection<Guid>?> GetAccessibleCompanyIdsAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<bool> CanAccessCompanyAsync(
        ClaimsPrincipal user,
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task EnsureCanAccessCompanyAsync(
        ClaimsPrincipal user,
        Guid companyId,
        CancellationToken cancellationToken = default);
}
