namespace Jobsy.Api.Models;

public record AdminAuditItemDto(
    Guid Id,
    DateTime OccurredAtUtc,
    Guid? ActorUserId,
    string ActorRole,
    string ActorKind,
    string ActorDisplayName,
    string Action,
    string TargetType,
    string TargetId,
    string TargetLabel,
    string? Reason,
    string? DetailsJson,
    string Result,
    string CorrelationId);

public record AdminAuditPageDto(
    IReadOnlyList<AdminAuditItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount);

public record AdminAuditSummaryDto(
    bool MaskingEnabled,
    int AdminAuditRetentionDays,
    int PersonalDataAccessLogRetentionDays,
    int PlatformLogRetentionDays,
    DateTime? LastRetentionRunUtc,
    string? LastRetentionDetailsJson,
    int AdminsWithMfa,
    int AdminsTotal,
    int? FailedAdminLogins24h);

public record AdminMfaRoleCountDto(
    string Role,
    int Enrolled,
    int NotEnrolled,
    int ViaIdp,
    int Total);

public record AdminMfaMissingDto(
    Guid UserId,
    string MaskedName,
    string MaskedEmail,
    string Role,
    DateTime? LastActiveUtc);

public record AdminMfaOverviewDto(
    IReadOnlyList<AdminMfaRoleCountDto> ByRole,
    IReadOnlyList<AdminMfaMissingDto> WithoutMfa,
    IReadOnlyList<AdminAuditItemDto> RecentEvents);
