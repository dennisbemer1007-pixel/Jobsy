namespace Jobsy.Core.Entities;

/// <summary>Platform commercial settings for Lobsy Flex placements.</summary>
public class FlexCommercialSettings
{
    public Guid Id { get; set; }

    /// <summary>Fixed Lobsy margin above backoffice buy price (default € 2,00 / hour).</summary>
    public decimal MarginPerHourEuro { get; set; } = 2.00m;

    /// <summary>Display name of the NEN 4400-1 backoffice partner.</summary>
    public string BackofficePartnerName { get; set; } = "Yellowstone";

    public DateTime UpdatedAtUtc { get; set; }
}
