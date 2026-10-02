namespace Jobsy.Core.Entities;

/// <summary>
/// Admin-configurable Lobsy commercial amounts (Flex, diepte-analyse, uitzend-abonnement, ContactUnlock).
/// Singleton row; defaults match the platform master spec.
/// </summary>
public class FlexCommercialSettings
{
    public const decimal DefaultMarginPerHourEuro = 2.00m;
    public const decimal DefaultDeepAnalysisPriceEuro = 2.99m;
    public const decimal DefaultAgencyAnnualPriceEuro = 4000m;
    public const decimal DefaultContactUnlockCostTokens = 1m;
    public const string DefaultBackofficePartnerName = "Yellowstone";

    public Guid Id { get; set; }

    /// <summary>Fixed Lobsy margin above backoffice buy price (default € 2,00 / hour).</summary>
    public decimal MarginPerHourEuro { get; set; } = DefaultMarginPerHourEuro;

    /// <summary>Display name of the NEN 4400-1 backoffice partner.</summary>
    public string BackofficePartnerName { get; set; } = DefaultBackofficePartnerName;

    public decimal DeepTestPriceCompetenceEuro { get; set; } = DefaultDeepAnalysisPriceEuro;
    public decimal DeepTestPriceCareerEuro { get; set; } = DefaultDeepAnalysisPriceEuro;
    public decimal DeepTestPriceValuesEuro { get; set; } = DefaultDeepAnalysisPriceEuro;
    public decimal DeepTestPriceCultureEuro { get; set; } = DefaultDeepAnalysisPriceEuro;

    /// <summary>Annual uitzendbureau carte-blanche subscription (default € 4.000).</summary>
    public decimal AgencyAnnualPriceEuro { get; set; } = DefaultAgencyAnnualPriceEuro;

    /// <summary>Token cost to unlock anonymous talent contact (default 1).</summary>
    public decimal ContactUnlockCostTokens { get; set; } = DefaultContactUnlockCostTokens;

    public DateTime UpdatedAtUtc { get; set; }
}
