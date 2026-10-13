namespace Jobsy.Core.Scholen.QuestionSets;

public enum PupilFlowStepKind
{
    Question = 0,
    Puzzle = 1,
    Island = 2,
    Done = 3
}

public sealed record PupilFlowStep(
    PupilFlowStepKind Kind,
    int Index,
    string? ItemId,
    string? WorldKey,
    string? PuzzleKey);

/// <summary>
/// Pure step engine for the pupil journey. Order: unanswered question gate → puzzle → island → question / done.
/// </summary>
public static class PupilFlow
{
    public static PupilFlowStep Next(
        PupilQuestionSetDef def,
        IReadOnlyDictionary<string, int> answers,
        bool islandDone,
        IReadOnlyDictionary<string, PupilPuzzleStatus>? puzzles = null)
    {
        ArgumentNullException.ThrowIfNull(def);
        ArgumentNullException.ThrowIfNull(answers);

        var first = FirstUnansweredIndex(def, answers);
        if (first >= def.Bank.AllItems.Count)
        {
            return new PupilFlowStep(PupilFlowStepKind.Done, first, null, null, null);
        }

        // Puzzle gate (from 07): only when the pupil is exactly at AfterItem and has no status yet.
        foreach (var slot in def.PuzzleSlots)
        {
            if (slot.AfterItem != first)
            {
                continue;
            }

            var hasStatus = puzzles is not null
                            && puzzles.ContainsKey(slot.PuzzleKey);
            if (!hasStatus)
            {
                return new PupilFlowStep(
                    PupilFlowStepKind.Puzzle,
                    first,
                    null,
                    null,
                    slot.PuzzleKey);
            }
        }

        if (answers.Count >= def.IslandAfter && !islandDone)
        {
            return new PupilFlowStep(
                PupilFlowStepKind.Island,
                first,
                null,
                "pauze-eiland",
                null);
        }

        var item = def.Bank.GetByGlobalIndex(first);
        return new PupilFlowStep(
            PupilFlowStepKind.Question,
            first,
            item?.Id,
            item?.WorldKey,
            null);
    }

    public static int FirstUnansweredIndex(
        PupilQuestionSetDef def,
        IReadOnlyDictionary<string, int> answers)
    {
        var total = def.Bank.AllItems.Count;
        for (var i = 0; i < total; i++)
        {
            var item = def.Bank.GetByGlobalIndex(i);
            if (item is null || !answers.ContainsKey(item.Id))
            {
                return i;
            }
        }

        return total;
    }

    public static string ToNextStepToken(PupilFlowStepKind kind) => kind switch
    {
        PupilFlowStepKind.Question => "question",
        PupilFlowStepKind.Puzzle => "puzzle",
        PupilFlowStepKind.Island => "island",
        PupilFlowStepKind.Done => "done",
        _ => "question"
    };

    /// <summary>Known pupil item id ranges (G78 9001–9060, VO 9101–9200).</summary>
    public static bool IsKnownPupilItemId(string itemId)
    {
        if (!int.TryParse(itemId, System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out var id))
        {
            return false;
        }

        return (id is >= 9001 and <= 9060) || (id is >= 9101 and <= 9200);
    }
}
