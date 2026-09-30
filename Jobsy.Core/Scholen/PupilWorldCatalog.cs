namespace Jobsy.Core.Scholen;

/// <summary>Fixed world order for the leerling wizard (04 shell; 05 fills questions).</summary>
public static class PupilWorldCatalog
{
    public sealed record WorldDef(
        string Key,
        string TitleKey,
        string SubtitleKey,
        int ItemCount,
        bool IsTestWorld,
        bool ShedsPlate);

    /// <summary>
    /// Order: Koraalrif → Schatgrot → Pauze-eiland (after 30 items) → Vuurtoren → Lagune.
    /// The 4 test worlds are shown as "Wereld 1–4"; Pauze-eiland is not numbered as a test world.
    /// </summary>
    public static readonly IReadOnlyList<WorldDef> Worlds =
    [
        new("koraalrif", "Leerling.World.Reef.Title", "Leerling.World.Reef.Sub", 15, true, true),
        new("schatgrot", "Leerling.World.Cave.Title", "Leerling.World.Cave.Sub", 15, true, true),
        new("pauze-eiland", "Leerling.World.Island.Title", "Leerling.World.Island.Sub", 0, false, false),
        new("vuurtoren", "Leerling.World.Lighthouse.Title", "Leerling.World.Lighthouse.Sub", 15, true, true),
        new("lagune", "Leerling.World.Lagoon.Title", "Leerling.World.Lagoon.Sub", 15, true, true),
    ];

    public const int TotalTestItems = 60;
    public const int ItemsPerPlate = 6;
    public const int PlateCount = 10;

    /// <summary>Likert labels (values 1–5), same scale as adult tests.</summary>
    public static readonly IReadOnlyList<(int Value, string Key)> AnswerLabels =
    [
        (1, "Leerling.Answer.No"),
        (2, "Leerling.Answer.NotReally"),
        (3, "Leerling.Answer.Sometimes"),
        (4, "Leerling.Answer.Quite"),
        (5, "Leerling.Answer.Yes"),
    ];

    public static int PlatesShed(int answeredCount)
        => Math.Clamp(answeredCount / ItemsPerPlate, 0, PlateCount);

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

    /// <summary>Scene depth 0–11 from answered count (decorative ocean).</summary>
    public static int SceneDepth(int answeredCount)
    {
        if (answeredCount <= 0)
        {
            return 0;
        }

        if (answeredCount >= TotalTestItems)
        {
            return 11;
        }

        return Math.Clamp(1 + answeredCount / 6, 1, 10);
    }
}
