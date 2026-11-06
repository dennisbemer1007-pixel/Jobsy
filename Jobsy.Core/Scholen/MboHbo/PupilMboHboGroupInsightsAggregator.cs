using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen;

namespace Jobsy.Core.Scholen.MboHbo;

/// <summary>
/// Class-level teacher insights for MBO/HBO from progress JSON (not VO PupilResult fields).
/// </summary>
public static class PupilMboHboGroupInsightsAggregator
{
    private static readonly string[] RiasecOrder = ["R", "I", "A", "S", "E", "C"];

    public static TeacherGroupAggregate Aggregate(
        IReadOnlyList<PupilMboHboProgressState> completedStates,
        SchoolLevel level)
    {
        ArgumentNullException.ThrowIfNull(completedStates);
        var completed = completedStates.Count;
        if (completed < SchoolAnonymity.MinGroupSize)
        {
            return new TeacherGroupAggregate(
                Visible: false,
                CompletedCount: completed,
                RiasecBars: [],
                TopValues: [],
                TopCultures: [],
                CompetenceBands: [],
                DreamJobs: [],
                UndecidedDreamJobCount: 0,
                NotFilledDreamJobCount: 0);
        }

        var catalog = PupilMboHboCatalog.Default;
        var riasec = RiasecOrder.ToDictionary(l => l, _ => 0, StringComparer.OrdinalIgnoreCase);
        var values = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var cultures = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var competenceBands = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var state in completedStates)
        {
            CountHollandLetters(state, level, riasec);

            var topValue = TopWerkDimension(state, level, catalog, culture: false);
            if (!string.IsNullOrWhiteSpace(topValue))
            {
                values[topValue] = values.GetValueOrDefault(topValue) + 1;
            }

            var topCulture = TopWerkDimension(state, level, catalog, culture: true);
            if (!string.IsNullOrWhiteSpace(topCulture))
            {
                cultures[topCulture] = cultures.GetValueOrDefault(topCulture) + 1;
            }

            var band = CompetenceBand(state, level);
            if (band is not null)
            {
                competenceBands[band] = competenceBands.GetValueOrDefault(band) + 1;
            }
        }

        var riasecBars = RiasecOrder
            .Select(letter => new NamedCount(letter, riasec[letter]))
            .ToList();

        var topValues = values
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .Select(kv => new NamedCount(kv.Key, kv.Value))
            .ToList();

        var topCultures = cultures
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .Select(kv => new NamedCount(kv.Key, kv.Value))
            .ToList();

        var bands = competenceBands
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => new NamedCount(kv.Key, kv.Value))
            .ToList();

        return new TeacherGroupAggregate(
            Visible: true,
            CompletedCount: completed,
            RiasecBars: riasecBars,
            TopValues: topValues,
            TopCultures: topCultures,
            CompetenceBands: bands,
            DreamJobs: [],
            UndecidedDreamJobCount: 0,
            NotFilledDreamJobCount: 0);
    }

    private static void CountHollandLetters(
        PupilMboHboProgressState state,
        SchoolLevel level,
        Dictionary<string, int> riasec)
    {
        var interesse = PupilMboHboScoring.BuildOutcome(PupilMboHboBlock.Interesse, state, level);
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

    private static string? TopWerkDimension(
        PupilMboHboProgressState progress,
        SchoolLevel level,
        PupilMboHboCatalog catalog,
        bool culture)
    {
        var byKey = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var q in catalog.QuestionsFor(PupilMboHboBlock.Werkwaarden, level))
        {
            if (!progress.Answers.TryGetValue(q.Id, out var v))
            {
                continue;
            }

            if (IsWerkcultuurDimension(q.Dimension) != culture)
            {
                continue;
            }

            var key = PupilMboHboDimensionLabels.BarGroupKey(q.Dimension);
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (!byKey.TryGetValue(key, out var list))
            {
                list = [];
                byKey[key] = list;
            }

            list.Add(v);
        }

        return byKey
            .Select(kv => (Key: kv.Key, Score: AveragePercent(kv.Value)))
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Key)
            .FirstOrDefault();
    }

    private static bool IsWerkcultuurDimension(string dimension)
    {
        if (string.IsNullOrWhiteSpace(dimension))
        {
            return false;
        }

        var raw = dimension.Trim();
        return raw.StartsWith("Werkcultuur", StringComparison.OrdinalIgnoreCase)
               || raw.StartsWith("Werkcontext", StringComparison.OrdinalIgnoreCase);
    }

    private static string? CompetenceBand(PupilMboHboProgressState state, SchoolLevel level)
    {
        var outcome = PupilMboHboScoring.BuildOutcome(PupilMboHboBlock.Competentie, state, level);
        if (outcome.Bars.Count == 0)
        {
            return null;
        }

        var avg = outcome.Bars.Average(b => b.Percent);
        if (avg < 50)
        {
            return "Laag";
        }

        if (avg < 70)
        {
            return "Midden";
        }

        return "Hoog";
    }

    private static int AveragePercent(IReadOnlyList<int> values)
        => values.Count == 0 ? 0 : LikertAnswerJson.ToPercent(values);
}
