using System.Text.Json;
using Jobsy.Core.Entities.Scholen;

namespace Jobsy.Core.Scholen;

/// <summary>
/// Pure aggregator for class-level results. Applies k-anonymity (k ≥ 5)
/// and dream-job "Overig" for counts &lt; 2. Reused by school (02), teacher (03) and admin (07).
/// </summary>
public static class ClassResultsAggregator
{
    private static readonly string[] RiasecOrder = ["R", "I", "A", "S", "E", "C"];

    public static ClassResultsAggregate Aggregate(
        IReadOnlyList<PupilResult> results,
        int totalCodes)
    {
        ArgumentNullException.ThrowIfNull(results);
        var completed = results.Count;
        var completionPct = totalCodes <= 0 ? 0d : Math.Round(100d * completed / totalCodes, 1);

        if (completed < SchoolAnonymity.MinGroupSize)
        {
            return new ClassResultsAggregate(
                TotalCodes: totalCodes,
                CompletedCount: completed,
                CompletionPercent: completionPct,
                TotalsVisible: false,
                RiasecTop3: [],
                TopValues: [],
                DreamJobs: []);
        }

        var riasecCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var letter in RiasecOrder)
        {
            riasecCounts[letter] = 0;
        }

        var valueCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var dreamCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var r in results)
        {
            CountRiasec(r, riasecCounts);
            if (!string.IsNullOrWhiteSpace(r.TopValue))
            {
                var key = r.TopValue.Trim();
                valueCounts[key] = valueCounts.GetValueOrDefault(key) + 1;
            }

            if (!string.IsNullOrWhiteSpace(r.DreamJobKey))
            {
                var key = r.DreamJobKey.Trim();
                dreamCounts[key] = dreamCounts.GetValueOrDefault(key) + 1;
            }
        }

        var riasecTop3 = riasecCounts
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => Array.IndexOf(RiasecOrder, kv.Key.ToUpperInvariant()))
            .Take(3)
            .Where(kv => kv.Value > 0)
            .Select(kv => new NamedCount(kv.Key.ToUpperInvariant(), kv.Value))
            .ToList();

        var topValues = valueCounts
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .Select(kv => new NamedCount(kv.Key, kv.Value))
            .ToList();

        var dreamJobs = CollapseDreamJobs(dreamCounts);

        return new ClassResultsAggregate(
            TotalCodes: totalCodes,
            CompletedCount: completed,
            CompletionPercent: completionPct,
            TotalsVisible: true,
            RiasecTop3: riasecTop3,
            TopValues: topValues,
            DreamJobs: dreamJobs);
    }

    /// <summary>School-wide RIASEC top-3 over completed results (same k gate).</summary>
    public static IReadOnlyList<NamedCount> SchoolRiasecTop3(IReadOnlyList<PupilResult> results)
    {
        if (results.Count < SchoolAnonymity.MinGroupSize)
        {
            return [];
        }

        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var letter in RiasecOrder)
        {
            counts[letter] = 0;
        }

        foreach (var r in results)
        {
            CountRiasec(r, counts);
        }

        return counts
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => Array.IndexOf(RiasecOrder, kv.Key.ToUpperInvariant()))
            .Take(3)
            .Where(kv => kv.Value > 0)
            .Select(kv => new NamedCount(kv.Key.ToUpperInvariant(), kv.Value))
            .ToList();
    }

    private static void CountRiasec(PupilResult r, Dictionary<string, int> counts)
    {
        if (!string.IsNullOrWhiteSpace(r.HollandCode))
        {
            foreach (var ch in r.HollandCode.Trim().ToUpperInvariant())
            {
                var letter = ch.ToString();
                if (counts.ContainsKey(letter))
                {
                    counts[letter]++;
                }
            }

            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(r.RiasecScoresJson) ? "{}" : r.RiasecScoresJson);
            string? top = null;
            var topScore = double.MinValue;
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Value.TryGetDouble(out var score) && score > topScore)
                {
                    topScore = score;
                    top = prop.Name;
                }
            }

            if (!string.IsNullOrWhiteSpace(top) && counts.ContainsKey(top))
            {
                counts[top]++;
            }
        }
        catch (JsonException)
        {
            // ignore malformed snapshot
        }
    }

    private static IReadOnlyList<NamedCount> CollapseDreamJobs(Dictionary<string, int> dreamCounts)
    {
        var kept = new List<NamedCount>();
        var overig = 0;
        foreach (var kv in dreamCounts.OrderByDescending(kv => kv.Value)
                     .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (kv.Value < SchoolAnonymity.MinDreamJobCount)
            {
                overig += kv.Value;
            }
            else
            {
                kept.Add(new NamedCount(kv.Key, kv.Value));
            }
        }

        if (overig > 0)
        {
            kept.Add(new NamedCount("Overig", overig));
        }

        return kept;
    }
}

public sealed record NamedCount(string Key, int Count);

public sealed record ClassResultsAggregate(
    int TotalCodes,
    int CompletedCount,
    double CompletionPercent,
    bool TotalsVisible,
    IReadOnlyList<NamedCount> RiasecTop3,
    IReadOnlyList<NamedCount> TopValues,
    IReadOnlyList<NamedCount> DreamJobs);
