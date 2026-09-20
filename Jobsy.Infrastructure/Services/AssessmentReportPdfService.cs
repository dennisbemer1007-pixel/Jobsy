using System.Globalization;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Jobsy.Infrastructure.Services;

public sealed class AssessmentReportPdfService : IAssessmentReportPdfService
{
    private static readonly Color BrandNavy = Color.FromHex("#0f2d5c");
    private static readonly Color Slate = Color.FromHex("#2c3a4a");
    private static readonly Color Muted = Color.FromHex("#5a6a7a");
    private static readonly Color AccentTeal = Color.FromHex("#1a7a6d");

    private readonly JobsyDbContext _db;
    private readonly IPlatformCompanySettingsService _companySettings;

    static AssessmentReportPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public AssessmentReportPdfService(JobsyDbContext db, IPlatformCompanySettingsService companySettings)
    {
        _db = db;
        _companySettings = companySettings;
    }

    public async Task<AssessmentReportPdf?> TryRenderAsync(
        Guid userId,
        AssessmentKind kind,
        CancellationToken cancellationToken = default)
    {
        var deep = await _db.CandidateDeepAnalyses.AsNoTracking()
            .FirstOrDefaultAsync(
                d => d.UserId == userId && d.Kind == kind && d.Status == CandidateDeepAnalysisStatuses.Completed,
                cancellationToken);
        if (deep is null)
        {
            return null;
        }

        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var platform = await _companySettings.GetAsync(cancellationToken);
        var brand = string.IsNullOrWhiteSpace(platform.CompanyName) ? "Lobsy" : platform.CompanyName.Trim();
        var culture = CultureInfo.GetCultureInfo("nl-NL");
        var title = kind == AssessmentKind.Career
            ? "Uitgebreid loopbaanrapport"
            : "Uitgebreide competentie-analyse";
        var tags = CompetencyTestCatalog.ParseTagsJson(deep.TagsJson);
        var generated = (deep.ReportGeneratedAtUtc ?? deep.CompletedAtUtc ?? DateTime.UtcNow)
            .ToLocalTime()
            .ToString("d", culture);

        var answers = DeepAnalysisCatalog.ParseAnswersJson(deep.AnswersJson);
        var domainScores = DeepAnalysisCatalog.ScoreDomains(answers, kind);
        var scoreLines = domainScores
            .Select(s => $"{LabelDomain(s.Domain)}: {s.Percent}%")
            .ToList();
        if (scoreLines.Count == 0)
        {
            scoreLines = kind == AssessmentKind.Career
                ? await CareerLinesAsync(userId, cancellationToken)
                : await CompetenceLinesAsync(userId, cancellationToken);
        }

        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Slate));

                page.Header().Column(h =>
                {
                    h.Item().Text(brand).FontSize(11).FontColor(AccentTeal).SemiBold();
                    h.Item().Text(title).FontSize(20).FontColor(BrandNavy).Bold();
                    h.Item().Text($"Vertrouwelijk · {user.FullName} · {generated}")
                        .FontSize(9).FontColor(Muted);
                });

                page.Content().PaddingTop(16).Column(col =>
                {
                    col.Spacing(10);
                    col.Item().Text(
                            "Dit rapport is geen medische of klinische diagnose. Het vat je 150 unieke antwoorden samen voor matching en loopbaanoriëntatie.")
                        .FontColor(Muted).Italic();

                    if (scoreLines.Count > 0)
                    {
                        col.Item().Text("Scores uit de 150-vragen analyse").FontSize(13).Bold().FontColor(BrandNavy);
                        foreach (var line in scoreLines)
                        {
                            col.Item().Text(line);
                        }
                    }

                    if (tags.Count > 0)
                    {
                        col.Item().PaddingTop(8).Text("Verrijkte matchingtags").FontSize(13).Bold().FontColor(BrandNavy);
                        col.Item().Text(string.Join(" · ", tags));
                    }

                    var advice = DeepAnalysisCatalog.CareerAdviceParagraphs(domainScores);
                    col.Item().PaddingTop(12).Text(kind == AssessmentKind.Career
                            ? "Carrière-advies"
                            : "Toelichting")
                        .FontSize(13).Bold().FontColor(BrandNavy);
                    foreach (var paragraph in advice)
                    {
                        col.Item().Text(paragraph);
                    }

                    col.Item().PaddingTop(8).Text(
                            "Dit rapport is geen medische of klinische diagnose. Ruwe antwoorden blijven in jouw account.")
                        .FontColor(Muted).Italic();
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span("Pagina ").FontColor(Muted).FontSize(8);
                    x.CurrentPageNumber().FontColor(Muted).FontSize(8);
                    x.Span(" / ").FontColor(Muted).FontSize(8);
                    x.TotalPages().FontColor(Muted).FontSize(8);
                });
            });
        }).GeneratePdf();

        var slug = AssessmentKindLabels.ToSlug(kind);
        var date = DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        return new AssessmentReportPdf($"Lobsy-{slug}-rapport-{date}.pdf", bytes);
    }

    private async Task<List<string>> CompetenceLinesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var row = await _db.CandidateCompetencies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (row is null || !CandidateCompetencyStatuses.IsCompleted(row.Status))
        {
            return [];
        }

        return
        [
            $"Samenwerken: {row.SamenwerkenPercent}%",
            $"Resultaatgerichtheid: {row.ResultaatgerichtheidPercent}%",
            $"Stressbestendigheid: {row.StressbestendigheidPercent}%",
            $"Innovatie: {row.InnovatiePercent}%",
            $"Extraversie: {row.ExtraversiePercent}%"
        ];
    }

    private async Task<List<string>> CareerLinesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var row = await _db.CandidateCareerInterests.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (row is null || !CandidateCompetencyStatuses.IsCompleted(row.Status))
        {
            return [];
        }

        return
        [
            $"Holland-code: {row.HollandCode}",
            $"Realistic: {row.RealisticPercent}%",
            $"Investigative: {row.InvestigativePercent}%",
            $"Artistic: {row.ArtisticPercent}%",
            $"Social: {row.SocialPercent}%",
            $"Enterprising: {row.EnterprisingPercent}%",
            $"Conventional: {row.ConventionalPercent}%"
        ];
    }

    private static string LabelDomain(string domain) => domain switch
    {
        "Openheid" => "Openheid",
        "Consciëntieusheid" => "Consciëntieusheid",
        "Extraversie" => "Extraversie",
        "Vriendelijkheid" => "Vriendelijkheid",
        "EmotioneleStabiliteit" => "Emotionele stabiliteit",
        CareerTestCatalog.Realistic => "Realistic",
        CareerTestCatalog.Investigative => "Investigative",
        CareerTestCatalog.Artistic => "Artistic",
        CareerTestCatalog.Social => "Social",
        CareerTestCatalog.Enterprising => "Enterprising",
        CareerTestCatalog.Conventional => "Conventional",
        _ => domain
    };
}
