namespace Jobsy.Core.Interfaces;

public interface IAmbassadeurInviteService
{
    Task<AmbassadeurInviteResult> InviteAsync(
        string email,
        string fullName,
        CancellationToken cancellationToken = default);
}

public sealed record AmbassadeurInviteResult(
    Guid UserId,
    string Email,
    string FullName,
    string TemporaryPassword,
    bool CreatedNew);
