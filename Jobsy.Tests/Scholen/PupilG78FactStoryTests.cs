using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Scholen;

namespace Jobsy.Tests.Scholen;

public sealed class PupilG78FactStoryTests
{
    [Fact]
    public void Story_has_no_section_headings_and_uses_paragraphs()
    {
        var (result, progress) = FixtureHelpsOthers();
        result.ScoringVersion = "g78-1";
        var pkg = PupilG78FactStory.Build(result, progress, "nl");
        var paragraphs = PupilG78FactStory.SplitParagraphs(pkg.Body);
        Assert.InRange(paragraphs.Count, 2, 4);
        Assert.DoesNotContain("Zo kom jij over", pkg.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("Dit vind je belangrijk", pkg.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("Belangrijk voor jou", pkg.Body, StringComparison.Ordinal);
    }

    [Fact]
    public void Mentioned_items_map_to_answered_dimensions()
    {
        var (result, progress) = FixtureHelpsOthers();
        result.ScoringVersion = "g78-1";
        var pkg = PupilG78FactStory.Build(result, progress, "nl");
        Assert.Contains("helpt", pkg.Body, StringComparison.OrdinalIgnoreCase);
        Assert.True(pkg.UsedItemIds.Count > 0);
        Assert.All(pkg.UsedItemIds, id => Assert.InRange(id, 9001, 9060));
    }

    [Fact]
    public void Opposite_answers_produce_different_stories()
    {
        var a = PupilG78FactStory.Build(FixtureHelpsOthers().Result, FixtureHelpsOthers().Progress, "nl").Body;
        var b = PupilG78FactStory.Build(FixtureLeadsGroup().Result, FixtureLeadsGroup().Progress, "nl").Body;
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Fallback_works_without_ai_and_preferences_skip_story_items()
    {
        var (result, progress) = FixtureHelpsOthers();
        result.ScoringVersion = "g78-1";
        var pkg = PupilG78FactStory.Build(result, progress, "nl");
        Assert.False(string.IsNullOrWhiteSpace(pkg.Body));
        foreach (var used in pkg.UsedItemIds)
        {
            Assert.DoesNotContain(pkg.Preferences.BelangrijkItems,
                t => t.Contains(PupilQuestionBankNlStrings.Get($"LeerlingQ.{used}"), StringComparison.Ordinal));
        }
    }

    [Fact]
    public void Renderer_g78_has_no_tiles()
    {
        var renderer = new PupilStoryRenderer();
        var (result, progress) = FixtureHelpsOthers();
        result.ScoringVersion = "g78-1";
        var view = renderer.Render(result, progress, "nl");
        Assert.Empty(view.Tiles);
        Assert.InRange(PupilG78FactStory.SplitParagraphs(view.Body).Count, 2, 4);
    }

    private static (PupilResult Result, PupilProgress Progress) FixtureHelpsOthers()
    {
        var answers = NeutralAnswers();
        foreach (var id in new[] { "9006", "9019", "9024", "9032", "9037", "9042" })
        {
            answers[id] = 5;
        }

        answers["9016"] = 2;
        answers["9020"] = 2;
        return Build(answers, """["sport","dieren"]""");
    }

    private static (PupilResult Result, PupilProgress Progress) FixtureLeadsGroup()
    {
        var answers = NeutralAnswers();
        foreach (var id in new[] { "9005", "9010", "9020", "9026", "9031", "9041", "9052", "9058" })
        {
            answers[id] = 5;
        }

        answers["9019"] = 1;
        answers["9024"] = 1;
        return Build(answers, """["theater","lezen"]""");
    }

    private static (PupilResult Result, PupilProgress Progress) Build(Dictionary<string, int> answers, string likes)
    {
        var id = Guid.NewGuid();
        var result = new PupilResult
        {
            PupilCodeId = id,
            ScoringVersion = "g78-1",
            HollandCode = "SE",
            TopValue = "Connection",
            TopCulture = "Collaboration",
            CompetenceScoresJson = """{"Samenwerken":80,"Resultaatgerichtheid":50,"Stressbestendigheid":50,"Innovatie":50,"Extraversie":70}""",
            RiasecScoresJson = """{"Realistic":40,"Investigative":50,"Artistic":45,"Social":80,"Enterprising":75,"Conventional":40}""",
            ValuesScoresJson = """{"Autonomy":50,"Connection":85,"Achievement":55,"Stability":45,"Impact":50}""",
            CultureScoresJson = """{"Autonomy":55,"Informal":45,"Collaboration":70,"Flexibility":50,"Innovation":45,"PeopleFirst":60}"""
        };
        var progress = new PupilProgress
        {
            PupilCodeId = id,
            AnswersJson = System.Text.Json.JsonSerializer.Serialize(answers),
            LikesJson = likes,
            VakScoresJson = """{"engels":{"leuk":5,"goed":4},"gym":{"leuk":4,"goed":5}}"""
        };
        return (result, progress);
    }

    [Fact]
    public void Write_example_stories_for_pr()
    {
        var maker = FixtureMaker();
        var stories = new[]
        {
            ("Helpt anderen", PupilG78FactStory.Build(FixtureHelpsOthers().Result, FixtureHelpsOthers().Progress, "nl").Body),
            ("Neemt voortouw", PupilG78FactStory.Build(FixtureLeadsGroup().Result, FixtureLeadsGroup().Progress, "nl").Body),
            ("Maker", PupilG78FactStory.Build(maker.Result, maker.Progress, "nl").Body),
        };
        var text = string.Join("\n\n---\n\n", stories.Select(s => $"## {s.Item1}\n\n{s.Item2}"));
        Directory.CreateDirectory("/opt/cursor/artifacts");
        File.WriteAllText("/opt/cursor/artifacts/g78-dit-ben-jij-example-stories.md", text);
    }

    private static (PupilResult Result, PupilProgress Progress) FixtureMaker()
    {
        var answers = NeutralAnswers();
        foreach (var id in new[] { "9016", "9022", "9028", "9017", "9025", "9033", "9038", "9044" })
        {
            answers[id] = 5;
        }

        answers["9020"] = 1;
        return Build(answers, """["techniek","natuur"]""");
    }

    private static Dictionary<string, int> NeutralAnswers()
    {
        var d = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var id = 9001; id <= 9060; id++)
        {
            d[id.ToString()] = 3;
        }

        return d;
    }
}
