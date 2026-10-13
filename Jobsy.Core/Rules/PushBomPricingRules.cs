using Jobsy.Core.Entities;

namespace Jobsy.Core.Rules;

public static class PushBomPricingRules
{
    /// <summary>
    /// Resolves token cost for a candidate reach count from active tiers.
    /// Returns null when no tier matches.
    /// </summary>
    public static decimal? ResolveCost(IEnumerable<PushBomPricingTier> tiers, int candidateCount)
    {
        if (candidateCount <= 0)
        {
            return null;
        }

        var match = tiers
            .Where(t => t.IsActive)
            .Where(t => candidateCount >= t.MinCandidates)
            .Where(t => t.MaxCandidates is null || candidateCount <= t.MaxCandidates.Value)
            .OrderBy(t => t.MinCandidates)
            .FirstOrDefault();

        return match?.CostTokens;
    }
}
