using Jobsy.Core.Entities;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Interfaces;

public sealed record FeedbackSubmitRequest(
    FeedbackType Type,
    string Description,
    string PageUrl,
    string? BrowserInfo,
    string? DeviceInfo,
    string? ScreenshotDataUrl,
    Guid? UserId,
    string? UserRole,
    string? UserDisplayName);

public sealed record FeedbackListQuery(
    FeedbackType? Type = null,
    FeedbackStatus? Status = null,
    int Take = 200);

public sealed record FeedbackAutomationResult(
    PlatformFeedback Feedback,
    bool Launched,
    string Message);

public interface IFeedbackService
{
    Task<PlatformFeedback> SubmitAsync(FeedbackSubmitRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlatformFeedback>> ListAsync(
        FeedbackListQuery query,
        CancellationToken cancellationToken = default);

    Task<PlatformFeedback?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    string BuildPrompt(PlatformFeedback feedback);

    Task<PlatformFeedback> SavePromptAsync(
        Guid id,
        string? editedPrompt,
        CancellationToken cancellationToken = default);

    Task<FeedbackAutomationResult> LaunchAutomationAsync(
        Guid id,
        string prompt,
        CancellationToken cancellationToken = default);

    Task<PlatformFeedback?> UpdateStatusAsync(
        Guid id,
        FeedbackStatus status,
        CancellationToken cancellationToken = default);

    Task<PlatformFeedback?> AttachPullRequestAsync(
        Guid id,
        string pullRequestUrl,
        CancellationToken cancellationToken = default);

    Task<PlatformFeedback?> RefreshAutomationAsync(Guid id, CancellationToken cancellationToken = default);

    Task ApplyCursorWebhookAsync(
        string agentId,
        string status,
        string? pullRequestUrl,
        string? branchName,
        string? summary,
        CancellationToken cancellationToken = default);

    Task<int> RefreshPendingAutomationsAsync(CancellationToken cancellationToken = default);
}
