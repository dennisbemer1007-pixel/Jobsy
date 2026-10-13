using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class AssessmentRetakeService : IAssessmentRetakeService
{
    private readonly JobsyDbContext _db;
    private readonly IAssessmentAdjustmentService _adjustments;

    public AssessmentRetakeService(JobsyDbContext db, IAssessmentAdjustmentService adjustments)
    {
        _db = db;
        _adjustments = adjustments;
    }

    public async Task<CandidateAssessmentAttempt> StartAsync(
        Guid userId,
        AssessmentKind kind,
        AssessmentVariant variant,
        CancellationToken cancellationToken = default)
    {
        await _adjustments.EnsureRemainingAsync(userId, kind, variant, cancellationToken);

        var open = await _db.CandidateAssessmentAttempts
            .FirstOrDefaultAsync(
                a => a.UserId == userId
                     && a.Kind == kind
                     && a.Variant == variant
                     && a.Status == CandidateAssessmentAttempt.AttemptStatus.Open,
                cancellationToken);
        if (open is not null)
        {
            return open;
        }

        var attempt = new CandidateAssessmentAttempt
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Kind = kind,
            Variant = variant,
            AnswersJson = "{}",
            Status = CandidateAssessmentAttempt.AttemptStatus.Open,
            StartedAtUtc = DateTime.UtcNow,
            Origin = CandidateAssessmentAttempt.AttemptOrigin.Retake
        };
        _db.CandidateAssessmentAttempts.Add(attempt);
        await _db.SaveChangesAsync(cancellationToken);
        return attempt;
    }

    public async Task AbandonAsync(Guid userId, Guid attemptId, CancellationToken cancellationToken = default)
    {
        var attempt = await _db.CandidateAssessmentAttempts
            .FirstOrDefaultAsync(a => a.Id == attemptId && a.UserId == userId, cancellationToken);
        if (attempt is null || attempt.Status != CandidateAssessmentAttempt.AttemptStatus.Open)
        {
            return;
        }

        attempt.Status = CandidateAssessmentAttempt.AttemptStatus.Abandoned;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CandidateAssessmentAttempt>> ListHistoryAsync(
        Guid userId,
        AssessmentKind kind,
        AssessmentVariant variant,
        CancellationToken cancellationToken = default)
    {
        return await _db.CandidateAssessmentAttempts.AsNoTracking()
            .Where(a => a.UserId == userId
                        && a.Kind == kind
                        && a.Variant == variant
                        && a.Status == CandidateAssessmentAttempt.AttemptStatus.Completed)
            .OrderByDescending(a => a.CompletedAtUtc)
            .ToListAsync(cancellationToken);
    }
}
