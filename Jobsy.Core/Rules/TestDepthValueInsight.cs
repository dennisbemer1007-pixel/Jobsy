using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>
/// Rules for progressive test depth value (5 → 10 → 25 → 150). No scores in candidate copy from here.
/// </summary>
public static class TestDepthValueInsight
{
    public static int MiniThemeCount(int answeredCount, int quickScanVersion = AssessmentTestVersions.V1)
    {
        var reached = TestDepthRules.Reached(AssessmentKind.Competence, answeredCount, quickScanVersion);
        return reached switch
        {
            TestDepthLevel.First => 1,
            TestDepthLevel.Deeper => 3,
            TestDepthLevel.Full => 3,
            _ => 0
        };
    }

    public static IReadOnlyList<string> TopCompetenceDomains(
        IEnumerable<(string Code, int? Value)> dimensions,
        int maxThemes)
    {
        if (maxThemes <= 0)
        {
            return [];
        }

        var tieBreak = new[]
        {
            CompetencyTestCatalog.Samenwerken,
            CompetencyTestCatalog.Resultaatgerichtheid,
            CompetencyTestCatalog.Stressbestendigheid,
            CompetencyTestCatalog.Innovatie,
            CompetencyTestCatalog.Extraversie
        };
        return DimensionRanking.Rank(dimensions, tieBreak)
            .Take(maxThemes)
            .Select(x => x.Code)
            .ToList();
    }

    public static bool ShowsPartialResultPage(
        bool mockupEnabled,
        AssessmentKind kind,
        int answeredCount,
        bool freeCompleted,
        int quickScanVersion = AssessmentTestVersions.V1)
    {
        if (!mockupEnabled || kind != AssessmentKind.Competence || freeCompleted)
        {
            return false;
        }

        return answeredCount >= TestDepthRules.FirstCount
               && answeredCount < TestDepthRules.FullCount(kind, quickScanVersion);
    }
}
