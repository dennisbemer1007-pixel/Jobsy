namespace Jobsy.Core.Rules;

/// <summary>
/// Pure commission / revenue-share rules for the salesmanager network (ex-VAT amounts).
/// Staffels (admin-configurable via <c>SalesCommercialSettings</c>):
/// standard 25% / 10% / 5% over 3 years; referred year-1 20% with 5% referrer override.
/// </summary>
public static class SalesCommissionRules
{
    public const decimal VatRate = 0.21m;
    public const decimal FirstYearOnboardingEuro = 2500.00m;
    public const decimal FounderBonusRate = 0.20m;
    public const int MaxFounderSlots = 10;
    public const int CommissionYearLengthDays = 365;

    /// <summary>Ambassador (Ondernemer) share of token purchase value → company tegoed.</summary>
    public const decimal AmbassadorShareRate = 0.15m;

    /// <summary>Default year-1 commission for a salesmanager who was not referred (standard track).</summary>
    public const decimal DefaultDirectCommissionRate = 0.25m;

    /// <summary>Default year-2 direct commission (standard and referred tracks).</summary>
    public const decimal DefaultYear2DirectCommissionRate = 0.10m;

    /// <summary>Default year-3 direct commission (standard and referred tracks).</summary>
    public const decimal DefaultYear3DirectCommissionRate = 0.05m;

    /// <summary>Default year-1 commission when the salesmanager was aangedragen (referred track).</summary>
    public const decimal DefaultReferredYear1DirectCommissionRate = 0.20m;

    /// <summary>Default referrer override: 5% of token purchases in year 1 of a referred salesmanager.</summary>
    public const decimal DefaultIndirectCommissionRate = 0.05m;

    /// <summary>
    /// Legacy default cash commission for partner affiliates — retired in favour of token rewards.
    /// Kept for existing settings rows / migrations; no longer applied on token purchases.
    /// </summary>
    public const decimal DefaultPartnerCommissionRate = 0.05m;

    /// <summary>
    /// Token bonus credited to the referring partner when a referred company spends its welcome token.
    /// </summary>
    public const decimal PartnerReferralRewardTokens = 0.5m;

    /// <summary>Default commission duration in days (3 years).</summary>
    public const int DefaultCommissionDurationDays = 1095;

    /// <summary>Legacy alias for the default year-1 SM share.</summary>
    public const decimal SalesManagerShareRate = DefaultDirectCommissionRate;

    /// <summary>Legacy alias — year-1 window uses the configurable direct rate.</summary>
    public const decimal Year1TokenCommissionRate = DefaultDirectCommissionRate;

    /// <summary>Legacy alias — year-2 staffel.</summary>
    public const decimal Year2TokenCommissionRate = DefaultYear2DirectCommissionRate;

    public const string CurrentAgreementVersion = "2026-08-28-sm-mediation";

    /// <summary>Partner affiliate (BM/IM) mediation agreement version — server-controlled.</summary>
    public const string CurrentPartnerAgreementVersion = "2026-08-06-partner-mediation";

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
    /// Window starts at <paramref name="firstYearStartedAt"/> and lasts <paramref name="durationDays"/>.
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

    /// <summary>0 = year 1, 1 = year 2, 2 = year 3+. Null when outside the window.</summary>
    public static int? CommissionYearIndex(
        DateTime? firstYearStartedAt,
        DateTime asOfUtc,
        int durationDays = DefaultCommissionDurationDays,
        int yearLengthDays = CommissionYearLengthDays)
    {
        if (!IsWithinCommissionWindow(firstYearStartedAt, asOfUtc, durationDays)
            || firstYearStartedAt is null
            || yearLengthDays <= 0)
        {
            return null;
        }

        var elapsed = (asOfUtc - firstYearStartedAt.Value).TotalDays;
        if (elapsed < 0)
        {
            return null;
        }

        return Math.Min(2, (int)(elapsed / yearLengthDays));
    }

    /// <summary>
    /// Direct salesmanager token commission rate for the current staffel year; otherwise null.
    /// </summary>
    public static decimal? TokenCommissionRate(
        DateTime? firstYearStartedAt,
        DateTime asOfUtc,
        decimal directRate = DefaultDirectCommissionRate,
        int durationDays = DefaultCommissionDurationDays,
        decimal year2Rate = DefaultYear2DirectCommissionRate,
        decimal year3Rate = DefaultYear3DirectCommissionRate)
    {
        var year = CommissionYearIndex(firstYearStartedAt, asOfUtc, durationDays);
        return year switch
        {
            0 => directRate < 0 ? null : directRate,
            1 => year2Rate < 0 ? null : year2Rate,
            2 => year3Rate < 0 ? null : year3Rate,
            _ => null
        };
    }

    /// <summary>
    /// Indirect (referring) salesmanager rate — year 1 of the window only, when a positive rate is configured.
    /// </summary>
    public static decimal? IndirectCommissionRate(
        DateTime? firstYearStartedAt,
        DateTime asOfUtc,
        decimal indirectRate = DefaultIndirectCommissionRate,
        int durationDays = DefaultCommissionDurationDays)
    {
        if (indirectRate <= 0)
        {
            return null;
        }

        var year = CommissionYearIndex(firstYearStartedAt, asOfUtc, durationDays);
        return year == 0 ? indirectRate : null;
    }

    public static decimal Year1RateForSalesManager(bool wasReferred, decimal standardYear1, decimal referredYear1)
        => wasReferred ? referredYear1 : standardYear1;

    public static decimal PlatformShareRate(decimal directRate, decimal indirectRate = 0m)
    {
        var remainder = 1m - AmbassadorShareRate - Math.Max(0m, directRate) - Math.Max(0m, indirectRate);
        return remainder < 0 ? 0m : remainder;
    }

    public static bool IsEligibleFounderSlot(int? slot) =>
        slot is >= 1 and <= MaxFounderSlots;
}
