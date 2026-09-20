using Jobsy.Core.Entities;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jobsy.Infrastructure.Services;

public sealed class DeepAnalysisService : IDeepAnalysisService
{
    public const string UpsellCopyNl =
        "Wil je een diepgaand inzicht in jouw unieke werkstijl en een officiële PDF-rapportage voor je sollicitaties? Ontgrendel de uitgebreide diepte-analyse voor € 2,99.";

    private readonly JobsyDbContext _db;
    private readonly ILogger<DeepAnalysisService> _logger;

    public DeepAnalysisService(JobsyDbContext db, ILogger<DeepAnalysisService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<DeepAnalysisStateDto> GetStateAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.CandidateDeepAnalyses.AsNoTracking()
            .FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);
        return ToDto(row);
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
            AmountEuro = DeepAnalysisCheckout.PriceEuro,
            Status = DeepAnalysisCheckoutStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.DeepAnalysisCheckouts.Add(checkout);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Deep analysis checkout for user {UserId}: €{Amount} ({PaymentId})",
            userId, DeepAnalysisCheckout.PriceEuro, paymentId);

        return new DeepAnalysisCheckoutResult(
            checkout.Id,
            paymentId,
            $"/candidate/deep-analysis/checkout?paymentId={Uri.EscapeDataString(paymentId)}",
            DeepAnalysisCheckout.PriceEuro,
            IsStub: true);
    }

    public async Task<bool> TryFulfillPaidCheckoutAsync(
        string paymentId,
        CancellationToken cancellationToken = default)
    {
        var checkout = await _db.DeepAnalysisCheckouts
            .FirstOrDefaultAsync(c => c.PaymentId == paymentId, cancellationToken);
        if (checkout is null)
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
            ?? throw new InvalidOperationException("Diepte-analyse is nog niet ontgrendeld. Betaal eerst € 2,99.");

        if (!CandidateDeepAnalysisStatuses.IsUnlocked(row.Status))
        {
            throw new InvalidOperationException("Diepte-analyse is nog niet ontgrendeld. Betaal eerst € 2,99.");
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

            // Merge enriched tags into Quick-Scan match index when present.
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
        return ToDto(row);
    }

    private static DeepAnalysisStateDto ToDto(CandidateDeepAnalysis? row)
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
            DeepAnalysisCheckout.PriceEuro,
            CompetencyTestCatalog.ParseTagsJson(row?.TagsJson),
            row?.UnlockedAtUtc,
            row?.CompletedAtUtc,
            row?.ReportGeneratedAtUtc,
            unlocked
                ? DeepAnalysisCatalog.Questions
                    .Select(q => new DeepAnalysisQuestionDto(q.Id, q.Family, q.Domain, q.Reverse, q.PromptNl))
                    .ToList()
                : [],
            UpsellCopyNl);
    }
}
