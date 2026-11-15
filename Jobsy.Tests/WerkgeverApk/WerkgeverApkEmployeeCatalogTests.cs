using Jobsy.Core.Enums;
using Jobsy.Core.Scholen.MboHbo;
using Jobsy.Core.WerkgeverApk;

namespace Jobsy.Tests.WerkgeverApk;

public sealed class WerkgeverApkEmployeeCatalogTests
{
    [Fact]
    public void Interesse_first_question_is_work_focused_not_school()
    {
        var q = WerkgeverApkEmployeeCatalog.Default.QuestionsFor(PupilMboHboBlock.Interesse, SchoolLevel.Mbo3)[0];
        Assert.Contains("met mijn handen", q.Question, StringComparison.Ordinal);
        Assert.DoesNotContain("later", q.Question, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("school", q.Scenario, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Question_counts_match_employee_flow_blocks()
    {
        var catalog = WerkgeverApkEmployeeCatalog.Default;
        Assert.Equal(48, catalog.QuestionCount(PupilMboHboBlock.Interesse, SchoolLevel.Mbo3));
        Assert.Equal(40, catalog.QuestionCount(PupilMboHboBlock.Competentie, SchoolLevel.Mbo3));
        Assert.Equal(41, catalog.QuestionCount(PupilMboHboBlock.Werkwaarden, SchoolLevel.Mbo3));
    }
}