using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Enums;

namespace Jobsy.Core.Scholen;

/// <summary>Apply a successful answer PUT to a progress snapshot (Web cache after save).</summary>
public static class PupilProgressStatePatch
{
    public static PupilProgressStateDto AfterAnswer(
        PupilProgressStateDto state,
        string itemId,
        int value,
        PupilAnswerResponse answer)
    {
        var answers = new Dictionary<string, int>(state.Answers, StringComparer.Ordinal);
        answers[itemId] = value;

        var completed = answer.Completed || string.Equals(answer.NextStep, "done", StringComparison.OrdinalIgnoreCase);
        var status = completed ? PupilCodeStatus.Completed : state.Status;
        var newShell = state.NewShell
                       || answer.AnsweredCount >= state.TotalItems
                       || answer.PlatesShed > state.PlatesShed;

        return state with
        {
            Status = status,
            CurrentIndex = answer.CurrentIndex,
            AnsweredCount = answer.AnsweredCount,
            PlatesShed = answer.PlatesShed,
            NewShell = newShell,
            CurrentItemId = answer.NextItemId,
            CurrentWorldKey = answer.NextWorldKey,
            Completed = completed,
            NeedsIsland = answer.NeedsIsland,
            NextStep = answer.NextStep,
            NextPuzzleKey = answer.NextPuzzleKey,
            Answers = answers,
        };
    }
}
