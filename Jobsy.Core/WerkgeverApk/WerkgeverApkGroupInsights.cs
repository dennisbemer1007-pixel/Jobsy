using Jobsy.Core.Enums;
using Jobsy.Core.Scholen.MboHbo;

namespace Jobsy.Core.WerkgeverApk;

public sealed record WerkgeverApkBarCount(string Label, int Count, int Total);

public sealed record WerkgeverApkDualBar(string Label, int EmployeeAverage, int ManagerAverage);

public sealed record WerkgeverApkGroupInsightsDto(
    bool Visible,
    int SharedCompletedCount,
    IReadOnlyList<WerkgeverApkBarCount> RiasecBars,
    IReadOnlyList<WerkgeverApkBarCount> TopStrengths,
    IReadOnlyList<WerkgeverApkBarCount> TopValues,
    IReadOnlyList<WerkgeverApkDualBar> ManagerVsEmployeeBars);

public static class WerkgeverApkGroupInsights
{
    private static readonly string[] RiasecOrder = ["R", "I", "A", "S", "E", "C"];

    private static readonly Dictionary<char, string> RiasecKidLabels = new()
    {
        ['R'] = "Aanpakken met je handen",
        ['I'] = "Uitzoeken hoe het zit",
        ['A'] = "Iets moois of nieuws maken",
        ['S'] = "Mensen helpen",
        ['E'] = "Aanjagen en verkopen",
        ['C'] = "Netjes organiseren"
    };

    public static WerkgeverApkGroupInsightsDto Aggregate(
        IReadOnlyList<PupilMboHboProgressState> sharedEmployeeProgress,
        IReadOnlyList<IReadOnlyDictionary<string, int>> managerSheets,
        SchoolLevel level = SchoolLevel.Mbo3)
    {
        ArgumentNullException.ThrowIfNull(sharedEmployeeProgress);
        ArgumentNullException.ThrowIfNull(managerSheets);

        var count = sharedEmployeeProgress.Count;
        if (count < WerkgeverApkAnonymity.MinGroupSize)
        {
            return new WerkgeverApkGroupInsightsDto(
                Visible: false,
                SharedCompletedCount: count,
                RiasecBars: [],
                TopStrengths: [],
                TopValues: [],
                ManagerVsEmployeeBars: []);
        }

        var catalog = PupilMboHboCatalog.Default;
        var riasec = RiasecOrder.ToDictionary(l => l, _ => 0, StringComparer.OrdinalIgnoreCase);
        var strengths = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var values = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        var employeeTotals = WerkgeverApkManagerQuestionCatalog.All.ToDictionary(q => q.Label, _ => new List<int>());
        var managerTotals = WerkgeverApkManagerQuestionCatalog.All.ToDictionary(q => q.Label, _ => new List<int>());

        for (var i = 0; i < count; i++)
        {
            var state = sharedEmployeeProgress[i];
            CountHollandLetters(state, level, catalog, riasec);

            foreach (var q in WerkgeverApkManagerQuestionCatalog.All)
            {
                if (q.Category == WerkgeverApkManagerQuestionCategory.Competence)
                {
                    var self = WerkgeverApkEmployeeDimensionScores.SelfScore(q, state, level);
                    if (self >= 4)
                    {
                        strengths[q.Label] = strengths.GetValueOrDefault(q.Label) + 1;
                    }
                }
                else
                {
                    var self = WerkgeverApkEmployeeDimensionScores.SelfScore(q, state, level);
                    if (self >= 4)
                    {
                        values[q.Label] = values.GetValueOrDefault(q.Label) + 1;
                    }
                }

                var selfScore = WerkgeverApkEmployeeDimensionScores.SelfScore(q, state, level);
                if (selfScore is not null)
                {
                    employeeTotals[q.Label].Add(selfScore.Value);
                }
            }

            if (i < managerSheets.Count)
            {
                foreach (var q in WerkgeverApkManagerQuestionCatalog.All)
                {
                    if (managerSheets[i].TryGetValue(q.Id, out var manager))
                    {
                        managerTotals[q.Label].Add(manager);
                    }
                }
            }
        }

        var riasecBars = RiasecOrder
            .Select(letter => new WerkgeverApkBarCount(
                RiasecKidLabels.GetValueOrDefault(letter[0], letter),
                riasec[letter],
                count))
            .ToList();

        var topStrengths = TopBars(strengths, count, take: 5);
        var topValues = TopBars(values, count, take: 5);

        var dualBars = WerkgeverApkManagerQuestionCatalog.All
            .Select(q =>
            {
                var emp = employeeTotals[q.Label];
                var mgr = managerTotals[q.Label];
                var empAvg = emp.Count == 0 ? 0 : (int)Math.Round(emp.Average(), MidpointRounding.AwayFromZero);
                var mgrAvg = mgr.Count == 0 ? 0 : (int)Math.Round(mgr.Average(), MidpointRounding.AwayFromZero);
                return new WerkgeverApkDualBar(q.Label, empAvg, mgrAvg);
            })
            .ToList();

        return new WerkgeverApkGroupInsightsDto(
            Visible: true,
            SharedCompletedCount: count,
            RiasecBars: riasecBars,
            TopStrengths: topStrengths,
            TopValues: topValues,
            ManagerVsEmployeeBars: dualBars);
    }

    private static IReadOnlyList<WerkgeverApkBarCount> TopBars(
        Dictionary<string, int> counts,
        int total,
        int take)
        => counts
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Take(take)
            .Select(kv => new WerkgeverApkBarCount(kv.Key, kv.Value, total))
            .ToList();

    private static void CountHollandLetters(
        PupilMboHboProgressState state,
        SchoolLevel level,
        PupilMboHboCatalog catalog,
        Dictionary<string, int> riasec)
    {
        var interesse = PupilMboHboScoring.BuildOutcome(PupilMboHboBlock.Interesse, state, level, catalog);
        var holland = PupilMboHboScoring.HollandCodeFromInteresse(interesse.Bars);
        foreach (var ch in holland)
        {
            var letter = ch.ToString();
            if (riasec.TryGetValue(letter, out var count))
            {
                riasec[letter] = count + 1;
            }
        }
    }
}
