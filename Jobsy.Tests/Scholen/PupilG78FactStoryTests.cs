using System.Text.RegularExpressions;
using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Scholen;

namespace Jobsy.Tests.Scholen;

public sealed class PupilG78FactStoryTests
{
    [Fact]
    public void Story_has_no_section_headings_and_uses_paragraphs()
    {
        var (result, progress) = FixtureHelpsOthers();
        var pkg = PupilG78FactStory.Build(result, progress, "nl");
        var paragraphs = PupilG78FactStory.SplitParagraphs(pkg.Body);
        Assert.InRange(paragraphs.Count, 2, 4);
        Assert.DoesNotContain("Zo kom jij over", pkg.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("Ook je ", pkg.Body, StringComparison.Ordinal);
    }

    [Fact]
    public void Story_uses_proper_je_grammar()
    {
        var fixtures = new[] { FixtureHelpsOthers(), FixtureLeadsGroup(), FixtureMaker() };
        foreach (var (result, progress) in fixtures)
        {
            var body = PupilG78FactStory.Build(result, progress, "nl").Body;
            Assert.DoesNotContain("Ook je ", body, StringComparison.Ordinal);
            Assert.False(Regex.IsMatch(body, @"\bme\b", RegexOptions.IgnoreCase));
            Assert.False(Regex.IsMatch(body, @"\bmij\b", RegexOptions.IgnoreCase));
            Assert.False(Regex.IsMatch(body, @"\bmijn\b", RegexOptions.IgnoreCase));
            Assert.DoesNotContain("om me heen", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("past je vindt", body, StringComparison.OrdinalIgnoreCase);
            Assert.False(Regex.IsMatch(body, @"\b(groep|bent),?\s+helpt\s+graag", RegexOptions.IgnoreCase));
            Assert.False(Regex.IsMatch(body, @"\bdat\s+(wilt|vindt|helpt)\s", RegexOptions.IgnoreCase));
        }
    }

    [Fact]
    public void Story_item_ids_are_unique_in_narrative()
    {
        var (result, progress) = FixtureHelpsOthers();
        var pkg = PupilG78FactStory.Build(result, progress, "nl");
        Assert.Equal(pkg.UsedItemIds.Count, pkg.UsedItemIds.Distinct().Count());
    }

    [Fact]
    public void Opposite_answers_produce_different_stories()
    {
        var a = PupilG78FactStory.Build(FixtureHelpsOthers().Result, FixtureHelpsOthers().Progress, "nl").Body;
        var b = PupilG78FactStory.Build(FixtureLeadsGroup().Result, FixtureLeadsGroup().Progress, "nl").Body;
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void School_subjects_in_story_match_fixture_vakken()
    {
        var (result, progress) = FixtureMaker();
        var body = PupilG78FactStory.Build(result, progress, "nl").Body;
        Assert.Contains("Natuur", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Engels en Gym", body, StringComparison.Ordinal);
    }

    [Fact]
    public void Preferences_group_school_and_hobbies()
    {
        var (result, progress) = FixtureHelpsOthers();
        var prefs = PupilG78FactStory.Build(result, progress, "nl").Preferences;
        Assert.Contains(prefs.SchoolLeuk, s => s.Contains("Teken", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(prefs.HobbyLeuk, s => s.Contains("Sport", StringComparison.OrdinalIgnoreCase));
        Assert.NotEmpty(prefs.BelangrijkItems);
    }

    [Fact]
    public void Renderer_g78_has_no_tiles()
    {
        var renderer = new PupilStoryRenderer();
        var (result, progress) = FixtureHelpsOthers();
        var view = renderer.Render(result, progress, "nl");
        Assert.Empty(view.Tiles);
        Assert.InRange(PupilG78FactStory.SplitParagraphs(view.Body).Count, 2, 4);
    }

    [Fact]
    public void Write_example_stories_for_pr()
    {
        var stories = new[]
        {
            ("Helpt anderen", PupilG78FactStory.Build(FixtureHelpsOthers().Result, FixtureHelpsOthers().Progress, "nl").Body),
            ("Neemt voortouw", PupilG78FactStory.Build(FixtureLeadsGroup().Result, FixtureLeadsGroup().Progress, "nl").Body),
            ("Maker", PupilG78FactStory.Build(FixtureMaker().Result, FixtureMaker().Progress, "nl").Body),
        };
        var text = string.Join("\n\n---\n\n", stories.Select(s => $"## {s.Item1}\n\n{s.Item2}"));
        Directory.CreateDirectory("/opt/cursor/artifacts");
        File.WriteAllText("/opt/cursor/artifacts/g78-dit-ben-jij-example-stories.md", text);
    }

    private static (PupilResult Result, PupilProgress Progress) FixtureHelpsOthers()
    {
        var answers = NeutralAnswers();
        foreach (var id in new[] { "9006", "9019", "9024", "9032", "9037", "9042", "9007", "9021" })
        {
            answers[id] = 5;
        }

        answers["9016"] = 2;
        answers["9015"] = 2;
        answers["9020"] = 2;
        return Build(
            answers,
            """["sport","dieren"]""",
            """{"tekenen":{"leuk":5,"goed":4},"muziek":{"leuk":5,"goed":3}}""",
            "SC",
            "Connection");
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
        return Build(
            answers,
            """["theater","lezen"]""",
            """{"geschiedenis":{"leuk":5,"goed":4},"engels":{"leuk":4,"goed":5}}""",
            "ES",
            "Autonomy");
    }

    private static (PupilResult Result, PupilProgress Progress) FixtureMaker()
    {
        var answers = NeutralAnswers();
        foreach (var id in new[] { "9016", "9022", "9028", "9017", "9025", "9033", "9038", "9044" })
        {
            answers[id] = 5;
        }

        answers["9020"] = 1;
        return Build(
            answers,
            """["techniek","natuur"]""",
            """{"natuur-techniek":{"leuk":5,"goed":5},"rekenen":{"leuk":4,"goed":5}}""",
            "IR",
            "Achievement");
    }

    private static (PupilResult Result, PupilProgress Progress) Build(
        Dictionary<string, int> answers,
        string likes,
        string vakJson,
        string holland,
        string topValue)
    {
        var id = Guid.NewGuid();
        var result = new PupilResult
        {
            PupilCodeId = id,
            ScoringVersion = "g78-1",
            HollandCode = holland,
            TopValue = topValue,
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
            VakScoresJson = vakJson
        };
        return (result, progress);
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
