using Jobsy.Core.Rules;

namespace Jobsy.Core.Scholen;

/// <summary>Maps every G78 item (9001–9060) to competency dimensions for page-wide growth exclusion.</summary>
public static class PupilG78ItemDimensionMap
{
    private static readonly Dictionary<int, HashSet<string>> ByItem = BuildMap();

    public static IReadOnlySet<string> DimensionsFor(int itemId)
    {
        if (ByItem.TryGetValue(itemId, out var set))
        {
            return set;
        }

        return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    public static HashSet<string> DimensionsOnPage(IEnumerable<int> pageItemIds)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in pageItemIds)
        {
            if (ByItem.TryGetValue(id, out var dims))
            {
                set.UnionWith(dims);
            }
        }

        return set;
    }

    public static bool PageMentionsDimension(IEnumerable<int> pageItemIds, string competencyCategory)
        => DimensionsOnPage(pageItemIds).Contains(competencyCategory);

    private static Dictionary<int, HashSet<string>> BuildMap()
    {
        var d = new Dictionary<int, HashSet<string>>();
        void Add(int id, params string[] dims)
        {
            d[id] = dims.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        void C(int id, string cat) => Add(id, cat);

        // Competence bank → direct category
        C(9001, CompetencyTestCatalog.Samenwerken);
        C(9002, CompetencyTestCatalog.Resultaatgerichtheid);
        C(9003, CompetencyTestCatalog.Stressbestendigheid);
        C(9004, CompetencyTestCatalog.Innovatie);
        C(9005, CompetencyTestCatalog.Extraversie);
        C(9006, CompetencyTestCatalog.Samenwerken);
        C(9007, CompetencyTestCatalog.Resultaatgerichtheid);
        C(9008, CompetencyTestCatalog.Stressbestendigheid);
        C(9009, CompetencyTestCatalog.Innovatie);
        C(9010, CompetencyTestCatalog.Extraversie);
        C(9011, CompetencyTestCatalog.Samenwerken);
        C(9012, CompetencyTestCatalog.Resultaatgerichtheid);
        C(9013, CompetencyTestCatalog.Stressbestendigheid);
        C(9014, CompetencyTestCatalog.Innovatie);
        C(9015, CompetencyTestCatalog.Extraversie);

        // Career → narrative dimensions
        Add(9016, CompetencyTestCatalog.Innovatie, CompetencyTestCatalog.Resultaatgerichtheid);
        Add(9017, CompetencyTestCatalog.Innovatie);
        Add(9018, CompetencyTestCatalog.Innovatie);
        Add(9019, CompetencyTestCatalog.Samenwerken);
        Add(9020, CompetencyTestCatalog.Extraversie);
        Add(9021, CompetencyTestCatalog.Resultaatgerichtheid);
        Add(9022, CompetencyTestCatalog.Innovatie, CompetencyTestCatalog.Resultaatgerichtheid);
        Add(9023, CompetencyTestCatalog.Innovatie);
        Add(9024, CompetencyTestCatalog.Samenwerken);
        Add(9025, CompetencyTestCatalog.Innovatie);
        Add(9026, CompetencyTestCatalog.Extraversie);
        Add(9027, CompetencyTestCatalog.Resultaatgerichtheid);
        Add(9028, CompetencyTestCatalog.Innovatie, CompetencyTestCatalog.Resultaatgerichtheid);
        Add(9029, CompetencyTestCatalog.Innovatie);
        Add(9030, CompetencyTestCatalog.Samenwerken, CompetencyTestCatalog.Extraversie);

        // Values
        Add(9031, CompetencyTestCatalog.Innovatie);
        Add(9032, CompetencyTestCatalog.Samenwerken);
        Add(9033, CompetencyTestCatalog.Resultaatgerichtheid);
        Add(9034, CompetencyTestCatalog.Stressbestendigheid, CompetencyTestCatalog.Resultaatgerichtheid);
        Add(9035, CompetencyTestCatalog.Samenwerken);
        Add(9036, CompetencyTestCatalog.Innovatie, CompetencyTestCatalog.Extraversie);
        Add(9037, CompetencyTestCatalog.Samenwerken);
        Add(9038, CompetencyTestCatalog.Resultaatgerichtheid);
        Add(9039, CompetencyTestCatalog.Stressbestendigheid, CompetencyTestCatalog.Resultaatgerichtheid);
        Add(9040, CompetencyTestCatalog.Samenwerken);
        Add(9041, CompetencyTestCatalog.Innovatie);
        Add(9042, CompetencyTestCatalog.Samenwerken);
        Add(9043, CompetencyTestCatalog.Resultaatgerichtheid, CompetencyTestCatalog.Stressbestendigheid);
        Add(9044, CompetencyTestCatalog.Resultaatgerichtheid, CompetencyTestCatalog.Stressbestendigheid);
        Add(9045, CompetencyTestCatalog.Samenwerken);

        // Culture
        Add(9046, CompetencyTestCatalog.Extraversie, CompetencyTestCatalog.Innovatie);
        Add(9047, CompetencyTestCatalog.Samenwerken);
        Add(9048, CompetencyTestCatalog.Samenwerken);
        Add(9049, CompetencyTestCatalog.Extraversie);
        Add(9050, CompetencyTestCatalog.Innovatie);
        Add(9051, CompetencyTestCatalog.Innovatie);
        Add(9052, CompetencyTestCatalog.Innovatie, CompetencyTestCatalog.Resultaatgerichtheid);
        Add(9053, CompetencyTestCatalog.Samenwerken);
        Add(9054, CompetencyTestCatalog.Samenwerken);
        Add(9055, CompetencyTestCatalog.Resultaatgerichtheid);
        Add(9056, CompetencyTestCatalog.Innovatie);
        Add(9057, CompetencyTestCatalog.Innovatie);
        Add(9058, CompetencyTestCatalog.Extraversie, CompetencyTestCatalog.Innovatie);
        Add(9059, CompetencyTestCatalog.Samenwerken);
        Add(9060, CompetencyTestCatalog.Samenwerken);

        return d;
    }
}
