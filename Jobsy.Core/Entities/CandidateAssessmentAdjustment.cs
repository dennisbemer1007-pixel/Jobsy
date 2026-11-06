using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

/// <summary>
/// One consumed adjustment (saved edit or completed retake) for a candidate test+variant.
/// </summary>
public class CandidateAssessmentAdjustment
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    public AssessmentKind Kind { get; set; }
    public AssessmentVariant Variant { get; set; }
    public AssessmentAdjustmentType Type { get; set; }

    public DateTime AtUtc { get; set; }

    /// <summary>Optional link to the retake/edit attempt for idempotency.</summary>
    public Guid? AttemptId { get; set; }

    /// <summary>Client/server idempotency key so double-submit counts once.</summary>
    public string? IdempotencyKey { get; set; }
}
