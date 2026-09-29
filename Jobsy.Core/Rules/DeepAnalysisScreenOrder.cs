using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>
/// Screen order for deep-analysis questionnaires: questions are shown grouped by domain
/// (GroupBy first-seen domain order). Catalog <see cref="DeepAnalysisQuestion.Id"/> values
/// stay as answer keys; display numbers are 1…N in this grouped order.
/// </summary>
public static class DeepAnalysisScreenOrder
{
    public static IReadOnlyList<DeepAnalysisQuestion> For(AssessmentKind kind)
        => Grouped(DeepAnalysisCatalog.QuestionsFor(kind), q => q.Domain);

    public static IReadOnlyList<T> Grouped<T>(IEnumerable<T> questions, Func<T, string> domainSelector)
    {
        ArgumentNullException.ThrowIfNull(questions);
        ArgumentNullException.ThrowIfNull(domainSelector);
        return questions
            .GroupBy(domainSelector, StringComparer.OrdinalIgnoreCase)
            .SelectMany(g => g)
            .ToList();
    }

    public static IReadOnlyList<int> Ids(IEnumerable<DeepAnalysisQuestion> screenOrder)
        => screenOrder.Select(q => q.Id).ToList();

    /// <summary>1-based position on screen, or the raw id when missing.</summary>
    public static int NumberOf(IReadOnlyList<int> orderedIds, int questionId)
    {
        for (var i = 0; i < orderedIds.Count; i++)
        {
            if (orderedIds[i] == questionId)
            {
                return i + 1;
            }
        }

        return questionId;
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
}
