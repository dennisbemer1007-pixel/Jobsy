using Jobsy.Core.Entities;

namespace Jobsy.Core.Interfaces;

public interface ISalesManagerInviteService
{
    Task<SalesManagerInviteResult> InviteAsync(
        string email,
        string fullName,
        CancellationToken cancellationToken = default);
}

public sealed record SalesManagerInviteResult(
    Guid UserId,
    string Email,
    string FullName,
    string TemporaryPassword,
    bool CreatedNewUser);
