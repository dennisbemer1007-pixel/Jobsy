using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Reports;
using Jobsy.Core.Reports.Career;
using Jobsy.Core.Reports.Culture;
using Jobsy.Core.Reports.Values;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Jobsy.Tests;

public class TestResultatenPartBTests
{
    [Fact]
    public void Values_catalog_title_is_waardenrapport_not_competence()
    {
        Assert.Equal("Jouw waardenrapport", DeepReportCatalog.Get("title.values", "nl"));
        Assert.Equal("Your work values report", DeepReportCatalog.Get("title.values", "en"));
        Assert.DoesNotContain("competentie", DeepReportCatalog.Get("title.values", "nl"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Values_and_culture_builders_avoid_career_advice_default()
    {
        var culture = CultureDeepReportBuilder.Build(SyntheticDomains(AssessmentKind.Culture), null, DateTime.UtcNow);
        var values = ValuesDeepReportBuilder.Build(SyntheticDomains(AssessmentKind.Values), null, DateTime.UtcNow);

        Assert.DoesNotContain("Werk dat bij je past", culture.Summary.Resolve("nl"), StringComparison.Ordinal);
        Assert.DoesNotContain("Werk dat bij je past", values.Summary.Resolve("nl"), StringComparison.Ordinal);
        Assert.DoesNotContain("Werk dat bij je past", culture.Summary.Resolve("en"), StringComparison.Ordinal);
        Assert.All(culture.ActionPlan, s =>
        {
            Assert.DoesNotContain("Werk dat bij je past", s.Title.Resolve("nl"), StringComparison.Ordinal);
            Assert.DoesNotContain("Werk dat bij je past", s.Body.Resolve("nl"), StringComparison.Ordinal);
        });
    }

    [Fact]
    public void English_value_labels_are_polished_not_raw_codes()
    {
        Assert.Equal("Setting your own direction", DeepReportCatalog.ValueLabel("Autonomy", "en"));
        Assert.Equal("Belonging and connection", DeepReportCatalog.ValueLabel("Connection", "en"));
        Assert.Equal("Hands-on work", DeepReportCatalog.RiasecLabel("R", "en"));
        Assert.Equal("Organising and order", DeepReportCatalog.RiasecLabel("C", "en"));
        Assert.Equal("Calm under pressure", DeepReportCatalog.CultureLabel("EmotionalStability", "en"));
    }

    [Fact]
    public async Task Sample_pdf_renders_without_paid_row_and_localizes_filename()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new SampleAssessmentReportPdfService(cache, new FakeCompanySettings());

        var nl = await sut.RenderSampleAsync(AssessmentKind.Values, "nl");
        var en = await sut.RenderSampleAsync(AssessmentKind.Values, "en");
        Assert.NotEmpty(nl.Content);
        Assert.NotEmpty(en.Content);
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(nl.Content.AsSpan(0, 4)));
        Assert.Contains("voorbeeld", nl.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("report", en.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(DeepReportCatalog.Get("pdf.watermark", "nl"), "VOORBEELD");
        Assert.Equal(DeepReportCatalog.Get("pdf.watermark", "en"), "SAMPLE");
    }

    [Fact]
    public async Task Sample_preview_returns_two_png_pages_and_total_count()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new SampleAssessmentReportPdfService(cache, new FakeCompanySettings());

        var preview = await sut.RenderSamplePreviewAsync(AssessmentKind.Career, "nl");
        Assert.Equal(2, preview.PagePngBase64.Count);
        Assert.Equal(DeepReportCapabilities.For(AssessmentKind.Career).PdfPageCount, preview.TotalPages);
        Assert.All(preview.PagePngBase64, b64 =>
        {
            var bytes = Convert.FromBase64String(b64);
            Assert.True(bytes.Length > 8);
            Assert.Equal(0x89, bytes[0]); // PNG magic
            Assert.Equal((byte)'P', bytes[1]);
            Assert.Equal((byte)'N', bytes[2]);
            Assert.Equal((byte)'G', bytes[3]);
        });
    }

    [Fact]
    public void Deep_pdf_renderers_produce_non_empty_pdf_bytes()
    {
        var values = ValuesDeepReportBuilder.Build(SyntheticDomains(AssessmentKind.Values), null, DateTime.UtcNow);
        var culture = CultureDeepReportBuilder.Build(SyntheticDomains(AssessmentKind.Culture), null, DateTime.UtcNow);
        var career = CareerDeepReportBuilder.Build(
            SyntheticDomains(AssessmentKind.Career),
            CareerCompassBuilder.Build(DeepAnalysisCatalog.ToRiasecScores(SyntheticDomains(AssessmentKind.Career)), true),
            null,
            DateTime.UtcNow);

        var v = AssessmentReportPdfService.RenderValuesDeep("Lobsy", [], "Test", "1 jan 2026", values, "nl");
        var c = AssessmentReportPdfService.RenderCultureDeep("Lobsy", [], "Test", "1 jan 2026", culture, "nl");
        var r = AssessmentReportPdfService.RenderCareerDeep("Lobsy", [], "Test", "1 jan 2026", career, "en");
        Assert.All(new[] { v, c, r }, b =>
        {
            Assert.NotEmpty(b);
            Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(b.AsSpan(0, 4)));
        });
    }

    [Fact]
    public void Norms_comparison_flag_false_when_means_null()
    {
        var career = CareerDeepReportBuilder.Build(
            SyntheticDomains(AssessmentKind.Career),
            CareerCompassSnapshot.Empty(true),
            normMeans: null,
            DateTime.UtcNow);
        Assert.False(career.ComparisonAvailable);

        var means = CareerTestCatalog.RiasecCodes.ToDictionary(c => c, _ => 50.0, StringComparer.OrdinalIgnoreCase);
        var withNorms = CareerDeepReportBuilder.Build(
            SyntheticDomains(AssessmentKind.Career),
            CareerCompassSnapshot.Empty(true),
            means,
            DateTime.UtcNow);
        Assert.True(withNorms.ComparisonAvailable);
    }

    [Fact]
    public void Report_language_maps_ui_to_nl_or_en()
    {
        Assert.Equal("en", ReportLanguage.FromUi("en"));
        Assert.Equal("nl", ReportLanguage.FromUi("pl"));
        Assert.Equal("nl", ReportLanguage.FromUi("ar"));
    }

    [Fact]
    public void Capabilities_cards_have_matching_builder_sections()
    {
        foreach (var kind in new[] { AssessmentKind.Career, AssessmentKind.Culture, AssessmentKind.Values })
        {
            var keys = DeepReportCapabilities.CardKeys(kind);
            Assert.Contains(DeepReportCardKey.RadarVsNorm, keys);
            Assert.Contains(DeepReportCardKey.ActionPlan, keys);
            Assert.Contains(DeepReportCardKey.StrengthsPitfalls, keys);
        }

        Assert.Contains(DeepReportCardKey.HollandCode, DeepReportCapabilities.CardKeys(AssessmentKind.Career));
        Assert.Contains(DeepReportCardKey.ValuesRanking, DeepReportCapabilities.CardKeys(AssessmentKind.Values));
        Assert.Contains(DeepReportCardKey.Employers, DeepReportCapabilities.CardKeys(AssessmentKind.Culture));
    }

    [Fact]
    public void Values_domains_are_ranked_high_to_low()
    {
        var report = ValuesDeepReportBuilder.Build(SyntheticDomains(AssessmentKind.Values), null, DateTime.UtcNow);
        Assert.Equal(5, report.Domains.Count);
        for (var i = 1; i < report.Domains.Count; i++)
        {
            Assert.True(report.Domains[i - 1].Score >= report.Domains[i].Score);
        }
    }

    private static List<DeepAnalysisDomainScore> SyntheticDomains(AssessmentKind kind)
    {
        var answers = new Dictionary<int, int>();
        var i = 0;
        foreach (var q in DeepAnalysisCatalog.QuestionsFor(kind))
        {
            answers[q.Id] = 2 + (i % 4);
            i++;
        }

        return DeepAnalysisCatalog.ScoreDomains(answers, kind).ToList();
    }

    private sealed class FakeCompanySettings : IPlatformCompanySettingsService
    {
        public Task<PlatformCompanySnapshot> GetAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new PlatformCompanySnapshot(
                "Lobsy", "Test", null, null, null, null, null, null, null, null, null, null));

        public Task<PlatformCompanySnapshot> UpdateAsync(
            PlatformCompanyUpdate update,
            CancellationToken cancellationToken = default)
            => GetAsync(cancellationToken);

        public byte[] GetBrandLogoPng() => [];

        public byte[] GetBrandWatermarkPng() => [];
    }
}
