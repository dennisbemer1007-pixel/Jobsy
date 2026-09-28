using Jobsy.Core.Enums;
using Jobsy.Core.Models;

namespace Jobsy.Core.Interfaces;

public interface IAssessmentAdjustmentService
{
    Task<AssessmentAdjustmentDto> GetAsync(
        Guid userId,
        AssessmentKind kind,
        AssessmentVariant variant,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Throws <see cref="AssessmentAdjustmentLimitException"/> when remaining is 0.
    /// </summary>
    Task EnsureRemainingAsync(
        Guid userId,
        AssessmentKind kind,
        AssessmentVariant variant,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a consumed adjustment in the same unit of work. Idempotent on <paramref name="idempotencyKey"/>.
    /// </summary>
    Task<AssessmentAdjustmentDto> RecordAsync(
        Guid userId,
        AssessmentKind kind,
        AssessmentVariant variant,
        AssessmentAdjustmentType type,
        Guid? attemptId,
        string? idempotencyKey,
        CancellationToken cancellationToken = default);
}

public sealed class AssessmentAdjustmentLimitException : Exception
{
    public AssessmentAdjustmentLimitException(int max)
        : base("Assessment adjustment limit reached.")
    {
        Max = max;
    }

    public int Max { get; }
    public int Remaining => 0;
    public string Code => Rules.AssessmentAdjustmentRules.LimitErrorCode;
}
