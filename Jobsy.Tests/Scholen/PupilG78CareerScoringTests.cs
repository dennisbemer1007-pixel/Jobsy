using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;

namespace Jobsy.Tests.Scholen;

public class PupilG78CareerScoringTests
{
    [Theory]
    [InlineData(5, 5, 1.0)]
    [InlineData(4, 3, 0.5)]
    [InlineData(3, 5, 0.0)]
    public void SubjectWeight_follows_leuk_and_goed_rules(int leuk, int goed, double expected)
    {
        Assert.Equal(expected, PupilVakScores.SubjectWeight(leuk, goed));
    }

    [Fact]
    public void TopJobKeys_prefers_vak_weight_when_complete()
    {
        var result = MinimalResult("IS");
        var progress = new PupilProgress
        {
            LikesJson = """["dieren","natuur"]""",
            VakScoresJson = FullVakJson()
        };
        var vak = PupilVakScores.Parse(progress.VakScoresJson);
        var keys = PupilG78CareerScoring.TopJobKeys(result, progress, vak, take: 5);
        Assert.Equal(5, keys.Count);
        Assert.Contains("dierenarts", keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void TopBeroepssoorten_returns_ranked_list()
    {
        var result = MinimalResult("RI");
        var vak = PupilVakScores.Parse(FullVakJson());
        var ranks = PupilG78CareerScoring.TopBeroepssoorten(result, vak, take: 10);
        Assert.Equal(10, ranks.Count);
        Assert.True(ranks[0].Score >= ranks[^1].Score);
    }

    private static PupilResult MinimalResult(string holland) => new()
    {
        HollandCode = holland,
        RiasecScoresJson = """{"Realistic":70,"Investigative":65,"Artistic":40,"Social":55,"Enterprising":30,"Conventional":35}""",
        CompetenceScoresJson = "{}",
        ValuesScoresJson = "{}",
        CultureScoresJson = "{}",
        TopValue = "Connection"
    };

    private static string FullVakJson()
    {
        var map = new Dictionary<string, object>();
        foreach (var key in PupilVakScores.AllSubjectKeys)
        {
            map[key] = new { leuk = 4, goed = 4 };
        }

        map["natuur-techniek"] = new { leuk = 5, goed = 5 };
        return System.Text.Json.JsonSerializer.Serialize(map);
    }
}
