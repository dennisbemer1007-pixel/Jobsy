namespace Jobsy.Core.Entities;

/// <summary>
/// Singleton commercial settings for partner sales materials and vacancy-type pricing.
/// </summary>
public class SalesCommercialSettings
{
    public Guid Id { get; set; }

    /// <summary>List price in euro per token (default €25).</summary>
    public decimal BaseTokenValueEuro { get; set; } = 25m;

    /// <summary>Token cost for the 1-week Funda carousel highlight.</summary>
    public decimal HighlightCarouselTokens { get; set; } = 2m;

    /// <summary>Token value attributed to pulsing map markers (sales display / breakdown).</summary>
    public decimal HighlightPulseTokens { get; set; } = 1m;

    /// <summary>Highlight duration in days for paid carousel placement.</summary>
    public int HighlightCarouselDays { get; set; } = 7;

    /// <summary>
    /// Free start-highlight granted when a company registers via a salesmanager tracking code.
    /// Applied automatically on the first published vacancy (no flat discount).
    /// </summary>
    public decimal StartHighlightBonusTokens { get; set; } = 2m;

    /// <summary>
    /// Year-1 commission for a salesmanager who was not referred (default 25%).
    /// Configurable by Admin.
    /// </summary>
    public decimal DirectCommissionRate { get; set; } = 0.25m;

    /// <summary>Year-2 direct commission (default 10%).</summary>
    public decimal Year2DirectCommissionRate { get; set; } = 0.10m;

    /// <summary>Year-3 direct commission (default 5%).</summary>
    public decimal Year3DirectCommissionRate { get; set; } = 0.05m;

    /// <summary>Year-1 commission when the salesmanager was aangedragen (default 20%).</summary>
    public decimal ReferredYear1DirectCommissionRate { get; set; } = 0.20m;

    /// <summary>
    /// Referrer override: share of token purchases in year 1 of a referred salesmanager (default 5%).
    /// </summary>
    public decimal IndirectCommissionRate { get; set; } = 0.05m;

    /// <summary>
    /// Commission rate for partner affiliates (Bedrijfsmanager / Intermediair) on token purchases (default 5%).
    /// </summary>
    public decimal PartnerCommissionRate { get; set; } = 0.05m;

    /// <summary>
    /// Maximum duration (days) commission accrues for an onboarded entrepreneur (default 1095 = 3 years).
    /// </summary>
    public int CommissionDurationDays { get; set; } = 1095;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
