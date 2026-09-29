namespace Jobsy.Core.Rules;

/// <summary>
/// Each candidate may adjust a given test+variant at most <see cref="MaxAdjustments"/> times.
/// A saved edit and a completed retake each count as one. The first completion does not count.
/// </summary>
public static class AssessmentAdjustmentRules
{
    public const int MaxAdjustments = 3;

    public const string LimitErrorCode = "assessment_adjustment_limit";

    public static int Remaining(int used)
        => Math.Clamp(MaxAdjustments - Math.Max(0, used), 0, MaxAdjustments);
}
