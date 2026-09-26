using Jobsy.Core.Contracts;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Interfaces;

public interface ICandidateKompasService
{
    Task<CandidateKompasDto> GetAsync(Guid userId, CancellationToken cancellationToken = default);
}

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
