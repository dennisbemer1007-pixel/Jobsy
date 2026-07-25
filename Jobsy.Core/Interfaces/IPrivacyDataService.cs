using System.Security.Claims;

namespace Jobsy.Core.Interfaces;

public interface IPrivacyDataService
{
    Task<object> ExportAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);

    Task DeleteOrAnonymizeAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
}
