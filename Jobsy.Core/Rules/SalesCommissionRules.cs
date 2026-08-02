namespace Jobsy.Core.Rules;

/// <summary>
/// Pure commission / revenue-share rules for the salesmanager network (ex-VAT amounts).
/// Defaults: ambassador 15% tokens, direct SM 15% (1 year), indirect referring SM 3%, platform remainder.
/// Rates and duration are Admin-configurable via <c>SalesCommercialSettings</c>; these constants are defaults.
/// </summary>
public static class SalesCommissionRules
{
    public const decimal VatRate = 0.21m;
    public const decimal FirstYearOnboardingEuro = 2500.00m;
    public const decimal FounderBonusRate = 0.20m;
    public const int MaxFounderSlots = 10;

    /// <summary>Ambassador (Ondernemer) share of token purchase value → company tegoed.</summary>
    public const decimal AmbassadorShareRate = 0.15m;

    /// <summary>Default direct commission for the primary salesmanager (Admin-configurable).</summary>
    public const decimal DefaultDirectCommissionRate = 0.15m;

    /// <summary>Default passive referral bonus for the referring salesmanager (Admin-configurable).</summary>
    public const decimal DefaultIndirectCommissionRate = 0.03m;

    /// <summary>Default commission duration in days for an onboarded entrepreneur (1 year).</summary>
    public const int DefaultCommissionDurationDays = 365;

    /// <summary>Legacy alias for the default direct SM share.</summary>
    public const decimal SalesManagerShareRate = DefaultDirectCommissionRate;

    /// <summary>Legacy alias — year-1 window uses the configurable direct rate.</summary>
    public const decimal Year1TokenCommissionRate = DefaultDirectCommissionRate;

    /// <summary>Legacy alias — after the commission window no ongoing SM share.</summary>
    public const decimal Year2TokenCommissionRate = 0m;

    public const string CurrentAgreementVersion = "2026-07-27-sm-mediation";

    public static decimal FounderBonusExVat =>
        decimal.Round(FirstYearOnboardingEuro * FounderBonusRate, 2, MidpointRounding.AwayFromZero);

    public static decimal VatOn(decimal amountExVat) =>
        decimal.Round(amountExVat * VatRate, 2, MidpointRounding.AwayFromZero);

    public static decimal InclVat(decimal amountExVat) => amountExVat + VatOn(amountExVat);

    public static decimal ShareEuro(decimal purchaseAmountEuro, decimal rate) =>
        decimal.Round(purchaseAmountEuro * rate, 2, MidpointRounding.AwayFromZero);

    public static decimal AmbassadorTokens(int packSize) =>
        decimal.Round(packSize * AmbassadorShareRate, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Whether commission still accrues for a referred entrepreneur at <paramref name="asOfUtc"/>.
    /// Window starts at <paramref name="firstYearStartedAt"/> and lasts <paramref name="durationDays"/> (default 365).
    /// </summary>
    public static bool IsWithinCommissionWindow(
        DateTime? firstYearStartedAt,
        DateTime asOfUtc,
        int durationDays = DefaultCommissionDurationDays)
    {
        if (firstYearStartedAt is null || durationDays <= 0)
        {
            return false;
        }

        var end = firstYearStartedAt.Value.AddDays(durationDays);
        return asOfUtc < end;
    }

    /// <summary>
    /// Direct salesmanager token commission rate when inside the configured window; otherwise null.
    /// </summary>
    public static decimal? TokenCommissionRate(
        DateTime? firstYearStartedAt,
        DateTime asOfUtc,
        decimal directRate = DefaultDirectCommissionRate,
        int durationDays = DefaultCommissionDurationDays)
    {
        if (!IsWithinCommissionWindow(firstYearStartedAt, asOfUtc, durationDays))
        {
            return null;
        }

        return directRate < 0 ? null : directRate;
    }

    /// <summary>
    /// Indirect (referring) salesmanager rate when inside the window and a positive rate is configured.
    /// </summary>
    public static decimal? IndirectCommissionRate(
        DateTime? firstYearStartedAt,
        DateTime asOfUtc,
        decimal indirectRate = DefaultIndirectCommissionRate,
        int durationDays = DefaultCommissionDurationDays)
    {
        if (indirectRate <= 0 || !IsWithinCommissionWindow(firstYearStartedAt, asOfUtc, durationDays))
        {
            return null;
        }

        return indirectRate;
    }

    public static decimal PlatformShareRate(decimal directRate, decimal indirectRate = 0m)
    {
        var remainder = 1m - AmbassadorShareRate - Math.Max(0m, directRate) - Math.Max(0m, indirectRate);
        return remainder < 0 ? 0m : remainder;
    }

    public static bool IsEligibleFounderSlot(int? slot) =>
        slot is >= 1 and <= MaxFounderSlots;
}
