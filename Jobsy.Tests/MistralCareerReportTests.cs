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
        Assert.Empty(compass!.SuperMatches);
        Assert.DoesNotContain(compass.Strengths, s => s.Contains("hands-on", StringComparison.OrdinalIgnoreCase));
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
        Assert.Contains(report.Occupations, o => o.TitleNl == "Hovenier" && o.MatchPercent == 92);

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
    public void Radar_labels_are_wrapped_instead_of_cut_off()
    {
        var root = RepoRoot.Find();
        var chart = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Candidate/ScoreRadarChart.razor"));
        var svg = File.ReadAllText(Path.Combine(root, "Jobsy.Web/Components/Candidate/ScoreRadarSvg.cs"));
        Assert.DoesNotContain("[..14]", chart, StringComparison.Ordinal);
        Assert.Contains("tspan", svg, StringComparison.Ordinal);
    }
}
