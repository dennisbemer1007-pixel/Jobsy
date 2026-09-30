namespace Jobsy.Core.Rules;

/// <summary>
/// Pure commission / revenue-share rules for the salesmanager network (ex-VAT amounts).
/// Staffels (admin-configurable via <c>SalesCommercialSettings</c>):
/// standard 25% / 10% / 5% over 3 years; referred year-1 20% with 5% referrer override.
/// Window starts at the organisation's first credited purchase (D1).
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

    /// <summary>Snapshotted terms for one organisation commercial unit (frozen at activation).</summary>
    public sealed record CommissionTerms(
        decimal DirectYear1Rate,
        decimal Year2Rate,
        decimal Year3Rate,
        decimal IndirectRate,
        int DurationDays,
        DateTime StartsAtUtc);

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
    /// Calendar year within the window: 1 / 2 / 3, or null before start or after duration.
    /// Year 1 = [start, start+365d), year 2 = [+365d, +730d), year 3 = [+730d, +DurationDays).
    /// </summary>
    public static int? YearFor(CommissionTerms terms, DateTime purchaseAtUtc)
    {
        ArgumentNullException.ThrowIfNull(terms);
        if (terms.DurationDays <= 0)
        {
            return null;
        }

        if (purchaseAtUtc < terms.StartsAtUtc)
        {
            return null;
        }

        var end = terms.StartsAtUtc.AddDays(terms.DurationDays);
        if (purchaseAtUtc >= end)
        {
            return null;
        }

        var elapsed = (purchaseAtUtc - terms.StartsAtUtc).TotalDays;
        if (elapsed < CommissionYearLengthDays)
        {
            return 1;
        }

        if (elapsed < CommissionYearLengthDays * 2)
        {
            return 2;
        }

        return 3;
    }

    public static decimal? DirectRate(CommissionTerms terms, int? year) => year switch
    {
        1 => terms.DirectYear1Rate < 0 ? null : terms.DirectYear1Rate,
        2 => terms.Year2Rate < 0 ? null : terms.Year2Rate,
        3 => terms.Year3Rate < 0 ? null : terms.Year3Rate,
        _ => null
    };

    /// <summary>Indirect (referrer) rate applies in year 1 only.</summary>
    public static decimal? IndirectRate(CommissionTerms terms, int? year)
    {
        if (year != 1 || terms.IndirectRate <= 0)
        {
            return null;
        }

        return terms.IndirectRate;
    }

    public static bool BonusTokensAllowed(CommissionTerms terms, DateTime purchaseAtUtc)
        => YearFor(terms, purchaseAtUtc) is not null;

    /// <summary>
    /// Whether commission still accrues for a referred entrepreneur at <paramref name="asOfUtc"/>.
    /// Prefer <see cref="YearFor"/> with snapshotted <see cref="CommissionTerms"/>.
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
    /// Prefer <see cref="DirectRate"/> with snapshotted terms.
    /// </summary>
    public static decimal? TokenCommissionRate(
        DateTime? firstYearStartedAt,
        DateTime asOfUtc,
        decimal directRate = DefaultDirectCommissionRate,
        int durationDays = DefaultCommissionDurationDays,
        decimal year2Rate = DefaultYear2DirectCommissionRate,
        decimal year3Rate = DefaultYear3DirectCommissionRate)
    {
        if (firstYearStartedAt is null)
        {
            return null;
        }

        var terms = new CommissionTerms(
            directRate,
            year2Rate,
            year3Rate,
            IndirectRate: 0m,
            durationDays,
            firstYearStartedAt.Value);
        return DirectRate(terms, YearFor(terms, asOfUtc));
    }

    /// <summary>
    /// Indirect (referring) salesmanager rate — year 1 of the window only, when a positive rate is configured.
    /// Prefer <see cref="IndirectRate"/> with snapshotted terms.
    /// </summary>
    public static decimal? IndirectCommissionRate(
        DateTime? firstYearStartedAt,
        DateTime asOfUtc,
        decimal indirectRate = DefaultIndirectCommissionRate,
        int durationDays = DefaultCommissionDurationDays)
    {
        if (firstYearStartedAt is null || indirectRate <= 0)
        {
            return null;
        }

        var terms = new CommissionTerms(
            DirectYear1Rate: 0m,
            Year2Rate: 0m,
            Year3Rate: 0m,
            indirectRate,
            durationDays,
            firstYearStartedAt.Value);
        return IndirectRate(terms, YearFor(terms, asOfUtc));
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
