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
    public void Compact_section_headings_stay_with_their_body()
    {
        foreach (var kind in new[]
                 {
                     AssessmentKind.Career, AssessmentKind.Competence, AssessmentKind.Culture, AssessmentKind.Values
                 })
        {
            AssertNoOrphanSectionHeading(Render(kind, compact: true, uiLang: "nl"), kind.ToString());
        }

        AssertNoOrphanSectionHeading(Render(AssessmentKind.Career, compact: true, uiLang: "en"), "career-en");
        AssertNoOrphanSectionHeading(Render(AssessmentKind.Culture, compact: true, uiLang: "en"), "culture-en");

        var career = Lines(Render(AssessmentKind.Career, compact: true, uiLang: "nl"));
        var careerJobs = CareerReport().Occupations.Select(o => o.Title("nl")).ToList();
        AssertHeadingHasBodyOnSamePage(career, "Beroepen die bij je passen", careerJobs[0] + " —");
        AssertSectionStartsFreshWhenItContinues(
            career,
            "Beroepen die bij je passen",
            line => careerJobs.Any(title => line.StartsWith(title + " —", StringComparison.Ordinal))
                    || line.StartsWith("Het percentage is een gewogen gemiddelde", StringComparison.Ordinal));

        var culture = Lines(Render(AssessmentKind.Culture, compact: true, uiLang: "nl"));
        AssertHeadingHasBodyOnSamePage(
            culture,
            "Zo lees je je scores",
            "Een hoger percentage betekent dat die manier van werken");

        var cultureEn = Lines(Render(AssessmentKind.Culture, compact: true, uiLang: "en"));
        AssertHeadingHasBodyOnSamePage(
            cultureEn,
            "How to read your scores",
            "A higher percent means that way of working");

        var values = Lines(Render(AssessmentKind.Values, compact: true, uiLang: "nl"));
        AssertHeadingHasBodyOnSamePage(
            values,
            "Zo lees je dit, en wat daarna",
            "Een hoger percentage betekent dat die waarde");

        var competenceReport = CompetenceReport();
        competenceReport.Occupations.Clear();
        for (var i = 1; i <= CareerCompassSanitize.MaxCatalogueJobs; i++)
        {
            competenceReport.Occupations.Add(new CompetenceDeepOccupation
            {
                Title = $"Layoutberoep {i:00}",
                MatchPercent = 60,
                Reason = "Dit sluit aan bij je score en hoort bij dit beroep in het compacte rapport."
            });
        }

        var competence = Lines(AssessmentReportPdfService.RenderCompetenceDeep(
            "Lobsy", [], "Test Kandidaat", "5 oktober 2026", competenceReport, compact: true, uiLang: "nl"));
        AssertHeadingHasBodyOnSamePage(competence, "Beroepen die bij je passen", "Layoutberoep 01 —");
        AssertSectionStartsFreshWhenItContinues(
            competence,
            "Beroepen die bij je passen",
            line => line.StartsWith("Layoutberoep ", StringComparison.Ordinal));
        AssertNoOrphanSectionHeading(competence, "competence-jobs");
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

    /// <summary>
    /// Content that starts a fresh page sits just under the header (around y=760 on A4).
    /// A heading lower than this was started in a gap and must not continue on the next page.
    /// </summary>
    private const double FreshPageHeadingMinY = 640;

    private static readonly HashSet<string> SectionHeadings = new(StringComparer.Ordinal)
    {
        "Beroepen die bij je passen",
        "Jobs that fit you",
        "Zo lees je je scores",
        "How to read your scores",
        "Zo lees je dit, en wat daarna",
        "How to read this and what is next",
        "Wat dit betekent",
        "What this means",
        "Jouw beroepsletters",
        "Your job letters",
        "Jouw actieplan",
        "Your action plan",
        "Sterke punten & valkuilen",
        "Strengths & pitfalls",
        "Wat je hiermee kunt doen",
        "What you can do next",
        "Werk dat bij je past",
        "Over deze test",
        "Hoe jij graag werkt",
        "How you like to work",
        "Hoe jij in een team past",
        "How you show up in a team",
        "Jouw waarden op volgorde",
        "Your values ranked",
        "Wat dit voor je werk betekent",
        "What this means for your work",
        "Werkplekken die bij je passen",
        "Workplaces that fit you",
        "Werkplekken die bij deze waarden passen",
        "Workplaces that fit these values"
    };

    private sealed record PdfLine(int Page, double Y, string Text);

    private static void AssertNoOrphanSectionHeading(byte[] pdf, string label)
        => AssertNoOrphanSectionHeading(Lines(pdf), label);

    private static void AssertNoOrphanSectionHeading(IReadOnlyList<PdfLine> lines, string label)
    {
        foreach (var page in lines.GroupBy(l => l.Page))
        {
            var last = page.Where(l => !IsFooter(l.Text)).OrderBy(l => l.Y).FirstOrDefault();
            if (last is null)
            {
                continue;
            }

            var text = Compact(last.Text);
            Assert.False(SectionHeadings.Contains(text), $"{label} page {page.Key} ends on section heading '{text}'.");
        }
    }

    private static void AssertHeadingHasBodyOnSamePage(IReadOnlyList<PdfLine> lines, string heading, string bodyMarker)
    {
        var title = lines.Single(l => Compact(l.Text) == heading);
        var bodyOnSamePage = lines.Any(l =>
            l.Page == title.Page
            && l.Y < title.Y - 1
            && Compact(l.Text).Contains(bodyMarker, StringComparison.Ordinal));
        Assert.True(bodyOnSamePage, $"'{heading}' is split from '{bodyMarker}' (page {title.Page}, y {title.Y:0}).");
    }

    private static void AssertSectionStartsFreshWhenItContinues(
        IReadOnlyList<PdfLine> lines, string heading, Func<string, bool> continuesOnNextPage)
    {
        var title = lines.Single(l => Compact(l.Text) == heading);
        var spills = lines.Any(l => l.Page > title.Page && continuesOnNextPage(Compact(l.Text)));
        if (!spills)
        {
            return;
        }

        Assert.True(
            title.Y >= FreshPageHeadingMinY,
            $"'{heading}' continues on the next page but starts mid-page at y {title.Y:0}.");
    }

    private static bool IsFooter(string text)
        => text.Contains("persoonlijk rapport", StringComparison.Ordinal)
           || text.Contains("Pagina ", StringComparison.Ordinal);

    private static string Compact(string text)
        => string.Join(' ', text.Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static List<PdfLine> Lines(byte[] pdf)
    {
        using var doc = PdfDocument.Open(pdf);
        var lines = new List<PdfLine>();
        var pageNo = 1;
        foreach (var page in doc.GetPages())
        {
            foreach (var group in page.GetWords().GroupBy(w => Math.Round(w.BoundingBox.Bottom)))
            {
                var text = string.Join(' ', group.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text));
                lines.Add(new PdfLine(pageNo, group.Average(w => w.BoundingBox.Bottom), text));
            }

            pageNo++;
        }

        return lines;
    }

    private static byte[] Render(AssessmentKind kind, bool compact, string uiLang) => kind switch
    {
        AssessmentKind.Career => AssessmentReportPdfService.RenderCareerDeep(
            "Lobsy", [], "Test Kandidaat", "5 oktober 2026", CareerReport(),
            ReportLanguage.FromUi(uiLang), education: null, compact: compact, uiLang: uiLang),
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
