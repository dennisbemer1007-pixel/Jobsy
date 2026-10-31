using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Enums;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.PupilPassport;

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
    public void TopJobKeys_only_includes_matches_above_seventy_percent()
    {
        var result = MinimalResult("IS");
        result.RiasecScoresJson =
            """{"realistic":78,"investigative":88,"artistic":42,"social":58,"enterprising":32,"conventional":36}""";
        var progress = new PupilProgress
        {
            LikesJson = """["dieren","natuur"]""",
            VakScoresJson = FullVakJson()
        };
        var vak = PupilVakScores.Parse(progress.VakScoresJson);
        var matches = PupilG78CareerScoring.TopJobMatches(result, progress, vak, minMatch: 0.70, maxCount: 5);
        Assert.NotEmpty(matches);
        Assert.All(matches, m => Assert.True(m.MatchScore > 0.70));
        var keys = PupilG78CareerScoring.TopJobKeys(result, progress, vak, take: 5);
        Assert.Equal(matches.Count, keys.Count);
    }

    [Fact]
    public void BuildGroep78_job_cards_use_distinct_beroepsgroep_labels_not_like_chips()
    {
        var matches = new[]
        {
            new PupilG78JobMatch("fotograaf", 0.82),
            new PupilG78JobMatch("contentmaker", 0.81),
            new PupilG78JobMatch("dierenarts", 0.79),
        };
        var cards = PupilPassportJobCards.BuildGroep78(matches, "nl");
        Assert.Equal(3, cards.Count);
        Assert.Equal(PupilG78BeroepssoortCatalog.Title("creatief-design", english: false), cards[0].DomainLabel);
        Assert.Equal(PupilG78BeroepssoortCatalog.Title("media-digital", english: false), cards[1].DomainLabel);
        Assert.Equal(PupilG78BeroepssoortCatalog.Title("dieren-leven", english: false), cards[2].DomainLabel);
        Assert.DoesNotContain("dieren", cards[0].DomainLabel, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PrimaryBeroepssoortKey_prefers_later_catalog_match_for_shared_jobs()
    {
        Assert.Equal("media-digital", PupilG78CareerScoring.PrimaryBeroepssoortKey("contentmaker"));
        Assert.Equal("creatief-design", PupilG78CareerScoring.PrimaryBeroepssoortKey("fotograaf"));
    }

    [Fact]
    public void TopJobKeys_prefers_vak_weight_when_complete()
    {
        var result = MinimalResult("IS");
        result.RiasecScoresJson =
            """{"realistic":78,"investigative":88,"artistic":42,"social":58,"enterprising":32,"conventional":36}""";
        var progress = new PupilProgress
        {
            LikesJson = """["dieren","natuur"]""",
            VakScoresJson = FullVakJson()
        };
        var vak = PupilVakScores.Parse(progress.VakScoresJson);
        var keys = PupilG78CareerScoring.TopJobKeys(result, progress, vak, take: 5);
        Assert.NotEmpty(keys);
        Assert.True(keys.Count <= 5);
        var dierenJobs = new[] { "dierenverzorger", "dierenarts", "bioloog" };
        Assert.NotNull(keys.FirstOrDefault(k => dierenJobs.Contains(k, StringComparer.OrdinalIgnoreCase)));
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
