using Jobsy.Core.Enums;
using Jobsy.Core.Scholen.MboHbo;

namespace Jobsy.Core.WerkgeverApk;

public sealed record WerkgeverApkFunctieBar(string Label, int StrongCount, int GrowthCount, int Total);

public sealed record WerkgeverApkFunctieTrainingHint(string Label, string Hint);

public sealed record WerkgeverApkFunctieGroupTab(
    string EscoOccupationId,
    string TabTitle,
    int SharedCount,
    bool Visible);

public sealed record WerkgeverApkFunctieGroupInsightsDto(
    bool Visible,
    int SharedCompletedCount,
    string SelectedEscoOccupationId,
    string SelectedTitle,
    IReadOnlyList<WerkgeverApkFunctieBar> Bars,
    IReadOnlyList<WerkgeverApkFunctieTrainingHint> Training,
    IReadOnlyList<WerkgeverApkFunctieGroupTab> Tabs);

public static class WerkgeverApkFunctieGroupInsights
{
    private const int StrongThreshold = 4;

    public static WerkgeverApkFunctieGroupInsightsDto Aggregate(
        IReadOnlyList<(string? EscoOccupationId, PupilMboHboProgressState Progress)> sharedCompleted,
        string? selectedEscoId,
        SchoolLevel level = SchoolLevel.Mbo3)
    {
        ArgumentNullException.ThrowIfNull(sharedCompleted);

        var tabs = BuildTabs(sharedCompleted);
        var selected = ResolveSelected(selectedEscoId, tabs);
        var cohort = sharedCompleted
            .Where(x => !string.IsNullOrWhiteSpace(x.EscoOccupationId)
                        && string.Equals(x.EscoOccupationId, selected, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var count = cohort.Count;

        if (count < WerkgeverApkAnonymity.MinGroupSize)
        {
            var title = WerkgeverApkFunctieProfileCatalog.DisplayName(selected);
            return new WerkgeverApkFunctieGroupInsightsDto(
                Visible: false,
                SharedCompletedCount: count,
                SelectedEscoOccupationId: selected ?? "",
                SelectedTitle: title,
                Bars: [],
                Training: [],
                Tabs: tabs);
        }

        var profile = WerkgeverApkFunctieProfileCatalog.Resolve(selected);
        var catalog = WerkgeverApkEmployeeCatalog.Default;
        var bars = new List<WerkgeverApkFunctieBar>();

        foreach (var point in profile.Points)
        {
            var strong = 0;
            var growth = 0;
            var scored = 0;
            foreach (var (_, state) in cohort)
            {
                var pointScore = ScorePoint(point, state, catalog, level);
                if (pointScore is null)
                {
                    continue;
                }

                scored++;
                if (pointScore >= StrongThreshold)
                {
                    strong++;
                }
                else
                {
                    growth++;
                }
            }

            if (scored > 0)
            {
                bars.Add(new WerkgeverApkFunctieBar(point.Label, strong, growth, scored));
            }
        }

        var training = bars
            .OrderByDescending(b => b.GrowthCount)
            .ThenBy(b => b.Label, StringComparer.OrdinalIgnoreCase)
            .Where(b => b.GrowthCount > 0)
            .Take(3)
            .Select(b =>
            {
                var hint = profile.Points.FirstOrDefault(p => p.Label == b.Label)?.TrainingHint ?? b.Label;
                return new WerkgeverApkFunctieTrainingHint(b.Label, hint);
            })
            .ToList();

        return new WerkgeverApkFunctieGroupInsightsDto(
            Visible: true,
            SharedCompletedCount: count,
            SelectedEscoOccupationId: selected ?? "",
            SelectedTitle: profile.DisplayTitle,
            Bars: bars,
            Training: training,
            Tabs: tabs);
    }

    private static int? ScorePoint(
        WerkgeverApkFunctieProfilePoint point,
        PupilMboHboProgressState progress,
        WerkgeverApkEmployeeCatalog catalog,
        SchoolLevel level)
        => WerkgeverApkFunctieComparisonEngine.ScoreForPoint(point, progress, catalog, level);

    private static IReadOnlyList<WerkgeverApkFunctieGroupTab> BuildTabs(
        IReadOnlyList<(string? EscoOccupationId, PupilMboHboProgressState Progress)> sharedCompleted)
    {
        return sharedCompleted
            .Where(x => !string.IsNullOrWhiteSpace(x.EscoOccupationId))
            .GroupBy(x => x.EscoOccupationId!, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var title = WerkgeverApkFunctieProfileCatalog.DisplayName(g.Key);
                return new WerkgeverApkFunctieGroupTab(
                    g.Key,
                    title,
                    g.Count(),
                    g.Count() >= WerkgeverApkAnonymity.MinGroupSize);
            })
            .OrderByDescending(t => t.SharedCount)
            .ThenBy(t => t.TabTitle, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? ResolveSelected(string? selectedEscoId, IReadOnlyList<WerkgeverApkFunctieGroupTab> tabs)
    {
        if (!string.IsNullOrWhiteSpace(selectedEscoId)
            && tabs.Any(t => string.Equals(t.EscoOccupationId, selectedEscoId, StringComparison.OrdinalIgnoreCase)))
        {
            return selectedEscoId.Trim();
        }

        return tabs.Count == 0 ? null : tabs[0].EscoOccupationId;
    }

}