using System.Globalization;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class DeepAnalysisService : IDeepAnalysisService
{
    private readonly JobsyDbContext _db;
    private readonly IFlexCommercialService _commercial;
    private readonly IHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DeepAnalysisService> _logger;

    public DeepAnalysisService(
        JobsyDbContext db,
        IFlexCommercialService commercial,
        IHostEnvironment environment,
        IConfiguration configuration,
        ILogger<DeepAnalysisService> logger)
    {
        _db = db;
        _commercial = commercial;
        _environment = environment;
        _configuration = configuration;
        _logger = logger;
    }

    public static string FormatUpsellCopy(decimal priceEuro, AssessmentKind kind = AssessmentKind.Competence)
    {
        var price = priceEuro.ToString("0.00", CultureInfo.GetCultureInfo("nl-NL"));
        return kind == AssessmentKind.Career
            ? $"Wil je een diepgaand carrière-advies en een uitgebreid overzicht van al je opties inclusief PDF-rapport? Ontgrendel de uitgebreide beroepentest voor € {price}."
            : $"Ontgrendel je uitgebreide competentie-analyse inclusief officiële PDF-rapportage voor € {price}.";
    }

    public async Task<DeepAnalysisStateDto> GetStateAsync(
        Guid userId,
        AssessmentKind kind,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.CandidateDeepAnalyses.AsNoTracking()
            .FirstOrDefaultAsync(d => d.UserId == userId && d.Kind == kind, cancellationToken);
        var commercial = await _commercial.GetAsync(cancellationToken);
        return ToDto(kind, row, commercial.DeepAnalysisPriceEuro);
    }

    public async Task<DeepAnalysisCheckoutResult> StartCheckoutAsync(
        Guid userId,
        AssessmentKind kind,
        CancellationToken cancellationToken = default)
    {
        _ = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Gebruiker niet gevonden.");

        var existing = await _db.CandidateDeepAnalyses
            .FirstOrDefaultAsync(d => d.UserId == userId && d.Kind == kind, cancellationToken);
        if (existing is not null && CandidateDeepAnalysisStatuses.IsUnlocked(existing.Status))
        {
            throw new InvalidOperationException("Diepte-analyse is al ontgrendeld.");
        }

        if (!AllowStubPayments())
        {
            throw new InvalidOperationException(
                "Diepte-analyse-betalingen zijn buiten Development alleen beschikbaar met Mollie of AllowStubPayments.");
        }

        var commercial = await _commercial.GetAsync(cancellationToken);
        var price = commercial.DeepAnalysisPriceEuro;

        var open = await _db.DeepAnalysisCheckouts
            .Where(c => c.UserId == userId && c.Kind == kind && c.Status == DeepAnalysisCheckoutStatus.Pending)
            .ToListAsync(cancellationToken);
        foreach (var prior in open)
        {
            prior.Status = DeepAnalysisCheckoutStatus.Cancelled;
        }

        var slug = AssessmentKindLabels.ToSlug(kind);
        var paymentId = $"stub_deep_{slug}_{Guid.NewGuid():N}";
        var checkout = new DeepAnalysisCheckout
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Kind = kind,
            PaymentId = paymentId,
            AmountEuro = price,
            Status = DeepAnalysisCheckoutStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.DeepAnalysisCheckouts.Add(checkout);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Deep analysis ({Kind}) checkout for user {UserId}: €{Amount} ({PaymentId})",
            kind, userId, price, paymentId);

        return new DeepAnalysisCheckoutResult(
            checkout.Id,
            paymentId,
            $"/candidate/deep-analysis/checkout?kind={Uri.EscapeDataString(slug)}&paymentId={Uri.EscapeDataString(paymentId)}",
            price,
            IsStub: true,
            kind);
    }

    public async Task<bool> TryFulfillPaidCheckoutAsync(
        string paymentId,
        Guid? expectedUserId = null,
        bool allowDevStubMarkPaid = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(paymentId))
        {
            return false;
        }

        var checkout = await _db.DeepAnalysisCheckouts
            .FirstOrDefaultAsync(c => c.PaymentId == paymentId, cancellationToken);
        if (checkout is null)
        {
            return false;
        }

        if (expectedUserId is Guid uid && checkout.UserId != uid)
        {
            return false;
        }

        if (checkout.Status == DeepAnalysisCheckoutStatus.Paid)
        {
            await UnlockForUserAsync(checkout.UserId, checkout.Kind, cancellationToken);
            return true;
        }

        if (checkout.Status != DeepAnalysisCheckoutStatus.Pending)
        {
            return false;
        }

        var canStubMarkPaid = allowDevStubMarkPaid
            && AllowStubPayments()
            && paymentId.StartsWith("stub_deep_", StringComparison.Ordinal);

        if (!canStubMarkPaid)
        {
            return false;
        }

        checkout.Status = DeepAnalysisCheckoutStatus.Paid;
        checkout.PaidAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await UnlockForUserAsync(checkout.UserId, checkout.Kind, cancellationToken);
        return true;
    }

    public async Task UnlockForUserAsync(
        Guid userId,
        AssessmentKind kind,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.CandidateDeepAnalyses
            .FirstOrDefaultAsync(d => d.UserId == userId && d.Kind == kind, cancellationToken);
        var now = DateTime.UtcNow;
        if (row is null)
        {
            row = new CandidateDeepAnalysis
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Kind = kind,
                Status = CandidateDeepAnalysisStatuses.Draft,
                AnswersJson = "{}",
                TagsJson = "[]",
                UnlockedAtUtc = now,
                UpdatedAtUtc = now
            };
            _db.CandidateDeepAnalyses.Add(row);
        }
        else if (!CandidateDeepAnalysisStatuses.IsUnlocked(row.Status))
        {
            row.Status = CandidateDeepAnalysisStatuses.Draft;
            row.UnlockedAtUtc = now;
            row.UpdatedAtUtc = now;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<DeepAnalysisStateDto> SaveAsync(
        Guid userId,
        AssessmentKind kind,
        IReadOnlyDictionary<int, int> answers,
        bool complete,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.CandidateDeepAnalyses
            .FirstOrDefaultAsync(d => d.UserId == userId && d.Kind == kind, cancellationToken)
            ?? throw new InvalidOperationException("Diepte-analyse is nog niet ontgrendeld. Betaal eerst via de checkout.");

        if (!CandidateDeepAnalysisStatuses.IsUnlocked(row.Status))
        {
            throw new InvalidOperationException("Diepte-analyse is nog niet ontgrendeld. Betaal eerst via de checkout.");
        }

        if (answers.Count == 0)
        {
            var existingAnswers = DeepAnalysisCatalog.ParseAnswersJson(row.AnswersJson);
            if (existingAnswers.Count > 0)
            {
                throw new InvalidOperationException(
                    "Lege antwoorden overschrijven je bestaande diepte-analyse niet. Stuur de huidige antwoorden mee.");
            }

            var commercialEmpty = await _commercial.GetAsync(cancellationToken);
            return ToDto(kind, row, commercialEmpty.DeepAnalysisPriceEuro);
        }

        var error = DeepAnalysisCatalog.ValidateAnswers(answers, complete);
        if (error is not null)
        {
            throw new InvalidOperationException(error);
        }

        var now = DateTime.UtcNow;
        row.AnswersJson = DeepAnalysisCatalog.SerializeAnswers(answers);
        row.UpdatedAtUtc = now;

        if (complete)
        {
            var tags = DeepAnalysisCatalog.DeriveEnrichedTags(answers, kind);
            row.Status = CandidateDeepAnalysisStatuses.Completed;
            row.TagsJson = CompetencyTestCatalog.SerializeTags(tags);
            row.CompletedAtUtc = now;
            row.ReportGeneratedAtUtc = now;
            await MergeTagsIntoQuickScanAsync(userId, kind, answers, tags, now, cancellationToken);
        }
        else
        {
            row.Status = CandidateDeepAnalysisStatuses.Draft;
            row.CompletedAtUtc = null;
            row.ReportGeneratedAtUtc = null;
            row.TagsJson = "[]";
        }

        await _db.SaveChangesAsync(cancellationToken);
        var commercial = await _commercial.GetAsync(cancellationToken);
        return ToDto(kind, row, commercial.DeepAnalysisPriceEuro);
    }

    private async Task MergeTagsIntoQuickScanAsync(
        Guid userId,
        AssessmentKind kind,
        IReadOnlyDictionary<int, int> answers,
        IReadOnlyList<string> tags,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (kind == AssessmentKind.Career)
        {
            var career = await _db.CandidateCareerInterests
                .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
            var riasec = DeepAnalysisCatalog.ToRiasecScores(
                DeepAnalysisCatalog.ScoreDomains(answers, AssessmentKind.Career));
            var compassTags = CareerTestCatalog.DeriveRiasecTags(riasec);

            if (career is null)
            {
                career = new CandidateCareerInterest
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Status = CandidateCompetencyStatuses.Completed,
                    AnswersJson = "{}",
                    CreatedAtUtc = now
                };
                _db.CandidateCareerInterests.Add(career);
            }

            career.Status = CandidateCompetencyStatuses.Completed;
            career.RealisticPercent = riasec.Realistic;
            career.InvestigativePercent = riasec.Investigative;
            career.ArtisticPercent = riasec.Artistic;
            career.SocialPercent = riasec.Social;
            career.EnterprisingPercent = riasec.Enterprising;
            career.ConventionalPercent = riasec.Conventional;
            career.HollandCode = CareerTestCatalog.HollandCode(riasec);
            career.RiasecTagsJson = CareerTestCatalog.SerializeTags(compassTags);
            var existing = CareerTestCatalog.ParseTagsJson(career.MatchTagsJson).ToList();
            foreach (var tag in tags.Concat(compassTags))
            {
                if (!existing.Contains(tag, StringComparer.OrdinalIgnoreCase))
                {
                    existing.Add(tag);
                }
            }

            career.MatchTagsJson = CareerTestCatalog.SerializeTags(existing);
            career.CompletedAtUtc ??= now;
            career.UpdatedAtUtc = now;
            return;
        }

        var quick = await _db.CandidateCompetencies
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (quick is null || !CandidateCompetencyStatuses.IsCompleted(quick.Status))
        {
            return;
        }

        var competenceTags = CompetencyTestCatalog.ParseTagsJson(quick.MatchTagsJson).ToList();
        foreach (var tag in tags)
        {
            if (!competenceTags.Contains(tag, StringComparer.OrdinalIgnoreCase))
            {
                competenceTags.Add(tag);
            }
        }

        quick.MatchTagsJson = CompetencyTestCatalog.SerializeTags(competenceTags);
        quick.UpdatedAtUtc = now;
    }

    private bool AllowStubPayments() =>
        _environment.IsDevelopment()
        || _configuration.GetValue("JobsyAuth:AllowStubPayments", false);

    private static DeepAnalysisStateDto ToDto(AssessmentKind kind, CandidateDeepAnalysis? row, decimal priceEuro)
    {
        var status = row?.Status ?? CandidateDeepAnalysisStatuses.Locked;
        var answers = DeepAnalysisCatalog.ParseAnswersJson(row?.AnswersJson);
        var unlocked = CandidateDeepAnalysisStatuses.IsUnlocked(status);
        return new DeepAnalysisStateDto(
            kind,
            status,
            unlocked,
            CandidateDeepAnalysisStatuses.IsCompleted(status),
            answers.Count,
            DeepAnalysisCatalog.QuestionCount,
            priceEuro,
            CompetencyTestCatalog.ParseTagsJson(row?.TagsJson),
            row?.UnlockedAtUtc,
            row?.CompletedAtUtc,
            row?.ReportGeneratedAtUtc,
            answers,
            unlocked
                ? DeepAnalysisCatalog.QuestionsFor(kind)
                    .Select(q => new DeepAnalysisQuestionDto(q.Id, q.Family, q.Domain, q.Reverse, q.PromptNl))
                    .ToList()
                : [],
            FormatUpsellCopy(priceEuro, kind));
    }
}
