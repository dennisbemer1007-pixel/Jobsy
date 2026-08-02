namespace Jobsy.Core.Rules;

/// <summary>
/// Pure commission / revenue-share rules for the salesmanager network (ex-VAT amounts).
/// Token purchases by referred companies split: 15% ambassador, 5% salesmanager, 80% platform.
/// </summary>
public static class SalesCommissionRules
{
    public const decimal VatRate = 0.21m;
    public const decimal FirstYearOnboardingEuro = 2500.00m;
    public const decimal FounderBonusRate = 0.20m;
    public const int MaxFounderSlots = 10;

    /// <summary>Ambassador (Ondernemer 1) share of token purchase value → company tegoed.</summary>
    public const decimal AmbassadorShareRate = 0.15m;

    /// <summary>Salesmanager commission on token purchase value.</summary>
    public const decimal SalesManagerShareRate = 0.05m;

    /// <summary>Platform remainder after ambassador + salesmanager shares.</summary>
    public const decimal PlatformShareRate = 0.80m;

    /// <summary>Legacy alias — fixed 5% salesmanager share (year windows removed).</summary>
    public const decimal Year1TokenCommissionRate = SalesManagerShareRate;

    /// <summary>Legacy alias — fixed 5% salesmanager share (year windows removed).</summary>
    public const decimal Year2TokenCommissionRate = SalesManagerShareRate;

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
    /// Salesmanager token commission rate. Fixed 5% for referred companies (no year window).
    /// <paramref name="firstYearStartedAt"/> retained for call-site compatibility; ignored.
    /// </summary>
    public static decimal? TokenCommissionRate(DateTime? firstYearStartedAt, DateTime asOfUtc)
    {
        _ = asOfUtc;
        // Only referred suppliers with a partnership start receive ongoing token commission.
        return firstYearStartedAt is null ? null : SalesManagerShareRate;
    }

    public static bool IsEligibleFounderSlot(int? slot) =>
        slot is >= 1 and <= MaxFounderSlots;
}
