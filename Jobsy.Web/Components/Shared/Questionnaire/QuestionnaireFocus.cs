namespace Jobsy.Web.Components.Shared.Questionnaire;

/// <summary>
/// Shared current/dimmed/expanded/scroll focus for likert questionnaire pages.
/// </summary>
public sealed class QuestionnaireFocus
{
    public int? CurrentId { get; private set; }
    public int? DimmedId { get; private set; }
    public int? ExpandedId { get; private set; }
    public int? ScrollToId { get; private set; }
    public bool SmoothScroll { get; private set; }

    public void Initialize(
        IReadOnlyList<int> orderedIds,
        IReadOnlyDictionary<int, int> answers)
    {
        CurrentId = FirstUnanswered(orderedIds, answers) ?? FirstId(orderedIds);
        DimmedId = CurrentId is int cur ? NextUnansweredAfter(orderedIds, answers, cur) : null;
        ExpandedId = null;
        ScrollToId = CurrentId;
        SmoothScroll = false;
    }

    public void AfterAnswer(
        int answeredId,
        IReadOnlyList<int> orderedIds,
        IReadOnlyDictionary<int, int> answers)
    {
        ExpandedId = null;
        CurrentId = NextUnansweredAfter(orderedIds, answers, answeredId) ?? answeredId;
        DimmedId = CurrentId is int cur ? NextUnansweredAfter(orderedIds, answers, cur) : null;
        ScrollToId = CurrentId;
        SmoothScroll = true;
    }

    public void Expand(
        int id,
        IReadOnlyList<int> orderedIds,
        IReadOnlyDictionary<int, int> answers)
    {
        ExpandedId = id;
        CurrentId = id;
        DimmedId = NextUnansweredAfter(orderedIds, answers, id);
        ScrollToId = id;
        SmoothScroll = true;
    }

    public void GoNext(
        IReadOnlyList<int> orderedIds,
        IReadOnlyDictionary<int, int> answers)
    {
        CurrentId = FirstUnanswered(orderedIds, answers) ?? CurrentId;
        DimmedId = CurrentId is int cur ? NextUnansweredAfter(orderedIds, answers, cur) : null;
        ScrollToId = CurrentId;
        SmoothScroll = true;
    }

    public static int? FirstUnanswered(
        IReadOnlyList<int> orderedIds,
        IReadOnlyDictionary<int, int> answers)
    {
        foreach (var id in orderedIds)
        {
            if (!answers.ContainsKey(id))
            {
                return id;
            }
        }

        return null;
    }

    public static int? NextUnansweredAfter(
        IReadOnlyList<int> orderedIds,
        IReadOnlyDictionary<int, int> answers,
        int answeredId)
    {
        var index = -1;
        for (var i = 0; i < orderedIds.Count; i++)
        {
            if (orderedIds[i] == answeredId)
            {
                index = i;
                break;
            }
        }

        for (var i = index + 1; i < orderedIds.Count; i++)
        {
            if (!answers.ContainsKey(orderedIds[i]))
            {
                return orderedIds[i];
            }
        }

        return FirstUnanswered(orderedIds, answers);
    }

    private static int? FirstId(IReadOnlyList<int> orderedIds)
        => orderedIds.Count > 0 ? orderedIds[0] : null;
}
