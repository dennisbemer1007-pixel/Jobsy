using Jobsy.Core.Entities;
using Jobsy.Core.Rules;

namespace Jobsy.Tests;

public class OnboardingImpressionComposerTests
{
    [Fact]
    public void Mini_career_answers_never_surface_artistic_in_riasec_top()
    {
        var answers = new Dictionary<int, int>
        {
            [1] = 5,
            [6] = 5,
            [14] = 5,
            [18] = 5,
            [22] = 5
        };
        var json = CareerTestCatalog.SerializeAnswers(answers);
        var resolved = ProvisionalAssessmentScores.ResolveCareer(
            CandidateCompetencyStatuses.Draft, json, null, null, null, null, null, null);

        var core = OnboardingImpressionComposer.Compose(null, resolved.Scores, null, null);

        Assert.NotEmpty(core.RiasecTop);
        Assert.DoesNotContain(
            core.RiasecTop,
            x => string.Equals(x.Code, CareerTestCatalog.Artistic, StringComparison.Ordinal));
    }

    [Fact]
    public void Compose_picks_expected_tiles_for_fixed_mini_answers()
    {
        var competencyAnswers = new Dictionary<int, int>
        {
            [1] = 5,
            [6] = 1,
            [11] = 1,
            [16] = 1,
            [21] = 1
        };
        var competency = ProvisionalAssessmentScores.ResolveCompetency(
            CandidateCompetencyStatuses.Draft,
            CompetencyTestCatalog.SerializeAnswers(competencyAnswers),
            null, null, null, null, null).Scores;

        var careerAnswers = new Dictionary<int, int>
        {
            [1] = 1,
            [6] = 1,
            [14] = 5,
            [18] = 1,
            [22] = 1
        };
        var career = ProvisionalAssessmentScores.ResolveCareer(
            CandidateCompetencyStatuses.Draft,
            CareerTestCatalog.SerializeAnswers(careerAnswers),
            null, null, null, null, null, null).Scores;

        var cultureAnswers = new Dictionary<int, int>
        {
            [1] = 2,
            [3] = 2,
            [5] = 5,
            [7] = 2,
            [11] = 2
        };
        var culture = ProvisionalAssessmentScores.ResolveCulture(
            CandidateCompetencyStatuses.Draft,
            CulturePersonalityCatalog.SerializeAnswers(cultureAnswers),
            null).Scores;

        var valuesAnswers = new Dictionary<int, int>
        {
            [1] = 1,
            [6] = 1,
            [11] = 5,
            [16] = 1,
            [21] = 1
        };
        var values = ProvisionalAssessmentScores.ResolveValues(
            CandidateCompetencyStatuses.Draft,
            SchwartzValuesCatalog.SerializeAnswers(valuesAnswers),
            null, null, null, null, null).Scores;

        var core = OnboardingImpressionComposer.Compose(competency, career, culture, values);

        Assert.Equal(CompetencyTestCatalog.Samenwerken, core.Strengths[0].Code);
        Assert.Equal(CareerTestCatalog.Social, core.RiasecTop[0].Code);
        Assert.Equal(CulturePersonalityCatalog.Collaboration, core.CultureHighlight!.Code);
        Assert.Equal(SchwartzValuesCatalog.Achievement, core.TopValue!.Code);
        Assert.Contains("samen", core.Strengths[0].Sentence, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("anderen", core.RiasecTop[0].Sentence, StringComparison.OrdinalIgnoreCase);
    }
}
