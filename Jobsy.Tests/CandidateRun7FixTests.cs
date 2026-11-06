using Jobsy.Core.Admin;
using Jobsy.Core.Enums;
using Jobsy.Core.Reports;
using Jobsy.Core.Reports.Career;
using Jobsy.Core.Reports.Culture;
using Jobsy.Core.Reports.Values;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Services;
using Jobsy.Web.Components.Candidate;
using Jobsy.Web.Components.Candidate.Discovery;
using Jobsy.Web.Models;
using Jobsy.Web.Services;

namespace Jobsy.Tests;

public class CandidateRun7FixTests
{
    [Theory]
    [InlineData(AssessmentKind.Career, 8)]
    [InlineData(AssessmentKind.Values, 7)]
    [InlineData(AssessmentKind.Culture, 8)]
    public void Deep_pdf_page_count_matches_the_offer(AssessmentKind kind, int pages)
    {
        Assert.Equal(pages, DeepReportCapabilities.For(kind).PdfPageCount);
        var bytes = Render(kind);
        Assert.Equal(pages, PdfPageCounter.Count(bytes));
    }

    [Fact]
    public void Values_choose_lines_are_specific()
    {
        var report = ValuesDeepReportBuilder.Build(Domains(AssessmentKind.Values), null, DateTime.UtcNow);
        var bodies = report.ActionPlan.Select(s => s.Body.Resolve("nl")).ToList();
        Assert.Contains(bodies, b => b.Contains("zelf", StringComparison.OrdinalIgnoreCase)
            || b.Contains("resultaat", StringComparison.OrdinalIgnoreCase)
            || b.Contains("samen", StringComparison.OrdinalIgnoreCase)
            || b.Contains("ritme", StringComparison.OrdinalIgnoreCase)
            || b.Contains("anderen", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(bodies, b => b.Contains("Bij het kiezen van werk: let op organisaties", StringComparison.Ordinal));
    }

    [Fact]
    public void Teamleider_goal_still_matches_when_compass_occupations_are_empty()
    {
        var hits = CareerGoalFit.Pick(
            CareerGoalFit.DreamTitles("Teamleider logistiek"),
            "Teamleider logistiek",
            "orderpicker",
            "mbo");
        Assert.Contains(hits, h => h.Title.Contains("Teamleider", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Finished_journey_revisit_shows_ten_layers_off()
    {
        Assert.True(JourneyChrome.ShowFinished(journeyFinished: true, isLightScreen: false));
        Assert.Equal(10, JourneyChrome.LayersOff(showFinished: true, screen: "6", railStep: 6, shedStep: 5));
        Assert.Equal(5, JourneyChrome.LayersOff(showFinished: false, screen: "6", railStep: 6, shedStep: 5));
    }

    [Fact]
    public void Sentence_case_and_passport_skill_is_not_listed_again()
    {
        Assert.Equal("Basiskennis logistiek", CareerPlanViewBuilder.SentenceCaseTitle("Basiskennis Logistiek"));
        Assert.Equal("Ervaring in de logistiek", CareerPlanViewBuilder.SentenceCaseTitle("Ervaring in logistiek"));

        var plan = new CareerPathPlanApiModel
        {
            Steps =
            [
                new CareerPathStepApiModel
                {
                    Id = "a",
                    Order = 1,
                    Title = "Leidinggevende vaardigheden",
                    Status = "Completed",
                    SkillsGap = ["Leidinggevende vaardigheden"]
                },
                new CareerPathStepApiModel
                {
                    Id = "b",
                    Order = 2,
                    Title = "Teamleider",
                    Status = "Active",
                    SkillsGap = ["Leidinggevende vaardigheden", "Een certificaat"]
                }
            ]
        };

        var detail = CareerPlanViewBuilder.BuildStep(plan, 2);
        Assert.NotNull(detail);
        Assert.DoesNotContain(detail!.Missing, line => line.Text.Contains("Leidinggevende", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(detail.Missing, line => line.Text.Contains("certificaat", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Test_account_copy_hides_price_and_mollie()
    {
        var text = TestAccountCopy.StripPrice("De uitgebreide test (200 vragen, € 2,99) via Mollie.");
        Assert.DoesNotContain("2,99", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Mollie", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("uitgebreide test", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Stored_story_drops_the_employer_line_when_employers_are_off()
    {
        const string story = "Wat mij drijft is rust. Dat zoek ik terug in cultuur en beloftes van een werkgever.";
        var shown = WhoAmIStoryBuilder.ForDisplay(story, employersEnabled: false);
        Assert.DoesNotContain("werkgever", shown, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("werkdag", shown, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Test_unlock_reset_audit_key_is_stable()
        => Assert.Equal("user.test-unlock.reset", AdminAuditKeys.UserTestUnlockReset);

    private static byte[] Render(AssessmentKind kind) => kind switch
    {
        AssessmentKind.Values => AssessmentReportPdfService.RenderValuesDeep(
            "Lobsy", [], "Test", "4 okt 2026",
            ValuesDeepReportBuilder.Build(Domains(kind), null, DateTime.UtcNow), "nl", employersOn: false),
        AssessmentKind.Culture => AssessmentReportPdfService.RenderCultureDeep(
            "Lobsy", [], "Test", "4 okt 2026",
            CultureDeepReportBuilder.Build(Domains(kind), null, DateTime.UtcNow), "nl", employersOn: false),
        _ => AssessmentReportPdfService.RenderCareerDeep(
            "Lobsy", [], "Test", "4 okt 2026",
            CareerDeepReportBuilder.Build(
                Domains(AssessmentKind.Career),
                CareerCompassBuilder.Build(DeepAnalysisCatalog.ToRiasecScores(Domains(AssessmentKind.Career)), true),
                null,
                DateTime.UtcNow),
            "nl")
    };

    private static List<DeepAnalysisDomainScore> Domains(AssessmentKind kind)
    {
        var codes = kind switch
        {
            AssessmentKind.Career => CareerTestCatalog.RiasecCodes,
            AssessmentKind.Culture => CulturePersonalityCatalog.CategoryCodes,
            _ => SchwartzValuesCatalog.CategoryCodes
        };
        var i = 0;
        return codes.Select(code => new DeepAnalysisDomainScore(code, 40 + (i++ * 7 % 50), 4)).ToList();
    }
}
