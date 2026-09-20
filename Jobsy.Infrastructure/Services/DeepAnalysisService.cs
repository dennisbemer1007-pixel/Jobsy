using System.Globalization;
using Jobsy.Core.Entities;
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

    public static string FormatUpsellCopy(decimal priceEuro)
    {
        var price = priceEuro.ToString("0.00", CultureInfo.GetCultureInfo("nl-NL"));
        return $"Wil je een diepgaand inzicht in jouw unieke werkstijl en een officiële PDF-rapportage voor je sollicitaties? Ontgrendel de uitgebreide diepte-analyse voor € {price}.";
    }

    public async Task<DeepAnalysisStateDto> GetStateAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.CandidateDeepAnalyses.AsNoTracking()
            .FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);
        var commercial = await _commercial.GetAsync(cancellationToken);
        return ToDto(row, commercial.DeepAnalysisPriceEuro);
    }

    public async Task<DeepAnalysisCheckoutResult> StartCheckoutAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        _ = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Gebruiker niet gevonden.");

        var existing = await _db.CandidateDeepAnalyses
            .FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);
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
            .Where(c => c.UserId == userId && c.Status == DeepAnalysisCheckoutStatus.Pending)
            .ToListAsync(cancellationToken);
        foreach (var prior in open)
        {
            prior.Status = DeepAnalysisCheckoutStatus.Cancelled;
        }

        var paymentId = $"stub_deep_{Guid.NewGuid():N}";
        var checkout = new DeepAnalysisCheckout
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PaymentId = paymentId,
            AmountEuro = price,
            Status = DeepAnalysisCheckoutStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.DeepAnalysisCheckouts.Add(checkout);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Deep analysis checkout for user {UserId}: €{Amount} ({PaymentId})",
            userId, price, paymentId);

        return new DeepAnalysisCheckoutResult(
            checkout.Id,
            paymentId,
            $"/candidate/deep-analysis/checkout?paymentId={Uri.EscapeDataString(paymentId)}",
            price,
            IsStub: true);
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
            await UnlockForUserAsync(checkout.UserId, cancellationToken);
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
        await UnlockForUserAsync(checkout.UserId, cancellationToken);
        return true;
    }

    public async Task UnlockForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var row = await _db.CandidateDeepAnalyses
            .FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);
        var now = DateTime.UtcNow;
        if (row is null)
        {
            row = new CandidateDeepAnalysis
            {
                Id = Guid.NewGuid(),
                UserId = userId,
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
        IReadOnlyDictionary<int, int> answers,
        bool complete,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.CandidateDeepAnalyses
            .FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken)
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
            return ToDto(row, commercialEmpty.DeepAnalysisPriceEuro);
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
            var tags = DeepAnalysisCatalog.DeriveEnrichedTags(answers);
            row.Status = CandidateDeepAnalysisStatuses.Completed;
            row.TagsJson = CompetencyTestCatalog.SerializeTags(tags);
            row.CompletedAtUtc = now;
            row.ReportGeneratedAtUtc = now;

            var quick = await _db.CandidateCompetencies
                .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
            if (quick is not null && CandidateCompetencyStatuses.IsCompleted(quick.Status))
            {
                var existing = CompetencyTestCatalog.ParseTagsJson(quick.MatchTagsJson).ToList();
                foreach (var tag in tags)
                {
                    if (!existing.Contains(tag, StringComparer.OrdinalIgnoreCase))
                    {
                        existing.Add(tag);
                    }
                }

                quick.MatchTagsJson = CompetencyTestCatalog.SerializeTags(existing);
                quick.UpdatedAtUtc = now;
            }
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
        return ToDto(row, commercial.DeepAnalysisPriceEuro);
    }

    private bool AllowStubPayments() =>
        _environment.IsDevelopment()
        || _configuration.GetValue("JobsyAuth:AllowStubPayments", false);

    private static DeepAnalysisStateDto ToDto(CandidateDeepAnalysis? row, decimal priceEuro)
    {
        var status = row?.Status ?? CandidateDeepAnalysisStatuses.Locked;
        var answers = DeepAnalysisCatalog.ParseAnswersJson(row?.AnswersJson);
        var unlocked = CandidateDeepAnalysisStatuses.IsUnlocked(status);
        return new DeepAnalysisStateDto(
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
            unlocked
                ? DeepAnalysisCatalog.Questions
                    .Select(q => new DeepAnalysisQuestionDto(q.Id, q.Family, q.Domain, q.Reverse, q.PromptNl))
                    .ToList()
                : [],
            FormatUpsellCopy(priceEuro));
    }
}
