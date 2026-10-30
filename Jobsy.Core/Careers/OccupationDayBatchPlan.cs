using Jobsy.Core.Options;

namespace Jobsy.Core.Careers;

/// <summary>
/// Offline counts and OpenAI cost/duration estimates for filling every ESCO occupation day (NL + stored translations).
/// No API calls — safe to run in CI and admin dashboards.
/// </summary>
public static class OccupationDayBatchPlan
{
    public const string DefaultModel = OccupationDayInLifeOptions.DefaultModel;

    /// <summary>Average OpenAI chat calls to finish one occupation (Dutch compose + one call per missing UI translation).</summary>
    public static double TypicalCallsPerOccupation => 1.25 + OccupationDayTranslations.TargetLanguages.Count;

    public const int TypicalInputTokensPerCall = 1_400;
    public const int TypicalOutputTokensPerCall = 480;

    /// <summary>gpt-4o list prices (USD per 1M tokens), March 2026 reference.</summary>
    public const decimal Gpt4oInputUsdPerMillion = 2.50m;
    public const decimal Gpt4oOutputUsdPerMillion = 10.00m;

    public static int CatalogTotal => OccupationCatalog.Shared.All.Count;

    public static OccupationDayStoredCounts CountStored(IEnumerable<OccupationDayStoredRow> rows)
    {
        var list = rows as IReadOnlyList<OccupationDayStoredRow> ?? rows.ToList();
        var dutch = 0;
        var complete = 0;
        foreach (var row in list)
        {
            if (HasDutch(row))
            {
                dutch++;
            }

            if (OccupationDayTranslations.IsComplete(row.TranslationsJson, row.ContentHash))
            {
                complete++;
            }
        }

        var perLanguage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var language in OccupationDayTranslations.TargetLanguages.Prepend("nl"))
        {
            perLanguage[language] = language.Equals("nl", StringComparison.OrdinalIgnoreCase)
                ? dutch
                : CountLanguage(list, language);
        }

        return new OccupationDayStoredCounts(
            list.Count,
            dutch,
            complete,
            Math.Max(0, CatalogTotal - complete),
            perLanguage);
    }

    public static OccupationDayBatchEstimate Estimate(int occupationsToFill, OccupationDayInLifeOptions? options = null)
    {
        options ??= new OccupationDayInLifeOptions();
        occupationsToFill = Math.Max(0, occupationsToFill);
        var calls = (int)Math.Ceiling(occupationsToFill * TypicalCallsPerOccupation);
        var inputTokens = calls * TypicalInputTokensPerCall;
        var outputTokens = calls * TypicalOutputTokensPerCall;
        var inputUsd = inputTokens / 1_000_000m * Gpt4oInputUsdPerMillion;
        var outputUsd = outputTokens / 1_000_000m * Gpt4oOutputUsdPerMillion;
        var delayMs = Math.Max(0, options.DelayMilliseconds);
        var delaySeconds = occupationsToFill * TypicalCallsPerOccupation * delayMs / 1000.0;
        const double secondsPerCall = 4.5;
        var apiSeconds = calls * secondsPerCall;
        var totalSeconds = delaySeconds + apiSeconds;
        return new OccupationDayBatchEstimate(
            options.Model ?? DefaultModel,
            occupationsToFill,
            calls,
            inputTokens,
            outputTokens,
            inputUsd + outputUsd,
            TimeSpan.FromSeconds(totalSeconds));
    }

    public static OccupationDayBatchEstimate EstimateRemaining(OccupationDayStoredCounts counts, OccupationDayInLifeOptions? options = null)
        => Estimate(counts.RemainingComplete, options);

    private static bool HasDutch(OccupationDayStoredRow row)
        => !string.IsNullOrWhiteSpace(row.Morning)
           && !string.IsNullOrWhiteSpace(row.Midday)
           && !string.IsNullOrWhiteSpace(row.Afternoon)
           && !string.IsNullOrWhiteSpace(row.Closing);

    private static int CountLanguage(IReadOnlyList<OccupationDayStoredRow> rows, string language)
    {
        var count = 0;
        foreach (var row in rows)
        {
            if (OccupationDayTranslations.TryGet(row.TranslationsJson, language, row.ContentHash, out _))
            {
                count++;
            }
        }

        return count;
    }
}

public sealed record OccupationDayStoredRow(string EscoId, string? ContentHash, string? TranslationsJson, string? Morning, string? Midday, string? Afternoon, string? Closing);

public sealed record OccupationDayStoredCounts(
    int RowsInDatabase,
    int DutchComplete,
    int AllLanguagesComplete,
    int RemainingComplete,
    IReadOnlyDictionary<string, int> PerLanguage);

public sealed record OccupationDayBatchEstimate(
    string Model,
    int Occupations,
    int OpenAiCalls,
    int InputTokens,
    int OutputTokens,
    decimal EstimatedUsd,
    TimeSpan EstimatedDuration);
