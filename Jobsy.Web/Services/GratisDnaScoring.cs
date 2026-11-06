using Jobsy.Core.Rules;

namespace Jobsy.Web.Services;

public static class GratisDnaScoring
{
    public static OnboardingImpressionCore Compose(GratisDnaStoredAnswers answers)
    {
        var competency = ProvisionalAssessmentScores.ResolveCompetency(
            null,
            CompetencyTestCatalog.SerializeAnswers(answers.Competency),
            null, null, null, null, null);

        var career = ProvisionalAssessmentScores.ResolveCareer(
            null,
            CareerTestCatalog.SerializeAnswers(answers.Career),
            null, null, null, null, null, null);

        var culture = ProvisionalAssessmentScores.ResolveCulture(
            null,
            CulturePersonalityCatalog.SerializeAnswers(answers.Culture),
            null);

        var values = ProvisionalAssessmentScores.ResolveValues(
            null,
            SchwartzValuesCatalog.SerializeAnswers(answers.Values),
            null, null, null, null, null);

        return OnboardingImpressionComposer.Compose(
            competency.Scores,
            career.Scores,
            culture.Scores,
            values.Scores);
    }

    public static int CountAnswers(GratisDnaStoredAnswers answers)
        => answers.Competency.Count
           + answers.Career.Count
           + answers.Culture.Count
           + answers.Values.Count;
}
