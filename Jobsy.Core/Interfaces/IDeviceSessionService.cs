namespace Jobsy.Core.Interfaces;

public interface IDeviceSessionService
{
    Task<DeviceSessionCreateResult> CreateAsync(
        Guid userId,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<DeviceSessionRotateResult?> RotateAsync(
        string refreshToken,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeviceSessionListItem>> ListAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> RevokeAsync(
        Guid userId,
        Guid deviceSessionId,
        string reason,
        CancellationToken cancellationToken = default);

    Task RevokeAllAsync(
        Guid userId,
        string reason,
        bool bumpSessionVersion = true,
        CancellationToken cancellationToken = default);

    Task TouchLastUsedAsync(Guid deviceSessionId, CancellationToken cancellationToken = default);

    Task<DeviceHandoffCreateResult> CreateHandoffAsync(
        Guid userId,
        bool rememberDevice,
        string? returnUrl,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<DeviceHandoffExchangeResult?> ExchangeHandoffAsync(
        string code,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<SessionValiditySnapshot> GetSessionValidityAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task IncrementSessionVersionAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record DeviceSessionCreateResult(
    Guid DeviceSessionId,
    Guid FamilyId,
    string RefreshToken,
    DateTime ExpiresAtUtc,
    string? DeviceName);

public sealed record DeviceSessionRotateResult(
    Guid UserId,
    Guid DeviceSessionId,
    string RefreshToken,
    DateTime ExpiresAtUtc,
    string Email,
    string FullName,
    string Role,
    Guid? CompanyId,
    IReadOnlyList<Guid> CompanyIds,
    bool ShowCandidateHowTo,
    bool HasCandidateApplications,
    bool HasSalesReferral,
    int SessionVersion,
    string? SessionToken);

public sealed record DeviceSessionListItem(
    Guid Id,
    string DeviceName,
    DateTime LastUsedAtUtc,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc,
    bool IsCurrent);

public sealed record DeviceHandoffCreateResult(string Code, DateTime ExpiresAtUtc);

public sealed record DeviceHandoffExchangeResult(
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    Guid? CompanyId,
    IReadOnlyList<Guid> CompanyIds,
    bool ShowCandidateHowTo,
    bool HasCandidateApplications,
    bool HasSalesReferral,
    int SessionVersion,
    string? SessionToken,
    string? ReturnUrl,
    DeviceSessionCreateResult? DeviceSession);

public sealed record SessionValiditySnapshot(int SessionVersion, int MinimumSessionVersion);
