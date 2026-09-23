namespace Jobsy.Core.Entities;

/// <summary>
/// Whitelisted direct-employer career site (no agencies / aggregators).
/// </summary>
public class AtsScrapeSource
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Hostname only, e.g. werkenbij.gemeente.denhaag.nl</summary>
    public string Domain { get; set; } = string.Empty;
    /// <summary>Entry URL for vacancy listing / crawl start.</summary>
    public string ListUrl { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public double DefaultLatitude { get; set; }
    public double DefaultLongitude { get; set; }
    public string? DefaultLocationLabel { get; set; }
    public Guid? PreferredCompanyId { get; set; }
    public Company? PreferredCompany { get; set; }
    public DateTime? LastScrapedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<AtsScrapedListing> Listings { get; set; } = new List<AtsScrapedListing>();
}
