using Jobsy.Core.Rules;
using Jobsy.Web.Services;

namespace Jobsy.Tests;

public class GratisDnaScoringTests
{
    [Fact]
    public void Twenty_answers_yield_four_tiles()
    {
        var answers = FullAnswers(5);
        var core = GratisDnaScoring.Compose(answers);

        Assert.Equal(20, GratisDnaScoring.CountAnswers(answers));
        Assert.NotEmpty(core.Strengths);
        Assert.NotEmpty(core.RiasecTop);
        Assert.NotNull(core.CultureHighlight);
        Assert.NotNull(core.TopValue);
    }

    [Fact]
    public void Fewer_answers_only_tiles_with_data()
    {
        var answers = new GratisDnaStoredAnswers
        {
            Competency = new Dictionary<int, int> { [1] = 5, [6] = 4 },
            Career = new Dictionary<int, int>(),
            Culture = new Dictionary<int, int>(),
            Values = new Dictionary<int, int>()
        };

        var core = GratisDnaScoring.Compose(answers);

        Assert.NotEmpty(core.Strengths);
        Assert.Empty(core.RiasecTop);
        Assert.Null(core.CultureHighlight);
        Assert.Null(core.TopValue);
    }

    [Fact]
    public void Invalid_values_are_ignored_by_count()
    {
        var answers = new GratisDnaStoredAnswers
        {
            Competency = new Dictionary<int, int> { [1] = 5 },
            Career = new Dictionary<int, int>(),
            Culture = new Dictionary<int, int>(),
            Values = new Dictionary<int, int>()
        };
        Assert.Equal(1, GratisDnaScoring.CountAnswers(answers));
    }

    private static GratisDnaStoredAnswers FullAnswers(int value)
    {
        var answers = new GratisDnaStoredAnswers();
        foreach (var id in OnboardingWizardCatalog.CompetencyQuestionIds)
        {
            answers.Competency[id] = value;
        }

        foreach (var id in OnboardingWizardCatalog.CareerQuestionIds)
        {
            answers.Career[id] = value;
        }

        foreach (var id in OnboardingWizardCatalog.CultureQuestionIds)
        {
            answers.Culture[id] = value;
        }

        foreach (var id in OnboardingWizardCatalog.ValuesQuestionIds)
        {
            answers.Values[id] = value;
        }

        return answers;
    }
}
