using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Reports.TestReflection;

public enum TestReflectionFeedback
{
    Yes = 0,
    Partial = 1,
    No = 2
}

public sealed record TestReflectionTheme(string Title, string Summary);

public sealed record TestReflectionFull(
    AssessmentKind Kind,
    TestDepthLevel Level,
    string Language,
    string HeroTitle,
    string HeroSubtitle,
    string BadgeLabel,
    IReadOnlyList<TestReflectionTheme> Themes,
    string PracticeTitle,
    IReadOnlyList<string> PracticeExamples,
    string WatchTitle,
    IReadOnlyList<string> WatchPoints,
    IReadOnlyList<string> EnergyGives,
    IReadOnlyList<string> EnergyDrains,
    string EnvironmentTitle,
    string EnvironmentBody,
    string? DreamCallout,
    bool FromOpenAi);

public sealed record TestReflectionState(
    TestReflectionFull? Mirror,
    TestReflectionFeedback? Feedback,
    bool Enabled,
    bool Eligible);
