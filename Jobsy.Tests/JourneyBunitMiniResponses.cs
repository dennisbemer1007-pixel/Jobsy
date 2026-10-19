using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

internal static class JourneyBunitMiniResponses
{
    public static string CompletedMiniJson(AssessmentKind kind)
    {
        var version = AssessmentTestVersions.V3;
        var ids = TestDepthRules.MiniIds(kind, version);
        var answers = string.Join(",", ids.Select(id => $"\"{id}\":4"));
        return $$"""{"status":"Completed","answers":{{{answers}}},"quickScanVersion":{{version}},"completedAtUtc":"2026-10-03T12:00:00Z"}""";
    }

    public static string DiscoveryOverviewPanelJson(
        int competencyAnswered = 5,
        int careerAnswered = 5,
        int cultureAnswered = 5,
        int valuesAnswered = 5)
        => $$"""{"profileRevision":"bunit","competencyAnswered":{{competencyAnswered}},"careerAnswered":{{careerAnswered}},"cultureAnswered":{{cultureAnswered}},"valuesAnswered":{{valuesAnswered}}}""";

    public static bool IsDiscoveryOverviewPath(string path)
        => path.Contains("discovery-overview", StringComparison.OrdinalIgnoreCase);

    public static bool IsOnboardingStatePath(string path)
        => path.Contains("/onboarding", StringComparison.OrdinalIgnoreCase) && !IsDiscoveryOverviewPath(path);
}
