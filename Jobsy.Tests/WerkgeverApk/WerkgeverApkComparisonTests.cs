using Jobsy.Core.Enums;
using Jobsy.Core.Scholen.MboHbo;
using Jobsy.Core.WerkgeverApk;

namespace Jobsy.Tests.WerkgeverApk;

public sealed class WerkgeverApkComparisonTests
{
    [Fact]
    public void BuildComparison_includes_starter_conversation_questions()
    {
        var dto = WerkgeverApkComparisonEngine.BuildComparison(
            PupilMboHboProgressState.Parse("{}"),
            new Dictionary<string, int>(),
            SchoolLevel.Mbo3);
        Assert.True(dto.ConversationQuestions.Count >= 3);
    }

    [Fact]
    public void BuildComparison_lists_agree_when_dimension_scores_align()
    {
        var q = WerkgeverApkManagerQuestionCatalog.All[0];
        var employee = StateForQuestion(q, selfValue: 5);
        var manager = new Dictionary<string, int> { [q.Id] = 5 };
        var dto = WerkgeverApkComparisonEngine.BuildComparison(employee, manager, SchoolLevel.Mbo3);
        Assert.Contains(dto.Agree, t => t.Label == q.Label);
    }

    [Fact]
    public void BuildComparison_lists_differ_when_gap_large()
    {
        var q = WerkgeverApkManagerQuestionCatalog.All[0];
        var employee = StateForQuestion(q, selfValue: 1);
        var manager = new Dictionary<string, int> { [q.Id] = 5 };
        var dto = WerkgeverApkComparisonEngine.BuildComparison(employee, manager, SchoolLevel.Mbo3);
        Assert.Contains(dto.Differ, t => t.Label == q.Label);
    }

    private static PupilMboHboProgressState StateForQuestion(WerkgeverApkManagerQuestion q, int selfValue)
    {
        var catalog = PupilMboHboCatalog.Default;
        var block = q.Category == WerkgeverApkManagerQuestionCategory.Competence
            ? PupilMboHboBlock.Competentie
            : PupilMboHboBlock.Werkwaarden;
        var answers = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var item in catalog.QuestionsFor(block, SchoolLevel.Mbo3))
        {
            if (!string.IsNullOrWhiteSpace(q.EmployeeCompetenceDimension)
                && string.Equals(item.Dimension, q.EmployeeCompetenceDimension, StringComparison.OrdinalIgnoreCase))
            {
                answers[item.Id] = selfValue;
            }

            if (!string.IsNullOrWhiteSpace(q.EmployeeValueBarKey))
            {
                var key = PupilMboHboDimensionLabels.BarGroupKey(item.Dimension);
                if (key.Contains(q.EmployeeValueBarKey, StringComparison.OrdinalIgnoreCase))
                {
                    answers[item.Id] = selfValue;
                }
            }
        }

        return PupilMboHboProgressState.Parse(System.Text.Json.JsonSerializer.Serialize(new { answers }));
    }
}
