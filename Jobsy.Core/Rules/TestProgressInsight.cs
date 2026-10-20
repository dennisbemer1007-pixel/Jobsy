using Jobsy.Core.Enums;

namespace Jobsy.Core.Rules;

/// <summary>Gradual “picture so far” labels for Quick-Scan (blocks of five answers).</summary>
public static class TestProgressInsight
{
    public const int BlockSize = 5;

    public static IReadOnlyList<string> DomainLabelsSoFar(
        AssessmentKind _,
        IReadOnlyList<int> orderedQuestionIds,
        IReadOnlyDictionary<int, int> answers,
        Func<int, string> domainForQuestionId)
    {
        if (answers.Count < BlockSize)
        {
            return [];
        }

        var labels = new List<string>();
        foreach (var id in orderedQuestionIds)
        {
            if (!answers.ContainsKey(id))
            {
                continue;
            }

            var domain = domainForQuestionId(id);
            var label = DeepAnalysisQuestionHelp.DomainLabel(domain);
            if (string.IsNullOrWhiteSpace(label))
            {
                continue;
            }

            if (!labels.Contains(label, StringComparer.OrdinalIgnoreCase))
            {
                labels.Add(label);
            }
        }

        return labels;
    }

    public static string PrefixKey(AssessmentKind kind)
        => kind switch
        {
            AssessmentKind.Career => "TestFlow.ProgressInsight.Prefix.Career",
            AssessmentKind.Culture => "TestFlow.ProgressInsight.Prefix.Culture",
            AssessmentKind.Values => "TestFlow.ProgressInsight.Prefix.Values",
            _ => "TestFlow.ProgressInsight.Prefix.Competence"
        };
}
