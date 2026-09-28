using System.Text.Json;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Reports;
using Jobsy.Core.Reports.Career;
using Jobsy.Core.Reports.Culture;
using Jobsy.Core.Reports.Values;
using Jobsy.Core.Rules;
using Microsoft.Extensions.Caching.Memory;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Jobsy.Infrastructure.Services;

public sealed class SampleAssessmentReportPdfService : ISampleAssessmentReportPdfService
{
    private static readonly Color BrandNavy = Color.FromHex("#0f2d5c");
    private static readonly Color AccentTeal = Color.FromHex("#1a7a6d");
    private static readonly Color SoftSky = Color.FromHex("#e8f3fa");
    private static readonly Color Slate = Color.FromHex("#2c3a4a");
    private static readonly Color Muted = Color.FromHex("#5a6a7a");
    private static readonly Color Watermark = Color.FromHex("#c9a227");

    private readonly IMemoryCache _cache;
    private readonly IPlatformCompanySettingsService _companySettings;

    static SampleAssessmentReportPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public SampleAssessmentReportPdfService(
        IMemoryCache cache,
        IPlatformCompanySettingsService companySettings)
    {
        _cache = cache;
        _companySettings = companySettings;
    }

    public Task<AssessmentReportPdf> RenderSampleAsync(
        AssessmentKind kind,
        string? lang,
        CancellationToken ct = default)
    {
        var reportLang = ReportLanguage.FromUi(lang);
        var version = kind switch
        {
            AssessmentKind.Career => CareerDeepReportJson.CurrentReportVersion,
            AssessmentKind.Culture => CultureDeepReportJson.CurrentReportVersion,
            AssessmentKind.Values => ValuesDeepReportJson.CurrentReportVersion,
            _ => 1
        };
        var cacheKey = $"sample-pdf:{kind}:{reportLang}:{version}";
        if (!_cache.TryGetValue(cacheKey, out byte[]? bytes) || bytes is null)
        {
            bytes = BuildSample(kind, reportLang);
            _cache.Set(cacheKey, bytes, TimeSpan.FromHours(12));
        }

        var slug = AssessmentKindLabels.ToSlug(kind);
        var name = DeepReportCatalog.FileName(new AssessmentKindSlug($"voorbeeld-{slug}"), reportLang, DateTime.UtcNow);
        return Task.FromResult(new AssessmentReportPdf(name, bytes));
    }

    private byte[] BuildSample(AssessmentKind kind, string lang)
    {
        var brand = "Lobsy";
        var logo = _companySettings.GetBrandLogoPng();
        var fullName = ReportLanguage.IsEnglish(lang) ? "Sample candidate" : "Voorbeeldkandidaat";
        var generated = DateTime.UtcNow.ToString("d MMMM yyyy",
            ReportLanguage.IsEnglish(lang)
                ? System.Globalization.CultureInfo.GetCultureInfo("en-GB")
                : System.Globalization.CultureInfo.GetCultureInfo("nl-NL"));
        var watermark = DeepReportCatalog.Get("pdf.watermark", lang);
        var coverNote = DeepReportCatalog.Get("pdf.sampleCover", lang);

        var answers = LoadSampleAnswers(kind);
        var domains = DeepAnalysisCatalog.ScoreDomains(answers, kind);

        byte[] inner = kind switch
        {
            AssessmentKind.Career => AssessmentReportPdfService.RenderCareerDeep(
                brand, logo, fullName, generated,
                CareerDeepReportBuilder.Build(domains, CareerCompassBuilder.Build(DeepAnalysisCatalog.ToRiasecScores(domains), true), null, DateTime.UtcNow),
                lang),
            AssessmentKind.Culture => AssessmentReportPdfService.RenderCultureDeep(
                brand, logo, fullName, generated,
                CultureDeepReportBuilder.Build(domains, null, DateTime.UtcNow),
                lang),
            AssessmentKind.Values => AssessmentReportPdfService.RenderValuesDeep(
                brand, logo, fullName, generated,
                ValuesDeepReportBuilder.Build(domains, null, DateTime.UtcNow),
                lang),
            _ => AssessmentReportPdfService.RenderCareerDeep(
                brand, logo, fullName, generated,
                CareerDeepReportBuilder.Build(domains, CareerCompassSnapshot.Empty(true), null, DateTime.UtcNow),
                lang)
        };

        // Re-render with watermark overlay (simpler: wrap a short watermarked 2-page sample).
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(11).FontColor(Slate));
                page.Header().Background(SoftSky).Padding(12).Row(r =>
                {
                    r.RelativeItem().Text(brand).Bold().FontColor(BrandNavy).FontSize(16);
                    r.ConstantItem(160).AlignRight().Text(coverNote).FontColor(AccentTeal).FontSize(9);
                });
                page.Content().Layers(layers =>
                {
                    layers.PrimaryLayer().PaddingTop(24).Column(col =>
                    {
                        col.Spacing(10);
                        col.Item().Text(DeepReportCatalog.Get(kind switch
                        {
                            AssessmentKind.Culture => "title.culture",
                            AssessmentKind.Values => "title.values",
                            AssessmentKind.Competence => "title.competence",
                            _ => "title.career"
                        }, lang)).FontSize(20).Bold().FontColor(BrandNavy);
                        col.Item().Text(fullName);
                        col.Item().Text(generated).FontColor(Muted);
                        col.Item().PaddingTop(12).Text(DeepReportCatalog.Get("pdf.scores", lang)).Bold();
                        foreach (var s in domains.OrderByDescending(d => d.Percent).Take(8))
                        {
                            var label = kind switch
                            {
                                AssessmentKind.Culture => DeepReportCatalog.CultureLabel(s.Domain, lang),
                                AssessmentKind.Values => DeepReportCatalog.ValueLabel(s.Domain, lang),
                                AssessmentKind.Career => DeepReportCatalog.RiasecLabel(s.Domain, lang),
                                _ => s.Domain
                            };
                            col.Item().Text($"{label}: {s.Percent}%");
                        }
                    });
                    layers.Layer().AlignCenter().AlignMiddle()
                        .Rotate(-28)
                        .Text(watermark)
                        .FontSize(64)
                        .FontColor(Watermark)
                        .Bold();
                });
                page.Footer().AlignCenter().Text(watermark).FontColor(Muted).FontSize(9);
            });
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.Content().Layers(layers =>
                {
                    layers.PrimaryLayer().Column(col =>
                    {
                        col.Item().Text(DeepReportCatalog.Get("pdf.overview", lang)).FontSize(14).Bold().FontColor(BrandNavy);
                        col.Item().PaddingTop(8).Text(DeepReportCatalog.Get("pdf.disclaimer", lang)).FontColor(Muted).Italic();
                        col.Item().PaddingTop(16).Text($"{domains.Count} · {inner.Length}").FontColor(Muted).FontSize(8);
                    });
                    layers.Layer().AlignCenter().AlignMiddle()
                        .Rotate(-28)
                        .Text(watermark)
                        .FontSize(64)
                        .FontColor(Watermark)
                        .Bold();
                });
                page.Footer().AlignCenter().Text(watermark).FontColor(Muted).FontSize(9);
            });
        }).GeneratePdf();
    }

    private static IReadOnlyDictionary<int, int> LoadSampleAnswers(AssessmentKind kind)
    {
        var name = kind switch
        {
            AssessmentKind.Culture => "culture-sample-answers.json",
            AssessmentKind.Values => "values-sample-answers.json",
            AssessmentKind.Competence => "competence-sample-answers.json",
            _ => "career-sample-answers.json"
        };

        var asm = typeof(DeepReportCatalog).Assembly;
        var resource = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(name, StringComparison.OrdinalIgnoreCase));
        if (resource is null)
        {
            return SyntheticAnswers(kind);
        }

        using var stream = asm.GetManifestResourceStream(resource)!;
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        var dict = JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? [];
        return dict.ToDictionary(kv => int.Parse(kv.Key), kv => kv.Value);
    }

    private static IReadOnlyDictionary<int, int> SyntheticAnswers(AssessmentKind kind)
    {
        var questions = DeepAnalysisCatalog.QuestionsFor(kind);
        var map = new Dictionary<int, int>();
        var i = 0;
        foreach (var q in questions)
        {
            map[q.Id] = 2 + (i % 4); // 2–5
            i++;
        }

        return map;
    }
}
