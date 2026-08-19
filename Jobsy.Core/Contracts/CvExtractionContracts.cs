namespace Jobsy.Core.Contracts;

/// <summary>Fields OpenAI (or a stub) extracted from an uploaded CV. Null/empty means "not clearly present".</summary>
public sealed record CvExtractedProfile(
    string? FirstName = null,
    string? LastName = null,
    string? PhoneNumber = null,
    string? AboutMe = null,
    IReadOnlyList<string>? DrivingLicenses = null,
    IReadOnlyList<string>? Educations = null,
    IReadOnlyList<string>? Roles = null,
    IReadOnlyList<CandidateEmployerHistoryDto>? Employers = null,
    IReadOnlyList<CandidateCertificateDto>? Certificates = null);

public sealed record CvProfileMergeResult(
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    CandidatePreferencesDto Preferences,
    IReadOnlyList<string> FilledFields);
