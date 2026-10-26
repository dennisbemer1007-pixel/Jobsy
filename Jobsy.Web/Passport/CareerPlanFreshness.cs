using Jobsy.Core.Rules;
using Jobsy.Web.Models;

namespace Jobsy.Web.Passport;

/// <summary>
/// Detects career steps generated for a different dream job (e.g. piloot pad after dream change).
/// </summary>
internal static class CareerPlanFreshness
{
    internal static bool StepsMatchDream(CareerPathPlanApiModel plan)
    {
        var dream = plan.DreamTitle?.Trim();
        if (string.IsNullOrWhiteSpace(dream) || plan.Steps.Count == 0)
        {
            return false;
        }

        var anchor = OccupationTaxonomy.Resolve(dream);
        if (anchor is null)
        {
            return true;
        }

        foreach (var step in plan.Steps)
        {
            if (StepMatchesDream(anchor, step.Title) || StepMatchesDream(anchor, step.Summary))
            {
                return true;
            }

            foreach (var gap in step.SkillsGap)
            {
                if (StepMatchesDream(anchor, gap))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool StepMatchesDream(OccupationNode anchor, string? text)
    {
        var node = OccupationTaxonomy.Resolve(text);
        return node is not null
               && OccupationTaxonomy.Affinity(anchor, node) >= OccupationTaxonomy.SameSectorMin;
    }
}
