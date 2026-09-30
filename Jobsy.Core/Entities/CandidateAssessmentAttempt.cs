using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

/// <summary>
/// Open or historical retake attempt. Current result stays until an attempt is completed.
/// </summary>
public class CandidateAssessmentAttempt
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public AssessmentKind Kind { get; set; }
    public AssessmentVariant Variant { get; set; }

    public string AnswersJson { get; set; } = "{}";
    public string Status { get; set; } = AttemptStatus.Open;

    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public string? ScoresJson { get; set; }
    public string? ReportJson { get; set; }
    public int? ReportVersion { get; set; }

    /// <summary>Snapshot of the previous current result at completion time (history).</summary>
    public string? PreviousSnapshotJson { get; set; }

    /// <summary>How this open attempt was started: <c>Edit</c> (draft after completion) or <c>Retake</c>.</summary>
    public string Origin { get; set; } = "Edit";

    public static class AttemptStatus
    {
        public const string Open = "Open";
        public const string Completed = "Completed";
        public const string Abandoned = "Abandoned";
    }

    public static class AttemptOrigin
    {
        public const string Edit = "Edit";
        public const string Retake = "Retake";
    }
}
