using Jobsy.Core.Enums;

namespace Jobsy.Core.Contracts;

public sealed record ExternalVacancyImportRequest(string Url);

public sealed record ExternalVacancyListItemDto(
    Guid Id,
    string Title,
    string CompanyName,
    string Place,
    CandidateExternalVacancyStatus Status,
    DateTime SavedAtUtc);

public sealed record ExternalVacancyDetailDto(
    Guid Id,
    string SourceUrl,
    string Title,
    string CompanyName,
    string Place,
    string HoursText,
    string PayText,
    string StartText,
    string TrainingText,
    IReadOnlyList<string> RequirementsBullets,
    IReadOnlyDictionary<string, string> StructuredFacts,
    ExternalVacancyMatchInsightsDto MatchInsights,
    int? TravelMinutesEstimate,
    CandidateExternalVacancyStatus Status,
    DateTime SavedAtUtc,
    bool CanApply,
    IReadOnlyList<string> SuggestedEmployerEmails);

public sealed record ExternalVacancyMatchInsightsDto(
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Challenges);

public sealed record ExternalVacancyApplyRequest(
    string EmployerEmail,
    string Motivation,
    IReadOnlyList<string>? SharedFactKeys);

public sealed record ExternalVacancyApplyResultDto(bool Succeeded, string? ErrorCode, string? Message);

public sealed record ExternalVacancyEmployerInviteDto(
    string VacancyTitle,
    string CompanyName,
    string Place,
    string CandidateFirstName,
    string MotivationPreview,
    IReadOnlyList<(string Label, string Value)> SharedFacts,
    string EmployerEmail,
    DateTime ExpiresAtUtc);

public sealed record ExternalVacancyAdminMetricsDto(
    int SentCount,
    int ReminderCount,
    int ClickedCount,
    int EmployerAccountCreatedCount,
    int AcceptedCount);

public sealed record ExternalVacancyExtractionResult(
    string Title,
    string Company,
    string Place,
    string Hours,
    string Pay,
    string Start,
    string Training,
    IReadOnlyList<string> RequirementBullets);
