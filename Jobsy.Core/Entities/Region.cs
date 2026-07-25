namespace Jobsy.Core.Entities;

public class Region
{
    public Guid Id { get; set; }
    public Guid OrganizationCompanyId { get; set; }
    public Company OrganizationCompany { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public ICollection<RegionCompany> Companies { get; set; } = new List<RegionCompany>();
}
