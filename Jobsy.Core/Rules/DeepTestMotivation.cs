using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>Backward-compatible alias for <see cref="TestProgressMotivation"/> (uitgebreide test).</summary>
public static class DeepTestMotivation
{
    public const string PartStart = TestProgressMotivation.PartStart;
    public const string Halfway = TestProgressMotivation.Halfway;
    public const string LastPart = TestProgressMotivation.LastPart;
    public const string BeforePause = TestProgressMotivation.BeforePause;

    public static string? ForProgress(AssessmentKind kind, int answered, int total, int partIndex)
        => TestProgressMotivation.ForProgress(kind, answered, total, partIndex);

    public static string? ForQuickScan(AssessmentKind kind, int answered, int total, int questionIndex)
        => TestProgressMotivation.ForQuickScan(kind, answered, total, questionIndex);

    public static bool IsPausePoint(AssessmentKind kind, int answeredAfter)
        => TestProgressMotivation.IsPausePoint(kind, answeredAfter);

    public static int PartIndexForGlobalIndex(AssessmentKind kind, int globalIndex)
        => TestProgressMotivation.PartIndexForGlobalIndex(kind, globalIndex);

    public static int QuickScanBlockIndex(int questionIndex, int blockSize = 5)
        => TestProgressMotivation.QuickScanBlockIndex(questionIndex, blockSize);

    public static string PartDisplayName(AssessmentKind kind, TestDepthPart part)
        => TestProgressMotivation.PartDisplayName(kind, part);
}
