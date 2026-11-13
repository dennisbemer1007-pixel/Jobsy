using Jobsy.Core.Enums;

namespace Jobsy.Core.Scholen;

/// <summary>Which competency/value/career category a chosen g78-2 pole actually supports (B foils → none).</summary>
public static class PupilG78ContrastPoleSupport
{
    public static bool TowardB(int raw) => raw is 4 or 5;

    public static bool TowardA(int raw) => raw is 1 or 2;

    /// <summary>Category credited in scores, growth, and strength picks; null when the pole is a foil.</summary>
    public static string? StrengthCategory(PupilQuestion q, bool towardB)
    {
        if (towardB)
        {
            return string.IsNullOrEmpty(q.CategoryB) ? null : q.CategoryB;
        }

        return q.Category;
    }

    public static string? StrengthCategory(PupilQuestion q, int raw) =>
        raw is < 1 or > 5 or 3 ? null : StrengthCategory(q, TowardB(raw));

    public static bool PoleSupportsCompetency(PupilQuestion q, int raw, string competencyCategory)
    {
        var cat = StrengthCategory(q, raw);
        return cat is not null
               && string.Equals(cat, competencyCategory, StringComparison.OrdinalIgnoreCase);
    }

    public static bool ClauseSupportsCompetency(G78AnswerClause clause, string competencyCategory)
        => !string.IsNullOrWhiteSpace(clause.Category)
           && string.Equals(clause.Category, competencyCategory, StringComparison.OrdinalIgnoreCase);
}
