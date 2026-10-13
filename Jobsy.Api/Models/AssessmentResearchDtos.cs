namespace Jobsy.Api.Models;

public sealed record ResearchFeatureStatusDto(
    bool Enabled,
    string ConsentTextVersion,
    int TargetRespondents);

public sealed record ResearchConsentStateDto(
    bool HasConsent,
    DateTime? ResearchConsentAt,
    string? ResearchConsentVersion,
    string? AgeBand,
    string? Gender,
    string? EducationLevel,
    string? NativeLanguage,
    string? TestLanguage);

public sealed record ResearchDemographicsRequest(
    string? AgeBand,
    string? Gender,
    string? EducationLevel,
    string? NativeLanguage,
    string? TestLanguage);

public sealed record AcceptResearchConsentRequest(ResearchDemographicsRequest? Demographics);

public sealed record StartPilotRequest(string? TestLanguage);

public sealed record SubmitPilotAttemptRequest(
    string TestKey,
    int TestVersion,
    Dictionary<int, int>? Answers,
    Dictionary<int, int>? ItemDurationMs,
    Dictionary<int, bool>? ItemUnclear,
    int? TotalDurationMs,
    string? EndComment);

public sealed record CreatePilotCampaignRequest(string? Title);

public sealed record ResearchAdminSettingsDto(bool ResearchConsentEnabled);
