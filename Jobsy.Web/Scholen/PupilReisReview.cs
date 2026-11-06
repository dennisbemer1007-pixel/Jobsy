using Jobsy.Core.Contracts.Scholen;
using Jobsy.Core.Scholen.QuestionSets;

namespace Jobsy.Web.Scholen;

/// <summary>Read-only “Vorige vraag” view for /leerling/reis?prev=1 (does not change saved progress).</summary>
internal static class PupilReisReview
{
    public static PupilProgressStateDto? WithPreviousQuestion(
        PupilProgressStateDto progress,
        PupilQuestionSetDef def)
    {
        if (string.IsNullOrWhiteSpace(progress.CurrentItemId))
        {
            return null;
        }

        var current = def.Bank.GetById(progress.CurrentItemId);
        if (current is null || current.GlobalIndex <= 0)
        {
            return null;
        }

        var previous = def.Bank.GetByGlobalIndex(current.GlobalIndex - 1);
        if (previous is null)
        {
            return null;
        }

        return progress with
        {
            CurrentItemId = previous.Id,
            CurrentIndex = previous.GlobalIndex,
        };
    }
}
