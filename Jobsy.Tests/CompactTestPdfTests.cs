using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Reports;
using Jobsy.Core.Reports.Career;
using Jobsy.Core.Reports.Competence;
using Jobsy.Core.Reports.Culture;
using Jobsy.Core.Reports.Values;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Services;
using Jobsy.Tests.Uat;
using Jobsy.Web.Admin;
using Jobsy.Web.Localization;
using UglyToad.PdfPig;

namespace Jobsy.Tests;

public class CompactTestPdfTests
{
    [Fact]
    public void Flag_defaults_off_and_admin_copy_exists_in_five_languages()
    {
        Assert.False(FeatureFlagSnapshot.Defaults.CompactTestPdfEnabled);
        Assert.False(new FeatureFlagSnapshot(false, true).IsEnabled(PlatformFeature.CompactTestPdf));
        Assert.True(new FeatureFlagSnapshot(false, true, CompactTestPdfEnabled: true)
            .IsEnabled(PlatformFeature.CompactTestPdf));

        var entry = PlatformSettingsCatalog.Entries.Single(e => e.Key == "CompactTestPdfEnabled");
        Assert.False(entry.Read(new PlatformFeatureSnapshot(false, true, "http://localhost", null)) is true);

        foreach (var lang in new[] { "nl", "en", "pl", "ro", "ar" })
        {
            var title = UiStrings.Get("AdminSettings.CompactTestPdf.Enabled.Title", lang);
            var desc = UiStrings.Get("AdminSettings.CompactTestPdf.Enabled.Desc", lang);
            Assert.False(string.IsNullOrWhiteSpace(title));
            Assert.False(string.IsNullOrWhiteSpace(desc));
            Assert.DoesNotContain("AdminSettings.", title, StringComparison.Ordinal);
        }

        Assert.Equal("Jouw profiel", CompactPdfCopy.Profile("nl"));
        Assert.Equal("Your profile", CompactPdfCopy.Profile("en"));
        Assert.Equal("Twój profil", CompactPdfCopy.Profile("pl"));
        Assert.Equal("Profilul tău", CompactPdfCopy.Profile("ro"));
        Assert.Equal("ملفك", CompactPdfCopy.Profile("ar"));
    }

    [Fact]
    public void Service_uses_the_flag_and_keeps_the_long_layout_as_default()
    {
        var source = File.ReadAllText(Path.Combine(
            RepoRoot.Find(), "Jobsy.Infrastructure/Services/AssessmentReportPdfService.cs"));
        Assert.Contains("PlatformFeature.CompactTestPdf", source, StringComparison.Ordinal);
        Assert.Contains("CompactDeepReportPdf.Career", source, StringComparison.Ordinal);
        Assert.Contains("CompactDeepReportPdf.Competence", source, StringComparison.Ordinal);
        Assert.Contains("CompactDeepReportPdf.Culture", source, StringComparison.Ordinal);
        Assert.Contains("CompactDeepReportPdf.Values", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(AssessmentKind.Career)]
    [InlineData(AssessmentKind.Competence)]
    [InlineData(AssessmentKind.Values)]
    [InlineData(AssessmentKind.Culture)]
    public void Compact_full_profile_is_short_visual_and_still_complete(AssessmentKind kind)
    {
        var compact = Render(kind, compact: true, uiLang: "nl");
        var classic = Render(kind, compact: false, uiLang: "nl");
        var compactPages = Pages(compact);
        var classicPages = Pages(classic);

        Assert.InRange(compactPages.Count, 1, 5);
        Assert.True(classicPages.Count > 5, $"{kind} classic layout should stay multi-page");
        Assert.True(compactPages.Count < classicPages.Count);

        var text = string.Join("\n", compactPages.Select(p => p.Text));
        Assert.Contains("Jouw profiel", text, StringComparison.Ordinal);
        Assert.Contains("Sterkste drie", text, StringComparison.Ordinal);
        Assert.Contains("actieplan", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("geen medische", text, StringComparison.OrdinalIgnoreCase);
        Assert.True(compactPages.Any(p => p.Paths.Count > 0), "chart bars should be drawn");

        if (kind == AssessmentKind.Career)
        {
            var report = CareerReport();
            var top = report.Domains.OrderByDescending(d => d.Score).First();
            Assert.Contains(DeepReportCatalog.RiasecLabel(top.Domain, "nl"), text, StringComparison.Ordinal);
            Assert.Contains(report.Occupations[0].Title("nl"), text, StringComparison.Ordinal);
            Assert.Contains(report.HollandCode, text, StringComparison.Ordinal);
        }

        if (kind == AssessmentKind.Competence)
        {
            var report = CompetenceReport();
            Assert.Contains(report.Traits[0].LabelNl, text, StringComparison.Ordinal);
            Assert.Contains("Dit is geen diagnose", text, StringComparison.Ordinal);
            Assert.Contains("ipip.ori.org", text, StringComparison.Ordinal);
            if (report.Occupations.Count > 0)
            {
                Assert.Contains(report.Occupations[0].Title, text, StringComparison.Ordinal);
            }
        }

        var dir = Path.Combine(Path.GetTempPath(), "compact-pdf-samples");
        Directory.CreateDirectory(dir);
        File.WriteAllBytes(Path.Combine(dir, $"{kind}-compact.pdf"), compact);
        File.WriteAllBytes(Path.Combine(dir, $"{kind}-classic.pdf"), classic);
    }

    [Fact]
    public void Compact_english_and_polish_chrome_follow_the_ui_language()
    {
        var en = string.Join("\n", Pages(Render(AssessmentKind.Career, compact: true, uiLang: "en")).Select(p => p.Text));
        Assert.Contains("Your profile", en, StringComparison.Ordinal);
        Assert.Contains("Strongest three", en, StringComparison.Ordinal);
        Assert.Contains("Hands-on work", en, StringComparison.OrdinalIgnoreCase);

        var pl = string.Join("\n", Pages(Render(AssessmentKind.Values, compact: true, uiLang: "pl")).Select(p => p.Text));
        Assert.Contains("Twój profil", pl, StringComparison.Ordinal);
        Assert.Contains("Trzy najsilniejsze", pl, StringComparison.Ordinal);
    }

    [Fact]
    public void Sparse_profile_is_not_padded_to_four_pages()
    {
        var sparse = new ValuesDeepReport
        {
            Summary = new LocalizedReportText { Nl = "Kort.", En = "Short." },
            Domains = [new DeepDomainScore { Domain = "Stability", Score = 20 }],
            GeneratedAtUtc = new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc)
        };
        var bytes = AssessmentReportPdfService.RenderValuesDeep(
            "Lobsy", [], "Test Kandidaat", "5 oktober 2026", sparse, "nl", employersOn: false, compact: true, uiLang: "nl");
        var count = Pages(bytes).Count;
        Assert.InRange(count, 1, 3);
    }

    private static List<UglyToad.PdfPig.Content.Page> Pages(byte[] pdf)
    {
        var doc = PdfDocument.Open(pdf);
        return doc.GetPages().ToList();
    }

    private static byte[] Render(AssessmentKind kind, bool compact, string uiLang) => kind switch
    {
        AssessmentKind.Career => AssessmentReportPdfService.RenderCareerDeep(
            "Lobsy", [], "Test Kandidaat", "5 oktober 2026", CareerReport(),
            ReportLanguage.FromUi(uiLang), compact, uiLang),
        AssessmentKind.Competence => AssessmentReportPdfService.RenderCompetenceDeep(
            "Lobsy", [], "Test Kandidaat", "5 oktober 2026", CompetenceReport(), compact, uiLang),
        AssessmentKind.Culture => AssessmentReportPdfService.RenderCultureDeep(
            "Lobsy", [], "Test Kandidaat", "5 oktober 2026",
            CultureDeepReportBuilder.Build(Domains(AssessmentKind.Culture), null, Stamp),
            ReportLanguage.FromUi(uiLang), employersOn: false, compact, uiLang),
        _ => AssessmentReportPdfService.RenderValuesDeep(
            "Lobsy", [], "Test Kandidaat", "5 oktober 2026",
            ValuesDeepReportBuilder.Build(Domains(AssessmentKind.Values), null, Stamp),
            ReportLanguage.FromUi(uiLang), employersOn: false, compact, uiLang)
    };

    private static readonly DateTime Stamp = new(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc);

    private static CareerDeepReport CareerReport()
        => CareerDeepReportBuilder.Build(
            Domains(AssessmentKind.Career),
            CareerCompassBuilder.Build(DeepAnalysisCatalog.ToRiasecScores(Domains(AssessmentKind.Career)), true),
            null,
            Stamp);

    private static CompetenceDeepReport CompetenceReport()
    {
        var answers = new Dictionary<int, int>();
        foreach (var question in DeepAnalysisCatalog.QuestionsFor(AssessmentKind.Competence))
        {
            answers[question.Id] = 2 + (question.Id % 4);
        }

        return CompetenceDeepReportBuilder.Build(
            answers,
            new Johnson2014NormProvider(),
            jobTitle: null,
            occupations: null,
            aiSummary: null,
            aiPlan: null,
            fromOpenAi: false,
            generatedAtUtc: Stamp);
    }

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
