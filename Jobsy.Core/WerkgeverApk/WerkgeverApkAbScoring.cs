using Jobsy.Core.Scholen.MboHbo;

namespace Jobsy.Core.WerkgeverApk;

/// <summary>Counts A/B picks per dimension; maps to comparison Likert-style scores.</summary>
public static class WerkgeverApkAbScoring
{
    public sealed record DimensionCounts(IReadOnlyDictionary<string, int> Counts);

    public static DimensionCounts CountDimensions(
        PupilMboHboProgressState progress,
        WerkgeverApkEmployeeAbCatalog catalog)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in catalog.Items)
        {
            if (!progress.Answers.TryGetValue(item.Id, out var value)
                || !WerkgeverApkAnswerValidation.IsValidForItem(item, value))
            {
                continue;
            }

            if (item.Kind == WerkgeverApkAbItemKind.Ab)
            {
                var dim = value == WerkgeverApkAnswerValidation.AbChoiceA ? item.DimensionA : item.DimensionB;
                if (!string.IsNullOrWhiteSpace(dim))
                {
                    counts[dim] = counts.GetValueOrDefault(dim) + 1;
                }
            }
            else if (item.Kind == WerkgeverApkAbItemKind.Check && !string.IsNullOrWhiteSpace(item.DimensionA))
            {
                counts[item.DimensionA] = counts.GetValueOrDefault(item.DimensionA) + value;
            }
        }

        return new DimensionCounts(counts);
    }

    /// <summary>Relative strength 1–5 for manager comparison (no cijfer shown to users).</summary>
    public static int? RelativeLikertScore(DimensionCounts counts, string dimensionKey)
    {
        if (counts.Counts.Count == 0)
        {
            return null;
        }

        var match = counts.Counts
            .Where(kv => DimensionKeyMatches(kv.Key, dimensionKey))
            .Select(kv => kv.Value)
            .DefaultIfEmpty(0)
            .Max();
        if (match == 0)
        {
            return null;
        }

        var max = counts.Counts.Values.Max();
        if (max <= 0)
        {
            return null;
        }

        var ratio = (double)match / max;
        return (int)Math.Clamp(Math.Round(1 + ratio * 4, MidpointRounding.AwayFromZero), 1, 5);
    }

    public static PupilMboHboScoring.BlockOutcome BuildInteresseOutcome(
        PupilMboHboProgressState progress,
        WerkgeverApkEmployeeAbCatalog catalog)
    {
        var counts = CountDimensions(progress, catalog);
        var riasec = new[] { 'R', 'I', 'A', 'S', 'E', 'C' };
        var labels = new Dictionary<char, string>
        {
            ['R'] = "R (Praktisch)",
            ['I'] = "I (Onderzoekend)",
            ['A'] = "A (Creatief)",
            ['S'] = "S (Sociaal)",
            ['E'] = "E (Ondernemend)",
            ['C'] = "C (Geordend)",
        };

        var bars = riasec
            .Select(letter =>
            {
                var label = labels[letter];
                var score = counts.Counts
                    .Where(kv => kv.Key.TrimStart().StartsWith(letter, StringComparison.Ordinal))
                    .Sum(kv => kv.Value);
                var pct = counts.Counts.Values.DefaultIfEmpty(1).Max();
                var percent = pct > 0 ? (int)Math.Round(100.0 * score / pct, MidpointRounding.AwayFromZero) : 0;
                return new PupilMboHboScoring.NamedScore(label, Math.Clamp(percent, 0, 100));
            })
            .OrderByDescending(b => b.Percent)
            .ToList();

        return new PupilMboHboScoring.BlockOutcome(PupilMboHboBlock.Interesse, bars, "", []);
    }

    public static IReadOnlyList<string> ValidateMinimumCoverage(WerkgeverApkEmployeeAbCatalog catalog, int minPerDimension)
    {
        var issues = new List<string>();
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in catalog.Items)
        {
            if (item.Kind == WerkgeverApkAbItemKind.Ab)
            {
                Bump(counts, item.DimensionA);
                if (!string.IsNullOrWhiteSpace(item.DimensionB))
                {
                    Bump(counts, item.DimensionB);
                }
            }
            else
            {
                Bump(counts, item.DimensionA);
            }
        }

        foreach (var kv in counts.Where(kv => kv.Value < minPerDimension))
        {
            issues.Add($"{kv.Key}: {kv.Value} (min {minPerDimension})");
        }

        return issues;
    }

    private static void Bump(Dictionary<string, int> counts, string? dim)
    {
        if (string.IsNullOrWhiteSpace(dim))
        {
            return;
        }

        counts[dim] = counts.GetValueOrDefault(dim) + 1;
    }

    private static bool DimensionKeyMatches(string haystack, string needle)
    {
        if (string.IsNullOrWhiteSpace(haystack) || string.IsNullOrWhiteSpace(needle))
        {
            return false;
        }

        return haystack.Contains(needle, StringComparison.OrdinalIgnoreCase)
               || needle.Contains(haystack, StringComparison.OrdinalIgnoreCase);
    }
}