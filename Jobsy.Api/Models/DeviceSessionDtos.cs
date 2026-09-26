namespace Jobsy.Api.Models;

public record CreateDeviceSessionRequest(string? UserAgent = null);

public record DeviceSessionCreatedDto(
    Guid DeviceSessionId,
    string RefreshToken,
    DateTime ExpiresAtUtc,
    string? DeviceName,
    int SessionVersion);

public record DeviceSessionRefreshRequest(string RefreshToken, string? UserAgent = null);

public record DeviceSessionRefreshResponse(
    string RefreshToken,
    Guid DeviceSessionId,
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

public record DeviceSessionListItemDto(
    Guid Id,
    string DeviceName,
    DateTime LastUsedAtUtc,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc,
    bool IsCurrent);

public record SessionValidityDto(int SessionVersion, int MinimumSessionVersion);

public record CreateDeviceHandoffRequest(
    bool RememberDevice = true,
    string? ReturnUrl = null,
    string? UserAgent = null);

public record ProvisionDeviceHandoffRequest(
    string Email,
    bool RememberDevice = true,
    string? ReturnUrl = null,
    string? UserAgent = null);

public record DeviceHandoffCreatedDto(string Code, DateTime ExpiresAtUtc);

public record ExchangeDeviceHandoffRequest(string Code, string? UserAgent = null);

public record DeviceHandoffExchangeResponse(
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
    Guid? DeviceSessionId,
    string? RefreshToken,
    DateTime? DeviceExpiresAtUtc);
