namespace Jobsy.Core.Entities;

/// <summary>
/// Annual agency (uitzendbureau) subscription: € 4.000 / year for unlimited
/// carte-blanche vacancy publishing on subscribed establishment pins.
/// </summary>
public class AgencyAnnualSubscription
{
    public const decimal AnnualPriceEuro = 4000m;

    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Note { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
