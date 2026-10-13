using Jobsy.Core.Contracts;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Interfaces;

public interface ICandidateKompasService
{
    Task<CandidateKompasDto> GetAsync(Guid userId, string? language = null, CancellationToken cancellationToken = default);

    Task<CandidateDnaSummaryDto> GetDnaAsync(Guid userId, string? language = null, CancellationToken cancellationToken = default);
}

/// <summary>Slim read model for Mijn DNA (no questions, answers, profile, or matches).</summary>
public sealed record CandidateDnaSummaryDto(
    WhoAmIStorySummaryDto? WhoAmI,
    int ProfileCompletenessPercent,
    CandidateDnaCompetencySummaryDto Competencies,
    CandidateDnaCareerSummaryDto CareerInterests,
    CandidateDnaCultureSummaryDto Culture,
    CandidateDnaValuesSummaryDto Values);

public sealed record CandidateDnaCompetencySummaryDto(
    string Status,
    int AnsweredCount,
    int QuestionCount,
    CompetencyScores? Scores,
    CompetencyScores? PreviewScores,
    bool DeepCompleted);

public sealed record CandidateDnaCareerSummaryDto(
    string Status,
    int AnsweredCount,
    int QuestionCount,
    RiasecScores? Scores,
    RiasecScores? PreviewScores,
    bool DeepCompleted);

public sealed record CandidateDnaCultureSummaryDto(
    string Status,
    int AnsweredCount,
    int QuestionCount,
    CulturePersonalityScores? Scores,
    CulturePersonalityScores? PreviewScores,
    bool DeepCompleted);

public sealed record CandidateDnaValuesSummaryDto(
    string Status,
    int AnsweredCount,
    int QuestionCount,
    SchwartzValuesScores? Scores,
    SchwartzValuesScores? PreviewScores,
    bool DeepCompleted);

public sealed record CandidateKompasDto(
    MeProfileSummaryDto Profile,
    CandidateCompetencyStateDto Competencies,
    CandidateCareerInterestStateDto CareerInterests,
    CandidateCulturePersonalityStateDto Culture,
    CandidateValuesStateDto Values,
    DeepAnalysisStateDto CompetenceDeep,
    DeepAnalysisStateDto CareerDeep,
    DeepAnalysisStateDto CultureDeep,
    DeepAnalysisStateDto ValuesDeep,
    IReadOnlyList<CandidateMatchedVacancyDto> TopMatches,
    string InsightsStatus,
    WhoAmIStorySummaryDto? WhoAmI = null,
    int ProfileCompletenessPercent = 0);

/// <summary>Read-only story snippet for Mijn DNA (never generated inside GET).</summary>
public sealed record WhoAmIStorySummaryDto(
    string? Story,
    IReadOnlyList<string> Keywords,
    DateTime? GeneratedAtUtc,
    string Status);

/// <summary>Profile fields needed by Kompas/Profile without a second profile GET.</summary>
public sealed record MeProfileSummaryDto(
    Guid Id,
    string Email,
    string FullName,
    string Role,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    bool HasDateOfBirth,
    bool OpenForWork,
    bool WhatsAppContactAllowed,
    bool AuthenticatorEnabled,
    double? HomeLatitude,
    double? HomeLongitude,
    CandidatePreferencesDto? Preferences,
    CandidateUploadedCvSummaryDto? UploadedCv = null,
    IReadOnlyList<CandidateReferenceSummaryDto>? References = null);

public sealed record CandidateUploadedCvSummaryDto(
    string FileName,
    string ContentType,
    int SizeBytes,
    DateTime UploadedAtUtc,
    DateTime? ExtractedAtUtc = null,
    IReadOnlyList<string>? FilledFields = null);

public sealed record CandidateReferenceSummaryDto(
    Guid Id,
    string EmployerName,
    string ContactName,
    string Email,
    string Phone);
