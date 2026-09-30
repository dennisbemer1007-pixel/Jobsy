using Jobsy.Core.Enums;

namespace Jobsy.Core.Contracts.Scholen;

public sealed record SchoolListItemDto(
    Guid Id,
    string Name,
    string City,
    string? BrinCode,
    bool IsActive,
    DateOnly? ProcessorAgreementSignedOn,
    int ClassCount,
    int TeacherCount);

public sealed record SchoolDetailDto(
    Guid Id,
    string Name,
    string City,
    string? BrinCode,
    IReadOnlyList<string> AllowedEmailDomains,
    bool IsActive,
    DateOnly? ProcessorAgreementSignedOn,
    string? ProcessorAgreementVersion,
    int ClassCount,
    int CodeCount,
    int CompletedCount,
    IReadOnlyList<SchoolAdminListItemDto> SchoolAdmins);

public sealed record SchoolAdminListItemDto(
    Guid UserId,
    string TeacherDisplayName,
    string Email,
    bool AuthenticatorEnabled,
    bool IsActive);

public sealed record CreateSchoolRequest(
    string Name,
    string City,
    string? BrinCode,
    IReadOnlyList<string>? AllowedEmailDomains,
    bool IsActive = true);

public sealed record UpdateSchoolRequest(
    string Name,
    string City,
    string? BrinCode,
    IReadOnlyList<string>? AllowedEmailDomains,
    bool? IsActive = null);

public sealed record RecordProcessorAgreementRequest(
    DateOnly SignedOn,
    string Version);

public sealed record InviteSchoolAdminRequest(
    string FullName,
    string Email);

public sealed record InviteTeacherRequest(
    string FullName,
    string Email,
    IReadOnlyList<Guid> ClassIds);

public sealed record SchoolStaffInviteResultDto(
    Guid InviteId,
    Guid UserId,
    string Email,
    string Role,
    DateTime ExpiresAtUtc);

/// <summary>DTO fields that may hold school/class labels (not pupil names) — allow-listed in NoPupilNameFieldsTests.</summary>
public sealed record SchoolClassSummaryDto(
    Guid Id,
    string ClassName,
    string SchoolName,
    SchoolLevel Level,
    int Year,
    int PupilCount,
    TestWindowState TestWindow);
