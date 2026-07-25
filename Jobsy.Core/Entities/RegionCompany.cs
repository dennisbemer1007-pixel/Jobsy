namespace Jobsy.Core.Entities;

public class RegionCompany
{
    public Guid RegionId { get; set; }
    public Region Region { get; set; } = null!;
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
}
