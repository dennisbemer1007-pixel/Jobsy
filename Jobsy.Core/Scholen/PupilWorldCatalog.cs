namespace Jobsy.Core.Scholen;

/// <summary>
/// Shared world keys, titles and subtitles for the leerling shell.
/// Item counts, plates and answer labels live on <c>PupilQuestionSetDef</c>.
/// </summary>
public static class PupilWorldCatalog
{
    public sealed record WorldDef(
        string Key,
        string TitleKey,
        string SubtitleKey,
        bool IsTestWorld,
        bool ShedsPlate);

    /// <summary>
    /// Order: Koraalrif → Schatgrot → Pauze-eiland → Vuurtoren → Lagune.
    /// The 4 test worlds are shown as "Wereld 1–4"; Pauze-eiland is not numbered as a test world.
    /// Island timing is set-aware via <c>PupilQuestionSetDef.IslandAfter</c>.
    /// </summary>
    public static readonly IReadOnlyList<WorldDef> Worlds =
    [
        new("koraalrif", "Leerling.World.Reef.Title", "Leerling.World.Reef.Sub", true, true),
        new("schatgrot", "Leerling.World.Cave.Title", "Leerling.World.Cave.Sub", true, true),
        new("pauze-eiland", "Leerling.World.Island.Title", "Leerling.World.Island.Sub", false, false),
        new("vuurtoren", "Leerling.World.Lighthouse.Title", "Leerling.World.Lighthouse.Sub", true, true),
        new("lagune", "Leerling.World.Lagoon.Title", "Leerling.World.Lagoon.Sub", true, true),
    ];

    public static WorldDef? Find(string worldKey)
        => Worlds.FirstOrDefault(w => string.Equals(w.Key, worldKey, StringComparison.OrdinalIgnoreCase));

    public static int TestWorldNumber(string worldKey)
    {
        var n = 0;
        foreach (var w in Worlds)
        {
            if (!w.IsTestWorld)
            {
                continue;
            }

            n++;
            if (string.Equals(w.Key, worldKey, StringComparison.OrdinalIgnoreCase))
            {
                return n;
            }
        }

        return 0;
    }
}
