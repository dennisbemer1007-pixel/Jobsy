using Jobsy.Core.Enums;

namespace Jobsy.Api.Models;

public sealed record SubmitFeedbackRequest(
    FeedbackType Type,
    string Description,
    string PageUrl,
    string? BrowserInfo,
    string? DeviceInfo,
    string? ScreenshotDataUrl);

public sealed record FeedbackListItemDto(
    Guid Id,
    DateTime CreatedAtUtc,
    string Type,
    string Status,
    string? UserRole,
    string? UserDisplayName,
    string PageUrl,
    string? PullRequestUrl,
    bool HasScreenshot,
    string? AutomationStatus,
    string? BranchName);

public sealed record FeedbackDetailDto(
    Guid Id,
    DateTime CreatedAtUtc,
    string Type,
    string Status,
    string Description,
    string? UserRole,
    string? UserDisplayName,
    string PageUrl,
    string? BrowserInfo,
    string? DeviceInfo,
    string? GeneratedPrompt,
    string? PullRequestUrl,
    string? CursorAgentId,
    string? BranchName,
    string? AutomationStatus,
    string? AutomationError,
    bool HasScreenshot);

public sealed record FeedbackPromptRequest(string? Prompt);

public sealed record FeedbackPromptDto(Guid Id, string Prompt, string BranchName);

public sealed record FeedbackAutomateRequest(string? Prompt);

public sealed record FeedbackAutomateResultDto(
    FeedbackDetailDto Feedback,
    bool Launched,
    string Message);

public sealed record FeedbackStatusRequest(FeedbackStatus Status);

public sealed record FeedbackPullRequestRequest(string PullRequestUrl);
