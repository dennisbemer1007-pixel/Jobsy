using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Core.Scholen.Matching;

namespace Jobsy.Core.Scholen.MboHbo;

public static class PupilMboHboOccupationMatch
{
    public sealed record RankedOccupation(string Label, string SectorKey, double Score);

    public static IReadOnlyList<RankedOccupation> TopOccupations(
        PupilMboHboProgressState progress,
        SchoolLevel level,
        PupilMboHboCatalog? catalog = null,
        int maxCount = PupilCareerMatchWeights.MaxSuggestions)
    {
        catalog ??= PupilMboHboCatalog.Default;
        var ranked = RankAll(progress, level, catalog);
        return SelectForDisplay(ranked, maxCount, PupilCareerMatchWeights.DefaultMinMatch);
    }

    /// <summary>Outcome screen: always show ranked suggestions (no 70% cutoff).</summary>
    public static IReadOnlyList<RankedOccupation> TopOccupationsForOutcome(
        PupilMboHboProgressState progress,
        SchoolLevel level,
        PupilMboHboCatalog? catalog = null,
        int maxCount = PupilCareerMatchWeights.MaxSuggestions)
    {
        catalog ??= PupilMboHboCatalog.Default;
        var ranked = RankAll(progress, level, catalog);
        return SelectForDisplay(ranked, maxCount, minMatch: 0);
    }

    private static IReadOnlyList<RankedOccupation> RankAll(
        PupilMboHboProgressState progress,
        SchoolLevel level,
        PupilMboHboCatalog catalog)
    {
        var riasec = BuildRiasec(progress, catalog, level);
        var vakScores = BuildVakScores(progress, catalog, level);
        var competence = BlockAverage(progress, catalog, level, PupilMboHboBlock.Competentie);
        var values = BlockAverage(progress, catalog, level, PupilMboHboBlock.Werkwaarden);

        var questions = catalog.QuestionsFor(PupilMboHboBlock.Beroepen, level);
        return questions.Select(q =>
            {
                progress.Answers.TryGetValue(q.Id, out var answer);
                var name = q.Question
                    .Replace("Werken als ", "", StringComparison.OrdinalIgnoreCase)
                    .TrimEnd('.');
                var letter = ParseRiasecLetter(q.Dimension);
                var riasecFit = RiasecFit(riasec, letter);
                var subjectFit = PupilSubjectOccupationFit.FitForMboHboOccupation(q.EscoUri, vakScores);
                var beroepFit = answer <= 0 ? 0 : (answer - 1) / 4.0;
                var score = PupilCareerMatchWeights.Riasec * riasecFit
                            + PupilCareerMatchWeights.Vakken * subjectFit
                            + PupilCareerMatchWeights.MboBeroepentest * beroepFit
                            + PupilCareerMatchWeights.MboCompetentie * competence
                            + PupilCareerMatchWeights.MboWerkwaarden * values;
                return new RankedOccupation(name, SectorGroupKey(q.Dimension), Math.Clamp(score, 0, 1));
            })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<RankedOccupation> SelectForDisplay(
        IReadOnlyList<RankedOccupation> ranked,
        int maxCount,
        double minMatch)
        => PupilCareerMatchSelection.SelectTop(
            ranked,
            r => r.Score,
            r => r.SectorKey,
            minMatch,
            maxCount,
            PupilCareerMatchWeights.MaxPerGroup);

    private static string SectorGroupKey(string dimension)
    {
        var idx = dimension.IndexOf('(');
        return (idx > 0 ? dimension[..idx] : dimension).Trim();
    }

    private static char ParseRiasecLetter(string dimension)
    {
        var open = dimension.LastIndexOf('(');
        var close = dimension.LastIndexOf(')');
        if (open >= 0 && close > open + 1)
        {
            return char.ToUpperInvariant(dimension[open + 1]);
        }

        return 'S';
    }

    private static Dictionary<char, double> BuildRiasec(
        PupilMboHboProgressState progress,
        PupilMboHboCatalog catalog,
        SchoolLevel level)
    {
        var letters = new Dictionary<char, List<int>>
        {
            ['R'] = [],
            ['I'] = [],
            ['A'] = [],
            ['S'] = [],
            ['E'] = [],
            ['C'] = []
        };

        foreach (var q in catalog.QuestionsFor(PupilMboHboBlock.Interesse, level))
        {
            if (!progress.Answers.TryGetValue(q.Id, out var v))
            {
                continue;
            }

            var letter = ParseRiasecLetter(q.Dimension);
            if (letters.TryGetValue(letter, out var bucket))
            {
                bucket.Add(v);
            }
        }

        return letters.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.Count == 0 ? 0.35 : kv.Value.Average(v => (v - 1) / 4.0));
    }

    private static IReadOnlyList<(string VakItemId, double Score)> BuildVakScores(
        PupilMboHboProgressState progress,
        PupilMboHboCatalog catalog,
        SchoolLevel level)
    {
        var list = new List<(string, double)>();
        foreach (var q in catalog.QuestionsFor(PupilMboHboBlock.Vakken, level))
        {
            if (!progress.VakScores.TryGetValue(q.Id, out var s))
            {
                continue;
            }

            list.Add((q.Id, PupilVakScores.SubjectScore(s.Leuk, s.Goed)));
        }

        return list;
    }

    private static double BlockAverage(
        PupilMboHboProgressState progress,
        PupilMboHboCatalog catalog,
        SchoolLevel level,
        PupilMboHboBlock block)
    {
        var vals = catalog.QuestionsFor(block, level)
            .Where(q => progress.Answers.ContainsKey(q.Id))
            .Select(q => progress.Answers[q.Id])
            .ToList();
        if (vals.Count == 0)
        {
            return 0.35;
        }

        return vals.Average(v => (v - 1) / 4.0);
    }

    private static double RiasecFit(IReadOnlyDictionary<char, double> riasec, char letter)
        => riasec.TryGetValue(char.ToUpperInvariant(letter), out var v) ? v : 0.35;
}
