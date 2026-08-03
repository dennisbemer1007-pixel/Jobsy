namespace Jobsy.Core.Rules;

/// <summary>
/// Pure Ambassadeur commission-tier rules (percentages as 0–100 display units, rates as 0–1).
/// </summary>
public static class AmbassadeurCommissionRules
{
    public const decimal DefaultBaseCommissionPercentage = 5.0m;
    public const int DefaultCandidateThreshold = 50;
    public const decimal DefaultPercentPerThreshold = 1.0m;
    public const decimal DefaultMaxCommissionPercentage = 15.0m;

    public const string CurrentAgreementVersion = "2026-08-03-ambassadeur-mediation";
    public const string TrackingCodePrefix = "AM-";

    /// <summary>
    /// Calculated commission percentage from registered candidate count and Admin settings.
    /// Does not apply an override — use <see cref="ResolveCurrentPercentage"/> for the effective value.
    /// </summary>
    public static decimal CalculatePercentage(
        int registeredCandidateCount,
        decimal basePercentage = DefaultBaseCommissionPercentage,
        int threshold = DefaultCandidateThreshold,
        decimal percentPerThreshold = DefaultPercentPerThreshold,
        decimal maxPercentage = DefaultMaxCommissionPercentage)
    {
        if (registeredCandidateCount < 0)
        {
            registeredCandidateCount = 0;
        }

        if (threshold <= 0)
        {
            threshold = DefaultCandidateThreshold;
        }

        var steps = registeredCandidateCount / threshold;
        var calculated = basePercentage + steps * percentPerThreshold;
        return ClampPercentage(calculated, maxPercentage);
    }

    /// <summary>
    /// Effective percentage: Admin override when set, otherwise threshold-based calculation; always capped.
    /// </summary>
    public static decimal ResolveCurrentPercentage(
        int registeredCandidateCount,
        decimal basePercentage,
        int threshold,
        decimal percentPerThreshold,
        decimal maxPercentage,
        decimal? percentageOverride)
    {
        if (percentageOverride is decimal overridden)
        {
            return ClampPercentage(overridden, maxPercentage);
        }

        return CalculatePercentage(
            registeredCandidateCount,
            basePercentage,
            threshold,
            percentPerThreshold,
            maxPercentage);
    }

    public static decimal PercentageToRate(decimal percentage) =>
        decimal.Round(percentage / 100m, 4, MidpointRounding.AwayFromZero);

    public static decimal ClampPercentage(decimal percentage, decimal maxPercentage)
    {
        if (percentage < 0m)
        {
            percentage = 0m;
        }

        var max = maxPercentage <= 0m ? DefaultMaxCommissionPercentage : maxPercentage;
        return percentage > max ? max : decimal.Round(percentage, 2, MidpointRounding.AwayFromZero);
    }

    public static bool IsAmbassadeurTrackingCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var normalized = code.Trim().ToUpperInvariant();
        return normalized.StartsWith(TrackingCodePrefix, StringComparison.Ordinal)
               && normalized.Length == TrackingCodePrefix.Length + 6;
    }
}
