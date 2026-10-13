using Jobsy.Core.Enums;

namespace Jobsy.Core.Interfaces;

public interface IAssessmentResearchService
{
    Task<bool> IsFeatureEnabledAsync(CancellationToken cancellationToken = default);

    Task AcceptResearchConsentAsync(
        Guid userId,
        ResearchDemographicsDto? demographics,
        CancellationToken cancellationToken = default);

    Task WithdrawResearchConsentAsync(Guid userId, CancellationToken cancellationToken = default);

    Task UpdateDemographicsAsync(
        Guid userId,
        ResearchDemographicsDto demographics,
        CancellationToken cancellationToken = default);

    Task RecordAccountAttemptAsync(
        Guid userId,
        AssessmentKind kind,
        int testVersion,
        string testLanguage,
        IReadOnlyDictionary<int, int> answers,
        IReadOnlyDictionary<int, int>? itemDurationMs,
        int? totalDurationMs,
        CancellationToken cancellationToken = default);

    Task BackfillConsentedUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<ResearchPilotStartDto?> StartPilotAsync(string campaignCode, string? testLanguage, CancellationToken cancellationToken = default);

    Task RecordPilotAttemptAsync(
        Guid pilotSessionId,
        AssessmentKind kind,
        int testVersion,
        IReadOnlyDictionary<int, int> answers,
        IReadOnlyDictionary<int, int>? itemDurationMs,
        IReadOnlyDictionary<int, bool>? itemUnclear,
        int? totalDurationMs,
        string? endComment,
        CancellationToken cancellationToken = default);

    Task<ResearchDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<byte[]> ExportCsvAsync(string? testKey, int? testVersion, CancellationToken cancellationToken = default);

    Task<ResearchPilotCampaignDto> CreatePilotCampaignAsync(
        string title,
        Guid adminUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ResearchPilotCampaignDto>> ListPilotCampaignsAsync(CancellationToken cancellationToken = default);
}

public sealed record ResearchDemographicsDto(
    string? AgeBand,
    string? Gender,
    string? EducationLevel,
    string? NativeLanguage,
    string? TestLanguage);

public sealed record ResearchPilotStartDto(
    Guid SessionId,
    string CampaignTitle,
    string RespondentHash);

public sealed record ResearchDashboardRowDto(
    string TestKey,
    int TestVersion,
    string Language,
    int ConsentedCount,
    int TargetCount,
    bool Suppressed);

public sealed record ResearchDashboardDto(
    IReadOnlyList<ResearchDashboardRowDto> Rows,
    int TargetPerCell);

public sealed record ResearchPilotCampaignDto(
    Guid Id,
    string Code,
    string Title,
    bool IsActive,
    DateTime CreatedAtUtc,
    string PilotUrl);
