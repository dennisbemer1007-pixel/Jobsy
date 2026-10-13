using Jobsy.Core.Enums;
using Jobsy.Core.Reports;
using Jobsy.Core.Reports.Career;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Services;
using UglyToad.PdfPig;

namespace Jobsy.Tests;

public class CareerDeepReportCopyTests
{
    [Fact]
    public void Riasec_labels_accept_letter_and_full_name()
    {
        Assert.Equal(DeepReportCatalog.RiasecLabel("R", "nl"), DeepReportCatalog.RiasecLabel("Realistic", "nl"));
        Assert.Equal(DeepReportCatalog.RiasecLabel("R", "en"), DeepReportCatalog.RiasecLabel("REALISTIC", "en"));
        Assert.Equal("Hands-on work", DeepReportCatalog.RiasecLabel("REALISTIC", "en"));
        Assert.Equal("Uitzoeken hoe het zit", DeepReportCatalog.RiasecLabel("Investigative", "nl"));
        Assert.DoesNotContain("riasec.", DeepReportCatalog.RiasecLabel("CONVENTIONAL", "nl"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Missing_catalog_key_is_a_plain_label_not_the_raw_key()
    {
        var shown = DeepReportCatalog.Get("riasec.NOT_A_CODE", "nl");
        Assert.DoesNotContain("riasec.", shown, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual("riasec.NOT_A_CODE", shown);
        Assert.False(string.IsNullOrWhiteSpace(shown));
    }

    [Fact]
    public void Uitgebreid_career_page_and_pdf_hide_raw_keys_and_vary_the_action_plan()
    {
        var domains = SyntheticDomains();
        var scores = DeepAnalysisCatalog.ToRiasecScores(domains);
        var report = CareerDeepReportBuilder.Build(
            domains,
            CareerCompassBuilder.Build(scores, true),
            null,
            new DateTime(2026, 10, 4, 12, 0, 0, DateTimeKind.Utc));

        var page = PageText(report, "nl") + "\n" + PageText(report, "en");
        AssertNoRawMarkers(page);

        var bodies = report.ActionPlan.Select(s => s.Body.Resolve("nl")).ToList();
        Assert.Equal(3, bodies.Count);
        Assert.Equal(3, bodies.Distinct(StringComparer.Ordinal).Count());
        Assert.Contains(bodies, b => b.Contains("beroepen die bij je passen", StringComparison.Ordinal));
        Assert.DoesNotContain(page, "topmatches", StringComparison.OrdinalIgnoreCase);

        var pdf = AssessmentReportPdfService.RenderCareerDeep(
            "Lobsy", [], "Test Kandidaat", "4 okt 2026", report, "nl");
        using var doc = PdfDocument.Open(pdf);
        var pdfText = string.Join('\n', doc.GetPages().Select(p => p.Text));
        AssertNoRawMarkers(pdfText);
        Assert.DoesNotContain(pdfText, "topmatches", StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertNoRawMarkers(string text)
    {
        Assert.DoesNotContain("riasec.", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("«", text, StringComparison.Ordinal);
        Assert.DoesNotContain("»", text, StringComparison.Ordinal);
    }

    private static string PageText(CareerDeepReport report, string lang)
    {
        var top = string.Join(", ", report.Domains.OrderByDescending(d => d.Score).Take(3)
            .Select(d => DeepReportCatalog.RiasecLabel(d.Domain, lang)));
        var places = string.Join(", ", report.Occupations.Take(3).Select(o => o.Title(lang)));
        var lines = new List<string>
        {
            report.Summary.Resolve(lang),
            DeepReportCatalog.Format("holland.body", lang, report.HollandCode, top, places)
        };
        lines.AddRange(report.Domains.Select(d => $"{DeepReportCatalog.RiasecLabel(d.Domain, lang)}: {d.Score}%"));
        foreach (var step in report.ActionPlan)
        {
            lines.Add(step.Title.Resolve(lang));
            lines.Add(step.Body.Resolve(lang));
        }

        foreach (var key in report.StrengthKeys.Concat(report.PitfallKeys))
        {
            lines.Add(DeepReportCatalog.TryGet(key, lang, out var text)
                ? text
                : DeepReportCatalog.RiasecLabel(key.Split('.').Last(), lang));
        }

        return string.Join('\n', lines);
    }

    private static List<DeepAnalysisDomainScore> SyntheticDomains()
    {
        var answers = new Dictionary<int, int>();
        var i = 0;
        foreach (var q in DeepAnalysisCatalog.QuestionsFor(AssessmentKind.Career))
        {
            answers[q.Id] = 2 + (i % 4);
            i++;
        }

        return DeepAnalysisCatalog.ScoreDomains(answers, AssessmentKind.Career).ToList();
    }
}
