using Jobsy.Core.Entities;
using Jobsy.Core.Reports.Career;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Services;
using Jobsy.Tests.Uat;
using UglyToad.PdfPig;

namespace Jobsy.Tests;

public class MistralCareerReportTests
{
    [Fact]
    public void Recorded_mistral_compass_still_builds_a_deep_report_and_a_long_pdf()
    {
        var json = """
            {
              "strengths": [
                "Je bent hands-on en pakt dingen snel aan",
                "Mensen helpen",
                "Overzicht houden",
                "Plannen"
              ],
              "superMatches": [
                {"title":"Hovenier of Kassenmedewerker","percent":92,"why":"This is a hands-on career match.","keys":["hovenier","kas"]}
              ],
              "strongChoices": [
                {"title":"Helpende zorg","percent":88,"why":"Jij wilt mensen helpen.","keys":["zorg"]},
                {"title":"Administratief medewerker","percent":86,"why":"Jij houdt van overzicht.","keys":["admin"]},
                {"title":"Onbekend beroep xyz","percent":99,"why":"Verzonnen titel.","keys":[]}
              ],
              "broadening": [
                {"title":"Kassamedewerker","percent":80,"why":"Jij staat graag in de winkel.","keys":["kassa"]}
              ],
              "practicalNotes": ["Kijk welke taken bij je passen."]
            }
            """;

        var compass = CareerCompassJson.TryDeserialize(json);
        Assert.NotNull(compass);
        Assert.DoesNotContain(compass!.Strengths, s => s.Contains("hands-on", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(compass.AllOccupations, m => m.Title == "Hovenier" && m.Percent == 92);
        Assert.Contains(compass.AllOccupations, m => m.Title == "Kasmedewerker" && m.Percent == 92);
        Assert.Contains(compass.AllOccupations, m => m.Title == "Helpende zorg en welzijn");
        Assert.DoesNotContain(compass.AllOccupations, m => m.Title.Contains("Onbekend", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(compass.AllOccupations, m => m.Percent >= 95);

        var domains = new List<DeepAnalysisDomainScore>
        {
            new("Realistic", 66, 8),
            new("Investigative", 38, 8),
            new("Artistic", 37, 8),
            new("Social", 64, 8),
            new("Enterprising", 43, 8),
            new("Conventional", 62, 8)
        };
        var report = CareerDeepReportBuilder.Build(
            domains,
            compass,
            null,
            new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc));

        Assert.NotNull(report);
        Assert.False(string.IsNullOrWhiteSpace(report.HollandCode));
        Assert.NotEmpty(report.ActionPlan);
        Assert.Contains(report.Occupations, o => o.TitleNl == "Hovenier" && o.MatchPercent == CareerCompassBuilder.CatalogueFit("Hovenier", new RiasecScores(66, 38, 37, 64, 43, 62)));

        var pdf = AssessmentReportPdfService.RenderCareerDeep(
            "Lobsy", [], "Test Kandidaat", "4 okt 2026", report, "nl");
        using var doc = PdfDocument.Open(pdf);
        var pages = doc.GetPages().ToList();
        Assert.True(pages.Count >= 6, $"Expected at least 6 PDF pages, got {pages.Count}.");
        var text = string.Join('\n', pages.Select(p => p.Text));
        Assert.DoesNotContain("riasec.", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("«", text, StringComparison.Ordinal);
        Assert.DoesNotContain("»", text, StringComparison.Ordinal);
        Assert.DoesNotContain("**", text, StringComparison.Ordinal);
        Assert.Contains("beroepsletters", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Invented_mistral_titles_are_filled_out_to_at_least_eight_catalogue_jobs()
    {
        var json = """
            {
              "strengths": ["Aanpakken", "Helpen", "Ordenen"],
              "superMatches": [
                {"title":"Elektrotechnicus","percent":89,"why":"Jij pakt technische klussen aan.","keys":["elektro"]}
              ],
              "strongChoices": [
                {"title":"Allround technisch talent","percent":91,"why":"Verzonnen titel.","keys":[]},
                {"title":"Bouwplaats alleskunner","percent":88,"why":"Verzonnen titel.","keys":[]}
              ],
              "broadening": [
                {"title":"Zorgheld op de afdeling","percent":80,"why":"Verzonnen titel.","keys":[]}
              ],
              "practicalNotes": ["Kijk welke taken bij je passen."]
            }
            """;

        var thin = CareerCompassJson.TryDeserialize(json);
        Assert.NotNull(thin);
        Assert.Contains(thin!.AllOccupations, m => m.Title == "Elektrotechnicus" && m.Percent == 89);
        Assert.True(thin.AllOccupations.Count() < CareerCompassSanitize.MinCatalogueJobs);
        Assert.DoesNotContain(thin.AllOccupations, m => m.Title.Contains("alleskunner", StringComparison.OrdinalIgnoreCase));

        var scores = new RiasecScores(66, 38, 37, 64, 43, 62);
        var compass = CareerCompassSanitize.EnsureDepth(thin, scores);
        var jobs = compass.AllOccupations.ToList();
        Assert.InRange(jobs.Count, CareerCompassSanitize.MinCatalogueJobs, CareerCompassSanitize.MaxCatalogueJobs);
        Assert.Contains(jobs, m => m.Title == "Elektrotechnicus" && m.Percent == CareerCompassBuilder.CatalogueFit("Elektrotechnicus", scores));
        Assert.Contains(jobs, m => !CareerGoalFit.IsClearlyHigherEducation(m.Title));

        var domains = new List<DeepAnalysisDomainScore>
        {
            new("Realistic", 66, 8),
            new("Investigative", 38, 8),
            new("Artistic", 37, 8),
            new("Social", 64, 8),
            new("Enterprising", 43, 8),
            new("Conventional", 62, 8)
        };
        var report = CareerDeepReportBuilder.Build(
            domains,
            compass,
            null,
            new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc));
        var titles = report.Occupations.Select(o => o.TitleNl).ToList();
        Assert.True(titles.Count >= CareerCompassSanitize.MinCatalogueJobs);
        foreach (var step in report.ActionPlan)
        {
            var body = step.Body.Resolve("nl");
            var namesListedJob = titles.Any(title => body.Contains(title, StringComparison.OrdinalIgnoreCase));
            var namesFallback = body.Contains("een beroep uit je lijst", StringComparison.Ordinal);
            Assert.True(namesListedJob || namesFallback, body);
            if ((step.Title.Nl ?? "").Contains("Mensen helpen", StringComparison.Ordinal))
            {
                Assert.DoesNotContain("Chauffeur", body, StringComparison.OrdinalIgnoreCase);
            }
        }

        var pdf = AssessmentReportPdfService.RenderCareerDeep(
            "Lobsy", [], "Test Kandidaat", "4 okt 2026", report, "nl");
        using var doc = PdfDocument.Open(pdf);
        var pages = doc.GetPages().ToList();
        Assert.True(pages.Count >= 6, $"Expected at least 6 PDF pages, got {pages.Count}.");
        var text = string.Join('\n', pages.Select(p => p.Text));
        Assert.DoesNotContain("als je taken vergelijkt. Het is geen cijfer.", text, StringComparison.Ordinal);
        Assert.DoesNotContain(text, "Allround technisch talent", StringComparison.OrdinalIgnoreCase);
        var cover = pages[0].Text;
        Assert.DoesNotContain("Overzicht", cover, StringComparison.Ordinal);
    }

    [Fact]
    public void Deep_reset_drops_the_deep_compass_and_restores_scores_from_the_basic_answers()
    {
        var answers = Enumerable.Range(1, CareerTestCatalog.QuestionCount).ToDictionary(id => id, _ => 4);
        var basic = CareerTestCatalog.Score(answers);
        Assert.NotNull(basic);
        var deep = CareerCompassBuilder.Build(new RiasecScores(66, 38, 37, 64, 43, 62), fromDeepAnalysis: true);
        if (!deep.HasOccupations)
        {
            deep = deep with
            {
                StrongChoices =
                [
                    new CareerOccupationMatch("Elektrotechnicus", 89, CareerCompassBuilder.BandStrong, "Technische klus.", ["elektro"])
                ]
            };
        }

        var row = new CandidateCareerInterest
        {
            Status = CandidateCompetencyStatuses.Completed,
            AnswersJson = CareerTestCatalog.SerializeAnswers(answers),
            CompassJson = CareerCompassJson.Serialize(deep with { FromDeepAnalysis = true }),
            RealisticPercent = 66,
            InvestigativePercent = 38,
            ArtisticPercent = 37,
            SocialPercent = 64,
            EnterprisingPercent = 43,
            ConventionalPercent = 62
        };

        Assert.True(CareerInterestDeepReset.ClearDeepCompass(row, new DateTime(2026, 10, 4, 0, 0, 0, DateTimeKind.Utc)));
        var stored = CareerCompassJson.TryDeserialize(row.CompassJson);
        Assert.True(stored is null || (!stored.FromDeepAnalysis && !stored.HasOccupations));
        Assert.DoesNotContain("Elektrotechnicus", row.CompassJson, StringComparison.Ordinal);
        Assert.Equal(basic!.Realistic, row.RealisticPercent);
        Assert.Equal(basic.Social, row.SocialPercent);
        Assert.Equal(CareerTestCatalog.HollandCode(basic), row.HollandCode);
        Assert.False(CareerInterestDeepReset.ClearDeepCompass(row, DateTime.UtcNow));
    }

    [Fact]
    public void Radar_labels_are_wrapped_instead_of_cut_off()
    {
        var root = RepoRoot.Find();
        var chart = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Candidate/ScoreRadarChart.razor"));
        var svg = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Candidate/ScoreRadarSvg.cs"));
        Assert.DoesNotContain("[..14]", chart, StringComparison.Ordinal);
        Assert.Contains("tspan", svg, StringComparison.Ordinal);
    }
}
