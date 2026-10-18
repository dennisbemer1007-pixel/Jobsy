namespace Jobsy.Core.Interfaces;

public sealed record ContextualTipsStateDto(
    bool MomentTipsEnabled,
    bool InterviewPrepReminderEnabled,
    IReadOnlyList<string> HiddenTipIds,
    int HiddenCount);

public sealed record UpdateContextualTipsSettingsRequest(
    bool? MomentTipsEnabled = null,
    bool? InterviewPrepReminderEnabled = null);

public interface IContextualTipsService
{
    Task<ContextualTipsStateDto> GetStateAsync(Guid userId, CancellationToken cancellationToken = default);

    Task HideTipAsync(Guid userId, string tipId, CancellationToken cancellationToken = default);

    Task RestoreHiddenAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<ContextualTipsStateDto> UpdateSettingsAsync(
        Guid userId,
        UpdateContextualTipsSettingsRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> IsTipVisibleAsync(Guid userId, string tipId, CancellationToken cancellationToken = default);
}
