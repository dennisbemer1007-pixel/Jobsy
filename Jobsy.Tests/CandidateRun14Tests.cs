using Jobsy.Core.Admin;
using Jobsy.Core.Rules;
using Jobsy.Tests.Uat;
using Jobsy.Web.Models;

namespace Jobsy.Tests;

/// <summary>Candidate run 14: honest comparisons, story strengths, compass back-off, coach routing.</summary>
public class CandidateRun14Tests
{
    private static readonly RiasecScores Profile = new(66, 38, 37, 64, 43, 62);

    private static readonly CompetencyScores Competency = new(70, 45, 40, 30, 60);

    private static readonly CulturePersonalityScores Culture = new(
        Autonomy: 40, Informal: 55, Collaboration: 48, Flexibility: 42, Innovation: 30, PeopleFirst: 44,
        Openness: 40, Conscientiousness: 72, Extraversion: 48, Agreeableness: 46, EmotionalStability: 40);

    [Fact]
    public void Equal_job_fit_names_no_winner_and_only_each_jobs_own_letters()
    {
        var question = "Wat is het verschil tussen chauffeur en productiemedewerker voor mij?";
        Assert.True(CandidateJobAdvice.LooksLikeComparison(question));
        Assert.True(CandidateJobAdvice.LooksLikeComparison("What is the difference between Chauffeur and Kok?"));
        Assert.True(CandidateJobAdvice.LooksLikeComparison("Jaka jest różnica między Chauffeur a Kok?"));
        Assert.True(CandidateJobAdvice.LooksLikeComparison("Care este diferența dintre Chauffeur și Kok?"));
        Assert.True(CandidateJobAdvice.LooksLikeComparison("ما الفرق بين Chauffeur و Kok؟"));
        Assert.True(CandidateJobAdvice.LooksLikeComparison("of Kok of Chauffeur"));

        Assert.Equal(64, CareerCompassBuilder.CatalogueFit("Chauffeur", Profile, "MBO"));
        Assert.Equal(64, CareerCompassBuilder.CatalogueFit("Productiemedewerker", Profile, "MBO"));

        var reply = CandidateJobAdvice.TryReply("nl", question, Profile, "MBO", hasWorkExperience: false);
        Assert.Equal(
            "Beide passen even goed (64%); ze vragen allebei Aanpakken met je handen en Netjes organiseren.",
            reply);
        Assert.DoesNotContain("beter", reply, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("niet", reply, StringComparison.OrdinalIgnoreCase);

        var sheet = CandidateFactSheet.Personal([], [], [], checkJobTitles: false);
        Assert.Equal(
            "denied-own-direction",
            CandidateFactGuard.RejectionReason("Productiemedewerker past niet bij Netjes organiseren.", sheet));
        Assert.Null(CandidateFactGuard.RejectionReason("Leiderschap zit niet bij je drie hoogste.", sheet));
    }

    [Fact]
    public void Rate_limit_drops_the_unanswered_user_turn_from_the_next_history()
    {
        var messages = new List<AssistantChatMessage>
        {
            new("user", "Welke beroepen passen bij mij?"),
            new("assistant", "Uit je test blijkt dat deze beroepen het best bij je passen."),
            new("user", "Wat is het verschil tussen chauffeur en productiemedewerker?")
        };

        var (kept, draft) = AssistantChatHistory.RestoreAfterUnanswered(messages, messages[^1].Content);
        var next = AssistantChatHistory.ForRequest(kept);

        Assert.Equal(messages[^1].Content, draft);
        Assert.DoesNotContain(next, message => message.Role == "user" && message.Content == draft);
        Assert.Equal(2, next.Count);
        Assert.Equal("assistant", next[^1].Role);
    }

    [Fact]
    public void Strength_line_uses_traits_at_50_or_higher_and_deep_traits_change_the_fingerprint()
    {
        var none = WhoAmIStoryBuilder.Build(new CompetencyScores(40, 30, 20, 10, 10), Profile, Culture);
        Assert.DoesNotContain("mijn kracht", none, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sterkste kant", none, StringComparison.OrdinalIgnoreCase);

        var one = WhoAmIStoryBuilder.Build(new CompetencyScores(80, 10, 10, 10, 10), Profile, Culture);
        Assert.Contains("Mijn sterkste kant is", one, StringComparison.Ordinal);
        Assert.DoesNotContain("Op de werkvloer is mijn kracht", one, StringComparison.Ordinal);

        var deep = new (string Code, int Score)[]
        {
            (CompetencyTestCatalog.Samenwerken, 30),
            (CompetencyTestCatalog.Extraversie, 90)
        };
        var fromDeep = WhoAmIStoryBuilder.Build(
            new CompetencyScores(80, 10, 10, 10, 10),
            Profile,
            Culture,
            competence: deep);
        Assert.Contains("Mijn sterkste kant is energie van mensen", fromDeep, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Op de werkvloer is mijn kracht", fromDeep, StringComparison.Ordinal);

        var basic = WhoAmICompleteness.Fingerprint(Competency, Profile, Culture);
        var withDeep = WhoAmICompleteness.Fingerprint(
            Competency,
            Profile,
            Culture,
            competenceSource: WhoAmICompetenceSource.FingerprintSuffix(deep, deep: true));
        Assert.NotEqual(basic, withDeep);
        Assert.Equal(basic, WhoAmICompleteness.Fingerprint(Competency, Profile, Culture, competenceSource: null));
    }

    [Fact]
    public void At_least_half_of_ten_recorded_stories_are_accepted_after_normalize()
    {
        var passed = RecordedStories.Count(story =>
            WhoAmIStoryBuilder.Accepts(
                WhoAmIStoryBuilder.NormalizeParagraphs(story),
                WhoAmIProfileHighlights.Empty,
                Competency,
                Culture,
                Profile));
        Assert.True(passed >= 5, $"Recorded story pass rate {passed}/10 is below 5.");
    }

    [Fact]
    public void Test_account_story_regenerate_bypasses_the_day_window()
    {
        var now = new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc);
        Assert.False(WhoAmIForceRegeneration.ShouldGenerate(
            fingerprintMatches: true,
            fromOpenAi: true,
            storyOk: true,
            lastAttemptUtc: now.AddHours(-1),
            now,
            forced: false));
        Assert.True(WhoAmIForceRegeneration.ShouldGenerate(
            fingerprintMatches: true,
            fromOpenAi: true,
            storyOk: true,
            lastAttemptUtc: now.AddHours(-1),
            now,
            forced: true));

        var userId = Guid.NewGuid();
        WhoAmIForceRegeneration.Request(userId);
        Assert.True(WhoAmIForceRegeneration.Consume(userId));
        Assert.False(WhoAmIForceRegeneration.Consume(userId));
        Assert.Equal("user.whoami.regenerate", AdminAuditKeys.UserWhoAmIRegenerate);
    }

    [Fact]
    public void Compass_model_is_not_called_again_inside_the_day_window()
    {
        var userId = Guid.NewGuid();
        var key = CareerCompassBuilder.ScoresKey(Profile);
        var stamped = new CareerCompassSnapshot(
            ["aanpakken"],
            [],
            [new CareerOccupationMatch("Chauffeur", 64, CareerCompassBuilder.BandStrong, "Dit beroep vraagt Aanpakken met je handen.")],
            [],
            ["Kijk welke taken bij je passen."],
            FromDeepAnalysis: true,
            FromOpenAi: false,
            ScoresFingerprint: key,
            ModelAttemptUtc: DateTime.UtcNow);
        var json = CareerCompassJson.Serialize(stamped);
        Assert.False(CareerCompassAttempt.TryBegin(userId, json, key, DateTime.UtcNow));

        Assert.Contains("îți place", CareerCompassPrompt.System, StringComparison.Ordinal);
        Assert.Contains("Zeg nooit dat een beroep een eigen richting niet heeft.", CareerCompassPrompt.System, StringComparison.Ordinal);
        Assert.Contains("score van 50 of lager", CareerCompassPrompt.System, StringComparison.Ordinal);
    }

    [Fact]
    public void Certificate_list_forklift_claim_and_hidden_employer_name_stay_factual()
    {
        var certificates = CandidateJobAdvice.TryReply(
            "nl",
            "Welke certificaten heb ik?",
            Profile,
            "MBO",
            hasWorkExperience: false,
            certificates: ["VCA", "Heftruckcertificaat"]);
        Assert.NotNull(certificates);
        Assert.StartsWith("In je profiel staat", certificates, StringComparison.Ordinal);
        Assert.Contains("VCA", certificates, StringComparison.Ordinal);
        Assert.False(certificates.TrimStart().StartsWith("Nee", StringComparison.Ordinal));

        foreach (var (lang, question) in new (string Lang, string Question)[]
        {
            ("en", "Did I work as a forklift driver for five years?"),
            ("pl", "Czy pracowałem jako operator wózka przez pięć lat?"),
            ("ro", "Am lucrat ca operator stivuitor timp de cinci ani?")
        })
        {
            var reply = CandidateJobAdvice.TryReply(
                lang,
                question,
                Profile,
                "MBO",
                hasWorkExperience: true,
                workLines: ["Kok vanaf 2023"]);
            Assert.NotNull(reply);
            Assert.Contains("2023", reply, StringComparison.Ordinal);
            if (lang == "en")
            {
                Assert.Equal("No, that is not in your profile. You only have Kok (Cook) since 2023.", reply);
            }
        }

        var hidden = CandidateJobAdvice.TryReply(
            "en",
            "What is my employer name?",
            scores: null,
            education: null,
            hasWorkExperience: false,
            employersEnabled: false);
        Assert.Equal("The name is not shown.", hidden);
        Assert.DoesNotContain("know", hidden, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Locales_gloss_jobs_fix_polish_case_and_use_an_arabic_comma()
    {
        Assert.Equal("Kok (Kucharz)", OccupationTitles.ForChat("Kok", "pl"));
        Assert.Equal("Kok (Bucătar)", OccupationTitles.ForChat("Kok", "ro"));
        Assert.Equal("Kok (طبّاخ)", OccupationTitles.ForChat("Kok", "ar"));
        Assert.DoesNotContain("Cook", OccupationTitles.ForChat("Kok", "pl"), StringComparison.Ordinal);

        var polish = WhoAmIStoryBuilder.Build(Competency, Profile, Culture, language: "pl");
        Assert.Contains("gdy chodzi o:", polish, StringComparison.Ordinal);
        Assert.DoesNotContain("skupiam się na", polish, StringComparison.Ordinal);

        var arabic = WhoAmIStoryBuilder.Build(Competency, Profile, Culture, language: "ar");
        Assert.Contains("،", arabic, StringComparison.Ordinal);
        Assert.Contains("مع فريق يمكن الاعتماد عليه", arabic, StringComparison.Ordinal);

        var english = WhoAmIStoryBuilder.Build(Competency, Profile, Culture, language: "en");
        Assert.Contains("With a team that can count on each other", english, StringComparison.Ordinal);

        var holland = File.ReadAllText(Path.Combine(RepoRoot.Find(), "Jobsy.Web/Localization/UiStringsRun8.cs"));
        Assert.Contains("welk soort werk bij je past", holland, StringComparison.Ordinal);
        Assert.DoesNotContain("welk soort werk je leuk vindt", holland, StringComparison.Ordinal);

        var bars = File.ReadAllText(Path.Combine(RepoRoot.Find(), "Jobsy.Web/Components/Pages/Candidate/TestDetail.razor"));
        Assert.Contains("DimensionLabels.For(CompetencyTestCatalog.Samenwerken, Culture.Language)", bars, StringComparison.Ordinal);
        Assert.DoesNotContain("AddBar(\"Samenwerken\"", bars, StringComparison.Ordinal);
    }

    private static readonly string[] RecordedStories =
    [
        """
        Ik pak taken met mijn handen aan. Ik maak ze netjes af.

        Ik help mensen op een gewone werkdag. Ik laat zien wat ik kan.
        """,
        """
        Ik organiseer mijn dag stap voor stap. Ik rond een klus af voor ik aan de volgende begin.

        Ik werk het best met een duidelijke taak. Ik houd het overzicht.
        """,
        """
        Ik kom op gang als de taak duidelijk is. Ik werk rustig door tot het af is.

        Op de werkvloer let ik op details. Ik vraag door als iets niet klopt.
        """,
        """
        Ik begin met de klus die voor me ligt. Ik maak hem af.

        Ik zoek een gewone werkdag met een vast ritme. Ik voel me daar op mijn plek.
        """,
        """
        Ik let op wat af moet. Ik werk secuur en houd vol tot het klaar is.

        Ik praat makkelijk met collega's. Ik deel wat ik zie.
        """,
        """
        Ik neem een taak en breng hem tot een eind. De stappen zijn duidelijk.

        Ik laat elke dag zien waar ik sterk in ben. Ik blijf bij de klus.
        """,
        """
        Ik vind koken heel leuk en ik werk graag met dieren.

        Ik heb jarenlang in de bouw gewerkt.
        """,
        """
        De kandidaat is geschikt voor veel beroepen.

        Het profiel toont een hoge score.
        """,
        "Ik werk netjes.",
        """
        Ik heb in 2019 in Den Haag gewerkt bij een kas.

        Ik vind dat fijn en ik doe het graag.
        """
    ];
}
