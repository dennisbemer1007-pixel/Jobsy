namespace Jobsy.Core.Entities;

/// <summary>
/// Platform token pack pricing (1, 5, 10, 50, 100).
/// </summary>
public class TokenPricing
{
    public Guid Id { get; set; }
    public int PackSize { get; set; }
    public decimal PriceEuro { get; set; }
    public bool IsActive { get; set; } = true;
}
