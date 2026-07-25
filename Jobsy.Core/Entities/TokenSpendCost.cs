using Jobsy.Core.Enums;

namespace Jobsy.Core.Entities;

/// <summary>
/// Cost in tokens for publish / highlight / pushbom / extend.
/// </summary>
public class TokenSpendCost
{
    public Guid Id { get; set; }
    public TokenSpendReason Reason { get; set; }
    public decimal CostTokens { get; set; }
    public bool IsActive { get; set; } = true;
}
