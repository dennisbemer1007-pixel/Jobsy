using System.Security.Claims;
using Jobsy.Core.Entities;

namespace Jobsy.Core.Interfaces;

public interface IUserLookupService
{
    Task<User?> FindByPrincipalAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
}
