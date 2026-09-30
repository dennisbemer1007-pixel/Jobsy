using Jobsy.Core.Entities;

namespace Jobsy.Core.Interfaces;

public interface IMfaTrustedDeviceService
{
    Task<(string RawToken, MfaTrustedDevice Row)> CreateAsync(
        Guid userId,
        string? userAgent,
        int sessionVersion,
        CancellationToken cancellationToken = default);

    Task<bool> TryValidateAsync(
        Guid userId,
        string? rawToken,
        int sessionVersion,
        bool authenticatorEnabled,
        CancellationToken cancellationToken = default);

    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<int> CountActiveAsync(Guid userId, CancellationToken cancellationToken = default);
}
