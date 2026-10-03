using System.Text.Json;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Scholen;

/// <summary>
/// Pure aggregator for class-level results. Applies k-anonymity (k ≥ 5)
/// and dream-job "Overig" for counts &lt; 2. Reused by school (02), teacher (03) and admin (07).
/// Callers must pass results from a single <see cref="PupilQuestionSet"/> only.
/// </summary>
public static class ClassResultsAggregator
{
    private static readonly string[] RiasecOrder = ["R", "I", "A", "S", "E", "C"];
    public const string UndecidedDreamJobKey = "weet-ik-nog-niet";

    /// <summary>
    /// Throws when any result's scoring version does not belong to <paramref name="questionSet"/>.
    /// G78 accepts <c>g78-*</c>; VO accepts <c>vo-*</c> or legacy <c>1</c>.
    /// </summary>
    public static void EnsureResultsBelongToSet(
        PupilQuestionSet questionSet,
        IReadOnlyList<PupilResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        foreach (var result in results)
        {
            if (!ScoringVersionBelongsTo(questionSet, result.ScoringVersion))
            {
                throw new InvalidOperationException(
                    $"Result scoring version '{result.ScoringVersion}' does not belong to question set '{questionSet}'.");
            }
        }
    }

    public static bool ScoringVersionBelongsTo(PupilQuestionSet questionSet, string? scoringVersion)
    {
        var v = (scoringVersion ?? string.Empty).Trim();
        return questionSet switch
        {
            PupilQuestionSet.Groep78 =>
                v.StartsWith("g78-", StringComparison.OrdinalIgnoreCase),
            PupilQuestionSet.Vo =>
                v.StartsWith("vo-", StringComparison.OrdinalIgnoreCase)
                || string.Equals(v, "1", StringComparison.Ordinal),
            _ => false
        };
    }

    public static ClassResultsAggregate Aggregate(
        IReadOnlyList<PupilResult> results,
        int totalCodes,
        PupilQuestionSet questionSet)
    {
        ArgumentNullException.ThrowIfNull(results);
        EnsureResultsBelongToSet(questionSet, results);
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

        var counts = CountAll(results);
        var riasecTop3 = counts.Riasec
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => Array.IndexOf(RiasecOrder, kv.Key.ToUpperInvariant()))
            .Take(3)
            .Where(kv => kv.Value > 0)
            .Select(kv => new NamedCount(kv.Key.ToUpperInvariant(), kv.Value))
            .ToList();

        var topValues = counts.Values
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .Select(kv => new NamedCount(kv.Key, kv.Value))
            .ToList();

        var dreamJobs = CollapseDreamJobs(counts.DreamJobs);

        return new ClassResultsAggregate(
            TotalCodes: totalCodes,
            CompletedCount: completed,
            CompletionPercent: completionPct,
            TotalsVisible: true,
            RiasecTop3: riasecTop3,
            TopValues: topValues,
            DreamJobs: dreamJobs);
    }

    /// <summary>
    /// Teacher group/overview insights: full RIASEC bars, cultures, competence bands.
    /// Empty lists when completed &lt; k.
    /// </summary>
    public static TeacherGroupAggregate AggregateTeacherGroup(
        IReadOnlyList<PupilResult> results,
        PupilQuestionSet questionSet)
    {
        ArgumentNullException.ThrowIfNull(results);
        EnsureResultsBelongToSet(questionSet, results);
        var completed = results.Count;
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
                UndecidedDreamJobCount: 0);
        }

        var counts = CountAll(results);
        var riasecBars = RiasecOrder
            .Select(letter => new NamedCount(letter, counts.Riasec.GetValueOrDefault(letter)))
            .ToList();

        var topValues = counts.Values
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .Select(kv => new NamedCount(kv.Key, kv.Value))
            .ToList();

        var topCultures = counts.Cultures
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .Select(kv => new NamedCount(kv.Key, kv.Value))
            .ToList();

        var competenceBands = counts.CompetenceBands
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => new NamedCount(kv.Key, kv.Value))
            .ToList();

        var undecided = counts.UndecidedDreamJobs;
        var dreamJobs = CollapseDreamJobs(counts.DreamJobs);

        return new TeacherGroupAggregate(
            Visible: true,
            CompletedCount: completed,
            RiasecBars: riasecBars,
            TopValues: topValues,
            TopCultures: topCultures,
            CompetenceBands: competenceBands,
            DreamJobs: dreamJobs,
            UndecidedDreamJobCount: undecided);
    }

    /// <summary>School-wide RIASEC top-3 over completed results of one test (same k gate).</summary>
    public static IReadOnlyList<NamedCount> SchoolRiasecTop3(
        IReadOnlyList<PupilResult> results,
        PupilQuestionSet questionSet)
    {
        EnsureResultsBelongToSet(questionSet, results);
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

    /// <summary>
    /// Raw dimension counts for aggregate snapshots (no k-gate). Dream jobs are not yet collapsed.
    /// </summary>
    public static RawResultCounts CountRaw(IReadOnlyList<PupilResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        var bag = CountAll(results);
        return new RawResultCounts(
            bag.Riasec,
            bag.Values,
            bag.Cultures,
            bag.CompetenceBands,
            bag.DreamJobs,
            bag.UndecidedDreamJobs);
    }

    /// <summary>Top-3 RIASEC letters by count (ties follow R-I-A-S-E-C).</summary>
    public static IReadOnlyDictionary<string, int> Top3Riasec(IReadOnlyDictionary<string, int> riasec)
    {
        return riasec
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => Array.IndexOf(RiasecOrder, kv.Key.ToUpperInvariant()))
            .Take(3)
            .Where(kv => kv.Value > 0)
            .ToDictionary(kv => kv.Key.ToUpperInvariant(), kv => kv.Value, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Dream jobs with count &lt; 2 collapse into "Overig".</summary>
    public static IReadOnlyDictionary<string, int> CollapseSparseDreamJobs(IReadOnlyDictionary<string, int> dreamCounts)
    {
        var dict = dreamCounts as Dictionary<string, int>
                   ?? dreamCounts.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
        return CollapseDreamJobs(dict).ToDictionary(n => n.Key, n => n.Count, StringComparer.OrdinalIgnoreCase);
    }

    public static Dictionary<string, int> MergeCounts(
        IEnumerable<IReadOnlyDictionary<string, int>> sources)
    {
        var merged = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in sources)
        {
            foreach (var kv in source)
            {
                merged[kv.Key] = merged.GetValueOrDefault(kv.Key) + kv.Value;
            }
        }

        return merged;
    }

    private static CountBag CountAll(IReadOnlyList<PupilResult> results)
    {
        var riasec = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var letter in RiasecOrder)
        {
            riasec[letter] = 0;
        }

        var values = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var cultures = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var competenceBands = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var dreamJobs = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var undecided = 0;

        foreach (var r in results)
        {
            CountRiasec(r, riasec);
            if (!string.IsNullOrWhiteSpace(r.TopValue))
            {
                var key = r.TopValue.Trim();
                values[key] = values.GetValueOrDefault(key) + 1;
            }

            if (!string.IsNullOrWhiteSpace(r.TopCulture))
            {
                var key = r.TopCulture.Trim();
                cultures[key] = cultures.GetValueOrDefault(key) + 1;
            }

            var band = CompetenceBand(r);
            if (band is not null)
            {
                competenceBands[band] = competenceBands.GetValueOrDefault(band) + 1;
            }

            if (string.IsNullOrWhiteSpace(r.DreamJobKey)
                || string.Equals(r.DreamJobKey.Trim(), UndecidedDreamJobKey, StringComparison.OrdinalIgnoreCase))
            {
                undecided++;
            }
            else
            {
                var key = r.DreamJobKey.Trim();
                dreamJobs[key] = dreamJobs.GetValueOrDefault(key) + 1;
            }
        }

        return new CountBag(riasec, values, cultures, competenceBands, dreamJobs, undecided);
    }

    private static void CountRiasec(PupilResult r, Dictionary<string, int> counts)
    {
        if (!string.IsNullOrWhiteSpace(r.HollandCode))
        {
            foreach (var ch in r.HollandCode.Trim().ToUpperInvariant())
            {
                var letter = ch.ToString();
                if (counts.TryGetValue(letter, out var value))
                {
                    counts[letter] = ++value;
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

            if (!string.IsNullOrWhiteSpace(top) && counts.TryGetValue(top, out var value))
            {
                counts[top] = ++value;
            }
        }
        catch (JsonException)
        {
            // ignore malformed snapshot
        }
    }

    /// <summary>
    /// Maps competence scores JSON to a coarse band label (counts only — never per code).
    /// Bands: Laag / Midden / Hoog based on average of numeric properties.
    /// </summary>
    private static string? CompetenceBand(PupilResult r)
    {
        try
        {
            using var doc = JsonDocument.Parse(
                string.IsNullOrWhiteSpace(r.CompetenceScoresJson) ? "{}" : r.CompetenceScoresJson);
            var scores = new List<double>();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Value.TryGetDouble(out var score))
                {
                    scores.Add(score);
                }
            }

            if (scores.Count == 0)
            {
                return null;
            }

            var avg = scores.Average();
            if (avg < 2.5)
            {
                return "Laag";
            }

            if (avg < 3.5)
            {
                return "Midden";
            }

            return "Hoog";
        }
        catch (JsonException)
        {
            return null;
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

    private sealed record CountBag(
        Dictionary<string, int> Riasec,
        Dictionary<string, int> Values,
        Dictionary<string, int> Cultures,
        Dictionary<string, int> CompetenceBands,
        Dictionary<string, int> DreamJobs,
        int UndecidedDreamJobs);
}

public sealed record NamedCount(string Key, int Count);

/// <summary>Ungated dimension counts used by the aggregate snapshotter (07).</summary>
public sealed record RawResultCounts(
    IReadOnlyDictionary<string, int> Riasec,
    IReadOnlyDictionary<string, int> Values,
    IReadOnlyDictionary<string, int> Cultures,
    IReadOnlyDictionary<string, int> CompetenceBands,
    IReadOnlyDictionary<string, int> DreamJobs,
    int UndecidedDreamJobs);

public sealed record ClassResultsAggregate(
    int TotalCodes,
    int CompletedCount,
    double CompletionPercent,
    bool TotalsVisible,
    IReadOnlyList<NamedCount> RiasecTop3,
    IReadOnlyList<NamedCount> TopValues,
    IReadOnlyList<NamedCount> DreamJobs);

public sealed record TeacherGroupAggregate(
    bool Visible,
    int CompletedCount,
    IReadOnlyList<NamedCount> RiasecBars,
    IReadOnlyList<NamedCount> TopValues,
    IReadOnlyList<NamedCount> TopCultures,
    IReadOnlyList<NamedCount> CompetenceBands,
    IReadOnlyList<NamedCount> DreamJobs,
    int UndecidedDreamJobCount);
