using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;

namespace Jobsy.Core.Contracts.Scholen;

public sealed record SchoolPortalClassListItemDto(
    Guid Id,
    string ClassName,
    SchoolLevel Level,
    int Year,
    int SchoolYearStart,
    string SchoolYearLabel,
    IReadOnlyList<string> TeacherNames,
    int CodeCount,
    int CompletedCount,
    bool ParentalInfoConfirmed,
    TestWindowState TestWindow,
    DateOnly? TestWindowClosesOn,
    PupilQuestionSet QuestionSet);

public sealed record SchoolPortalClassDetailDto(
    Guid Id,
    string ClassName,
    SchoolLevel Level,
    int Year,
    int SchoolYearStart,
    string SchoolYearLabel,
    int PupilCount,
    IReadOnlyList<SchoolPortalTeacherChipDto> Teachers,
    bool ParentalInfoConfirmed,
    DateTime? ParentalInfoConfirmedAtUtc,
    string? ParentalInfoConfirmedByName,
    string? ParentalInfoTextVersion,
    TestWindowState TestWindow,
    DateOnly? TestWindowClosesOn,
    bool ProcessorAgreementPresent,
    IReadOnlyList<SchoolPortalCodeRowDto> Codes,
    PupilQuestionSet QuestionSet,
    bool LevelLocked);

public sealed record SchoolPortalTeacherChipDto(Guid UserId, string DisplayName);

public sealed record SchoolPortalCodeRowDto(
    Guid Id,
    int Number,
    string DisplayCode,
    PupilCodeStatus Status,
    int ProgressCurrent,
    int ProgressTotal,
    DateTime? LastSeenAtUtc);

public sealed record CreateSchoolClassRequest(
    string ClassName,
    SchoolLevel Level,
    int Year,
    int PupilCount,
    int? SchoolYearStart,
    IReadOnlyList<Guid>? TeacherUserIds);

public sealed record UpdateSchoolClassRequest(
    string ClassName,
    SchoolLevel Level,
    int Year,
    IReadOnlyList<Guid>? TeacherUserIds);

public sealed record AddCodesRequest(int Count);

public sealed record ParentalConfirmationRequest(bool Confirmed);

public sealed record TestWindowRequest(
    string Action,
    DateOnly? ClosesOn = null);

public sealed record SchoolPortalResultsDto(
    Guid ClassId,
    string ClassName,
    bool PerCodeEnabled,
    ClassResultsAggregate Totals,
    IReadOnlyList<SchoolPortalPerCodeResultDto>? PerCode);

public sealed record SchoolPortalPerCodeResultDto(
    Guid CodeId,
    string DisplayCode,
    PupilCodeStatus Status,
    string? InterestCode,
    string? TopValue,
    string? DreamJobKey);

public sealed record SchoolPortalTeacherListItemDto(
    Guid UserId,
    string DisplayName,
    string Email,
    IReadOnlyList<SchoolPortalTeacherClassChipDto> Classes,
    bool AuthenticatorEnabled,
    bool InvitePending,
    DateTime? LastLoginAtUtc,
    bool IsActive);

public sealed record SchoolPortalTeacherClassChipDto(Guid ClassId, string ClassName);

public sealed record AssignTeacherClassesRequest(IReadOnlyList<Guid> ClassIds);

public sealed record SchoolDashboardDto(
    string SchoolName,
    string City,
    string SchoolYearLabel,
    int ClassCount,
    int CodeCount,
    int CompletedCount,
    double CompletedPercent,
    int TeacherCount,
    int TeachersWithoutMfa,
    IReadOnlyList<SchoolDashboardClassRowDto> Classes,
    IReadOnlyList<SchoolTodoItemDto> Todos,
    IReadOnlyList<NamedCountDto>? SchoolRiasecTop3,
    string? RetentionBanner = null);

public sealed record SchoolDashboardClassRowDto(
    Guid Id,
    string ClassName,
    string? TeacherNames,
    int CodeCount,
    int StartedCount,
    int CompletedCount,
    double CompletionPercent,
    TestWindowState TestWindow,
    DateOnly? TestWindowClosesOn);

public sealed record SchoolTodoItemDto(
    string Kind,
    string Title,
    string? Href,
    Guid? ClassId,
    Guid? TeacherUserId,
    DateOnly? DueOn);

public sealed record NamedCountDto(string Key, int Count);

public sealed record SchoolProfileDto(
    Guid Id,
    string Name,
    string City,
    string? BrinCode,
    IReadOnlyList<string> AllowedEmailDomains,
    DateOnly? ProcessorAgreementSignedOn,
    string? ProcessorAgreementVersion,
    bool IsActive);

public sealed record SchoolPrivacyDto(
    DateOnly? ProcessorAgreementSignedOn,
    string? ProcessorAgreementVersion,
    DateOnly RetentionCutoff,
    string RetentionYearLabel,
    IReadOnlyList<SchoolPrivacyClassConfirmationDto> ClassConfirmations,
    string OuderbriefText);

public sealed record SchoolPrivacyClassConfirmationDto(
    Guid ClassId,
    string ClassName,
    bool Confirmed,
    DateTime? ConfirmedAtUtc,
    string? ConfirmedByName);
