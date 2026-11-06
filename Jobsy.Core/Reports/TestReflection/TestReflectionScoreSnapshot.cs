using Jobsy.Core.Enums;

namespace Jobsy.Core.Reports.TestReflection;

public sealed record TestReflectionTrait(string Code, int Percent, string Label);

public sealed record TestReflectionScoreSnapshot(
    AssessmentKind Kind,
    int QuickScanVersion,
    int AnsweredCount,
    IReadOnlyList<TestReflectionTrait> Traits,
    string? DreamTitle);
