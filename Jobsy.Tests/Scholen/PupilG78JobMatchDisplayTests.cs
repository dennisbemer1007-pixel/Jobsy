using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.PupilPassport;

namespace Jobsy.Tests.Scholen;

public class PupilG78JobMatchDisplayTests
{
    [Fact]
    public void MatchPercentInt_uses_same_scale_as_threshold()
    {
        Assert.Equal(70, PupilG78CareerScoring.MatchPercentInt(0.70));
        Assert.Equal(82, PupilG78CareerScoring.MatchPercentInt(0.82));
        Assert.True(PupilG78CareerScoring.MatchPercentInt(0.705) >= 70);
    }

    [Fact]
    public void Groep78_story_job_ideas_match_pdf_top_jobs_when_scored()
    {
        var result = FixtureResult();
        var progress = FixtureProgress(result.PupilCodeId);
        var vak = PupilVakScores.Parse(progress.VakScoresJson);
        var keys = PupilStoryTemplates.SelectKeys(result, ["dieren", "tekenen", "natuur"], PupilQuestionSet.Groep78, progress)
            .JobIdeaKeys;
        var pdfKeys = PupilG78CareerScoring.TopJobKeys(result, progress, vak, take: 5);
        Assert.Equal(pdfKeys, keys);
    }

    [Fact]
    public void BuildGroep78_sets_match_percent_on_cards()
    {
        var matches = new[] { new PupilG78JobMatch("fotograaf", 0.823) };
        var cards = PupilPassportJobCards.BuildGroep78(matches, "nl");
        Assert.Single(cards);
        Assert.Equal(82, cards[0].MatchPercent);
    }

    private static PupilResult FixtureResult() => new()
    {
        PupilCodeId = Guid.NewGuid(),
        HollandCode = "IA",
        RiasecScoresJson =
            """{"realistic":55,"investigative":62,"artistic":88,"social":58,"enterprising":40,"conventional":35}""",
        CompetenceScoresJson = "{}",
        ValuesScoresJson = "{}",
        CultureScoresJson = "{}",
        TopValue = "Connection"
    };

    private static PupilProgress FixtureProgress(Guid codeId)
    {
        var vakMap = new Dictionary<string, object>();
        foreach (var key in PupilVakScores.AllSubjectKeys)
        {
            vakMap[key] = new { leuk = 3, goed = 3 };
        }

        vakMap["tekenen"] = new { leuk = 5, goed = 5 };
        vakMap["natuur-techniek"] = new { leuk = 5, goed = 4 };
        return new PupilProgress
        {
            PupilCodeId = codeId,
            LikesJson = """["dieren","tekenen","natuur"]""",
            VakScoresJson = System.Text.Json.JsonSerializer.Serialize(vakMap)
        };
    }
}
