using Jobsy.Core.Options;
using Jobsy.Core.Reports.Career;
using Jobsy.Core.Rules;
using Jobsy.Tests.Uat;

namespace Jobsy.Tests;

public class CandidateRun11Tests
{
    private static readonly RiasecScores Scores = new(66, 38, 37, 64, 43, 62);
    private static readonly CompetencyScores Competency = new(80, 70, 65, 55, 40);
    private static readonly CulturePersonalityScores Culture = new(
        Autonomy: 40, Informal: 60, Collaboration: 80, Flexibility: 55, Innovation: 50, PeopleFirst: 65,
        Openness: 55, Conscientiousness: 70, Extraversion: 60, Agreeableness: 75, EmotionalStability: 70);

    [Fact]
    public void Empty_profile_story_names_no_place_and_keeps_mbo()
    {
        var profile = new WhoAmIProfileHighlights(
            Roles: [],
            Educations: ["MBO"],
            Certificates: [],
            HomeCity: null);
        var story = WhoAmIStoryBuilder.Build(Competency, Scores, Culture, profile, employersEnabled: false);
        Assert.DoesNotContain("Den Haag", story, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Westland", story, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("gewerkt", story, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("MBO", story, StringComparison.Ordinal);
        Assert.Null(CandidateFactGuard.RejectionReason(story, CandidateFactSheet.ForWhoAmI(Competency, Scores, Culture, profile, null)));
        Assert.DoesNotContain("werkgever", story, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Diploma_word_is_allowed_when_education_is_mbo()
    {
        var sheet = CandidateFactSheet.Personal([], ["MBO"], []);
        Assert.Null(CandidateFactGuard.RejectionReason("Je hebt een MBO-diploma.", sheet));
        Assert.Equal("unknown-diploma", CandidateFactGuard.RejectionReason("Je hebt een MBO-diploma.", CandidateFactSheet.Personal([], [], [])));
        Assert.Equal("unknown-diploma", CandidateFactGuard.RejectionReason("Je opleiding is afgerond.", sheet));
        Assert.Equal("unknown-diploma", CandidateFactGuard.RejectionReason("Je hebt een HBO-diploma.", sheet));
    }

    [Fact]
    public void Invented_place_and_like_are_rejected()
    {
        var sheet = CandidateFactSheet.ForWhoAmI(Competency, Scores, Culture, WhoAmIProfileHighlights.Empty, null);
        Assert.Equal("invented-place", CandidateFactGuard.RejectionReason("Ik werk in Den Haag.", sheet));
        Assert.Equal("invented-like", CandidateFactGuard.RejectionReason("Je hebt een warm hart voor dieren.", sheet));
        Assert.Equal("invented-like", CandidateFactGuard.RejectionReason("Ik ben enthousiast over koken.", sheet));
    }

    [Fact]
    public void Score_labels_do_not_trip_the_repeat_rule()
    {
        const string story = """
            Ik kom tot mijn recht als ik mensen help en dingen netjes organiseer.

            Op de werkvloer is mijn kracht netjes werken. Mensen helpen past bij mij.
            """;
        Assert.True(WhoAmIStoryBuilder.Accepts(story, WhoAmIProfileHighlights.Empty, Competency, Culture, Scores));
        Assert.Contains("Noem geen woonplaats", WhoAmIPrompt.System, StringComparison.Ordinal);
    }

    [Fact]
    public void Compass_uses_calculated_percents_and_eight_to_twelve_jobs()
    {
        var snapshot = CareerCompassSanitize.EnsureDepth(CareerCompassSnapshot.Empty(), Scores, "MBO");
        var jobs = snapshot.AllOccupations.ToList();
        Assert.InRange(jobs.Count, 8, 12);
        Assert.All(jobs, job => Assert.Equal(CareerCompassBuilder.CatalogueFit(job.Title, Scores, "MBO"), job.Percent));
        Assert.Contains("Verzin geen 95-100", CareerCompassPrompt.System, StringComparison.Ordinal);
        Assert.Contains("8 tot 12", CareerCompassPrompt.System, StringComparison.Ordinal);
        var higher = jobs.Where(job => CareerGoalFit.IsClearlyHigherEducation(job.Title)).ToList();
        var mbo = jobs.Where(job => !CareerGoalFit.IsClearlyHigherEducation(job.Title)).ToList();
        if (higher.Count > 0 && mbo.Count > 0)
        {
            Assert.True(jobs.FindIndex(job => CareerGoalFit.IsClearlyHigherEducation(job.Title)) > jobs.FindIndex(job => !CareerGoalFit.IsClearlyHigherEducation(job.Title)));
        }
    }

    [Fact]
    public void Pitfalls_come_from_the_top_domains()
    {
        var domains = new List<DeepAnalysisDomainScore>
        {
            new("Realistic", 66, 8),
            new("Social", 64, 8),
            new("Conventional", 62, 8),
            new("Enterprising", 43, 8),
            new("Investigative", 38, 8),
            new("Artistic", 37, 8)
        };
        var report = CareerDeepReportBuilder.Build(domains, CareerCompassSnapshot.Empty(), null, DateTime.UtcNow);
        Assert.Contains("pitfall.Realistic", report.PitfallKeys);
        Assert.Contains("pitfall.Social", report.PitfallKeys);
        Assert.Contains("pitfall.Conventional", report.PitfallKeys);
        Assert.DoesNotContain("pitfall.Artistic", report.PitfallKeys);
        Assert.DoesNotContain("pitfall.Investigative", report.PitfallKeys);
    }

    [Fact]
    public void Mistral_slots_cover_the_other_candidate_texts()
    {
        var options = new MistralOptions
        {
            Model = "mistral-small-latest",
            Models = new MistralFeatureModels
            {
                CareerPath = "mistral-career-path",
                CompetenceReport = "mistral-competence",
                CultureFit = "mistral-culture",
                RoleFit = "mistral-role"
            }
        };
        Assert.Equal("mistral-career-path", options.ModelFor(Jobsy.Core.Enums.OpenAiFeature.CareerPathPlan));
        Assert.Equal("mistral-competence", options.ModelFor(Jobsy.Core.Enums.OpenAiFeature.CompetenceDeepReport));
        Assert.Equal("mistral-culture", options.ModelFor(Jobsy.Core.Enums.OpenAiFeature.CultureFit));
        Assert.Equal("mistral-role", options.ModelFor(Jobsy.Core.Enums.OpenAiFeature.RoleFitCheck));
        Assert.Contains(options.ActiveFeatureModels(), row => row.Feature == MistralFeatureSlots.CareerPath);
    }

    [Fact]
    public void Chat_titles_add_an_english_name_in_brackets()
    {
        Assert.Equal("Kok (Cook)", OccupationTitles.ForChat("Kok", "en"));
        Assert.Equal("Kok (Cook)", OccupationTitles.ForChat("Kok", "ar"));
        Assert.Equal("Kok", OccupationTitles.ForChat("Kok", "nl"));
    }

    [Fact]
    public void Story_template_follows_the_ui_language_and_skips_a_missing_city()
    {
        var en = WhoAmIStoryBuilder.Build(Competency, Scores, Culture, WhoAmIProfileHighlights.Empty, employersEnabled: false, language: "en");
        Assert.Contains("I do my best work", en, StringComparison.Ordinal);
        Assert.DoesNotContain("Den Haag", en, StringComparison.Ordinal);
        var ar = WhoAmIStoryBuilder.Build(Competency, Scores, Culture, WhoAmIProfileHighlights.Empty, employersEnabled: false, language: "ar");
        Assert.Contains("أعمل", ar, StringComparison.Ordinal);
    }

    [Fact]
    public void Mobile_coach_reserves_bubble_space_and_hides_only_the_tip()
    {
        var root = RepoRoot.Find();
        var css = File.ReadAllText(Path.Combine(root, "Jobsy.Web/wwwroot/css/app.css"));
        Assert.Contains("56px + 88px + 16px", css, StringComparison.Ordinal);
        Assert.Contains(".lobsy-coach-dock__tip {\n        display: none;", css, StringComparison.Ordinal);
        Assert.DoesNotContain(".lobsy-coach-dock {\n        visibility: hidden;", css, StringComparison.Ordinal);
        var chat = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/LobsyAssistantChat.razor"));
        Assert.Contains("Assistant.NewChat", chat, StringComparison.Ordinal);
        Assert.Contains("_sendGeneration", chat, StringComparison.Ordinal);
    }
}
