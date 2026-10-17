namespace Jobsy.Core.Contracts;

public sealed record KandidatenkaartAccessDto(
    bool FeatureEnabled,
    bool HasPaidAccess,
    bool ShowPreviewOnly,
    string? PricingNote);

public sealed record KandidatenkaartVacancyOptionDto(
    Guid Id,
    string Title,
    string LocationLabel,
    int PositionsTarget,
    int PositionsFilled,
    bool IsOpenForInvites);

public sealed record KandidatenkaartAreaDto(
    string AreaKey,
    string Label,
    int CandidateCount,
    int CountAbove80,
    int Count70To79,
    double CenterLat,
    double CenterLng,
    IReadOnlyList<IReadOnlyList<double>> PolygonRing,
    bool ActiveThisWeek);

public sealed record KandidatenkaartMapDto(
    KandidatenkaartAccessDto Access,
    Guid? VacancyId,
    int MinMatchPercent,
    int TravelKm,
    string? AvailabilityFilter,
    string? HoursFilter,
    IReadOnlyList<KandidatenkaartAreaDto> Areas,
    bool BelowThreshold,
    string? ThresholdMessage);

public sealed record KandidatenkaartInvitePreviewDto(
    string AreaLabel,
    int EligibleCount,
    int SkippedBelowMatch,
    int SkippedWeeklyCap,
    int SkippedPreferenceOff,
    int EstimatedRounds,
    string StagingNote);

public sealed record KandidatenkaartInviteResultDto(
    Guid CampaignId,
    int QueuedCount,
    int RoundNumber,
    string Message);

public sealed record KandidatenkaartCampaignStatusDto(
    Guid CampaignId,
    string AreaLabel,
    KandidatenkaartCampaignStatusView Status,
    int SentCount,
    int RespondedCount,
    int PendingCount,
    int CurrentRound,
    int? PositionsRemaining);

public enum KandidatenkaartCampaignStatusView
{
    Active,
    Staged,
    Completed,
    VacancyFull,
    Withdrawn
}

public sealed record CandidateKandidatenkaartSettingsDto(
    KandidatenkaartInvitePreferenceView InvitePreference,
    bool ShowReAvailabilityPrompt);

public enum KandidatenkaartInvitePreferenceView
{
    Off,
    Normal,
    Max
}

public sealed record CandidateKandidatenkaartSettingsUpdate(
    KandidatenkaartInvitePreferenceView InvitePreference);

public sealed record KandidatenkaartReAvailabilityAck(bool BecomeAvailable);
