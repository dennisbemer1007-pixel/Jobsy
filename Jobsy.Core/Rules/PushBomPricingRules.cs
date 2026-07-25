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

    public static string FormatTierLabel(PushBomPricingTier tier)
    {
        if (tier.MaxCandidates is null)
        {
            return $"{tier.MinCandidates}+ kandidaten";
        }

        if (tier.MinCandidates == tier.MaxCandidates)
        {
            return $"{tier.MinCandidates} kandidaat{(tier.MinCandidates == 1 ? "" : "en")}";
        }

        return $"{tier.MinCandidates}–{tier.MaxCandidates} kandidaten";
    }
}
