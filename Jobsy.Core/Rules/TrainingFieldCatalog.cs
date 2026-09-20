namespace Jobsy.Core.Rules;

/// <summary>Knelpuntvelden voor regionale deals: techniek, zorg, logistiek.</summary>
public static class TrainingFieldCatalog
{
    public const string Zorg = "zorg";
    public const string Techniek = "techniek";
    public const string Logistiek = "logistiek";

    public static readonly string[] ShortageFields = [Zorg, Techniek, Logistiek];

    private static readonly (string Field, string[] Needles)[] Map =
    [
        (Zorg, ["zorg", "verpleeg", "verzorg", "welzijn", "thuishulp", "agz", "vvt", "helpende"]),
        (Techniek, ["techniek", "install", "elektro", "metaal", "monteur", "werktuig", "installatie", "bouw"]),
        (Logistiek, ["logistiek", "magazijn", "heftruck", "chauffeur", "warehouse", "orderpick", "expeditie"])
    ];

    public static IReadOnlyList<string> Detect(params IEnumerable<string>?[] blobs)
    {
        var haystack = string.Join(' ', blobs.Where(b => b is not null).SelectMany(b => b!));
        var folded = CareerOccupationKeys.Fold(haystack);
        var hits = new List<string>();
        foreach (var (field, needles) in Map)
        {
            if (needles.Any(n => folded.Contains(n, StringComparison.Ordinal)))
            {
                hits.Add(field);
            }
        }

        return hits;
    }
}
