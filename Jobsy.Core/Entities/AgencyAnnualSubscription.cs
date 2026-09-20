namespace Jobsy.Core.Entities;

/// <summary>
/// Annual agency (uitzendbureau) subscription for unlimited
/// carte-blanche vacancy publishing on subscribed establishment pins.
/// Price is configured in <see cref="FlexCommercialSettings.AgencyAnnualPriceEuro"/>.
/// </summary>
public class AgencyAnnualSubscription
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Price charged / agreed at activation (snapshot of admin setting).</summary>
    public decimal PriceEuro { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
