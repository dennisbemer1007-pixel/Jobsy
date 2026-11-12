using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Scholen;

namespace Jobsy.Tests.Scholen;

public class PupilG78JobMatchExplanationTests
{
    [Fact]
    public void Explanation_mentions_only_drivers_from_pupil_data()
    {
        var result = ArtisticResult();
        var progress = ProgressWithLikes(["tekenen"], """{"9018":5}""");
        var text = PupilG78JobMatchExplanation.Build("fotograaf", result, progress, "nl");
        Assert.Contains("tekenen", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("%", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Contrasting_pupils_get_different_explanations_for_different_jobs()
    {
        var artistic = ArtisticResult();
        var tech = TechResult();
        var progressArt = ProgressWithLikes(["tekenen"], """{"9018":5}""");
        var progressTech = ProgressWithLikes(["techniek", "bouwen"], """{"9016":5,"9022":5}""");
        progressTech.VakScoresJson = VakJson(natuurTechniek: 5);

        var whyArt = PupilG78JobMatchExplanation.Build("fotograaf", artistic, progressArt, "nl");
        var whyTech = PupilG78JobMatchExplanation.Build("elektricien", tech, progressTech, "nl");
        Assert.NotEqual(whyArt, whyTech);
        Assert.Contains("tekenen", whyArt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("techniek", whyTech, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Pr_example_explanations_for_documentation()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("G78_WHY_EXAMPLES"), "1", StringComparison.Ordinal))
        {
            return;
        }

        var artistic = ArtisticResult();
        var tech = TechResult();
        var progressArt = ProgressWithLikes(["tekenen"], """{"9018":5}""");
        var progressTech = ProgressWithLikes(["techniek", "bouwen"], """{"9016":5,"9028":5}""");
        progressTech.VakScoresJson = VakJson(natuurTechniek: 5);
        var e1 = PupilG78JobMatchExplanation.Build("fotograaf", artistic, progressArt, "nl");
        var e2 = PupilG78JobMatchExplanation.Build("hovenier", tech, progressTech, "nl");
        var progressSurprise = ProgressWithLikes(["gamen"], """{"9025":5}""");
        var e3 = PupilG78JobMatchExplanation.Build("bioloog", artistic, progressSurprise, "nl");
        Directory.CreateDirectory("/opt/cursor/artifacts");
        File.WriteAllText("/opt/cursor/artifacts/g78-why-examples.txt",
            $"EXAMPLE 1 (fotograaf):\n{e1}\n\nEXAMPLE 2 (hovenier):\n{e2}\n\nEXAMPLE 3 (bioloog):\n{e3}\n");
        Assert.False(string.IsNullOrWhiteSpace(e1));
    }

    [Fact]
    public void Never_uses_placeholder_job_blurb_or_generic_phrasing()
    {
        var result = ArtisticResult();
        var progress = ProgressWithLikes(["tekenen"], """{"9018":5}""");
        var text = PupilG78JobMatchExplanation.Build("fotograaf", result, progress, "nl");
        Assert.DoesNotContain("beroep om over na te denken", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dingen zoals:", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Uit je test blijkt", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Dedupes_overlapping_realistic_answer_paraphrases()
    {
        var result = TechResult();
        var progress = ProgressWithLikes(["bouwen"], """{"9016":5,"9022":5,"9028":5}""");
        var text = PupilG78JobMatchExplanation.Build("hovenier", result, progress, "nl");
        Assert.DoesNotContain("met je handen", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("buiten", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Bioloog_does_not_cite_unrelated_tekenen_vak_when_only_gamen_like()
    {
        var result = ArtisticResult();
        var progress = ProgressWithLikes(["gamen"], """{"9025":5}""");
        var text = PupilG78JobMatchExplanation.Build("bioloog", result, progress, "nl");
        Assert.DoesNotContain("Tekenen", text, StringComparison.Ordinal);
        Assert.DoesNotContain("handvaardigheid", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Dutch_grammar_guardrails()
    {
        var result = ArtisticResult();
        var progress = ProgressWithLikes(["tekenen", "dieren"], """{"9018":5,"9025":5}""");
        var text = PupilG78JobMatchExplanation.Build("fotograaf", result, progress, "nl");
        Assert.DoesNotMatch(new System.Text.RegularExpressions.Regex(@"\sme\s"), text);
        Assert.DoesNotContain(" mijn ", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Ook je ", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_sentences_count_between_3_and_5_when_data_rich()
    {
        var result = TechResult();
        var progress = ProgressWithLikes(["techniek"], """{"9016":5,"9028":5}""");
        var sentences = PupilG78JobMatchExplanation.BuildSentences("hovenier", result, progress, "nl");
        Assert.InRange(sentences.Count, PupilG78JobMatchExplanation.MinSentences, PupilG78JobMatchExplanation.MaxSentences);
    }

    private static PupilResult ArtisticResult() => new()
    {
        HollandCode = "AI",
        RiasecScoresJson =
            """{"realistic":40,"investigative":55,"artistic":90,"social":50,"enterprising":35,"conventional":30}""",
        CompetenceScoresJson = "{}",
        ValuesScoresJson = "{}",
        CultureScoresJson = "{}",
    };

    private static PupilResult TechResult() => new()
    {
        HollandCode = "RI",
        RiasecScoresJson =
            """{"realistic":88,"investigative":70,"artistic":30,"social":45,"enterprising":40,"conventional":35}""",
        CompetenceScoresJson = "{}",
        ValuesScoresJson = "{}",
        CultureScoresJson = "{}",
    };

    private static PupilProgress ProgressWithLikes(IReadOnlyList<string> likes, string answersJson)
        => new()
        {
            LikesJson = System.Text.Json.JsonSerializer.Serialize(likes),
            VakScoresJson = VakJson(tekenen: 5, natuurTechniek: 4),
            AnswersJson = answersJson
        };

    private static string VakJson(int tekenen = 3, int natuurTechniek = 3)
    {
        var map = new Dictionary<string, object>();
        foreach (var key in PupilVakScores.AllSubjectKeys)
        {
            map[key] = new { leuk = 3, goed = 3 };
        }

        map["tekenen"] = new { leuk = tekenen, goed = tekenen };
        map["natuur-techniek"] = new { leuk = natuurTechniek, goed = natuurTechniek };
        return System.Text.Json.JsonSerializer.Serialize(map);
    }
}
