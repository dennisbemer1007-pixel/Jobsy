using Jobsy.Core.Rules;

namespace Jobsy.Core.Scholen;

/// <summary>Canonical competence examples per dimension (Bijvoorbeeld must match the label).</summary>
public static class PupilG78CompetencyExampleItems
{
    private static readonly IReadOnlyDictionary<string, int[]> Preferred = new Dictionary<string, int[]>(StringComparer.OrdinalIgnoreCase)
    {
        [CompetencyTestCatalog.Samenwerken] = [9006, 9001],
        [CompetencyTestCatalog.Resultaatgerichtheid] = [9007, 9021],
        [CompetencyTestCatalog.Stressbestendigheid] = [9002, 9008, 9003],
        [CompetencyTestCatalog.Innovatie] = [9004, 9009],
        [CompetencyTestCatalog.Extraversie] = [9005, 9010],
    };

    public static IReadOnlyList<int> ForCategory(string competencyCategory)
        => Preferred.TryGetValue(competencyCategory, out var ids) ? ids : [];

    public static bool ItemMatchesCategory(int itemId, string competencyCategory)
    {
        if (ForCategory(competencyCategory).Contains(itemId))
        {
            return true;
        }

        if (itemId > PupilG78CompetencyNarrative.PositiveTileCompetenceMax)
        {
            return false;
        }

        var bankCat = PupilG78ItemThemes.CompetenceCategory(itemId);
        return bankCat is not null
               && string.Equals(bankCat, competencyCategory, StringComparison.OrdinalIgnoreCase);
    }
}
