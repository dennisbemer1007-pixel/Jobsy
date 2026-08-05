namespace Jobsy.Core.Rules;

/// <summary>
/// Launch promo: vacancy <em>publish</em> is free until an admin-configured date (inclusive,
/// Europe/Amsterdam calendar day). Highlight and PushBom stay paid. Welcome tokens are skipped
/// during the free period.
/// </summary>
public static class FreePublishRules
{
    /// <summary>Default end date (inclusive): 18 November 2026 (Europe/Amsterdam).</summary>
    public static readonly DateOnly DefaultUntil = new(2026, 11, 18);

    private static readonly TimeZoneInfo Dutch = ResolveDutchTimeZone();

    /// <summary>
    /// True when the Europe/Amsterdam calendar date of <paramref name="utcNow"/> is on or before
    /// <paramref name="freePublishUntil"/> (inclusive). Null <paramref name="freePublishUntil"/>
    /// means the promo is off.
    /// </summary>
    public static bool IsActive(DateOnly? freePublishUntil, DateTime utcNow)
    {
        if (freePublishUntil is not DateOnly until)
        {
            return false;
        }

        var utc = utcNow.Kind == DateTimeKind.Utc
            ? utcNow
            : DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, Dutch);
        return DateOnly.FromDateTime(local) <= until;
    }

    /// <summary>
    /// Effective publish cost: 0 during the free period, otherwise the category price.
    /// </summary>
    public static decimal EffectivePublishCost(
        decimal categoryPublishCostTokens,
        DateOnly? freePublishUntil,
        DateTime utcNow)
        => IsActive(freePublishUntil, utcNow) ? 0m : categoryPublishCostTokens;

    private static TimeZoneInfo ResolveDutchTimeZone()
    {
        foreach (var id in new[] { "Europe/Amsterdam", "W. Europe Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }
}
