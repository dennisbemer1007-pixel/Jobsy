using Jobsy.Core.Scholen.QuestionSets;

namespace Jobsy.Core.Scholen;

public enum PupilGroep78JourneyNodeKind
{
    World = 0,
    Puzzle = 1,
    Island = 2,
    Vakken = 3,
    Result = 4,
}

public readonly record struct PupilGroep78JourneyNode(string Key, PupilGroep78JourneyNodeKind Kind);

/// <summary>Horizontal journey strip nodes for groep 7/8 (mockup <c>journey/jrow</c>).</summary>
public static class PupilGroep78Journey
{
    public static IReadOnlyList<PupilGroep78JourneyNode> BuildNodes(PupilQuestionSetDef def)
    {
        ArgumentNullException.ThrowIfNull(def);
        var list = new List<PupilGroep78JourneyNode>(def.Worlds.Count + def.PuzzleSlots.Count + 3);
        var cumulative = 0;
        var islandAdded = false;
        foreach (var world in def.Worlds)
        {
            list.Add(new(world.WorldKey, PupilGroep78JourneyNodeKind.World));
            cumulative += world.ItemCount;
            foreach (var slot in def.PuzzleSlots)
            {
                if (slot.AfterItem == cumulative)
                {
                    list.Add(new(slot.PuzzleKey, PupilGroep78JourneyNodeKind.Puzzle));
                }
            }

            if (!islandAdded && cumulative >= def.IslandAfter)
            {
                list.Add(new("pauze-eiland", PupilGroep78JourneyNodeKind.Island));
                islandAdded = true;
            }
        }

        if (!islandAdded)
        {
            list.Add(new("pauze-eiland", PupilGroep78JourneyNodeKind.Island));
        }

        list.Add(new("dit-ben-jij", PupilGroep78JourneyNodeKind.Result));
        return list;
    }

    public static string TitleKey(string? themeKey, PupilGroep78JourneyNode node)
        => node.Kind switch
        {
            PupilGroep78JourneyNodeKind.World or PupilGroep78JourneyNodeKind.Island or PupilGroep78JourneyNodeKind.Vakken
                => PupilJourneyTheme.WorldTitleKey(themeKey, node.Key),
            PupilGroep78JourneyNodeKind.Puzzle => node.Key.ToLowerInvariant() switch
            {
                "schelpenrij" => "Leerling.Puzzle.Schelpenrij.Short",
                "schatkaart" => "Leerling.Puzzle.Schatkaart.Short",
                "vuurtorenlampen" => "Leerling.Puzzle.Vuurtoren.Short",
                _ => "Leerling.Puzzle.Title",
            },
            PupilGroep78JourneyNodeKind.Result => "LeerlingStory.Title",
            _ => "Leerling.Reis.Journey",
        };

    public static string DotImageStem(string? themeKey, PupilGroep78JourneyNode node)
    {
        if (node.Kind == PupilGroep78JourneyNodeKind.Puzzle)
        {
            return "puzzle";
        }

        if (node.Kind == PupilGroep78JourneyNodeKind.Result)
        {
            themeKey = PupilJourneyTheme.NormalizeOrDefault(themeKey);
            return themeKey switch
            {
                PupilJourneyTheme.Eilandtuin => "tuin-kaart",
                PupilJourneyTheme.Sterrenkust => "sterren-kaart",
                _ => "zeekaart",
            };
        }

        return PupilJourneyTheme.WorldBandImageStem(themeKey, node.Key);
    }

    public static int IndexOfCurrent(IReadOnlyList<PupilGroep78JourneyNode> nodes, string currentKey)
    {
        for (var i = 0; i < nodes.Count; i++)
        {
            if (string.Equals(nodes[i].Key, currentKey, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return 0;
    }
}
