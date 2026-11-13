using Jobsy.Core.Enums;
using Jobsy.Core.Rules;

namespace Jobsy.Core.Scholen;

/// <summary>Drop g78-2 competence clauses that contradict their credited dimension (B foils, stoppen vs doorzetten).</summary>
internal static class PupilG78ContrastFactFilter
{
    public static bool UsableOnPage(G78AnswerClause clause, bool contrastV2)
    {
        if (!contrastV2 || clause.Model != AssessmentKind.Competence)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(clause.Category))
        {
            return AllowedCompetenceFoil(clause.Text);
        }

        if (!CompetencyTestCatalog.QuickScanCategories.Contains(clause.Category, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        var main = clause.Text.Trim().TrimEnd('.');
        return PupilG78CompetencyExampleItems.ChosenFactSupportsKidLabel(clause.Category, main);
    }

    private static bool AllowedCompetenceFoil(string text)
    {
        var t = text.ToLowerInvariant();
        if (t.Contains("wil je even stoppen", StringComparison.Ordinal)
            || t.Contains("iets leuks doen", StringComparison.Ordinal)
            || t.Contains("wil je stoppen", StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }
}
