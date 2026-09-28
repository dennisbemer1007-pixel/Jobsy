namespace Jobsy.Core.Privacy;

/// <summary>
/// k-anonymity and rounding rules for employer candidate-insights aggregates.
/// Counts under the threshold are never returned; remaining counts are rounded to the nearest 5
/// so radius/period switches cannot reveal small groups by subtraction.
/// </summary>
public static class CandidateInsightsPrivacy
{
    public const int KAnonymityThreshold = 10;
    public const int CountRoundingStep = 5;

    public const string StatusOk = "ok";
    public const string StatusInsufficient = "insufficient";

    public static bool MeetsThreshold(int count) => count >= KAnonymityThreshold;

    /// <summary>Round a count that already passed the ≥10 check to the nearest 5.</summary>
    public static int RoundCount(int count)
    {
        if (count < KAnonymityThreshold)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Count must meet k-anonymity before rounding.");
        }

        return (int)(Math.Round(count / (double)CountRoundingStep, MidpointRounding.AwayFromZero) * CountRoundingStep);
    }

    public static (string Status, int? Value) SuppressCount(int rawCount)
    {
        if (!MeetsThreshold(rawCount))
        {
            return (StatusInsufficient, null);
        }

        return (StatusOk, RoundCount(rawCount));
    }

    /// <summary>
    /// Percentage over a denominator that must itself be ≥10; the numerator bucket must also be ≥10.
    /// </summary>
    public static (string Status, int? Percent) SuppressPercent(int numerator, int denominator)
    {
        if (!MeetsThreshold(denominator) || !MeetsThreshold(numerator))
        {
            return (StatusInsufficient, null);
        }

        var pct = (int)Math.Round(100.0 * numerator / denominator, MidpointRounding.AwayFromZero);
        return (StatusOk, Math.Clamp(pct, 0, 100));
    }
}
