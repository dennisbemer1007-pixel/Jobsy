using Jobsy.Core.Enums;

namespace Jobsy.Core.Scholen.QuestionSets;

public sealed record PupilWorldPlan(string WorldKey, int ItemCount);

public sealed record PupilPuzzleSlot(int AfterItem, string PuzzleKey);

/// <summary>Outcome stored for a puzzle break (filled from 07).</summary>
public enum PupilPuzzleStatus
{
    Done = 1,
    Skipped = 2
}

/// <summary>
/// Immutable description of one pupil test (groep 7/8 or VO).
/// All numeric shell constants (island, plates, cheer) live here — never hardcoded in the flow.
/// </summary>
public sealed record PupilQuestionSetDef(
    PupilQuestionSet Set,
    string Key,
    string ScoringVersion,
    IPupilQuestionBank Bank,
    IReadOnlyList<PupilWorldPlan> Worlds,
    int IslandAfter,
    IReadOnlyList<PupilPuzzleSlot> PuzzleSlots,
    IReadOnlyList<(int Value, string Key)> AnswerLabels,
    int ItemsPerPlate,
    int PlateCount,
    string CheerKeyPrefix,
    int CheerCount,
    bool PartBreakAfterIsland,
    string StartTimeKey,
    string LabelKey)
{
    public int PlatesShed(int answeredCount)
        => Math.Clamp(answeredCount / ItemsPerPlate, 0, PlateCount);

    public int SceneDepth(int answeredCount)
    {
        if (answeredCount <= 0)
        {
            return 0;
        }

        if (answeredCount >= Bank.AllItems.Count)
        {
            return 11;
        }

        return Math.Clamp(1 + answeredCount / ItemsPerPlate, 1, 10);
    }

    public string CheerKey(int answeredCount)
        => CheerKeyPrefix + (((answeredCount / ItemsPerPlate) % CheerCount) + 1)
            .ToString(System.Globalization.CultureInfo.InvariantCulture);

    public string? WorldOf(int globalIndex)
        => Bank.GetByGlobalIndex(globalIndex)?.WorldKey;

    public int IndexInWorld(int globalIndex)
        => Bank.GetByGlobalIndex(globalIndex)?.IndexInWorld ?? 0;

    /// <summary>
    /// Journey rail keys: test worlds from this def with the shared Pauze-eiland
    /// inserted after the world that reaches <see cref="IslandAfter"/>.
    /// </summary>
    public IReadOnlyList<string> RailWorldKeys()
    {
        var keys = new List<string>(Worlds.Count + 1);
        var cumulative = 0;
        var islandInserted = false;
        foreach (var world in Worlds)
        {
            keys.Add(world.WorldKey);
            cumulative += world.ItemCount;
            if (!islandInserted && cumulative >= IslandAfter)
            {
                keys.Add("pauze-eiland");
                islandInserted = true;
            }
        }

        if (!islandInserted)
        {
            keys.Add("pauze-eiland");
        }

        return keys;
    }
}
