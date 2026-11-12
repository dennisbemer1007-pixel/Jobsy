using Jobsy.Core.Entities.Scholen;
using Jobsy.Core.Scholen;
using Jobsy.Core.Scholen.PupilPassport;

namespace Jobsy.Tests.Scholen;

public class PupilG78JobMatchExplanationTests
{
    [Fact]
    public void Explanation_mentions_only_drivers_from_pupil_data()
    {
        var result = ArtisticResult();
        var progress = ProgressWithLikes(["tekenen"], CareerAnswers("9018", 5), CompetenceAnswers(resultaat: 5));
        var text = PupilG78JobMatchExplanation.Build("fotograaf", result, progress, "nl");
        Assert.Contains("tekenen", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("%", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Never_uses_banned_fit_phrases()
    {
        var result = TechResult();
        var progress = ProgressWithLikes(["bouwen"], CareerAnswers("9016", 5, "9028", 5), CompetenceAnswers(resultaat: 5, stress: 2));
        var html = PupilG78JobMatchExplanation.BuildHtml("hovenier", result, progress, "nl");
        Assert.False(PupilG78JobMatchExplanation.ContainsBannedPhrase(html));
        Assert.DoesNotContain("past bij dit beroep", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Fotograaf_lists_mapped_competencies_only()
    {
        var result = ArtisticResult();
        var progress = ProgressWithLikes(["tekenen"], CareerAnswers("9018", 5, "9029", 5), CompetenceAnswers(resultaat: 5, innovatie: 4));
        var content = PupilG78JobMatchExplanation.BuildContent("fotograaf", result, progress, "nl");
        Assert.Single(content.Competencies);
        Assert.Contains("Netjes en precies werken", content.Competencies[0].JobAsksLabel, StringComparison.Ordinal);
        Assert.Contains("je gaf aan dat", content.Competencies[0].PupilReflection ?? "", StringComparison.Ordinal);
        Assert.DoesNotContain("dingen zoals:", content.FormatCompactPlain(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Competency_counts_per_job_match_dream_routes()
    {
        var result = TechResult();
        var progress = ProgressWithLikes(["bouwen"], CareerAnswers("9016", 5), CompetenceAnswers(resultaat: 5, stress: 2));
        Assert.Equal(2, PupilG78JobMatchExplanation.BuildContent("hovenier", result, progress, "nl").Competencies.Count);
        Assert.Equal(2, PupilG78JobMatchExplanation.BuildContent("bioloog", result, progress, "nl").Competencies.Count);
    }

    [Fact]
    public void Competencies_appear_once_not_in_answers_section()
    {
        var result = TechResult();
        var progress = ProgressWithLikes(["bouwen"], CareerAnswers("9016", 5, "9028", 5), CompetenceAnswers(resultaat: 5, stress: 2));
        var content = PupilG78JobMatchExplanation.BuildContent("hovenier", result, progress, "nl");
        var answersBlob = string.Join(' ', content.AnswerSentences);
        Assert.DoesNotContain("Netjes en precies werken", answersBlob, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Kalm blijven", answersBlob, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("g78-job-why-panel__comp-list", PupilG78JobMatchExplanation.BuildHtml("hovenier", result, progress, "nl"));
    }

    [Fact]
    public void Bioloog_omits_unrelated_tekenen_vak_when_only_gamen_like()
    {
        var result = ArtisticResult();
        var progress = ProgressWithLikes(["gamen"], CareerAnswers("9025", 5), CompetenceAnswers(resultaat: 4, innovatie: 5));
        var text = PupilG78JobMatchExplanation.Build("bioloog", result, progress, "nl");
        Assert.DoesNotContain("Tekenen", text, StringComparison.Ordinal);
        Assert.DoesNotContain("handvaardigheid", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Panel_html_has_three_section_headings_when_data_rich()
    {
        var result = TechResult();
        var progress = ProgressWithLikes(["bouwen"], CareerAnswers("9016", 5, "9028", 5), CompetenceAnswers(resultaat: 5, stress: 3));
        var html = PupilG78JobMatchExplanation.BuildHtml("hovenier", result, progress, "nl");
        Assert.Contains(PupilG78JobWhyContent.HeadingWork, html, StringComparison.Ordinal);
        Assert.Contains(PupilG78JobWhyContent.HeadingCompetencies, html, StringComparison.Ordinal);
        Assert.Contains(PupilG78JobWhyContent.HeadingAnswers, html, StringComparison.Ordinal);
    }

    [Fact]
    public void Weak_competence_uses_neutral_explore_wording()
    {
        var result = TechResult();
        var progress = ProgressWithLikes([], CareerAnswers("9016", 5), CompetenceAnswers(resultaat: 5, stress: 1));
        var plain = PupilG78JobMatchExplanation.Build("hovenier", result, progress, "nl");
        Assert.Contains("verder ontdekken", plain, StringComparison.OrdinalIgnoreCase);
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
        var progressArt = ProgressWithLikes(
            ["tekenen"],
            CareerAnswers("9018", 5, "9029", 5),
            CompetenceAnswers(samen: 3, resultaat: 3, stress: 3, innovatie: 3, extraversie: 3, "9007", 5));
        var progressTech = ProgressWithLikes(
            ["bouwen"],
            CareerAnswers("9016", 5, "9028", 5),
            CompetenceAnswers(samen: 3, resultaat: 3, stress: 1, innovatie: 3, extraversie: 3, "9002", 5, "9003", 2, "9008", 1),
            vakTeken: 3,
            vakNatuur: 5);
        var progressSurprise = ProgressWithLikes(
            ["gamen"],
            CareerAnswers("9025", 5),
            CompetenceAnswers(samen: 3, resultaat: 3, stress: 3, innovatie: 3, extraversie: 3, "9007", 4, "9009", 5));
        var e1 = PupilG78JobMatchExplanation.BuildContent("fotograaf", artistic, progressArt, "nl").FormatFullPlain();
        var e2 = PupilG78JobMatchExplanation.BuildContent("hovenier", tech, progressTech, "nl").FormatFullPlain();
        var e3 = PupilG78JobMatchExplanation.BuildContent("bioloog", artistic, progressSurprise, "nl").FormatFullPlain();
        Directory.CreateDirectory("/opt/cursor/artifacts");
        File.WriteAllText("/opt/cursor/artifacts/g78-why-examples.txt",
            $"EXAMPLE 1 (fotograaf):\n{e1}\n\nEXAMPLE 2 (hovenier):\n{e2}\n\nEXAMPLE 3 (bioloog):\n{e3}\n");
        Assert.False(string.IsNullOrWhiteSpace(e1));
    }

  private static string CareerAnswers(params object[] pairs)
    {
        var dict = new Dictionary<string, int>();
        for (var i = 0; i < pairs.Length; i += 2)
        {
            dict[pairs[i]!.ToString()!] = (int)pairs[i + 1]!;
        }

        return System.Text.Json.JsonSerializer.Serialize(dict);
    }

    private static string CompetenceAnswers(
        int samen = 3,
        int resultaat = 3,
        int stress = 3,
        int innovatie = 3,
        int extraversie = 3,
        params object[] extraPairs)
    {
        var pairs = new List<object>
        {
            "9001", samen, "9006", samen,
            "9002", resultaat, "9007", resultaat,
            "9003", stress, "9008", stress,
            "9004", innovatie, "9009", innovatie,
            "9005", extraversie, "9010", extraversie,
        };
        pairs.AddRange(extraPairs);
        return CareerAnswers(pairs.ToArray());
    }

    [Fact]
    public void Competency_bullets_use_distinct_item_paraphrases_per_job()
    {
        var result = ArtisticResult();
        var progressF = ProgressWithLikes(["tekenen"], CareerAnswers("9018", 5), CompetenceAnswers(3, 3, 3, 3, 3, "9007", 5));
        var progressH = ProgressWithLikes(["bouwen"], CareerAnswers("9016", 5), CompetenceAnswers(3, 3, 3, 3, 3, "9002", 5));
        var progressB = ProgressWithLikes(["gamen"], CareerAnswers("9025", 5), CompetenceAnswers(3, 3, 3, 3, 3, "9009", 5, "9007", 4));
        var f = PupilG78JobMatchExplanation.BuildContent("fotograaf", result, progressF, "nl").Competencies[0].FormattedBullet;
        var h = PupilG78JobMatchExplanation.BuildContent("hovenier", TechResult(), progressH, "nl").Competencies[0].FormattedBullet;
        var b = PupilG78JobMatchExplanation.BuildContent("bioloog", result, progressB, "nl").Competencies
            .First(c => c.JobAsksLabel.Contains("idee", StringComparison.OrdinalIgnoreCase)).FormattedBullet;
        Assert.Contains("plan maakt", f, StringComparison.Ordinal);
        Assert.Contains("huiswerk afmaakt", h, StringComparison.Ordinal);
        Assert.Contains("iets nieuws te proberen", b, StringComparison.Ordinal);
        Assert.DoesNotContain("huiswerk afmaakt", f, StringComparison.Ordinal);
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

    private static PupilProgress ProgressWithLikes(
        IReadOnlyList<string> likes,
        string careerAnswersJson,
        string? extraAnswersJson = null,
        int vakTeken = 5,
        int vakNatuur = 4)
    {
        var merged = new Dictionary<string, int>();
        foreach (var (k, v) in System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(careerAnswersJson)!)
        {
            merged[k] = v;
        }

        if (!string.IsNullOrWhiteSpace(extraAnswersJson))
        {
            foreach (var (k, v) in System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(extraAnswersJson)!)
            {
                merged[k] = v;
            }
        }

        return new PupilProgress
        {
            LikesJson = System.Text.Json.JsonSerializer.Serialize(likes),
            VakScoresJson = VakJson(tekenen: vakTeken, natuurTechniek: vakNatuur),
            AnswersJson = System.Text.Json.JsonSerializer.Serialize(merged)
        };
    }

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
