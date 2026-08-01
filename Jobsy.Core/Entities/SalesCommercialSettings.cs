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

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
