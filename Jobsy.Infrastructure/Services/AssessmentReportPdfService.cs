using System.Globalization;
using Jobsy.Core.Careers;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Features;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Reports;
using Jobsy.Core.Reports.Career;
using Jobsy.Core.Reports.Competence;
using Jobsy.Core.Reports.Culture;
using Jobsy.Core.Reports.Values;
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
    private readonly ILegalIdentity _legal;
    private readonly IFeatureFlags _features;
    private readonly IKindDeepReportService _kindReports;

    static AssessmentReportPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public AssessmentReportPdfService(
        JobsyDbContext db,
        IPlatformCompanySettingsService companySettings,
        ICareerCompassGenerationService careerCompass,
        IMemoryCache cache,
        ILegalIdentity legal,
        IFeatureFlags features,
        IKindDeepReportService kindReports)
    {
        _db = db;
        _companySettings = companySettings;
        _careerCompass = careerCompass;
        _cache = cache;
        _legal = legal;
        _features = features;
        _kindReports = kindReports;
    }

    private static DateTime ToAmsterdam(DateTime utc)
    {
        var instant = utc.Kind == DateTimeKind.Utc ? utc : DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        foreach (var id in new[] { "Europe/Amsterdam", "W. Europe Standard Time" })
        {
            try
            {
                return TimeZoneInfo.ConvertTimeFromUtc(instant, TimeZoneInfo.FindSystemTimeZoneById(id));
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return instant;
    }

    public Task<AssessmentReportPdf?> TryRenderAsync(
        Guid userId,
        AssessmentKind kind,
        CancellationToken cancellationToken = default)
        => TryRenderAsync(userId, kind, lang: null, cancellationToken);

    public async Task<AssessmentReportPdf?> TryRenderAsync(
        Guid userId,
        AssessmentKind kind,
        string? lang,
        CancellationToken cancellationToken = default)
    {
        var reportLang = ReportLanguage.FromUi(lang);
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

        var legal = await _legal.GetAsync(cancellationToken);
        var brand = string.IsNullOrWhiteSpace(legal.TradeName) ? legal.DisplayName : legal.TradeName.Trim();
        var logo = _companySettings.GetBrandLogoPng();
        var culture = ReportLanguage.IsEnglish(reportLang)
            ? CultureInfo.GetCultureInfo("en-GB")
            : CultureInfo.GetCultureInfo("nl-NL");
        var generated = ToAmsterdam(deep.ReportGeneratedAtUtc ?? deep.CompletedAtUtc ?? DateTime.UtcNow)
            .ToString("d MMMM yyyy", culture);

        var answers = DeepAnalysisCatalog.ParseAnswersJson(deep.AnswersJson, kind);
        var domainScores = DeepAnalysisCatalog.ScoreDomains(answers, kind);
        var employersOn = await _features.IsEnabledAsync(PlatformFeature.Employers, cancellationToken);

        byte[] bytes;
        if (kind == AssessmentKind.Career)
        {
            var careerDeep = CareerDeepReportJson.Deserialize(deep.ReportJson);
            var careerStale = deep.ReportVersion < CareerDeepReportJson.CurrentReportVersion || careerDeep is null;
            if (careerStale)
            {
                try
                {
                    careerDeep = await _kindReports.GetCareerAsync(userId, cancellationToken) ?? careerDeep;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    careerDeep ??= CareerDeepReportJson.Deserialize(deep.ReportJson);
                    _ = ex;
                }
            }

            if (careerDeep is not null)
            {
                var education = EducationLabel(user);
                var cacheKey = $"deep-pdf:{userId}:{kind}:{reportLang}:{careerDeep.ReportVersion}:{careerDeep.GeneratedAtUtc:O}:p8:fit:{education}";
                if (!_cache.TryGetValue(cacheKey, out byte[]? cached) || cached is null)
                {
                    cached = RenderCareerDeep(brand, logo, user.FullName, generated, careerDeep, reportLang, education);
                    _cache.Set(cacheKey, cached, DeepPdfCacheDuration);
                }

                bytes = cached;
            }
            else
            {
                var careerRow = await _db.CandidateCareerInterests
                    .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
                var compass = CareerCompassJson.TryDeserialize(careerRow?.CompassJson);
                if (compass is not { HasOccupations: true })
                {
                    var riasec = DeepAnalysisCatalog.ToRiasecScores(
                        DeepAnalysisCatalog.ScoreDomains(answers, AssessmentKind.Career));
                    var scoresKey = CareerCompassBuilder.ScoresKey(riasec);
                    var attemptAt = DateTime.UtcNow;
                    if (CareerCompassAttempt.TryBegin(userId, careerRow?.CompassJson, scoresKey, attemptAt))
                    {
                        try
                        {
                            compass = await _careerCompass.GenerateFromCareerDeepAsync(answers, cancellationToken);
                            compass = compass with { ScoresFingerprint = scoresKey, ModelAttemptUtc = attemptAt };
                            if (careerRow is not null)
                            {
                                careerRow.CompassJson = CareerCompassJson.Serialize(compass);
                                careerRow.UpdatedAtUtc = attemptAt;
                                await _db.SaveChangesAsync(cancellationToken);
                            }
                        }
                        finally
                        {
                            CareerCompassAttempt.End(userId, scoresKey);
                        }
                    }
                    else
                    {
                        compass = CareerCompassBuilder.Build(riasec, fromDeepAnalysis: true);
                    }
                }

                if (!employersOn)
                {
                    compass = compass with
                    {
                        PracticalNotes = CareerCompassBuilder.NotesForCandidate(compass.PracticalNotes, false)
                    };
                }

                bytes = RenderCareer(brand, logo, user.FullName, generated, compass, EducationLabel(user));
            }
        }
        else if (kind == AssessmentKind.Culture)
        {
            var cultureDeep = CultureDeepReportJson.Deserialize(deep.ReportJson);
            if (cultureDeep is not null)
            {
                var cacheKey = $"deep-pdf:{userId}:{kind}:{reportLang}:{cultureDeep.ReportVersion}:{cultureDeep.GeneratedAtUtc:O}:p8:e{employersOn}";
                if (!_cache.TryGetValue(cacheKey, out byte[]? cached) || cached is null)
                {
                    cached = RenderCultureDeep(brand, logo, user.FullName, generated, cultureDeep, reportLang, employersOn);
                    _cache.Set(cacheKey, cached, DeepPdfCacheDuration);
                }

                bytes = cached;
            }
            else
            {
                var scoreLines = domainScores
                    .Select(s => $"{DeepReportCatalog.CultureLabel(s.Domain, reportLang)}: {s.Percent}%")
                    .ToList();
                var tags = CulturePersonalityCatalog.ParseTags(deep.TagsJson)
                    .Select(t => DeepReportCatalog.CultureLabel(t, reportLang))
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToList();
                // Never use CareerAdviceParagraphs here (prints "Werk dat bij je past").
                var advice = CultureLegacyAdvice(domainScores, reportLang);
                bytes = RenderCulture(brand, logo, user.FullName, generated, scoreLines, tags, advice, reportLang);
            }
        }
        else if (kind == AssessmentKind.Values)
        {
            var valuesDeep = ValuesDeepReportJson.Deserialize(deep.ReportJson);
            if (valuesDeep is not null)
            {
                var cacheKey = $"deep-pdf:{userId}:{kind}:{reportLang}:{valuesDeep.ReportVersion}:{valuesDeep.GeneratedAtUtc:O}:p7:e{employersOn}";
                if (!_cache.TryGetValue(cacheKey, out byte[]? cached) || cached is null)
                {
                    cached = RenderValuesDeep(brand, logo, user.FullName, generated, valuesDeep, reportLang, employersOn);
                    _cache.Set(cacheKey, cached, DeepPdfCacheDuration);
                }

                bytes = cached;
            }
            else
            {
                var scoreLines = domainScores
                    .Select(s => $"{DeepReportCatalog.ValueLabel(s.Domain, reportLang)}: {s.Percent}%")
                    .ToList();
                var advice = ValuesLegacyAdvice(domainScores, reportLang);
                bytes = RenderScoreReport(
                    brand,
                    logo,
                    user.FullName,
                    generated,
                    DeepReportCatalog.Get("title.values", reportLang),
                    ReportLanguage.IsEnglish(reportLang)
                        ? "This report summarises your work values from the extended test — without clinical language."
                        : "Dit rapport vat je waarden op werk samen uit de uitgebreide test — zonder klinische taal.",
                    scoreLines,
                    [],
                    advice,
                    AccentTeal);
            }
        }
        else
        {
            var report = CompetenceDeepReportJson.Deserialize(deep.ReportJson);
            if (report is not null)
            {
                var cacheKey = $"deep-pdf:{userId}:{kind}:{reportLang}:{report.ReportVersion}:{report.GeneratedAtUtc:O}";
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
                var advice = DeepAnalysisCatalog.CareerAdviceParagraphs(domainScores, employersOn);
                bytes = RenderCompetence(brand, logo, user.FullName, generated, scoreLines, tags, advice);
            }
        }

        var slug = AssessmentKindLabels.ToSlug(kind);
        var fileName = DeepReportCatalog.FileName(new AssessmentKindSlug(slug), reportLang, DateTime.UtcNow);
        return new AssessmentReportPdf(fileName, bytes);
    }

    private static IReadOnlyList<string> CultureLegacyAdvice(
        IReadOnlyList<DeepAnalysisDomainScore> scores, string lang)
    {
        var top = scores.OrderByDescending(s => s.Percent).Take(3)
            .Select(s => DeepReportCatalog.CultureLabel(s.Domain, lang));
        return
        [
            ReportLanguage.IsEnglish(lang)
                ? $"You score highest on {string.Join(", ", top)}. Look for workplaces where those show up every week."
                : $"Je scoort het hoogst op {string.Join(", ", top)}. Zoek werkplekken waar dat elke week zichtbaar is."
        ];
    }

    private static IReadOnlyList<string> ValuesLegacyAdvice(
        IReadOnlyList<DeepAnalysisDomainScore> scores, string lang)
    {
        var top = scores.OrderByDescending(s => s.Percent).Take(2)
            .Select(s => DeepReportCatalog.ValueLabel(s.Domain, lang));
        return
        [
            ReportLanguage.IsEnglish(lang)
                ? $"Your top values are {string.Join(" and ", top)}. Use them when you choose roles and employers."
                : $"Jouw topwaarden zijn {string.Join(" en ", top)}. Gebruik die bij het kiezen van rollen en werkgevers."
        ];
    }

    internal static byte[] RenderCareer(
        string brand,
        byte[] logo,
        string fullName,
        string generated,
        CareerCompassSnapshot compass,
        string? education = null)
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
                        SoftMint, compass.SuperMatches, "Geen beroep in deze groep.");
                    WriteOccupationBand(col, CareerCompassBuilder.BandLabel(CareerCompassBuilder.BandStrong),
                        SoftSky, compass.StrongChoices, "Geen beroep in deze groep.");
                    WriteOccupationBand(col, CareerCompassBuilder.BandLabel(CareerCompassBuilder.BandBroaden),
                        WarmSand, compass.Broadening, "Geen beroep in deze groep.");
                    WriteFitFootnote(col, CompassFootnote(compass, education));

                    col.Item().PaddingTop(8).Background(SoftSky).Padding(12).Column(box =>
                    {
                        box.Spacing(6);
                        box.Item().Text("Wat betekent dit voor jou?").FontSize(13).Bold().FontColor(BrandNavy);
                        foreach (var note in compass.PracticalNotes)
                        {
                            if (note.Contains("Wat betekent dit voor jou", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

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
                        r.ConstantItem(58).AlignRight().Text($"{CareerCompassBuilder.FormatPercent(occupation.MatchPercent)}%")
                            .FontColor(BrandDeep).Bold();
                    });
                    col.Item().Element(e => ScoreBar(e, (double)occupation.MatchPercent, null, AccentTeal));
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
        IReadOnlyList<string> advice,
        string? lang = null)
        => RenderScoreReport(
            brand,
            logo,
            fullName,
            generated,
            DeepReportCatalog.Get("title.culture", lang),
            ReportLanguage.IsEnglish(lang)
                ? "This report summarises your 150 answers: how you like to work (culture fit) and how you show up in a team — without jargon."
                : "Dit rapport vat je 150 antwoorden samen: hoe jij graag werkt (cultuurfit) en hoe jij in een team past — zonder moeilijke testtaal.",
            scoreLines,
            tags,
            advice,
            AccentTeal);

    internal static byte[] RenderCareerDeep(
        string brand,
        byte[] logo,
        string fullName,
        string generated,
        CareerDeepReport report,
        string lang,
        string? education = null)
    {
        var title = DeepReportCatalog.Get("title.career", lang);
        var en = ReportLanguage.IsEnglish(lang);
        var top = string.Join(", ", report.Domains.OrderByDescending(d => d.Score).Take(3)
            .Select(d => DeepReportCatalog.RiasecLabel(d.Domain, lang)));
        var places = CareerDeepReportBuilder.TypicalPlaces(report, lang);
        var cover = report.Summary.Resolve(lang);
        if (!string.IsNullOrWhiteSpace(top))
        {
            cover += en
                ? $" Strongest directions: {top}."
                : $" Sterkste richtingen: {top}.";
        }

        if (!string.IsNullOrWhiteSpace(places))
        {
            cover += en
                ? $" Jobs on the next pages: {places}."
                : $" Beroepen op de volgende pagina's: {places}.";
        }

        return RenderExplicitPages(
            brand, logo, fullName, generated, lang, title, cover,
            [
                col => WriteCareerScorePage(col, lang, report),
                col =>
                {
                    Heading(col, DeepReportCatalog.Get("holland.title", lang));
                    col.Item().Text(DeepReportCatalog.Format("holland.body", lang, report.HollandCode, top, places));
                    col.Item().PaddingTop(8).Text(en
                        ? "Read the letters as a direction. A higher score means that kind of work showed up more often in your answers."
                        : "Lees de letters als een richting. Een hogere score betekent dat dat soort werk vaker in je antwoorden zat.").FontColor(Muted);
                    foreach (var domain in report.Domains.OrderByDescending(d => d.Score))
                    {
                        col.Item().Text($"{DeepReportCatalog.RiasecLabel(domain.Domain, lang)}: {domain.Score}%");
                    }
                },
                col =>
                {
                    Heading(col, en ? "Jobs that fit you" : "Beroepen die bij je passen");
                    if (report.Occupations.Count == 0)
                    {
                        col.Item().Text(en
                            ? "Your answers do not point to one job yet. Use the directions above as a start."
                            : "Je antwoorden wijzen nog niet naar één beroep. Gebruik de richtingen hierboven als start.").FontColor(Muted);
                    }
                    else
                    {
                        foreach (var o in report.Occupations.Take(CareerCompassSanitize.MaxCatalogueJobs))
                        {
                            col.Item().Text($"{o.Title(lang)} — {CareerCompassBuilder.FormatPercent(o.MatchPercent)}%").SemiBold();
                            col.Item().Text(o.Reason(lang)).FontSize(9).FontColor(Muted);
                        }

                        WriteFitFootnote(col, CareerFootnote(report, education, lang));
                    }
                },
                col => WriteActionPage(col, lang, report.ActionPlan),
                col => WriteStrengthPage(col, lang, report.StrengthKeys, report.PitfallKeys, "riasec"),
                col =>
                {
                    Heading(col, en ? "How to read your scores" : "Zo lees je je scores");
                    col.Item().Text(en
                        ? "A higher percent means that direction showed up more often in your answers. It is a starting point, not a grade."
                        : "Een hoger percentage betekent dat die richting vaker in je antwoorden zat. Het is een startpunt, geen cijfer.").FontColor(Muted);
                    foreach (var domain in report.Domains.OrderByDescending(d => d.Score))
                    {
                        var label = DeepReportCatalog.RiasecLabel(domain.Domain, lang);
                        col.Item().PaddingTop(6).Text($"{label}: {domain.Score}%").SemiBold();
                        col.Item().Text(CareerScoreSense(domain.Domain, en)).FontSize(9).FontColor(Muted);
                    }
                },
                col =>
                {
                    Heading(col, en ? "What you can do next" : "Wat je hiermee kunt doen");
                    col.Item().Text(en
                        ? "This week, talk to one person who does a job from your list. Ask what a normal Tuesday looks like, and which task they would skip if they could."
                        : "Praat deze week met één persoon die een beroep uit jouw lijst doet. Vraag hoe een gewone dinsdag eruitziet, en welke taak die persoon zou overslaan als dat mocht.").FontColor(Muted);
                    col.Item().PaddingTop(8).Text(en
                        ? "Write down three tasks that gave you energy. Put them next to the jobs on the page before this one. Keep the job whose day looks most like those tasks."
                        : "Schrijf drie taken op die je energie gaven. Leg ze naast de beroepen op de vorige pagina. Houd het beroep waarvan de dag het meest op die taken lijkt.").FontColor(Muted);
                    if (!string.IsNullOrWhiteSpace(places))
                    {
                        col.Item().PaddingTop(8).Text(en
                            ? $"Places and jobs to start with: {places}."
                            : $"Plekken en beroepen om mee te beginnen: {places}.").FontColor(Muted);
                    }

                    col.Item().PaddingTop(8).Text(en
                        ? "Your answers stay yours. A workplace only sees that a direction fits, not your raw scores."
                        : "Je antwoorden blijven van jou. Een werkplek ziet alleen dat een richting past, niet je ruwe scores.").FontColor(Muted);
                }
            ]);
    }

    internal static byte[] RenderCultureDeep(
        string brand,
        byte[] logo,
        string fullName,
        string generated,
        CultureDeepReport report,
        string lang,
        bool employersOn = true)
    {
        var title = DeepReportCatalog.Get("title.culture", lang);
        var en = ReportLanguage.IsEnglish(lang);
        var cultureAxes = report.Domains
            .Where(d => CulturePersonalityCatalog.CultureDimensionCodes.Contains(d.Domain, StringComparer.OrdinalIgnoreCase))
            .ToList();
        var facets = report.Domains
            .Where(d => CulturePersonalityCatalog.PersonalityFacetCodes.Contains(d.Domain, StringComparer.OrdinalIgnoreCase))
            .ToList();
        if (cultureAxes.Count == 0)
        {
            cultureAxes = report.Domains;
        }

        return RenderExplicitPages(
            brand, logo, fullName, generated, lang, title, report.Summary.Resolve(lang),
            [
                col => WriteScorePage(col, lang, cultureAxes.Select(d => (
                    DeepReportCatalog.CultureLabel(d.Domain, lang), d.Score, d.NormMean))),
                col =>
                {
                    if (employersOn)
                    {
                        Heading(col, en ? "Workplaces that fit you" : "Werkplekken die bij je passen");
                        foreach (var e in report.Employers.Take(6))
                        {
                            col.Item().Text(DeepReportCatalog.Get($"org.{e.OrgTypeKey}", lang)).SemiBold();
                            col.Item().Text(DeepReportCatalog.Get($"org.{e.OrgTypeKey}.why", lang)).FontSize(9).FontColor(Muted);
                        }
                    }
                    else
                    {
                        Heading(col, en ? "How you like to work" : "Hoe jij graag werkt");
                        col.Item().Text(en
                            ? "Look for a place where these ways of working show up in a normal week."
                            : "Zoek een plek waar deze manieren van werken in een gewone week zichtbaar zijn.").FontColor(Muted);
                        foreach (var d in cultureAxes.Take(3))
                        {
                            col.Item().Text($"{DeepReportCatalog.CultureLabel(d.Domain, lang)} — {d.Score}%").SemiBold();
                        }
                    }
                },
                col =>
                {
                    Heading(col, en ? "How you show up in a team" : "Hoe jij in een team past");
                    foreach (var d in facets)
                    {
                        col.Item().Text($"{DeepReportCatalog.CultureLabel(d.Domain, lang)} — {d.Score}%");
                    }
                },
                col => WriteActionPage(col, lang, report.ActionPlan),
                col => WriteStrengthPage(col, lang, report.StrengthKeys, report.PitfallKeys, "culture"),
                col =>
                {
                    Heading(col, en ? "How to read your scores" : "Zo lees je je scores");
                    col.Item().Text(en
                        ? "A higher percent means that way of working showed up more often. It is a starting point, not a grade."
                        : "Een hoger percentage betekent dat die manier van werken vaker in je antwoorden zat. Het is een startpunt, geen cijfer.").FontColor(Muted);
                },
                col =>
                {
                    Heading(col, en ? "What you can do next" : "Wat je hiermee kunt doen");
                    col.Item().Text(en
                        ? "Use this picture when you look at a workplace. Your answers stay yours."
                        : "Gebruik dit beeld als je naar een werkplek kijkt. Je antwoorden blijven van jou.").FontColor(Muted);
                }
            ]);
    }

    internal static byte[] RenderValuesDeep(
        string brand,
        byte[] logo,
        string fullName,
        string generated,
        ValuesDeepReport report,
        string lang,
        bool employersOn = true)
    {
        var title = DeepReportCatalog.Get("title.values", lang);
        var en = ReportLanguage.IsEnglish(lang);
        return RenderExplicitPages(
            brand, logo, fullName, generated, lang, title, report.Summary.Resolve(lang),
            [
                col => WriteScorePage(col, lang, report.Domains.Select(d => (
                    DeepReportCatalog.ValueLabel(d.Domain, lang), d.Score, d.NormMean))),
                col =>
                {
                    Heading(col, DeepReportCatalog.Get("values.rank.title", lang));
                    col.Item().Text(DeepReportCatalog.Get("values.rank.lead", lang)).FontColor(Muted).Italic();
                    var rank = 1;
                    foreach (var d in report.Domains)
                    {
                        col.Item().Text($"{rank}. {DeepReportCatalog.ValueLabel(d.Domain, lang)} — {d.Score}%").SemiBold();
                        col.Item().Text(ChooseLine(d.Domain, lang)).FontSize(9).FontColor(Muted);
                        rank++;
                    }
                },
                col =>
                {
                    if (employersOn)
                    {
                        Heading(col, en ? "Workplaces that fit these values" : "Werkplekken die bij deze waarden passen");
                        foreach (var e in report.Employers.Take(6))
                        {
                            col.Item().Text(DeepReportCatalog.Get($"org.{e.OrgTypeKey}", lang)).SemiBold();
                            col.Item().Text(DeepReportCatalog.Get($"org.{e.OrgTypeKey}.why", lang)).FontSize(9).FontColor(Muted);
                        }
                    }
                    else
                    {
                        Heading(col, en ? "What this means for your work" : "Wat dit voor je werk betekent");
                        col.Item().Text(en
                            ? "Use your top values when you choose tasks and a team. You do not need a company name for that."
                            : "Gebruik je topwaarden als je taken en een team kiest. Daar heb je geen bedrijfsnaam voor nodig.").FontColor(Muted);
                    }
                },
                col => WriteActionPage(col, lang, report.ActionPlan),
                col => WriteStrengthPage(col, lang, report.StrengthKeys, report.PitfallKeys, "value"),
                col =>
                {
                    Heading(col, en ? "How to read this and what is next" : "Zo lees je dit, en wat daarna");
                    col.Item().Text(en
                        ? "A higher percent means that value weighed more in your answers. Ask in a conversation how it shows up in a normal week."
                        : "Een hoger percentage betekent dat die waarde zwaarder woog in je antwoorden. Vraag in een gesprek hoe dat in een gewone week zichtbaar is.").FontColor(Muted);
                }
            ]);
    }

    private static byte[] RenderExplicitPages(
        string brand,
        byte[] logo,
        string fullName,
        string generated,
        string lang,
        string title,
        string summary,
        IReadOnlyList<Action<ColumnDescriptor>> pages)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(28);
                page.MarginVertical(24);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Slate));
                BrandHeader(page, brand, logo, title, fullName, generated, AccentTeal);
                page.Content().PaddingTop(28).Column(col =>
                {
                    col.Spacing(10);
                    col.Item().Text(title).FontSize(22).Bold().FontColor(BrandNavy);
                    col.Item().Text(summary).FontSize(12);
                    col.Item().PaddingTop(12).Text(DeepReportCatalog.Get("pdf.disclaimer", lang))
                        .FontColor(Muted).Italic().FontSize(9);
                });
                BrandFooter(page, brand);
            });

            foreach (var body in pages)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginHorizontal(28);
                    page.MarginVertical(24);
                    page.DefaultTextStyle(x => x.FontSize(10).FontColor(Slate));
                    BrandHeader(page, brand, logo, title, fullName, generated, AccentTeal);
                    page.Content().PaddingTop(14).Column(col =>
                    {
                        col.Spacing(8);
                        body(col);
                    });
                    BrandFooter(page, brand);
                });
            }
        }).GeneratePdf();
    }

    private static void Heading(ColumnDescriptor col, string text)
        => col.Item().Text(text).FontSize(14).Bold().FontColor(BrandNavy);

    private static void WriteCareerScorePage(ColumnDescriptor col, string lang, CareerDeepReport report)
    {
        var en = ReportLanguage.IsEnglish(lang);
        Heading(col, DeepReportCatalog.Get("pdf.overview", lang));
        col.Item().Text(en
            ? "Each line is how often that kind of work showed up in your 200 answers. A higher percent is a direction to explore, not a grade and not a promise of a job."
            : "Elke regel laat zien hoe vaak dat soort werk in je 200 antwoorden zat. Een hoger percentage is een richting om te verkennen, geen cijfer en geen belofte van een baan.").FontColor(Muted);
        foreach (var domain in report.Domains.OrderByDescending(d => d.Score))
        {
            var label = DeepReportCatalog.RiasecLabel(domain.Domain, lang);
            var line = domain.NormMean is double n
                ? $"{label}: {domain.Score}% (Ø {Math.Round(n)}%)"
                : $"{label}: {domain.Score}%";
            col.Item().PaddingTop(8).Text(line).SemiBold();
            col.Item().Text(CareerScoreSense(domain.Domain, en));
        }
    }

    private static string CareerScoreSense(string domain, bool en)
    {
        var key = domain.Trim().ToUpperInvariant();
        return key switch
        {
            "R" or "REALISTIC" => en
                ? "This is work you can point at when the day is over: making, fixing, moving or being outside."
                : "Dit is werk waar je aan het eind van de dag iets kunt aanwijzen: maken, repareren, verplaatsen of buiten zijn.",
            "I" or "INVESTIGATIVE" => en
                ? "This is work where you check, measure or find out why something happens before you change it."
                : "Dit is werk waarbij je eerst nakijkt, meet of uitzoekt waarom iets gebeurt, en daarna pas iets verandert.",
            "A" or "ARTISTIC" => en
                ? "This is work where how something looks, sounds or is told may be your own."
                : "Dit is werk waarbij hoe iets eruitziet, klinkt of verteld wordt van jou mag zijn.",
            "S" or "SOCIAL" => en
                ? "This is work with people: explaining, guiding or helping someone take the next step."
                : "Dit is werk met mensen: uitleggen, begeleiden of iemand helpen met de volgende stap.",
            "E" or "ENTERPRISING" => en
                ? "This is work with a goal in sight: convincing someone, starting something or keeping a team moving."
                : "Dit is werk met een doel in zicht: iemand overtuigen, iets starten of een team in beweging houden.",
            "C" or "CONVENTIONAL" => en
                ? "This is work with a clear order: lists, appointments, numbers and things in the right place."
                : "Dit is werk met een duidelijke volgorde: lijsten, afspraken, cijfers en spullen op de juiste plek.",
            _ => en
                ? "Use this direction when you compare tasks. It is not a mark."
                : "Gebruik deze richting als je taken vergelijkt. Het is geen cijfer."
        };
    }

    private static void WriteScorePage(
        ColumnDescriptor col,
        string lang,
        IEnumerable<(string Label, int Score, double? Norm)> domains)
    {
        Heading(col, DeepReportCatalog.Get("pdf.overview", lang));
        col.Item().Text(DeepReportCatalog.Get("pdf.scores", lang)).FontSize(12).Bold().FontColor(BrandNavy);
        foreach (var (label, score, norm) in domains)
        {
            var line = norm is double n
                ? $"{label}: {score}% (Ø {Math.Round(n)}%)"
                : $"{label}: {score}%";
            col.Item().Text(line);
        }
    }

    private static void WriteActionPage(ColumnDescriptor col, string lang, IReadOnlyList<DeepActionStep> plan)
    {
        Heading(col, DeepReportCatalog.Get("pdf.actionPlan", lang));
        col.Item().Text(DeepReportCatalog.Get("action.lead", lang)).FontColor(Muted).Italic();
        foreach (var step in plan.Take(3))
        {
            col.Item().Text(step.Title.Resolve(lang)).SemiBold();
            col.Item().Text(step.Body.Resolve(lang)).FontSize(9).FontColor(Muted);
        }
    }

    private static void WriteStrengthPage(
        ColumnDescriptor col,
        string lang,
        IReadOnlyList<string> strengths,
        IReadOnlyList<string> pitfalls,
        string label)
    {
        Heading(col, DeepReportCatalog.Get("pdf.strengths", lang));
        col.Item().Text(DeepReportCatalog.Get("strength.lead", lang)).FontColor(Muted).Italic();
        foreach (var key in strengths.Take(3))
        {
            col.Item().Text("• " + StrengthSentence(key, label, lang));
        }

        foreach (var key in pitfalls.Take(3))
        {
            col.Item().Text("△ " + StrengthSentence(key, label, lang)).FontColor(AccentCoral);
        }
    }

    private static string ChooseLine(string domain, string lang)
    {
        var specific = $"values.choose.{domain}";
        return DeepReportCatalog.TryGet(specific, lang, out var text)
            ? text
            : DeepReportCatalog.Get("values.choose", lang);
    }

    private static string StrengthSentence(string key, string label, string lang)
    {
        if (DeepReportCatalog.TryGet(key, lang, out var text))
        {
            return text;
        }

        var code = key.Split('.').LastOrDefault() ?? key;
        return label switch
        {
            "riasec" => DeepReportCatalog.RiasecLabel(code, lang),
            "culture" => DeepReportCatalog.CultureLabel(code, lang),
            _ => DeepReportCatalog.ValueLabel(code, lang)
        };
    }

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

    private static string? EducationLabel(User user)
    {
        var prefs = MatchingProfileMapper.DeserializePrefs(user.PreferencesJson);
        var label = string.Join(
            " ",
            (prefs.Educations ?? []).Append(prefs.EducationDirection ?? "").Where(item => !string.IsNullOrWhiteSpace(item)));
        return string.IsNullOrWhiteSpace(label) ? null : label;
    }

    private static string CompassFootnote(CareerCompassSnapshot compass, string? education)
    {
        var scores = FitPercentExplanation.TryScores(compass.ScoresFingerprint);
        if (scores is null)
        {
            return FitPercentExplanation.FormulaOnly("nl");
        }

        var items = new List<FitPercentExplanation>();
        foreach (var job in compass.AllOccupations)
        {
            var explain = FitPercentExplanation.Build(job.Title, scores, education);
            if (explain is not null)
            {
                items.Add(explain);
            }
        }

        return FitPercentExplanation.Footnote(items, "nl");
    }

    private static string CareerFootnote(CareerDeepReport report, string? education, string? lang)
    {
        int? Score(string code)
        {
            var hit = report.Domains.FirstOrDefault(domain =>
                string.Equals(domain.Domain, code, StringComparison.OrdinalIgnoreCase));
            return hit is null ? null : hit.Score;
        }

        var scores = FitPercentExplanation.TryScores(
            Score(CareerTestCatalog.Realistic),
            Score(CareerTestCatalog.Investigative),
            Score(CareerTestCatalog.Artistic),
            Score(CareerTestCatalog.Social),
            Score(CareerTestCatalog.Enterprising),
            Score(CareerTestCatalog.Conventional));
        if (scores is null)
        {
            return FitPercentExplanation.FormulaOnly(lang);
        }

        var items = new List<FitPercentExplanation>();
        foreach (var job in report.Occupations.Take(CareerCompassSanitize.MaxCatalogueJobs))
        {
            var explain = FitPercentExplanation.Build(job.TitleNl, scores, education);
            if (explain is not null)
            {
                items.Add(explain);
            }
        }

        return FitPercentExplanation.Footnote(items, lang);
    }

    private static void WriteFitFootnote(ColumnDescriptor col, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        col.Item().PaddingTop(4).Text(text).FontSize(8).FontColor(Muted);
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
                    r.ConstantItem(58).AlignRight().Text($"{CareerCompassBuilder.FormatPercent(item.Percent)}%").FontColor(BrandDeep).Bold();
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

    private static string FriendlyTag(string tag)
    {
        if (CareerTestCatalog.RiasecCodes.Contains(tag, StringComparer.OrdinalIgnoreCase))
        {
            return CareerCompassBuilder.TypeLabel(tag);
        }

        return tag;
    }
}
