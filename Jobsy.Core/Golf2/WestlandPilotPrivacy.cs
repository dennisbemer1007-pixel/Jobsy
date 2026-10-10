namespace Jobsy.Core.Golf2;

/// <summary>
/// k-anonymity for Westland pilot aggregates. Counts under 10 are suppressed; counts at or above 10
/// are returned exactly (no rounding unlike employer candidate-insights).
/// </summary>
public static class WestlandPilotPrivacy
{
    public const int KAnonymityThreshold = 10;

    public const string StatusOk = "ok";
    public const string StatusInsufficient = "insufficient";

    public static bool MeetsThreshold(int count) => count >= KAnonymityThreshold;

    public static (string Status, int? Value) SuppressCount(int rawCount)
    {
        if (!MeetsThreshold(rawCount))
        {
            return (StatusInsufficient, null);
        }

        return (StatusOk, rawCount);
    }

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
