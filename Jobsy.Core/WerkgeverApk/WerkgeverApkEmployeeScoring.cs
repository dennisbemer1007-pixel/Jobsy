using Jobsy.Core.Enums;
using Jobsy.Core.Scholen.MboHbo;

namespace Jobsy.Core.WerkgeverApk;

public static class WerkgeverApkEmployeeScoring
{
    public static PupilMboHboScoring.BlockOutcome BuildInteresseOutcome(
        PupilMboHboProgressState progress,
        SchoolLevel level)
    {
        var catalog = WerkgeverApkEmployeeCatalog.Default;
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
            ['R'] = "R (Praktisch)",
            ['I'] = "I (Onderzoekend)",
            ['A'] = "A (Artistiek)",
            ['S'] = "S (Sociaal)",
            ['E'] = "E (Ondernemend)",
            ['C'] = "C (Conventioneel)"
        };

        var bars = letters
            .Select(kv => new PupilMboHboScoring.NamedScore(
                labels[kv.Key],
                AveragePercent(kv.Value)))
            .OrderByDescending(b => b.Percent)
            .ToList();

        return new PupilMboHboScoring.BlockOutcome(PupilMboHboBlock.Interesse, bars, "", []);
    }

    private static int AveragePercent(IReadOnlyList<int> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        var avg = values.Average();
        return (int)Math.Round(avg / 5.0 * 100.0, MidpointRounding.AwayFromZero);
    }
}