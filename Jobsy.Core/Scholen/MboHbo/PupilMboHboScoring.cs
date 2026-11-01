using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Scholen.MboHbo;

public static class PupilMboHboScoring
{
    public sealed record BlockOutcome(
        PupilMboHboBlock Block,
        IReadOnlyList<NamedScore> Bars,
        string Summary,
        IReadOnlyList<string> Highlights);

    public sealed record NamedScore(string Label, int Percent);

    public static BlockOutcome BuildOutcome(
        PupilMboHboBlock block,
        PupilMboHboProgressState progress,
        SchoolLevel level,
        PupilMboHboCatalog? catalog = null)
    {
        catalog ??= PupilMboHboCatalog.Default;
        return block switch
        {
            PupilMboHboBlock.Vakken => ScoreVakken(progress, catalog, level),
            PupilMboHboBlock.Interesse => ScoreInteresse(progress, catalog, level),
            PupilMboHboBlock.Competentie => ScoreCompetentie(progress, catalog, level),
            PupilMboHboBlock.Beroepen => ScoreBeroepen(progress, catalog, level),
            PupilMboHboBlock.Werkwaarden => ScoreWerkwaarden(progress, catalog, level),
            _ => new BlockOutcome(block, [], "", [])
        };
    }

    public static bool DetectAnswerPattern(IReadOnlyList<int> values, double threshold = 0.90)
    {
        if (values.Count < 10)
        {
            return false;
        }

        var groups = values.GroupBy(v => v).OrderByDescending(g => g.Count()).ToList();
        if (groups.Count == 0)
        {
            return false;
        }

        return groups[0].Count() / (double)values.Count >= threshold;
    }

    public static bool BlockHasPatternSignal(
        PupilMboHboBlock block,
        PupilMboHboProgressState progress,
        SchoolLevel level,
        PupilMboHboCatalog? catalog = null)
    {
        catalog ??= PupilMboHboCatalog.Default;
        if (block == PupilMboHboBlock.Vakken)
        {
            var leuk = progress.VakScores.Values.Select(v => v.Leuk).ToList();
            var goed = progress.VakScores.Values.Select(v => v.Goed).ToList();
            return DetectAnswerPattern(leuk) || DetectAnswerPattern(goed);
        }

        var ids = catalog.QuestionsFor(block, level).Select(q => q.Id).ToList();
        var vals = ids.Where(progress.Answers.ContainsKey).Select(id => progress.Answers[id]).ToList();
        return DetectAnswerPattern(vals);
    }

    private static BlockOutcome ScoreVakken(
        PupilMboHboProgressState progress,
        PupilMboHboCatalog catalog,
        SchoolLevel level)
    {
        var questions = catalog.QuestionsFor(PupilMboHboBlock.Vakken, level);
        var ranked = questions
            .Select(q =>
            {
                progress.VakScores.TryGetValue(q.Id, out var s);
                var leuk = s?.Leuk ?? 0;
                var goed = s?.Goed ?? 0;
                var combined = leuk + goed;
                var label = q.Question.Replace("Het vak ", "", StringComparison.Ordinal)
                    .TrimEnd('.');
                return (label, combined, leuk, goed);
            })
            .OrderByDescending(x => x.combined)
            .ThenBy(x => x.label, StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();

        var bars = ranked.Take(5)
            .Select(x => new NamedScore(
                x.label,
                LikertAnswerJson.ToPercent([(x.leuk + x.goed) / 2])))
            .ToList();

        var top = ranked.Take(3).Select(x => x.label).ToList();
        var summary = top.Count > 0
            ? $"Je vakken met de hoogste scores: {string.Join(", ", top)}."
            : "Je hebt je vakken ingevuld.";

        return new BlockOutcome(
            PupilMboHboBlock.Vakken,
            bars,
            summary,
            top);
    }

    private static BlockOutcome ScoreInteresse(
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

            var letter = q.Dimension.TrimStart()[0];
            if (letters.TryGetValue(letter, out var bucket))
            {
                bucket.Add(v);
            }
        }

        var labels = new Dictionary<char, string>
        {
            ['R'] = "Praktisch",
            ['I'] = "Onderzoekend",
            ['A'] = "Artistiek",
            ['S'] = "Sociaal",
            ['E'] = "Ondernemend",
            ['C'] = "Conventioneel"
        };

        var bars = letters
            .Select(kv => new NamedScore(
                labels[kv.Key],
                AveragePercent(kv.Value)))
            .OrderByDescending(b => b.Percent)
            .ToList();

        var top2 = bars.Take(2).Select(b => b.Label).ToList();
        var summary = top2.Count == 2
            ? $"Je scoort het hoogst op {top2[0]} en {top2[1]}. Dat zijn activiteiten die je nu aantrekkelijk vindt."
            : "Dit is een beeld van wat je nu leuk vindt. Dat kan later veranderen.";

        return new BlockOutcome(PupilMboHboBlock.Interesse, bars, summary, top2);
    }

    private static BlockOutcome ScoreCompetentie(
        PupilMboHboProgressState progress,
        PupilMboHboCatalog catalog,
        SchoolLevel level)
    {
        var byDim = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var q in catalog.QuestionsFor(PupilMboHboBlock.Competentie, level))
        {
            if (!progress.Answers.TryGetValue(q.Id, out var v))
            {
                continue;
            }

            if (!byDim.TryGetValue(q.Dimension, out var list))
            {
                list = [];
                byDim[q.Dimension] = list;
            }

            list.Add(v);
        }

        var bars = byDim
            .Select(kv => new NamedScore(kv.Key, AveragePercent(kv.Value)))
            .OrderByDescending(b => b.Percent)
            .ToList();

        var strong = bars.Where(b => b.Percent >= 60).Take(3).Select(b => b.Label).ToList();
        var grow = bars.Where(b => b.Percent < 50).Take(1).Select(b => b.Label).ToList();
        var summary = strong.Count > 0
            ? $"Sterke kanten: {string.Join(", ", strong)}."
            : "Je hebt aangegeven waar je nu staat.";
        if (grow.Count > 0)
        {
            summary += $" Mogelijk wil je verder ontwikkelen: {grow[0]}.";
        }

        return new BlockOutcome(PupilMboHboBlock.Competentie, bars, summary, strong);
    }

    private static BlockOutcome ScoreBeroepen(
        PupilMboHboProgressState progress,
        PupilMboHboCatalog catalog,
        SchoolLevel level)
    {
        var ranked = PupilMboHboOccupationMatch.TopOccupations(progress, level, catalog);

        var bars = ranked
            .Select((x, i) => new NamedScore(x.Label, i + 1))
            .ToList();

        var top = ranked.Take(3).Select(x => x.Label).ToList();
        var summary = top.Count > 0
            ? $"Suggesties die nu goed bij je passen: {string.Join(", ", top)}."
            : "Je hebt aangegeven welke beroepen bij je passen. Dit is een suggestie — jij kiest zelf.";

        return new BlockOutcome(PupilMboHboBlock.Beroepen, bars, summary, top);
    }

    private static BlockOutcome ScoreWerkwaarden(
        PupilMboHboProgressState progress,
        PupilMboHboCatalog catalog,
        SchoolLevel level)
    {
        var byDim = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var q in catalog.QuestionsFor(PupilMboHboBlock.Werkwaarden, level))
        {
            if (!progress.Answers.TryGetValue(q.Id, out var v))
            {
                continue;
            }

            var key = ShortValueLabel(q.Dimension);
            if (!byDim.TryGetValue(key, out var list))
            {
                list = [];
                byDim[key] = list;
            }

            list.Add(v);
        }

        var bars = byDim
            .Select(kv => new NamedScore(kv.Key, AveragePercent(kv.Value)))
            .OrderByDescending(b => b.Percent)
            .ToList();

        var top = bars.Take(3).Select(b => b.Label).ToList();
        var summary = top.Count > 0
            ? $"Wat je belangrijk vindt: {string.Join(", ", top)}."
            : "Je hebt aangegeven wat je belangrijk vindt in werk.";

        return new BlockOutcome(PupilMboHboBlock.Werkwaarden, bars, summary, top);
    }

    private static int AveragePercent(IReadOnlyList<int> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        return LikertAnswerJson.ToPercent(values);
    }

    private static string ShortValueLabel(string dimension)
    {
        if (dimension.Contains("Zelfstandigheid", StringComparison.OrdinalIgnoreCase))
        {
            return "Zelfstandigheid";
        }

        if (dimension.Contains("Verbinding", StringComparison.OrdinalIgnoreCase))
        {
            return "Verbinding";
        }

        if (dimension.Contains("Prestatie", StringComparison.OrdinalIgnoreCase))
        {
            return "Prestatie";
        }

        if (dimension.Contains("Zekerheid", StringComparison.OrdinalIgnoreCase))
        {
            return "Zekerheid";
        }

        if (dimension.Contains("Impact", StringComparison.OrdinalIgnoreCase))
        {
            return "Impact";
        }

        if (dimension.Contains("salaris", StringComparison.OrdinalIgnoreCase))
        {
            return "Salaris";
        }

        if (dimension.Contains("parttime", StringComparison.OrdinalIgnoreCase))
        {
            return "Parttime";
        }

        if (dimension.Contains("thuis", StringComparison.OrdinalIgnoreCase))
        {
            return "Thuiswerken";
        }

        if (dimension.Contains("vaste plek", StringComparison.OrdinalIgnoreCase))
        {
            return "Vaste plek";
        }

        return dimension.Length > 28 ? dimension[..28] : dimension;
    }
}
