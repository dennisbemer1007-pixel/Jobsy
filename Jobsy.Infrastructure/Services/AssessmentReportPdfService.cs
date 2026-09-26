using System.Globalization;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Reports.Competence;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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

    /// <summary>Gold accent used only on the uitgebreid (deep) competence report — cover badge, tips, bar fills.</summary>
    private static readonly Color Gold = Color.FromHex("#c9a227");
    private static readonly Color SoftGold = Color.FromHex("#f7ecd4");

    private static readonly TimeSpan DeepPdfCacheDuration = TimeSpan.FromHours(12);

    private readonly JobsyDbContext _db;
    private readonly IPlatformCompanySettingsService _companySettings;
    private readonly ICareerCompassGenerationService _careerCompass;
    private readonly IMemoryCache _cache;

    static AssessmentReportPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public AssessmentReportPdfService(
        JobsyDbContext db,
        IPlatformCompanySettingsService companySettings,
        ICareerCompassGenerationService careerCompass,
        IMemoryCache cache)
    {
        _db = db;
        _companySettings = companySettings;
        _careerCompass = careerCompass;
        _cache = cache;
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

        var answers = DeepAnalysisCatalog.ParseAnswersJson(deep.AnswersJson, kind);
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
        else if (kind == AssessmentKind.Culture)
        {
            var scoreLines = domainScores
                .Select(s => $"{LabelCulture(s.Domain)}: {s.Percent}%")
                .ToList();
            var tags = CulturePersonalityCatalog.ParseTags(deep.TagsJson)
                .Select(FriendlyTag)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToList();
            var advice = DeepAnalysisCatalog.CareerAdviceParagraphs(domainScores);
            bytes = RenderCulture(brand, logo, user.FullName, generated, scoreLines, tags, advice);
        }
        else
        {
            // The 9-page rich report is built only from a stored CompetenceDeepReport (no AI on
            // download) and cached by report version/generation time; legacy rows without a
            // stored report fall back to the older fixed-score summary below.
            var report = CompetenceDeepReportJson.Deserialize(deep.ReportJson);
            if (report is not null)
            {
                var cacheKey = $"deep-pdf:{userId}:{kind}:{report.ReportVersion}:{report.GeneratedAtUtc:O}";
                if (!_cache.TryGetValue(cacheKey, out byte[]? cached) || cached is null)
                {
                    cached = RenderCompetenceDeep(brand, logo, user.FullName, generated, report);
                    _cache.Set(cacheKey, cached, DeepPdfCacheDuration);
                }

                bytes = cached;
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
                            "Dit rapport is persoonlijk en positief: bovenaan staan de beroepen die het best bij jouw 200 antwoorden passen. Daarna volgen sterke alternatieven en ruimer werk om verder te kijken. Geen ingewikkelde testtaal.")
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
        => RenderScoreReport(
            brand,
            logo,
            fullName,
            generated,
            "Jouw competentie-rapport",
            "Dit rapport vat je 150 antwoorden samen: hoe jij samenwerkt, afrondt, onder druk blijft en nieuwe dingen oppakt.",
            scoreLines,
            tags,
            advice,
            AccentCoral);

    /// <summary>
    /// Rich 9-page competence deep-analysis report built entirely from a stored
    /// <see cref="CompetenceDeepReport"/> (no AI on download): cover, overview, one page per
    /// trait (exactly <see cref="CompetenceDeepReport.Traits"/> order), work fit, and action plan.
    /// </summary>
    internal static byte[] RenderCompetenceDeep(
        string brand,
        byte[] logo,
        string fullName,
        string generated,
        CompetenceDeepReport report)
    {
        return Document.Create(container =>
        {
            AddCompetenceDeepCoverPage(container, brand, logo, fullName, generated, report);
            AddCompetenceDeepOverviewPage(container, brand, fullName, report);
            foreach (var trait in report.Traits)
            {
                AddCompetenceDeepTraitPage(container, brand, fullName, trait);
            }

            AddCompetenceDeepWorkFitPage(container, brand, fullName, report);
            AddCompetenceDeepActionPlanPage(container, brand, fullName, report);
        }).GeneratePdf();
    }

    /// <summary>Page 1 — cover. Deliberately does not use <see cref="BrandHeader"/>/<see cref="BrandFooter"/>.</summary>
    private static void AddCompetenceDeepCoverPage(
        IDocumentContainer container, string brand, byte[] logo, string fullName, string generated,
        CompetenceDeepReport report)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(0);
            page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.White));
            page.PageColor(BrandDeep);

            page.Content().Padding(44).Column(col =>
            {
                col.Spacing(16);

                if (logo is { Length: > 0 })
                {
                    col.Item().Height(48).AlignLeft().Image(logo).FitArea();
                }

                col.Item().PaddingTop(18).AlignLeft().Background(Gold)
                    .PaddingHorizontal(12).PaddingVertical(6)
                    .Text("Uitgebreid rapport").FontSize(10).Bold().FontColor(Colors.White);

                col.Item().Text($"{brand} · Uitgebreid rapport – Competentietest")
                    .FontSize(24).Bold().FontColor(Colors.White);
                col.Item().Text("150 vragen · Big Five (IPIP)").FontSize(11).FontColor(Gold);

                col.Item().PaddingTop(18).Background(SoftGold).Padding(16).Column(box =>
                {
                    box.Spacing(4);
                    box.Item().Text(fullName).FontSize(16).Bold().FontColor(BrandDeep);
                    box.Item().Text(generated).FontSize(10).FontColor(Slate);
                });

                col.Item().PaddingTop(10).Text(report.Summary).FontSize(10).FontColor(SoftSky);

                if (!string.IsNullOrWhiteSpace(report.NormSourceLine))
                {
                    col.Item().PaddingTop(4).Text(report.NormSourceLine!).FontSize(8).FontColor(SoftSky).Italic();
                }
            });
        });
    }

    /// <summary>Page 2 — "Jij in het kort" summary, per-trait bars vs. the norm average, and band labels.</summary>
    private static void AddCompetenceDeepOverviewPage(
        IDocumentContainer container, string brand, string fullName, CompetenceDeepReport report)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.MarginHorizontal(28);
            page.MarginVertical(24);
            page.DefaultTextStyle(x => x.FontSize(10).FontColor(Slate));
            DeepReportHeader(page, brand, fullName);

            page.Content().PaddingTop(12).Column(col =>
            {
                col.Spacing(8);
                col.Item().Text("Jij in het kort").FontSize(16).Bold().FontColor(BrandNavy);
                col.Item().Text(report.Summary).FontSize(10);

                col.Item().PaddingTop(6).Text("Jouw vijf eigenschappen").FontSize(13).Bold().FontColor(BrandNavy);
                foreach (var trait in report.Traits)
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text(trait.LabelNl).SemiBold();
                        r.ConstantItem(110).AlignRight()
                            .Text($"{trait.Score}/100 · {trait.Level}").FontColor(BrandDeep).SemiBold();
                    });
                    col.Item().Element(e => ScoreBar(e, trait.Score, trait.NormMean, Gold));
                    if (!string.IsNullOrWhiteSpace(trait.NormBand))
                    {
                        col.Item().Text(trait.NormBand!).FontSize(8).FontColor(Muted).Italic();
                    }
                }

                if (!string.IsNullOrWhiteSpace(report.NormSourceLine))
                {
                    col.Item().PaddingTop(6).Background(SoftSky).Padding(8)
                        .Text(report.NormSourceLine!).FontSize(8).FontColor(Muted).Italic();
                }
            });

            DeepReportFooter(page);
        });
    }

    /// <summary>
    /// Pages 3–7 — one page per trait (exactly <see cref="CompetenceDeepReport.Traits"/> order):
    /// big score/level/norm band, bar vs. average, six facet bars with norm markers, and the
    /// "Wat betekent dit?", "Zo zie je het op je werk", "Valkuil", "Tip", "Sterk in" sections.
    /// </summary>
    private static void AddCompetenceDeepTraitPage(
        IDocumentContainer container, string brand, string fullName, CompetenceDeepTraitReport trait)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.MarginHorizontal(28);
            page.MarginVertical(24);
            page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor(Slate));
            DeepReportHeader(page, brand, fullName);

            page.Content().PaddingTop(10).Column(col =>
            {
                col.Spacing(7);

                col.Item().Row(r =>
                {
                    r.RelativeItem().Column(head =>
                    {
                        head.Item().Text(trait.LabelNl).FontSize(17).Bold().FontColor(BrandNavy);
                        head.Item().Text($"{trait.Score}/100 · {trait.Level}")
                            .FontSize(11).FontColor(BrandDeep).SemiBold();
                    });
                    if (!string.IsNullOrWhiteSpace(trait.NormBand))
                    {
                        r.ConstantItem(190).AlignRight().AlignMiddle().Background(SoftSky)
                            .PaddingHorizontal(8).PaddingVertical(6)
                            .Text(trait.NormBand!).FontSize(8.5f).FontColor(Muted).Italic();
                    }
                });

                col.Item().Element(e => ScoreBar(e, trait.Score, trait.NormMean, Gold));
                if (trait.NormMean is double avg)
                {
                    col.Item().Text($"Gemiddelde van de normgroep: {Math.Round(avg)}/100")
                        .FontSize(8).FontColor(Muted);
                }

                if (trait.Facets.Count > 0)
                {
                    col.Item().PaddingTop(2).Text("Facetten").FontSize(11).Bold().FontColor(BrandNavy);
                    foreach (var facet in trait.Facets)
                    {
                        col.Item().Row(r =>
                        {
                            r.ConstantItem(140).Text(facet.LabelNl).FontSize(8.5f);
                            r.RelativeItem().Element(e => ScoreBar(e, facet.Score, facet.NormMean, AccentTeal));
                            r.ConstantItem(30).AlignRight().Text($"{facet.Score}")
                                .FontSize(8.5f).FontColor(BrandDeep).Bold();
                        });
                    }
                }

                col.Item().PaddingTop(2).Background(SoftSky).Padding(7).Column(b =>
                {
                    b.Spacing(1);
                    b.Item().Text("Wat betekent dit?").Bold().FontColor(BrandNavy).FontSize(9.5f);
                    b.Item().Text(trait.Meaning).FontSize(9);
                });

                col.Item().Background(SoftMint).Padding(7).Column(b =>
                {
                    b.Spacing(1);
                    b.Item().Text("Zo zie je het op je werk").Bold().FontColor(AccentTeal).FontSize(9.5f);
                    b.Item().Text(trait.WorkQuote).FontSize(9);
                });

                col.Item().Row(r =>
                {
                    r.RelativeItem().Background(WarmSand).Padding(7).Column(b =>
                    {
                        b.Item().Text("Valkuil").Bold().FontColor(AccentCoral).FontSize(9.5f);
                        b.Item().Text(trait.Pitfall).FontSize(9);
                    });
                    r.ConstantItem(8);
                    r.RelativeItem().Background(SoftGold).Padding(7).Column(b =>
                    {
                        b.Item().Text("Tip").Bold().FontColor(Gold).FontSize(9.5f);
                        b.Item().Text(trait.Tip).FontSize(9);
                    });
                });

                col.Item().Background(SoftSky).Padding(7).Column(b =>
                {
                    b.Spacing(1);
                    b.Item().Text("Sterk in").Bold().FontColor(BrandNavy).FontSize(9.5f);
                    b.Item().Text(trait.Strength).FontSize(9);
                });
            });

            DeepReportFooter(page);
        });
    }

    /// <summary>
    /// Page 8 — three cards (Hier bloei je op / Leidinggevende die past / Jij in een team) using the
    /// candidate's top-scoring trait, plus occupation matches with match-percent bars and reasons.
    /// </summary>
    private static void AddCompetenceDeepWorkFitPage(
        IDocumentContainer container, string brand, string fullName, CompetenceDeepReport report)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.MarginHorizontal(28);
            page.MarginVertical(24);
            page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor(Slate));
            DeepReportHeader(page, brand, fullName);

            page.Content().PaddingTop(12).Column(col =>
            {
                col.Spacing(8);
                col.Item().Text("Werk dat bij je past").FontSize(16).Bold().FontColor(BrandNavy);

                var top = report.Traits.OrderByDescending(t => t.Score).FirstOrDefault();
                if (top is not null)
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Background(SoftSky).Padding(8).Column(b =>
                        {
                            b.Spacing(2);
                            b.Item().Text("Hier bloei je op").Bold().FontColor(BrandNavy).FontSize(9.5f);
                            b.Item().Text(top.ThriveAtWork).FontSize(9);
                        });
                        r.ConstantItem(8);
                        r.RelativeItem().Background(SoftMint).Padding(8).Column(b =>
                        {
                            b.Spacing(2);
                            b.Item().Text("Leidinggevende die past").Bold().FontColor(AccentTeal).FontSize(9.5f);
                            b.Item().Text(top.FittingManager).FontSize(9);
                        });
                        r.ConstantItem(8);
                        r.RelativeItem().Background(SoftGold).Padding(8).Column(b =>
                        {
                            b.Spacing(2);
                            b.Item().Text("Jij in een team").Bold().FontColor(Gold).FontSize(9.5f);
                            b.Item().Text(top.InTeam).FontSize(9);
                        });
                    });
                }

                col.Item().PaddingTop(4).Text("Beroepen die bij je passen").FontSize(13).Bold().FontColor(BrandNavy);
                if (report.Occupations.Count == 0)
                {
                    col.Item().Text("Vul de vragenlijst volledig in voor persoonlijke beroepssuggesties.")
                        .FontColor(Muted);
                }

                foreach (var occupation in report.Occupations)
                {
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text(occupation.Title).FontSize(11).SemiBold().FontColor(BrandNavy);
                        r.ConstantItem(50).AlignRight().Text($"{occupation.MatchPercent}%")
                            .FontColor(BrandDeep).Bold();
                    });
                    col.Item().Element(e => ScoreBar(e, occupation.MatchPercent, null, AccentTeal));
                    col.Item().Text(occupation.Reason).FontSize(9).FontColor(Muted);
                }
            });

            DeepReportFooter(page);
        });
    }

    /// <summary>
    /// Page 9 — three action-plan steps with a "☐ Gedaan op: ____" checkbox line, then "Over deze
    /// test" crediting the IPIP Big Five source, the Johnson (2014) norm line, and a diagnosis disclaimer.
    /// </summary>
    private static void AddCompetenceDeepActionPlanPage(
        IDocumentContainer container, string brand, string fullName, CompetenceDeepReport report)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.MarginHorizontal(28);
            page.MarginVertical(24);
            page.DefaultTextStyle(x => x.FontSize(9.5f).FontColor(Slate));
            DeepReportHeader(page, brand, fullName);

            page.Content().PaddingTop(12).Column(col =>
            {
                col.Spacing(9);
                col.Item().Text("Jouw actieplan").FontSize(16).Bold().FontColor(BrandNavy);

                var step = 1;
                foreach (var action in report.ActionPlan.Take(3))
                {
                    col.Item().Background(SoftMint).Padding(9).Column(box =>
                    {
                        box.Spacing(2);
                        box.Item().Text($"{step}. {action.Title}").FontSize(11).Bold().FontColor(BrandNavy);
                        box.Item().Text(action.Body).FontSize(9);
                        box.Item().Text("☐ Gedaan op: ____").FontSize(9).FontColor(Muted);
                    });
                    step++;
                }

                col.Item().PaddingTop(6).Text("Over deze test").FontSize(12).Bold().FontColor(BrandNavy);
                col.Item().Text("150 vragen · Big Five (IPIP).").FontSize(9);
                col.Item().Text(
                        "Goldberg, L. R., Johnson, J. A., Eber, H. W., Hogan, R., Ashton, M. C., Cloninger, C. R., " +
                        "& Gough, H. G. (2006). The International Personality Item Pool and the future of " +
                        "public-domain personality measures. Journal of Research in Personality, 40(1), 84–96. " +
                        "ipip.ori.org")
                    .FontSize(8).FontColor(Muted);
                if (!string.IsNullOrWhiteSpace(report.NormSourceLine))
                {
                    col.Item().Text(report.NormSourceLine!).FontSize(8).FontColor(Muted).Italic();
                }

                col.Item().PaddingTop(4).Text("Dit is geen diagnose.").FontSize(9).FontColor(Muted).Italic();
            });

            DeepReportFooter(page);
        });
    }

    /// <summary>Shared header for the deep-report pages 2–9 (cover intentionally has none).</summary>
    private static void DeepReportHeader(PageDescriptor page, string brand, string fullName)
    {
        page.Header().Column(header =>
        {
            header.Item().PaddingBottom(6)
                .Text($"{brand} · Uitgebreid rapport · Competentietest · {fullName}")
                .FontSize(9).FontColor(Muted);
            header.Item().Height(2).Background(Gold);
        });
    }

    /// <summary>Shared footer for the deep-report pages 2–9: "persoonlijk en vertrouwelijk · pagina x / N".</summary>
    private static void DeepReportFooter(PageDescriptor page)
    {
        page.Footer().PaddingTop(4).Row(row =>
        {
            row.RelativeItem().Text("persoonlijk en vertrouwelijk").FontColor(Muted).FontSize(8);
            row.ConstantItem(110).AlignRight().Text(x =>
            {
                x.Span("pagina ").FontColor(Muted).FontSize(8);
                x.CurrentPageNumber().FontColor(Muted).FontSize(8);
                x.Span(" / ").FontColor(Muted).FontSize(8);
                x.TotalPages().FontColor(Muted).FontSize(8);
            });
        });
    }

    /// <summary>
    /// Vector horizontal bar (QuestPDF <c>Row</c>/<c>Layers</c> primitives, not a rasterized
    /// screenshot): a track filled up to <paramref name="scorePercent"/> in <paramref name="fillColor"/>,
    /// with an optional thin navy marker line at <paramref name="markerPercent"/> (e.g. the norm mean).
    /// </summary>
    private static void ScoreBar(IContainer container, double scorePercent, double? markerPercent, Color fillColor)
    {
        var score = (float)Math.Clamp(scorePercent, 0, 100);
        var left = Math.Max(score, 0.01f);
        var right = Math.Max(100f - score, 0.01f);

        container.Height(10).Layers(layers =>
        {
            layers.PrimaryLayer().Background(Line).Row(row =>
            {
                row.RelativeItem(left).Background(fillColor);
                row.RelativeItem(right);
            });

            if (markerPercent is double markerRaw)
            {
                var marker = (float)Math.Clamp(markerRaw, 0, 100);
                var markerLeft = Math.Max(marker, 0.01f);
                var markerRight = Math.Max(100f - marker, 0.01f);
                layers.Layer().Row(row =>
                {
                    row.RelativeItem(markerLeft);
                    row.ConstantItem(2).Background(BrandDeep);
                    row.RelativeItem(markerRight);
                });
            }
        });
    }

    private static byte[] RenderCulture(
        string brand,
        byte[] logo,
        string fullName,
        string generated,
        IReadOnlyList<string> scoreLines,
        IReadOnlyList<string> tags,
        IReadOnlyList<string> advice)
        => RenderScoreReport(
            brand,
            logo,
            fullName,
            generated,
            "Jouw cultuur- & persoonlijkheidsrapport",
            "Dit rapport vat je 150 antwoorden samen: hoe jij graag werkt (cultuurfit) en hoe jij in een team past — zonder moeilijke testtaal.",
            scoreLines,
            tags,
            advice,
            AccentTeal);

    private static byte[] RenderScoreReport(
        string brand,
        byte[] logo,
        string fullName,
        string generated,
        string title,
        string intro,
        IReadOnlyList<string> scoreLines,
        IReadOnlyList<string> tags,
        IReadOnlyList<string> advice,
        Color accent)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(28);
                page.MarginVertical(24);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Slate));
                BrandHeader(page, brand, logo, title, fullName, generated, accent);

                page.Content().PaddingTop(14).Column(col =>
                {
                    col.Spacing(10);
                    col.Item().Text(intro).FontColor(Muted).Italic();

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
        "Dominant" => "Het voortouw nemen",
        "Invloed" => "Mensen meenemen",
        "Stabiel" => "Rust en ritme",
        "Nauwkeurig" => "Nauwkeurig werken",
        _ => domain
    };

    private static string LabelCulture(string domain)
        => CulturePersonalityCatalog.EverydayLabel(domain) switch
        {
            var label when !string.IsNullOrWhiteSpace(label) && label != "hoe jij graag werkt"
                => char.ToUpperInvariant(label[0]) + label[1..],
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
