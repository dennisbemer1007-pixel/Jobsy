namespace Jobsy.Core.Rules;

/// <summary>
/// Pure commission rules for the salesmanager network (ex-VAT amounts).
/// </summary>
public static class SalesCommissionRules
{
    public const decimal VatRate = 0.21m;
    public const decimal FirstYearOnboardingEuro = 2500.00m;
    public const decimal FounderBonusRate = 0.20m;
    public const int MaxFounderSlots = 10;
    public const decimal Year1TokenCommissionRate = 0.10m;
    public const decimal Year2TokenCommissionRate = 0.05m;
    public const string CurrentAgreementVersion = "2026-07-27-sm-mediation";

    public static decimal FounderBonusExVat =>
        decimal.Round(FirstYearOnboardingEuro * FounderBonusRate, 2, MidpointRounding.AwayFromZero);

    public static decimal VatOn(decimal amountExVat) =>
        decimal.Round(amountExVat * VatRate, 2, MidpointRounding.AwayFromZero);

    public static decimal InclVat(decimal amountExVat) => amountExVat + VatOn(amountExVat);

    /// <summary>
    /// Token commission rate based on supplier first-year start. Year 1 = first 12 months, year 2 = next 12 months.
    /// After year 2: null (no commission).
    /// </summary>
    public static decimal? TokenCommissionRate(DateTime? firstYearStartedAt, DateTime asOfUtc)
    {
        if (firstYearStartedAt is null)
        {
            return null;
        }

        var start = firstYearStartedAt.Value;
        if (asOfUtc < start)
        {
            return null;
        }

        var year1End = start.AddYears(1);
        if (asOfUtc < year1End)
        {
            return Year1TokenCommissionRate;
        }

        var year2End = start.AddYears(2);
        if (asOfUtc < year2End)
        {
            return Year2TokenCommissionRate;
        }

        return null;
    }

    public static bool IsEligibleFounderSlot(int? slot) =>
        slot is >= 1 and <= MaxFounderSlots;
}
