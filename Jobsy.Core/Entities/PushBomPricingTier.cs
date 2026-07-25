namespace Jobsy.Core.Entities;

/// <summary>
/// Token cost for PushBom based on candidate reach count (e.g. 1–9 → 1 token, 10–25 → 2).
/// </summary>
public class PushBomPricingTier
{
    public Guid Id { get; set; }

    /// <summary>Inclusive lower bound of candidate count.</summary>
    public int MinCandidates { get; set; }

    /// <summary>Inclusive upper bound; null means unbounded (e.g. 51+).</summary>
    public int? MaxCandidates { get; set; }

    public decimal CostTokens { get; set; }

    public bool IsActive { get; set; } = true;
}
