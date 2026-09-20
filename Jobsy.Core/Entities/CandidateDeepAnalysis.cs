namespace Jobsy.Core.Entities;

/// <summary>
/// Paid 150-question deep psychometric analysis for one candidate.
/// Unlocked after successful <see cref="DeepAnalysisCheckout"/>.
/// </summary>
public class CandidateDeepAnalysis
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary><c>Locked</c>, <c>Draft</c>, or <c>Completed</c>.</summary>
    public string Status { get; set; } = CandidateDeepAnalysisStatuses.Locked;

    public string AnswersJson { get; set; } = "{}";

    /// <summary>JSON array of enriched match tags after completion.</summary>
    public string TagsJson { get; set; } = "[]";

    public DateTime? UnlockedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? ReportGeneratedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public static class CandidateDeepAnalysisStatuses
{
    public const string Locked = "Locked";
    public const string Draft = "Draft";
    public const string Completed = "Completed";

    public static bool IsUnlocked(string? status) =>
        string.Equals(status, Draft, StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, Completed, StringComparison.OrdinalIgnoreCase);

    public static bool IsCompleted(string? status) =>
        string.Equals(status, Completed, StringComparison.OrdinalIgnoreCase);
}
