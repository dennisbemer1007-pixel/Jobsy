using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Models;
using Jobsy.Core.Rules;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

public sealed class AssessmentAdjustmentService : IAssessmentAdjustmentService
{
    private readonly JobsyDbContext _db;

    public AssessmentAdjustmentService(JobsyDbContext db)
    {
        _db = db;
    }

    public async Task<AssessmentAdjustmentDto> GetAsync(
        Guid userId,
        AssessmentKind kind,
        AssessmentVariant variant,
        CancellationToken cancellationToken = default)
    {
        var used = await CountUsedAsync(userId, kind, variant, cancellationToken);
        var hasDraft = await _db.CandidateAssessmentAttempts.AsNoTracking()
            .AnyAsync(
                a => a.UserId == userId
                     && a.Kind == kind
                     && a.Variant == variant
                     && a.Status == CandidateAssessmentAttempt.AttemptStatus.Open,
                cancellationToken);
        var last = await _db.CandidateAssessmentAdjustments.AsNoTracking()
            .Where(a => a.UserId == userId && a.Kind == kind && a.Variant == variant)
            .OrderByDescending(a => a.AtUtc)
            .Select(a => (DateTime?)a.AtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        return ToDto(used, hasDraft, last);
    }

    public async Task EnsureRemainingAsync(
        Guid userId,
        AssessmentKind kind,
        AssessmentVariant variant,
        CancellationToken cancellationToken = default)
    {
        var used = await CountUsedAsync(userId, kind, variant, cancellationToken);
        if (AssessmentAdjustmentRules.Remaining(used) <= 0)
        {
            throw new AssessmentAdjustmentLimitException(AssessmentAdjustmentRules.MaxAdjustments);
        }
    }

    public async Task<AssessmentAdjustmentDto> RecordAsync(
        Guid userId,
        AssessmentKind kind,
        AssessmentVariant variant,
        AssessmentAdjustmentType type,
        Guid? attemptId,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existing = await _db.CandidateAssessmentAdjustments.AsNoTracking()
                .FirstOrDefaultAsync(
                    a => a.UserId == userId
                         && a.Kind == kind
                         && a.Variant == variant
                         && a.IdempotencyKey == idempotencyKey,
                    cancellationToken);
            if (existing is not null)
            {
                return await GetAsync(userId, kind, variant, cancellationToken);
            }
        }

        if (attemptId is Guid aid)
        {
            var byAttempt = await _db.CandidateAssessmentAdjustments.AsNoTracking()
                .AnyAsync(a => a.AttemptId == aid, cancellationToken);
            if (byAttempt)
            {
                return await GetAsync(userId, kind, variant, cancellationToken);
            }
        }

        await EnsureRemainingAsync(userId, kind, variant, cancellationToken);

        _db.CandidateAssessmentAdjustments.Add(new CandidateAssessmentAdjustment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Kind = kind,
            Variant = variant,
            Type = type,
            AtUtc = DateTime.UtcNow,
            AttemptId = attemptId,
            IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim()
        });
        await _db.SaveChangesAsync(cancellationToken);
        return await GetAsync(userId, kind, variant, cancellationToken);
    }

    private Task<int> CountUsedAsync(
        Guid userId,
        AssessmentKind kind,
        AssessmentVariant variant,
        CancellationToken cancellationToken)
        => _db.CandidateAssessmentAdjustments.AsNoTracking()
            .CountAsync(a => a.UserId == userId && a.Kind == kind && a.Variant == variant, cancellationToken);

    private static AssessmentAdjustmentDto ToDto(int used, bool hasDraft = false, DateTime? last = null)
        => new(used, AssessmentAdjustmentRules.Remaining(used), AssessmentAdjustmentRules.MaxAdjustments, hasDraft, last);
}
