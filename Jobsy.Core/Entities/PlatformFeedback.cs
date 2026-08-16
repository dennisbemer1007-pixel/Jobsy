using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

/// <summary>
/// In-app visual/functional feedback. Screenshots may contain on-screen PII — admin-only,
/// excluded from list APIs, and stripped on right-to-be-forgotten / 90-day retention
/// (any status). Page URLs are stored without query or fragment.
/// Named PlatformFeedback to avoid clashing with Sentry.UserFeedback.
/// </summary>
public class PlatformFeedback
{
    public Guid Id { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public FeedbackType Type { get; set; }
    public FeedbackStatus Status { get; set; } = FeedbackStatus.New;

    public string Description { get; set; } = string.Empty;
    public string PageUrl { get; set; } = string.Empty;

    /// <summary>Server-derived role label (never client-supplied). Null for guests.</summary>
    public string? UserRole { get; set; }

    public Guid? UserId { get; set; }
    public User? User { get; set; }

    /// <summary>Display name only (no e-mail). "Gast" when anonymous.</summary>
    public string? UserDisplayName { get; set; }

    public string? BrowserInfo { get; set; }
    public string? DeviceInfo { get; set; }

    public byte[]? ScreenshotBytes { get; set; }
    public string? ScreenshotContentType { get; set; }

    public string? GeneratedPrompt { get; set; }
    public DateTime? PromptEditedAtUtc { get; set; }

    public string? CursorAgentId { get; set; }
    public string? BranchName { get; set; }
    public string? PullRequestUrl { get; set; }

    public FeedbackAutomationStatus AutomationStatus { get; set; } = FeedbackAutomationStatus.None;
    public string? AutomationError { get; set; }
    public DateTime? AutomationLaunchedAtUtc { get; set; }
    public DateTime? AutomationFinishedAtUtc { get; set; }
}
