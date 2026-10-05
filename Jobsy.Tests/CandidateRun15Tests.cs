using System.Text.RegularExpressions;
using Jobsy.Core.Careers;
using QuestPDF.Infrastructure;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Reports;
using Jobsy.Core.Reports.Career;
using Jobsy.Core.Reports.Competence;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Services;
using UglyToad.PdfPig;

namespace Jobsy.Tests;

/// <summary>Candidate run 15: stale percents, deep scores, common jobs, coach facts and plain PDFs.</summary>
public class CandidateRun15Tests
{
    private static readonly RiasecScores Deep = new(66, 38, 37, 64, 43, 62);

    [Fact]
    public void Pre_esco_report_shows_the_recomputed_percent_and_drops_unknown_titles()
    {
        var report = new CareerDeepReport
        {
            Domains =
            [
                new DeepDomainScore { Domain = CareerTestCatalog.Realistic, Score = 66 },
                new DeepDomainScore { Domain = CareerTestCatalog.Investigative, Score = 38 },
                new DeepDomainScore { Domain = CareerTestCatalog.Artistic, Score = 37 },
                new DeepDomainScore { Domain = CareerTestCatalog.Social, Score = 64 },
                new DeepDomainScore { Domain = CareerTestCatalog.Enterprising, Score = 43 },
                new DeepDomainScore { Domain = CareerTestCatalog.Conventional, Score = 62 }
            ],
            Occupations =
            [
                new DeepOccupationFit { TitleNl = "Dierenverzorger", MatchPercent = 65, ReasonNl = "Oude tekst met 65%." },
                new DeepOccupationFit { TitleNl = "Medewerker bouw / afbouw", MatchPercent = 66, ReasonNl = "Onbekend." }
            ]
        };

        var shown = CareerDeepReportBuilder.ShownOccupations(report);
        Assert.NotEmpty(shown);
        Assert.DoesNotContain(shown, job => job.TitleNl.Contains("bouw", StringComparison.OrdinalIgnoreCase));
        foreach (var job in shown)
        {
            var fit = CareerCompassBuilder.CatalogueFit(job.TitleNl, Deep);
            Assert.Equal(fit, job.MatchPercent);
            Assert.DoesNotContain("65%", job.ReasonNl, StringComparison.Ordinal);
        }

        var animalFit = CareerCompassBuilder.CatalogueFit("dierenverzorger", Deep);
        Assert.NotNull(animalFit);
        Assert.NotEqual(65m, animalFit);
        var animal = shown.FirstOrDefault(job => string.Equals(job.TitleNl, "Dierenverzorger", StringComparison.OrdinalIgnoreCase));
        if (animal is not null)
        {
            Assert.Equal(animalFit, animal.MatchPercent);
        }

        QuestPDF.Settings.License = LicenseType.Community;
        var pdf = AssessmentReportPdfService.RenderCareerDeep(
            "Lobsy", [], "Test", "4 okt 2026", report, "nl", "mbo", compact: true);
        using var doc = PdfDocument.Open(pdf);
        var text = string.Join("\n", doc.GetPages().Select(page => page.Text));
        Assert.Contains(shown[0].TitleNl, text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(CareerCompassBuilder.FormatPercent(shown[0].MatchPercent, "nl"), text, StringComparison.Ordinal);
        Assert.DoesNotContain("65%", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Medewerker bouw", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch(new Regex(@"\d+[.,]\d+ / \d+"), text);
        Assert.DoesNotContain("ESCO id", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch(new Regex(@"[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}", RegexOptions.IgnoreCase), text);
    }

    [Fact]
    public void Stored_deep_career_scores_win_over_a_basic_row()
    {
        var report = new CareerDeepReport
        {
            Domains =
            [
                new DeepDomainScore { Domain = "Realistic", Score = 66 },
                new DeepDomainScore { Domain = "Investigative", Score = 38 },
                new DeepDomainScore { Domain = "Artistic", Score = 37 },
                new DeepDomainScore { Domain = "Social", Score = 64 },
                new DeepDomainScore { Domain = "Enterprising", Score = 43 },
                new DeepDomainScore { Domain = "Conventional", Score = 62 }
            ]
        };
        var scores = StoredDeepScores.Career(
            CandidateDeepAnalysisStatuses.Completed,
            "{}",
            CareerDeepReportJson.Serialize(report));
        Assert.NotNull(scores);
        Assert.Equal(66, scores!.Realistic);
        Assert.Equal(37, scores.Artistic);
        Assert.Equal(64, scores.Social);
        Assert.Equal(62, scores.Conventional);
        Assert.Null(StoredDeepScores.Career(CandidateDeepAnalysisStatuses.Draft, "{}", CareerDeepReportJson.Serialize(report)));
    }

    [Fact]
    public void Mbo_top_eight_leaves_out_niche_titles()
    {
        var basic = new RiasecScores(70, 38, 69, 44, 63, 44);
        var listed = CareerCompassBuilder.Listed(basic, "mbo");
        var top = listed.Take(8).ToList();
        Assert.True(top.Count >= 8);
        Assert.DoesNotContain(top, job => string.Equals(job.Title, "artistiek model", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(top, job => job.Title.Contains("harpbouwer", StringComparison.OrdinalIgnoreCase));
        Assert.All(top, job =>
        {
            var occupation = OccupationCatalog.Shared.Get(job.EscoId);
            Assert.NotNull(occupation);
            Assert.True(OccupationCatalog.Shared.IsCommonDutchTitle(occupation!));
        });
    }

    [Fact]
    public void Chauffeur_is_compared_as_vrachtwagenchauffeur()
    {
        var reply = CandidateJobAdvice.TryReply(
            "nl",
            "Wat is het verschil tussen chauffeur en kok voor mij?",
            Deep,
            "mbo",
            hasWorkExperience: false);
        Assert.NotNull(reply);
        Assert.StartsWith("Chauffeur staat niet als één beroep in de lijst.", reply, StringComparison.Ordinal);
        Assert.Contains("vrachtwagenchauffeur", reply, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("kok", reply, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Dutch_percent_uses_a_decimal_comma()
    {
        Assert.Equal("63,58", CareerCompassBuilder.FormatPercent(63.58m, "nl"));
        Assert.Equal("63,58", CareerCompassBuilder.FormatPercent(63.58m, "pl"));
        Assert.Equal("63.58", CareerCompassBuilder.FormatPercent(63.58m, "en"));
        Assert.Equal("5,01", FitPercentExplanation.Num(5.01m, "nl"));
    }

    [Fact]
    public void Competence_heading_names_the_highest_deep_bar()
    {
        Assert.Equal("Sterkst: Nieuwe dingen proberen", AssessmentOutcomeLines.StrongestLine("Nieuwe dingen proberen", "nl"));
        Assert.Equal("Strongest: Nieuwe dingen proberen", AssessmentOutcomeLines.StrongestLine("Nieuwe dingen proberen", "en"));
        Assert.Equal("Energie van mensen", DeepReportCatalog.CultureLabel(CulturePersonalityCatalog.Extraversion, "nl"));
        Assert.Equal("Energy from people", DeepReportCatalog.CultureLabel("Extraversion", "en"));
        Assert.DoesNotContain("Extraversie", DeepReportCatalog.CultureLabel("Extraversion", "nl"), StringComparison.Ordinal);
    }

    [Fact]
    public void Coach_answers_hours_availability_values_and_a_shadow_mail()
    {
        var hours = CandidateProfileFacts.TryReply("nl", "Hoeveel uur wil ik werken?", 8, 24, null, new DateOnly(2026, 10, 5), null, null);
        Assert.Equal("Je wilt 8 tot 24 uur per week werken.", hours);
        var ready = CandidateProfileFacts.TryReply("nl", "Wanneer ben ik beschikbaar?", 8, 24, null, new DateOnly(2026, 10, 5), null, null);
        Assert.Equal("Je bent per direct beschikbaar.", ready);
        var values = CandidateProfileFacts.TryReply("nl", "Wat vind ik belangrijk?", null, null, null, new DateOnly(2026, 10, 5), "Prestatie & groei", null);
        Assert.Equal("Het belangrijkst voor jou is Prestatie & groei.", values);
        var culture = CandidateProfileFacts.TryReply("nl", "Welke werksfeer past bij mij?", null, null, null, new DateOnly(2026, 10, 5), null, "Energie van mensen");
        Assert.Equal("De werksfeer die bij je past is Energie van mensen.", culture);
        var mail = CandidateProfileFacts.TryReply("nl", "Hoe schrijf ik een korte mail om te vragen of ik mag meelopen?", null, null, null, new DateOnly(2026, 10, 5), null, null);
        Assert.Contains("Meelopen", mail, StringComparison.Ordinal);
        Assert.DoesNotContain("niet in je profiel", mail, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Onderzoek horecaberoepen.", CandidateCoachPolish.Apply("onderzoek horecaberoven."));
    }

    [Fact]
    public void Compact_competence_pdf_uses_one_judgement_and_hides_an_unknown_percent()
    {
        var report = new CompetenceDeepReport
        {
            Summary = "Kort.",
            Traits =
            [
                new CompetenceDeepTraitReport
                {
                    Domain = "Openheid",
                    LabelNl = "Nieuwe dingen proberen",
                    Score = 43,
                    Level = "Gemiddeld",
                    NormBand = "Lager dan de meeste mensen",
                    Meaning = "Je probeert nieuwe dingen.",
                    WorkQuote = "Op het werk.",
                    Pitfall = "Te veel tegelijk.",
                    Tip = "Kies er één.",
                    Strength = "Nieuwsgierig."
                }
            ],
            Occupations =
            [
                new CompetenceDeepOccupation { Title = "Kok", MatchPercent = 70, Reason = "Oud." },
                new CompetenceDeepOccupation { Title = "Bloemist / groenpresentatie", MatchPercent = 69, Reason = "Oud." }
            ]
        };
        QuestPDF.Settings.License = LicenseType.Community;
        var pdf = CompactDeepReportPdf.Competence("Lobsy", [], "Test", "4 okt 2026", report, "nl");
        using var doc = PdfDocument.Open(pdf);
        var text = string.Join("\n", doc.GetPages().Select(page => page.Text));
        Assert.Contains("Lager dan de meeste mensen", text, StringComparison.Ordinal);
        Assert.DoesNotContain("43/100 · Gemiddeld", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Kok —", text, StringComparison.Ordinal);
        Assert.Contains("Kok", text, StringComparison.Ordinal);
        Assert.DoesNotContain("69%", text, StringComparison.Ordinal);
    }
}
