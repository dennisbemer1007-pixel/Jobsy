namespace Jobsy.Core.Entities;

public class EarlyAdapterRule
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MonthlyGrantTokens { get; set; }
    public decimal PurchaseDiscountPercent { get; set; }
    public bool IsActive { get; set; } = true;
}
