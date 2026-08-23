using System.Security.Claims;
using Jobsy.Core.Contracts;

namespace Jobsy.Core.Interfaces;

public interface IDashboardRefreshService
{
    Task<DashboardRefreshResultDto> RefreshAsync(
        ClaimsPrincipal user,
        string period,
        Guid? companyId,
        CancellationToken cancellationToken = default);
}
