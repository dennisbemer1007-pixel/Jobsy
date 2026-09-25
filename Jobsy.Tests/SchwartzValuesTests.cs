using Jobsy.Core.Enums;
using Jobsy.Core.Rules;
using Jobsy.Web.Models;
using Jobsy.Web.Services;

namespace Jobsy.Tests;

public class SchwartzValuesTests
{
    [Fact]
    public void Free_catalog_has_25_unique_questions_across_five_drivers()
    {
        Assert.Equal(25, SchwartzValuesCatalog.QuestionCount);
        Assert.Equal(5, SchwartzValuesCatalog.CategoryCodes.Length);
        Assert.Equal(25, SchwartzValuesCatalog.Questions.Count);
        Assert.Equal(25, SchwartzValuesCatalog.Questions.Select(q => q.TextKey).Distinct().Count());
        foreach (var code in SchwartzValuesCatalog.CategoryCodes)
        {
            Assert.Equal(5, SchwartzValuesCatalog.QuestionsFor(code).Count);
            Assert.Contains(SchwartzValuesCatalog.QuestionsFor(code), q => q.Reverse);
        }
    }

    [Fact]
    public void Deep_catalog_has_150_unique_values_items_and_supports_deep()
    {
        Assert.True(DeepAnalysisCatalog.SupportsDeepAnalysis(AssessmentKind.Values));
        Assert.Equal(150, DeepAnalysisCatalog.QuestionCountFor(AssessmentKind.Values));
        var questions = DeepAnalysisCatalog.QuestionsFor(AssessmentKind.Values);
        Assert.Equal(150, questions.Count);
        Assert.Equal(150, questions.Select(q => q.PromptNl).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        foreach (var code in SchwartzValuesCatalog.CategoryCodes)
        {
            Assert.Equal(30, questions.Count(q => q.Domain.Equals(code, StringComparison.OrdinalIgnoreCase)));
        }
    }

    [Fact]
    public void Score_and_fit_produce_stable_percentages()
    {
        var answers = new Dictionary<int, int>();
        foreach (var q in SchwartzValuesCatalog.Questions)
        {
            answers[q.Id] = q.Reverse ? 1 : 5;
        }

        var scores = SchwartzValuesCatalog.Score(answers);
        Assert.NotNull(scores);
        Assert.True(scores!.IsComplete);
        Assert.True(scores.Autonomy is >= 80);
        Assert.True(SchwartzValuesFitRules.Fit01(scores, null) > 0.7);

        var vacancyFit = SchwartzValuesFitRules.Fit01(
            scores,
            "Teamleider met targets en zelfstandigheid",
            "Samenwerken, resultaat en eigen regie");
        Assert.InRange(vacancyFit, 0, 1);
    }

    [Fact]
    public void WhoAmI_story_mentions_drivers_when_values_present()
    {
        var competency = new CompetencyScores(70, 65, 60, 55, 50);
        var career = new RiasecScores(40, 50, 45, 70, 55, 40);
        var culture = new CulturePersonalityScores(
            Autonomy: 60, Informal: 55, Collaboration: 70, Flexibility: 50, Innovation: 45, PeopleFirst: 65,
            Openness: 55, Conscientiousness: 60, Extraversion: 50, Agreeableness: 70, EmotionalStability: 55);
        var values = new SchwartzValuesScores(80, 75, 40, 35, 70);
        var story = WhoAmIStoryBuilder.Build(competency, career, culture, values: values);
        Assert.Contains("drijft", story, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Schwartz", story, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RIASEC", story, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Dna_hub_includes_waarden_card_with_deep_support()
    {
        var profile = new CandidateProfileService().GetProfile();
        Assert.Contains(profile.Tests, t => t.Id == "values");
        var values = Assert.Single(profile.Tests, t => t.Id == "values");
        Assert.Equal("Waarden & Drijfveren", values.Title);
        Assert.Contains("Schwartz", values.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.True(values.SupportsDeepAnalysis);
        Assert.Equal("/candidate/values", values.FreeTestHref);
        Assert.Equal("/candidate/deep-analysis/values", values.DeepAnalysisHref);

        var culture = Assert.Single(profile.Tests, t => t.Id == "culture");
        Assert.False(culture.SupportsDeepAnalysis);
    }

    [Fact]
    public void Assessment_kind_parses_values_slug()
    {
        Assert.True(AssessmentKindLabels.TryParse("values", out var kind));
        Assert.Equal(AssessmentKind.Values, kind);
        Assert.Equal("values", AssessmentKindLabels.ToSlug(AssessmentKind.Values));
        Assert.Contains("Waarden", AssessmentKindLabels.ToDutch(AssessmentKind.Values));
    }
}
