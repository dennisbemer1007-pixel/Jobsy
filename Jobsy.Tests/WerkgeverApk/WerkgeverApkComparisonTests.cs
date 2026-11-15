using Jobsy.Core.Enums;
using Jobsy.Core.Scholen.MboHbo;
using Jobsy.Core.WerkgeverApk;

namespace Jobsy.Tests.WerkgeverApk;

public sealed class WerkgeverApkComparisonTests
{
    [Fact]
    public void BuildComparison_includes_starter_conversation_questions()
    {
        var catalog = WerkgeverApkEmployeeAbCatalog.ForPackage(WerkgeverApkPackageKind.Basis);
        var dto = WerkgeverApkComparisonEngine.BuildComparison(
            PupilMboHboProgressState.Parse("{}"),
            new Dictionary<string, int>(),
            catalog,
            SchoolLevel.Mbo3);
        Assert.True(dto.ConversationQuestions.Count >= 3);
    }

    [Fact]
    public void BuildComparison_lists_agree_when_dimension_scores_align()
    {
        var q = WerkgeverApkManagerQuestionCatalog.All[0];
        var employee = StateForQuestion(q, selfValue: 5);
        var manager = new Dictionary<string, int> { [q.Id] = 5 };
        var catalog = WerkgeverApkEmployeeAbCatalog.ForPackage(WerkgeverApkPackageKind.Uitgebreid);
        var dto = WerkgeverApkComparisonEngine.BuildComparison(employee, manager, catalog, SchoolLevel.Mbo3);
        Assert.Contains(dto.Agree, t => t.Label == q.Label);
    }

    [Fact]
    public void BuildComparison_lists_differ_when_gap_large()
    {
        var q = WerkgeverApkManagerQuestionCatalog.All[0];
        var employee = StateForQuestion(q, selfValue: 1);
        var manager = new Dictionary<string, int> { [q.Id] = 5 };
        var catalog = WerkgeverApkEmployeeAbCatalog.ForPackage(WerkgeverApkPackageKind.Uitgebreid);
        var dto = WerkgeverApkComparisonEngine.BuildComparison(employee, manager, catalog, SchoolLevel.Mbo3);
        Assert.Contains(dto.Differ, t => t.Label == q.Label);
    }

    private static PupilMboHboProgressState StateForQuestion(WerkgeverApkManagerQuestion q, int selfValue)
    {
        var catalog = WerkgeverApkEmployeeAbCatalog.ForPackage(WerkgeverApkPackageKind.Uitgebreid);
        var answers = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var item in catalog.Items)
        {
            if (!string.IsNullOrWhiteSpace(q.EmployeeCompetenceDimension))
            {
                if (item.DimensionA.Contains(q.EmployeeCompetenceDimension, StringComparison.OrdinalIgnoreCase))
                {
                    answers[item.Id] = WerkgeverApkAnswerValidation.AbChoiceA;
                }
                else if (item.DimensionB?.Contains(q.EmployeeCompetenceDimension, StringComparison.OrdinalIgnoreCase) == true)
                {
                    answers[item.Id] = WerkgeverApkAnswerValidation.AbChoiceB;
                }
            }

            if (!string.IsNullOrWhiteSpace(q.EmployeeValueBarKey))
            {
                if (item.DimensionA.Contains(q.EmployeeValueBarKey, StringComparison.OrdinalIgnoreCase))
                {
                    answers[item.Id] = selfValue >= 3
                        ? WerkgeverApkAnswerValidation.AbChoiceA
                        : WerkgeverApkAnswerValidation.AbChoiceB;
                }
            }
        }

        return PupilMboHboProgressState.Parse(System.Text.Json.JsonSerializer.Serialize(new { answers }));
    }
}
