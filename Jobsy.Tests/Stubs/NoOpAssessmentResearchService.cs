using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;

namespace Jobsy.Tests.Stubs;

internal sealed class NoOpAssessmentResearchService : IAssessmentResearchService
{
    public Task<bool> IsFeatureEnabledAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);

    public Task AcceptResearchConsentAsync(Guid userId, ResearchDemographicsDto? demographics, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task WithdrawResearchConsentAsync(Guid userId, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task UpdateDemographicsAsync(Guid userId, ResearchDemographicsDto demographics, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RecordAccountAttemptAsync(
        Guid userId,
        AssessmentKind kind,
        int testVersion,
        string testLanguage,
        IReadOnlyDictionary<int, int> answers,
        IReadOnlyDictionary<int, int>? itemDurationMs,
        int? totalDurationMs,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task BackfillConsentedUserAsync(Guid userId, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<ResearchPilotStartDto?> StartPilotAsync(string campaignCode, string? testLanguage, CancellationToken cancellationToken = default)
        => Task.FromResult<ResearchPilotStartDto?>(null);

    public Task RecordPilotAttemptAsync(
        Guid pilotSessionId,
        AssessmentKind kind,
        int testVersion,
        IReadOnlyDictionary<int, int> answers,
        IReadOnlyDictionary<int, int>? itemDurationMs,
        IReadOnlyDictionary<int, bool>? itemUnclear,
        int? totalDurationMs,
        string? endComment,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<ResearchDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new ResearchDashboardDto([], 300));

    public Task<byte[]> ExportCsvAsync(string? testKey, int? testVersion, CancellationToken cancellationToken = default)
        => Task.FromResult(Array.Empty<byte>());

    public Task<ResearchPilotCampaignDto> CreatePilotCampaignAsync(string title, Guid adminUserId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<ResearchPilotCampaignDto>> ListPilotCampaignsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ResearchPilotCampaignDto>>([]);
}
