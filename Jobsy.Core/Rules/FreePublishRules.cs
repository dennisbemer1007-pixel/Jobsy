namespace Jobsy.Core.Rules;

/// <summary>
/// Launch promo: vacancy <em>publish</em> is free until an admin-configured date (inclusive).
/// Highlight and PushBom stay paid. Welcome tokens are skipped during the free period.
/// </summary>
public static class FreePublishRules
{
    /// <summary>Default end date (inclusive): 18 November 2026.</summary>
    public static readonly DateOnly DefaultUntil = new(2026, 11, 18);

    /// <summary>
    /// True when <paramref name="utcNow"/> falls on or before <paramref name="freePublishUntil"/> (inclusive).
    /// Null <paramref name="freePublishUntil"/> means the promo is off.
    /// </summary>
    public static bool IsActive(DateOnly? freePublishUntil, DateTime utcNow)
        => freePublishUntil is DateOnly until
           && DateOnly.FromDateTime(utcNow) <= until;

    /// <summary>
    /// Effective publish cost: 0 during the free period, otherwise the category price.
    /// </summary>
    public static decimal EffectivePublishCost(
        decimal categoryPublishCostTokens,
        DateOnly? freePublishUntil,
        DateTime utcNow)
        => IsActive(freePublishUntil, utcNow) ? 0m : categoryPublishCostTokens;
}
