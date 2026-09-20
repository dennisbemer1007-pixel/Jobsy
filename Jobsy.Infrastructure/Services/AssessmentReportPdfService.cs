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
    private static readonly Color BrandDeep = Color.FromHex("#0a2044");
    private static readonly Color SoftSky = Color.FromHex("#e8f3fa");
    private static readonly Color SoftMint = Color.FromHex("#e7f6ef");
    private static readonly Color WarmSand = Color.FromHex("#f6f0e7");
    private static readonly Color AccentTeal = Color.FromHex("#1a7a6d");
    private static readonly Color AccentCoral = Color.FromHex("#c45c3e");
    private static readonly Color Slate = Color.FromHex("#2c3a4a");
    private static readonly Color Muted = Color.FromHex("#5a6a7a");
    private static readonly Color Line = Color.FromHex("#d5e3ec");

    private readonly JobsyDbContext _db;
    private readonly IPlatformCompanySettingsService _companySettings;
    private readonly ICareerCompassGenerationService _careerCompass;

    static AssessmentReportPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public AssessmentReportPdfService(
        JobsyDbContext db,
        IPlatformCompanySettingsService companySettings,
        ICareerCompassGenerationService careerCompass)
    {
        _db = db;
        _companySettings = companySettings;
        _careerCompass = careerCompass;
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
        var logo = _companySettings.GetBrandLogoPng();
        var culture = CultureInfo.GetCultureInfo("nl-NL");
        var generated = (deep.ReportGeneratedAtUtc ?? deep.CompletedAtUtc ?? DateTime.UtcNow)
            .ToLocalTime()
            .ToString("d MMMM yyyy", culture);

        var answers = DeepAnalysisCatalog.ParseAnswersJson(deep.AnswersJson);
        var domainScores = DeepAnalysisCatalog.ScoreDomains(answers, kind);

        byte[] bytes;
        if (kind == AssessmentKind.Career)
        {
            var careerRow = await _db.CandidateCareerInterests
                .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
            var compass = CareerCompassJson.TryDeserialize(careerRow?.CompassJson);
            if (compass is not { HasOccupations: true })
            {
                compass = await _careerCompass.GenerateFromCareerDeepAsync(answers, cancellationToken);
                if (careerRow is not null)
                {
                    careerRow.CompassJson = CareerCompassJson.Serialize(compass);
                    careerRow.UpdatedAtUtc = DateTime.UtcNow;
                    await _db.SaveChangesAsync(cancellationToken);
                }
            }

            bytes = RenderCareer(brand, logo, user.FullName, generated, compass);
        }
        else
        {
            var scoreLines = domainScores
                .Select(s => $"{LabelCompetence(s.Domain)}: {s.Percent}%")
                .ToList();
            if (scoreLines.Count == 0)
            {
                scoreLines = await CompetenceLinesAsync(userId, cancellationToken);
            }

            var tags = CompetencyTestCatalog.ParseTagsJson(deep.TagsJson)
                .Select(FriendlyTag)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList();
            var advice = DeepAnalysisCatalog.CareerAdviceParagraphs(domainScores);
            bytes = RenderCompetence(brand, logo, user.FullName, generated, scoreLines, tags, advice);
        }

        var slug = AssessmentKindLabels.ToSlug(kind);
        var date = DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        return new AssessmentReportPdf($"Lobsy-{slug}-rapport-{date}.pdf", bytes);
    }

    internal static byte[] RenderCareer(
        string brand,
        byte[] logo,
        string fullName,
        string generated,
        CareerCompassSnapshot compass)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(28);
                page.MarginVertical(24);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Slate));
                BrandHeader(page, brand, logo, "Jouw loopbaanrapport", fullName, generated, AccentTeal);

                page.Content().PaddingTop(14).Column(col =>
                {
                    col.Spacing(10);
                    col.Item().Text(
                            "Geen ingewikkelde testtaal: dit is een helder overzicht van werk dat bij jou past, op basis van 150 vragen.")
                        .FontColor(Muted).Italic();

                    if (compass.Strengths.Count > 0)
                    {
                        col.Item().Text("Jij bent het sterkst in").FontSize(13).Bold().FontColor(BrandNavy);
                        col.Item().Text(string.Join(" · ", compass.Strengths));
                    }

                    WriteOccupationBand(col, CareerCompassBuilder.BandLabel(CareerCompassBuilder.BandSuper),
                        SoftMint, compass.SuperMatches, "Nog geen super-match boven 95%. Kijk bij sterke keus: daar zit vaak al iets dat heel dichtbij komt.");
                    WriteOccupationBand(col, CareerCompassBuilder.BandLabel(CareerCompassBuilder.BandStrong),
                        SoftSky, compass.StrongChoices, "Nog geen sterke keus boven 85%. De verbreding hieronder blijft de moeite waard.");
                    WriteOccupationBand(col, CareerCompassBuilder.BandLabel(CareerCompassBuilder.BandBroaden),
                        WarmSand, compass.Broadening, "Nog geen verbreding boven 75%. Zet je voorkeuren op de banenkaart en kijk welke taken je energie geven.");

                    col.Item().PaddingTop(8).Background(SoftSky).Padding(12).Column(box =>
                    {
                        box.Spacing(6);
                        box.Item().Text("Wat betekent dit voor jou?").FontSize(13).Bold().FontColor(BrandNavy);
                        foreach (var note in compass.PracticalNotes)
                        {
                            box.Item().Text(note);
                        }
                    });

                    col.Item().PaddingTop(6).Text(
                            "Dit rapport is geen medische of klinische diagnose. Je antwoorden blijven in jouw account. Werkgevers zien ze niet.")
                        .FontColor(Muted).Italic().FontSize(9);
                });

                BrandFooter(page, brand);
            });
        }).GeneratePdf();
    }

    private static byte[] RenderCompetence(
        string brand,
        byte[] logo,
        string fullName,
        string generated,
        IReadOnlyList<string> scoreLines,
        IReadOnlyList<string> tags,
        IReadOnlyList<string> advice)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(28);
                page.MarginVertical(24);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Slate));
                BrandHeader(page, brand, logo, "Jouw competentie-rapport", fullName, generated, AccentCoral);

                page.Content().PaddingTop(14).Column(col =>
                {
                    col.Spacing(10);
                    col.Item().Text(
                            "Dit rapport vat je 150 antwoorden samen: hoe jij samenwerkt, afrondt, onder druk blijft en nieuwe dingen oppakt.")
                        .FontColor(Muted).Italic();

                    if (scoreLines.Count > 0)
                    {
                        col.Item().Text("Jouw scores").FontSize(13).Bold().FontColor(BrandNavy);
                        foreach (var line in scoreLines)
                        {
                            col.Item().Text(line);
                        }
                    }

                    if (tags.Count > 0)
                    {
                        col.Item().PaddingTop(4).Text("Wat we meenemen in matching").FontSize(13).Bold().FontColor(BrandNavy);
                        col.Item().Text(string.Join(" · ", tags));
                    }

                    col.Item().PaddingTop(8).Text("Toelichting").FontSize(13).Bold().FontColor(BrandNavy);
                    foreach (var paragraph in advice)
                    {
                        col.Item().Text(paragraph);
                    }

                    col.Item().PaddingTop(8).Text(
                            "Dit rapport is geen medische of klinische diagnose. Ruwe antwoorden blijven in jouw account.")
                        .FontColor(Muted).Italic().FontSize(9);
                });

                BrandFooter(page, brand);
            });
        }).GeneratePdf();
    }

    private static void BrandHeader(
        PageDescriptor page,
        string brand,
        byte[] logo,
        string title,
        string fullName,
        string generated,
        Color accent)
    {
        page.Header().Column(header =>
        {
            header.Item().Background(SoftSky).Padding(14).Row(row =>
            {
                if (logo is { Length: > 0 })
                {
                    row.ConstantItem(48).Height(32).Image(logo).FitArea();
                    row.ConstantItem(10);
                }

                row.RelativeItem().AlignMiddle().Column(titleCol =>
                {
                    titleCol.Item().Text(brand).FontSize(18).Bold().FontColor(BrandNavy);
                    titleCol.Item().Text(title).FontSize(11).FontColor(accent);
                });

                row.ConstantItem(128).AlignMiddle().AlignRight().Background(Colors.White)
                    .PaddingHorizontal(8).PaddingVertical(6).Column(meta =>
                    {
                        meta.Item().Text("Vertrouwelijk").FontSize(9).Bold().FontColor(AccentCoral);
                        meta.Item().Text(fullName).FontSize(8).FontColor(Slate);
                        meta.Item().Text(generated).FontSize(8).FontColor(Muted);
                    });
            });
            header.Item().Height(3).Background(accent);
        });
    }

    private static void BrandFooter(PageDescriptor page, string brand)
    {
        page.Footer().Row(row =>
        {
            row.RelativeItem().Text($"{brand} · persoonlijk rapport").FontColor(Muted).FontSize(8);
            row.ConstantItem(90).AlignRight().Text(x =>
            {
                x.Span("Pagina ").FontColor(Muted).FontSize(8);
                x.CurrentPageNumber().FontColor(Muted).FontSize(8);
                x.Span(" / ").FontColor(Muted).FontSize(8);
                x.TotalPages().FontColor(Muted).FontSize(8);
            });
        });
    }

    private static void WriteOccupationBand(
        ColumnDescriptor col,
        string heading,
        Color background,
        IReadOnlyList<CareerOccupationMatch> items,
        string empty)
    {
        col.Item().Background(background).Padding(10).Column(box =>
        {
            box.Spacing(4);
            box.Item().Text(heading).FontSize(12).Bold().FontColor(BrandNavy);
            if (items.Count == 0)
            {
                box.Item().Text(empty).FontColor(Muted);
                return;
            }

            foreach (var item in items.Take(8))
            {
                box.Item().Row(r =>
                {
                    r.RelativeItem().Text($"{item.Title}").SemiBold();
                    r.ConstantItem(42).AlignRight().Text($"{item.Percent}%").FontColor(BrandDeep).Bold();
                });
                box.Item().Text(item.Why).FontSize(9).FontColor(Muted);
            }
        });
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

    private static string LabelCompetence(string domain) => domain switch
    {
        "Openheid" => "Openheid voor nieuwe dingen",
        "Consciëntieusheid" => "Afronden en betrouwbaar zijn",
        "Extraversie" => "Energie van mensen",
        "Vriendelijkheid" => "Aardig en meewerkend",
        "EmotioneleStabiliteit" => "Kalm onder druk",
        _ => domain
    };

    private static string FriendlyTag(string tag)
    {
        if (CareerTestCatalog.RiasecCodes.Contains(tag, StringComparer.OrdinalIgnoreCase))
        {
            return CareerCompassBuilder.TypeLabel(tag);
        }

        return tag;
    }
}
