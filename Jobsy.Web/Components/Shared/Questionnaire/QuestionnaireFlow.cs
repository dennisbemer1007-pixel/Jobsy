namespace Jobsy.Web.Components.Shared.Questionnaire;

public static class QuestionnaireFlow
{
    public static int? FirstUnansweredId(
        IEnumerable<QuestionnaireQuestion> questions,
        IReadOnlyDictionary<int, int> answers)
    {
        foreach (var question in questions)
        {
            if (!answers.ContainsKey(question.Id))
            {
                return question.Id;
            }
        }

        return null;
    }

    public static int? NextUnansweredAfter(
        IEnumerable<QuestionnaireQuestion> questions,
        IReadOnlyDictionary<int, int> answers,
        int answeredId)
    {
        var list = questions as IList<QuestionnaireQuestion> ?? questions.ToList();
        var index = -1;
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i].Id == answeredId)
            {
                index = i;
                break;
            }
        }

        for (var i = index + 1; i < list.Count; i++)
        {
            if (!answers.ContainsKey(list[i].Id))
            {
                return list[i].Id;
            }
        }

        return FirstUnansweredId(list, answers);
    }

    public static string? CategoryLabelFor(
        IEnumerable<QuestionnaireQuestion> questions,
        int? currentId)
    {
        if (currentId is null)
        {
            return null;
        }

        return questions.FirstOrDefault(q => q.Id == currentId)?.CategoryLabel;
    }

    public static (int Done, int Total) CategoryProgress(
        IEnumerable<QuestionnaireQuestion> questions,
        IReadOnlyDictionary<int, int> answers,
        int? currentId)
    {
        if (currentId is null)
        {
            return (0, 0);
        }

        var current = questions.FirstOrDefault(q => q.Id == currentId);
        if (current is null)
        {
            return (0, 0);
        }

        var inCategory = questions.Where(q => q.CategoryKey == current.CategoryKey).ToList();
        var done = inCategory.Count(q => answers.ContainsKey(q.Id));
        return (done, inCategory.Count);
    }
}
