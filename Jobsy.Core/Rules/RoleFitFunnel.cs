namespace Jobsy.Core.Rules;

/// <summary>
/// Overall Functie-Fit % (Tier 1 + culture + formal) plus stepping-stone titles.
/// Live vacancy cards stay in the API layer so they stay fresh.
/// </summary>
public static class RoleFitFunnel
{
    public const int DirectMatchMin = 60;
    public const int MaxSimilar = 4;
    public const int MaxDirect = 5;

    public static int CombineOverall(
        int roleFitPercent,
        bool availabilityOk,
        int? culturePercent,
        VacancyBarrierCheck? formal)
    {
        var tier1 = availabilityOk ? 100 : 35;
        var culture = culturePercent ?? Math.Clamp(roleFitPercent, 0, 100);
        var papers = formal is null || formal.TotalCount == 0
            ? (availabilityOk ? 100 : 70)
            : (int)Math.Round(100d * formal.MetCount / formal.TotalCount, MidpointRounding.AwayFromZero);
        var blended = 0.25 * tier1
                      + 0.30 * Math.Clamp(culture, 0, 100)
                      + 0.30 * Math.Clamp(papers, 0, 100)
                      + 0.15 * Math.Clamp(roleFitPercent, 0, 100);
        return (int)Math.Clamp(Math.Round(blended, MidpointRounding.AwayFromZero), 0, 100);
    }

    public static IReadOnlyList<RoleFitSimilarRole> SuggestSimilar(string jobTitle, RiasecScores career)
    {
        var title = RoleFitCheckBuilder.NormalizeTitle(jobTitle) ?? "deze functie";
        var folded = CareerOccupationKeys.Fold(title);
        var anchor = OccupationTaxonomy.Resolve(title);
        if (anchor is null)
        {
            return [];
        }

        return OccupationTaxonomy.All
            .Select(node => (Node: node, Affinity: OccupationTaxonomy.Affinity(anchor, node)))
            .Where(x => x.Node.Id != anchor.Id && x.Affinity >= OccupationTaxonomy.SameSectorMin)
            .Where(x => !SameRole(folded, x.Node.Title))
            .Select(x =>
            {
                var interest = InterestPercent(x.Node.Title, career);
                var fit = (int)Math.Clamp(
                    Math.Round(x.Affinity * 80 + interest * 0.2, MidpointRounding.AwayFromZero),
                    1,
                    99);
                var why = x.Node.SteppingStone && !anchor.SteppingStone
                    ? $"{x.Node.Title} zit in {anchor.SectorLabel}, met een kortere opleiding dan {anchor.Title.ToLowerInvariant()}."
                    : $"{x.Node.Title} hoort bij {anchor.SectorLabel} — dezelfde vakrichting als {anchor.Title.ToLowerInvariant()}.";
                return (x.Node, x.Affinity, Fit: fit, Why: why);
            })
            .OrderByDescending(x => !anchor.SteppingStone && x.Node.SteppingStone)
            .ThenByDescending(x => x.Affinity)
            .ThenByDescending(x => x.Fit)
            .ThenBy(x => x.Node.Title, StringComparer.OrdinalIgnoreCase)
            .Take(MaxSimilar)
            .Select(x => new RoleFitSimilarRole(x.Node.Title, x.Why, x.Fit))
            .ToList();
    }

    public static IReadOnlyList<RoleFitSimilarRole> MergeSimilar(
        IEnumerable<RoleFitSimilarRole>? preferred,
        IEnumerable<RoleFitSimilarRole>? fallback)
    {
        var list = new List<RoleFitSimilarRole>();
        foreach (var item in (preferred ?? []).Concat(fallback ?? []))
        {
            var title = RoleFitCheckBuilder.NormalizeTitle(item.Title);
            if (title is null)
            {
                continue;
            }

            if (list.Any(x => string.Equals(x.Title, title, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var why = string.IsNullOrWhiteSpace(item.Why) || CareerCompassBuilder.ContainsForbiddenJargon(item.Why)
                ? "Deze richting sluit beter aan bij hoe jij scoort."
                : item.Why.Trim();
            if (why.Contains('@', StringComparison.Ordinal))
            {
                continue;
            }

            list.Add(new RoleFitSimilarRole(title, why, Math.Clamp(item.FitPercent, 0, 100)));
            if (list.Count >= MaxSimilar)
            {
                break;
            }
        }

        return list;
    }

    public static bool CanStartImmediately(
        bool legalEligible,
        int matchPercent,
        bool? travelWithinPreference,
        VacancyBarrierCheck formal,
        bool availabilityOk)
        => legalEligible
           && matchPercent >= DirectMatchMin
           && travelWithinPreference != false
           && availabilityOk
           && formal.Items.All(i => i.Met);

    private static bool SameRole(string foldedTitle, string occupationTitle)
    {
        var foldedJob = CareerOccupationKeys.Fold(occupationTitle);
        if (foldedJob.Length == 0)
        {
            return true;
        }

        return foldedTitle.Contains(foldedJob, StringComparison.Ordinal)
               || foldedJob.Contains(foldedTitle, StringComparison.Ordinal)
               || CareerOccupationKeys.FromTitle(occupationTitle)
                   .Count(key => CareerOccupationKeys.Hits(foldedTitle, key)) >= 2;
    }

    private static int InterestPercent(string title, RiasecScores career)
    {
        var folded = CareerOccupationKeys.Fold(title);
        CareerOccupation? best = null;
        var hits = 0;
        foreach (var job in CareerCompassBuilder.Occupations)
        {
            var score = CareerOccupationKeys.FromTitle(job.Title).Count(key => CareerOccupationKeys.Hits(folded, key));
            if (CareerOccupationKeys.Hits(folded, CareerOccupationKeys.Fold(job.Title)))
            {
                score += 3;
            }

            if (score > hits)
            {
                hits = score;
                best = job;
            }
        }

        return best is null ? 70 : CareerCompassBuilder.Score(best, career).Percent;
    }
}
