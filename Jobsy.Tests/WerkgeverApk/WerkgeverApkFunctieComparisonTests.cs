using Jobsy.Core.Enums;
using Jobsy.Core.Scholen.MboHbo;
using Jobsy.Core.WerkgeverApk;

namespace Jobsy.Tests.WerkgeverApk;

public sealed class WerkgeverApkFunctieComparisonTests
{
    private const string OrderpickerEsco = "cd94def5-3442-4c2e-ae3d-0761a3008bcb";

    [Fact]
    public void Build_returns_null_without_occupation()
    {
        var dto = WerkgeverApkFunctieComparisonEngine.Build(null, PupilMboHboProgressState.Parse("{}"));
        Assert.Null(dto);
    }

    [Fact]
    public void Build_lists_strengths_from_high_competence_answers()
    {
        var state = StateWithDimension("Samenwerken", 5);
        var dto = WerkgeverApkFunctieComparisonEngine.Build(OrderpickerEsco, state, SchoolLevel.Mbo3);
        Assert.NotNull(dto);
        Assert.Contains(dto!.Strengths, s => s.Label.Contains("Samenwerken", StringComparison.Ordinal));
        Assert.All(dto.Strengths, s => Assert.DoesNotContain("ESCO", s.Sentence, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Growth_tooltips_quote_only_employee_answer_text()
    {
        var state = StateWithDimension("Creativiteit / vernieuwen (Innovatie)", 1);
        var dto = WerkgeverApkFunctieComparisonEngine.Build(OrderpickerEsco, state, SchoolLevel.Mbo3);
        Assert.NotNull(dto);
        Assert.NotEmpty(dto!.GrowthAreas);
        Assert.All(dto.GrowthAreas, g =>
        {
            Assert.Contains("In je antwoorden gaf je aan", g.Tooltip, StringComparison.Ordinal);
            Assert.DoesNotContain("AI", g.Tooltip, StringComparison.OrdinalIgnoreCase);
        });
        Assert.True(dto.GrowthAreas.Count <= 5);
    }

    private static PupilMboHboProgressState StateWithDimension(string dimension, int value)
    {
        var catalog = WerkgeverApkEmployeeCatalog.Default;
        var answers = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var q in catalog.QuestionsFor(PupilMboHboBlock.Competentie, SchoolLevel.Mbo3))
        {
            answers[q.Id] = string.Equals(q.Dimension, dimension, StringComparison.OrdinalIgnoreCase) ? value : 3;
        }

        foreach (var block in WerkgeverApkEntryPaths.EmployeeBlocks)
        {
            foreach (var q in catalog.QuestionsFor(block, SchoolLevel.Mbo3))
            {
                answers.TryAdd(q.Id, 3);
            }
        }

        return PupilMboHboProgressState.Parse(System.Text.Json.JsonSerializer.Serialize(new { answers }));
    }
}