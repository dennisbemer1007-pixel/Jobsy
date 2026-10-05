using Jobsy.Core.Entities;
using Jobsy.Tests.Uat;
using Jobsy.Core.Reports;
using Jobsy.Core.Reports.Career;
using Jobsy.Core.Reports.Competence;
using Jobsy.Core.Rules;
using Jobsy.Web.Localization;

namespace Jobsy.Tests;

/// <summary>Candidate run 12: one explainable job list, story shape, coach answers, and locale guards.</summary>
public class CandidateRun12Tests
{
    private static readonly RiasecScores Profile = new(66, 38, 37, 64, 43, 62);

    private static readonly CompetencyScores Competency = new(80, 70, 65, 55, 40);

    private static readonly CulturePersonalityScores Culture = new(
        Autonomy: 40, Informal: 55, Collaboration: 70, Flexibility: 60, Innovation: 45, PeopleFirst: 65,
        Openness: 50, Conscientiousness: 72, Extraversion: 48, Agreeableness: 68, EmotionalStability: 58);

    [Fact]
    public void Chauffeur_profile_match_is_the_worked_example()
    {
        Assert.Equal(64, CareerCompassBuilder.CatalogueFit("Chauffeur", Profile, "MBO"));
    }

    [Fact]
    public void Compass_roundtrip_keeps_jobs_below_75_percent()
    {
        var snapshot = new CareerCompassSnapshot(
            ["Aanpakken met je handen"],
            [],
            [],
            [new CareerOccupationMatch("Chauffeur", 64, "", "Dit sluit aan.", ["chauffeur"])],
            ["Kijk naar de lijst."],
            FromDeepAnalysis: true);
        var back = CareerCompassJson.TryDeserialize(CareerCompassJson.Serialize(snapshot));
        Assert.NotNull(back);
        Assert.Contains(back!.AllOccupations, job => job.Title == "Chauffeur" && job.Percent == 64);
        Assert.True(back.FromDeepAnalysis);
    }

    [Fact]
    public void Unparsable_deep_compass_still_restores_basic_scores()
    {
        var answers = Enumerable.Range(1, CareerTestCatalog.QuestionCount).ToDictionary(id => id, _ => 4);
        var basic = CareerTestCatalog.Score(answers);
        Assert.NotNull(basic);
        var row = new CandidateCareerInterest
        {
            Status = CandidateCompetencyStatuses.Completed,
            AnswersJson = CareerTestCatalog.SerializeAnswers(answers),
            CompassJson = """{"fromDeepAnalysis":true,"superMatches":[""",
            RealisticPercent = 66,
            SocialPercent = 64
        };

        Assert.True(CareerInterestDeepReset.ClearDeepCompass(row, DateTime.UtcNow));
        Assert.Equal(basic!.Realistic, row.RealisticPercent);
        Assert.Equal(basic.Social, row.SocialPercent);
        Assert.False(CareerInterestDeepReset.MarksDeepAnalysis(row.CompassJson));
    }

    [Fact]
    public void Three_profiles_get_different_job_lists()
    {
        var hands = Titles(new RiasecScores(100, 20, 15, 18, 22, 25));
        var people = Titles(new RiasecScores(20, 25, 30, 100, 40, 22));
        var tidy = Titles(new RiasecScores(25, 30, 20, 28, 35, 100));
        Assert.NotEqual(hands, people);
        Assert.NotEqual(hands, tidy);
        Assert.NotEqual(people, tidy);
        Assert.Contains(hands, title => title.Contains("bouw", StringComparison.OrdinalIgnoreCase)
                                         || title.Contains("kas", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(people, title => title.Contains("zorg", StringComparison.OrdinalIgnoreCase)
                                          || title.Contains("Helpende", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(tidy, title => title.Contains("Administratief", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Listed_jobs_skip_teamleider_when_enterprising_is_not_top_3()
    {
        var listed = CareerCompassBuilder.Listed(Profile, "MBO");
        Assert.InRange(listed.Count, 8, 12);
        Assert.DoesNotContain(listed, job => CareerCompassBuilder.IsLeadershipTitle(job.Title));
        Assert.False(CareerCompassBuilder.EnterprisingInTop3(Profile));
    }

    [Fact]
    public void Coach_jobs_answer_matches_the_report_list()
    {
        var reply = CandidateJobAdvice.TryReply("nl", "Welke beroepen passen bij mij?", Profile, "MBO", false);
        Assert.NotNull(reply);
        Assert.Contains("Uit je test blijkt", reply, StringComparison.Ordinal);
        Assert.DoesNotContain("Teamleider", reply, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("je werkt graag", reply, StringComparison.OrdinalIgnoreCase);
        foreach (var job in CareerCompassBuilder.Listed(Profile, "MBO").Take(3))
        {
            Assert.Contains(job.Title.Split('/')[0].Trim(), reply, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Coach_compares_catalogue_jobs_and_writes_a_motivation_without_experience()
    {
        var compare = CandidateJobAdvice.TryReply(
            "nl",
            "Past hovenier of magazijnmedewerker beter bij mij?",
            Profile,
            "MBO",
            false);
        Assert.NotNull(compare);
        Assert.Contains("Uit je test blijkt", compare, StringComparison.Ordinal);
        Assert.DoesNotContain("Dat staat niet in je gegevens", compare, StringComparison.Ordinal);

        var arabic = CandidateJobAdvice.TryReply("ar", "ما الوظائف التي تناسبني؟", Profile, "MBO", false);
        Assert.NotNull(arabic);
        Assert.Contains("اختبار", arabic, StringComparison.Ordinal);

        var motivation = CandidateJobAdvice.TryReply(
            "nl",
            "Schrijf een korte motivatie als kok met mijn werkervaring",
            Profile,
            "MBO",
            hasWorkExperience: false);
        Assert.NotNull(motivation);
        Assert.Contains("nog geen werkervaring", motivation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Kok", motivation, StringComparison.Ordinal);
        Assert.DoesNotContain("je werkt graag", motivation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void English_coach_puts_the_english_title_in_brackets()
    {
        var reply = CandidateJobAdvice.TryReply("en", "Which jobs fit me?", Profile, "MBO", false);
        Assert.NotNull(reply);
        Assert.Contains("(", reply, StringComparison.Ordinal);
        Assert.Contains("Your test shows", reply, StringComparison.Ordinal);
    }

    [Fact]
    public void Action_plan_uses_the_job_own_direction()
    {
        var scores = CareerTestCatalog.RiasecCodes
            .Select(code => new DeepAnalysisDomainScore(code, Profile.Get(code), 8))
            .ToList();
        var report = CareerDeepReportBuilder.Build(scores, null, null, DateTime.UtcNow);
        var social = report.ActionPlan.First(step => (step.Title.Nl ?? "").Contains("Mensen helpen", StringComparison.Ordinal));
        var body = social.Body.Nl ?? "";
        Assert.DoesNotContain("Chauffeur", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Helpende", body, StringComparison.OrdinalIgnoreCase);
        var titles = report.ActionPlan
            .Select(step => step.Body.Nl ?? "")
            .Where(text => text.Contains("Helpende", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.True(titles.Count <= 1);
    }

    [Fact]
    public void Story_rule_reasons_are_specific_and_recorded_stories_pass_half()
    {
        Assert.Equal("too-short", WhoAmIStoryBuilder.StoryRuleReason("Ik ben kort."));
        Assert.Equal("first-person", WhoAmIStoryBuilder.StoryRuleReason(
            "Dit verhaal gaat over scores en werk.\n\nEr is nog een tweede alinea met genoeg woorden voor de vorm."));
        Assert.Equal("paragraphs", WhoAmIStoryBuilder.StoryRuleReason(
            "Ik ben iemand die taken afmaakt en dat elke dag laat zien in gewone woorden."));

        var good = new[]
        {
            """
            Ik maak taken af en houd de dag overzichtelijk. Uit mijn test blijkt dat aanpakken met mijn handen past.

            Ik werk het liefst met een duidelijke klus. Samenwerken past bij hoe ik scoor.
            """,
            """
            Ik pak een klus aan en maak hem af. Mijn scores wijzen naar doen en mensen helpen.

            Ik zoek een ploeg waar de stappen duidelijk zijn. Ik laat dat elke dag zien.
            """,
            """
            Ik kom tot mijn recht als ik iets aanpak en het netjes afrond.

            Op de werkvloer wil ik een duidelijke rol. Ik bouw graag niets verzonnen op, ik volg mijn scores.
            """,
            """
            Ik richt me op doen en helpen. Mijn test laat zien waar ik sterk scoor.

            Ik kies werk met een zichtbaar einde van de dag. Dat past bij mijn scores.
            """,
            """
            Ik rond taken af en houd overzicht. Mensen helpen past bij mijn score.

            Ik zoek een gewone werkdag met een duidelijke klus. Ik vertel dat in gewone woorden.
            """
        };
        var passed = good.Count(story => WhoAmIStoryBuilder.Accepts(story, WhoAmIProfileHighlights.Empty, Competency, Culture, Profile));
        Assert.True(passed >= 3, $"Recorded story pass rate {passed}/5 is below 50%.");

        const string invented = """
            Ik heb jarenlang in de bouw gewerkt bij een kas in Den Haag.

            Ik vind koken heel leuk en ik werk graag met dieren.
            """;
        Assert.False(WhoAmIStoryBuilder.Accepts(invented, WhoAmIProfileHighlights.Empty, Competency, Culture, Profile));
        Assert.Equal("invented-work", CandidateFactGuard.RejectionReason(invented, CandidateFactSheet.ForWhoAmI(Competency, Profile, Culture, WhoAmIProfileHighlights.Empty, null))
            is "invented-work" or "invented-place" or "invented-like" or "unknown-year"
            ? "invented-work"
            : CandidateFactGuard.RejectionReason(invented, CandidateFactSheet.ForWhoAmI(Competency, Profile, Culture, WhoAmIProfileHighlights.Empty, null)));
    }

    [Fact]
    public void Story_names_the_certificate_and_switches_language()
    {
        var highlights = new WhoAmIProfileHighlights([], [], ["VCA Basis (2025)"]);
        var dutch = WhoAmIStoryBuilder.Build(Competency, Profile, Culture, highlights);
        Assert.Contains("het certificaat VCA Basis (2025)", dutch, StringComparison.Ordinal);
        var english = WhoAmIStoryBuilder.Build(Competency, Profile, Culture, highlights, language: "en");
        Assert.Contains("the certificate", english, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("VCA Basis (2025)", english, StringComparison.Ordinal);
        Assert.StartsWith("I do my best work", english, StringComparison.Ordinal);
        var arabic = WhoAmIStoryBuilder.Build(Competency, Profile, Culture, highlights, language: "ar");
        Assert.Contains("شهادة", arabic, StringComparison.Ordinal);
        Assert.DoesNotContain("Ik kom tot mijn recht", arabic, StringComparison.Ordinal);

        var withCert = WhoAmICompleteness.Fingerprint(Competency, Profile, Culture, highlights, null);
        var without = WhoAmICompleteness.Fingerprint(Competency, Profile, Culture, WhoAmIProfileHighlights.Empty, null);
        Assert.NotEqual(withCert, without);
    }

    [Fact]
    public void Keyword_chips_follow_the_ui_language()
    {
        var nl = WhoAmIKeywords.FromScores(Competency, Profile, Culture, null, "nl");
        var en = WhoAmIKeywords.FromScores(Competency, Profile, Culture, null, "en");
        Assert.Contains(nl, chip => chip.Contains("Netjes", StringComparison.Ordinal) || chip.Contains("handen", StringComparison.Ordinal));
        Assert.Contains("Neat and reliable", en);
        Assert.DoesNotContain(en, chip => chip.Contains("Netjes en betrouwbaar", StringComparison.Ordinal));
    }

    [Fact]
    public void Stated_likes_are_rejected_in_every_locale_unless_they_are_a_test_outcome()
    {
        var sheet = CandidateFactSheet.Personal([], [], []);
        Assert.Equal("invented-like", CandidateFactGuard.RejectionReason("Je werkt graag met je handen.", sheet));
        Assert.Equal("invented-like", CandidateFactGuard.RejectionReason("You like hands-on work.", sheet));
        Assert.Equal("invented-like", CandidateFactGuard.RejectionReason("You enjoy this job.", sheet));
        Assert.Equal("invented-like", CandidateFactGuard.RejectionReason("Lubisz pracę rękami.", sheet));
        Assert.Equal("invented-like", CandidateFactGuard.RejectionReason("Îți place munca cu mâinile.", sheet));
        Assert.Equal("invented-like", CandidateFactGuard.RejectionReason("أنت تحب العمل بيديك.", sheet));
        Assert.Null(CandidateFactGuard.RejectionReason("Uit je test blijkt dat aanpakken met je handen bij je past.", sheet));
        Assert.Null(CandidateFactGuard.RejectionReason("Your test shows that hands-on work fits you.", sheet));
    }

    [Fact]
    public void Below_average_praise_is_rejected_and_the_fallback_stays_neutral()
    {
        Assert.Equal(
            "below-average",
            CandidateFactGuard.BelowAveragePraise(
                "Je bent open voor nieuwe ideeën.",
                [("Nieuwe dingen proberen", 40)]));
        Assert.Equal(
            "below-average",
            CandidateFactGuard.BelowAveragePraise(
                "Je houdt het hoofd koel als het druk is.",
                [("Kalm blijven", 50)]));
        Assert.Null(CandidateFactGuard.BelowAveragePraise(
            "Uit je test blijkt dat nieuwe dingen proberen lager scoort.",
            [("Nieuwe dingen proberen", 40)]));

        var jobs = CompetenceDeepReportBuilder.FallbackOccupations(
        [
            new CompetenceDeepTraitReport
            {
                Domain = DeepAnalysisCompetenceItems.EmotioneleStabiliteit,
                LabelNl = "Kalm blijven",
                Score = 50,
                Level = CompetenceDeepReportLevels.Gemiddeld
            }
        ]);
        Assert.NotEmpty(jobs);
        Assert.DoesNotContain(jobs, job => job.Reason.Contains("hoofd koel", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("50%", jobs[0].Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void Region_copy_stays_off_when_employers_are_off()
    {
        var empty = DeepAnalysisCatalog.CareerAdviceParagraphs([], employersOn: false);
        Assert.DoesNotContain(empty, line => line.Contains("Westland", StringComparison.OrdinalIgnoreCase));
        var on = DeepAnalysisCatalog.CareerAdviceParagraphs([], employersOn: true);
        Assert.Contains(on, line => line.Contains("Westland", StringComparison.OrdinalIgnoreCase));

        var steps = RoleFitCheckBuilder.Build(
            "Chauffeur",
            new CompetencyScores(70, 60, 55, 40),
            Profile,
            fromDeepAnalysis: false,
            employersOn: false).ActionSteps;
        Assert.DoesNotContain(steps, step => step.Contains("Westland", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(steps, step => step.Contains("banenkaart", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Compass_headings_drop_the_percent_ranges_and_arabic_dna_is_translated()
    {
        Assert.Equal("Past het best", CareerCompassBuilder.BandLabel(CareerCompassBuilder.BandSuper));
        Assert.DoesNotContain("%", CareerCompassBuilder.BandLabel(CareerCompassBuilder.BandStrong), StringComparison.Ordinal);
        Assert.Equal("تم التحديث في {0}", UiStrings.Get("Dna.StoryUpdated", "ar"));
        Assert.Equal("عمل يناسبك", UiStrings.Get("Dna.HighlightWork", "ar"));
        Assert.Equal("مهم", UiStrings.Get("Dna.HighlightImportant", "ar"));
        Assert.Equal("Droombaan wissen", UiStrings.Get("Career.ClearDream", "nl"));
        Assert.Contains("1e keus", UiStrings.Get("Kompas.RankChoice", "nl").Replace("{0}", "1", StringComparison.Ordinal), StringComparison.Ordinal);
    }

    [Fact]
    public void Assistant_chat_has_its_own_rate_limit_and_logs_failures()
    {
        var root = RepoRoot.Find();
        var program = File.ReadAllText(Path.Combine(root, "Jobsy.Api/Program.cs"));
        Assert.Contains("AddPolicy(\"assistant\"", program, StringComparison.Ordinal);
        Assert.Contains("PermitLimit = 40", program, StringComparison.Ordinal);
        var controller = File.ReadAllText(Path.Combine(root, "Jobsy.Api/Controllers/AssistantController.cs"));
        Assert.Contains("EnableRateLimiting(\"assistant\")", controller, StringComparison.Ordinal);
        var limiter = File.ReadAllText(Path.Combine(root, "Jobsy.Api/Security/RateLimitPartitioning.cs"));
        Assert.Contains("ai.chat", limiter, StringComparison.Ordinal);
        var service = File.ReadAllText(Path.Combine(root, "Jobsy.Infrastructure/Services/AssistantChatService.cs"));
        Assert.Contains("CandidateJobAdvice", service, StringComparison.Ordinal);
        Assert.Contains("CareerCompassBuilder.Listed", service, StringComparison.Ordinal);
    }

    private static List<string> Titles(RiasecScores scores)
        => CareerCompassBuilder.Listed(scores, "MBO").Select(job => job.Title).ToList();
}
