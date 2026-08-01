using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

/// <summary>
/// Named commercial package (Standard packs or First Year / Enterprise Silver·Gold·Platinum).
/// </summary>
public class SalesPackage
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public SalesPackageCategory Category { get; set; }
    public int TokenAmount { get; set; }
    public decimal PriceEuro { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
