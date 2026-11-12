using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Scholen;

/// <summary>Blocks growth dimensions from appearing as strengths (eff ≥ 4) on story/tiles.</summary>
public static class PupilG78StrengthGuard
{
    public static bool IsForbiddenStrength(
        int itemId,
        int effectiveScore,
        IReadOnlySet<string> growthCategories)
    {
        if (effectiveScore < 4 || growthCategories.Count == 0)
        {
            return false;
        }

        var competenceCat = PupilG78ItemThemes.CompetenceCategory(itemId);
        if (competenceCat is not null
            && growthCategories.Contains(competenceCat))
        {
            return true;
        }

        foreach (var dim in PupilG78ItemDimensionMap.DimensionsFor(itemId))
        {
            if (growthCategories.Contains(dim))
            {
                return true;
            }
        }

        return false;
    }

    public static bool GrowthCategoryPublishedStrong(
        IReadOnlyCollection<int> publishedItemIds,
        string growthCategory,
        IReadOnlyDictionary<string, int> answers)
    {
        var bank = new PupilQuestionBank();
        foreach (var id in publishedItemIds)
        {
            if (!answers.TryGetValue(id.ToString(), out var raw))
            {
                continue;
            }

            var q = bank.Questions.FirstOrDefault(x => x.Id == id);
            if (q is null)
            {
                continue;
            }

            if (q.Model != AssessmentKind.Competence)
            {
                continue;
            }

            var eff = PupilQuestionStoryParaphrase.EffectiveScore(raw, q.Reverse);
            if (eff < 4)
            {
                continue;
            }

            if (string.Equals(q.Category, growthCategory, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
