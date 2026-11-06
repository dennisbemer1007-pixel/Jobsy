using System.Text.Json;
using Jobsy.Core.Entities;
using Jobsy.Core.Enums;
using Jobsy.Core.Interfaces;
using Jobsy.Core.Models;
using Jobsy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Infrastructure.Services;

/// <summary>
/// Enforces the 3-change limit after the first completion: drafts on Open attempts,
/// Edit/Retake recorded only when completing a changed result.
/// </summary>
public sealed class AssessmentSaveGuard
{
    private readonly JobsyDbContext _db;
    private readonly IAssessmentAdjustmentService _adjustments;

    public AssessmentSaveGuard(JobsyDbContext db, IAssessmentAdjustmentService adjustments)
    {
        _db = db;
        _adjustments = adjustments;
    }

    public sealed record State(
        bool IsCompleted,
        CandidateAssessmentAttempt? OpenAttempt,
        AssessmentAdjustmentDto Adjustments);

    public async Task<State> BeginAsync(
        Guid userId,
        AssessmentKind kind,
        AssessmentVariant variant,
        bool isCompleted,
        CancellationToken cancellationToken = default)
    {
        CandidateAssessmentAttempt? open = null;
        if (isCompleted)
        {
            open = await _db.CandidateAssessmentAttempts
                .FirstOrDefaultAsync(
                    a => a.UserId == userId
                         && a.Kind == kind
                         && a.Variant == variant
                         && a.Status == CandidateAssessmentAttempt.AttemptStatus.Open,
                    cancellationToken);
        }

        var adj = await _adjustments.GetAsync(userId, kind, variant, cancellationToken);
        return new State(isCompleted, open, adj);
    }

    public async Task SaveDraftAsync(
        Guid userId,
        AssessmentKind kind,
        AssessmentVariant variant,
        string answersJson,
        string? previousSnapshotJson,
        CancellationToken cancellationToken = default)
    {
        var open = await _db.CandidateAssessmentAttempts
            .FirstOrDefaultAsync(
                a => a.UserId == userId
                     && a.Kind == kind
                     && a.Variant == variant
                     && a.Status == CandidateAssessmentAttempt.AttemptStatus.Open,
                cancellationToken);
        if (open is null)
        {
            open = new CandidateAssessmentAttempt
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Kind = kind,
                Variant = variant,
                AnswersJson = answersJson,
                Status = CandidateAssessmentAttempt.AttemptStatus.Open,
                StartedAtUtc = DateTime.UtcNow,
                PreviousSnapshotJson = previousSnapshotJson,
                Origin = CandidateAssessmentAttempt.AttemptOrigin.Edit
            };
            _db.CandidateAssessmentAttempts.Add(open);
        }
        else
        {
            open.AnswersJson = answersJson;
            if (string.IsNullOrWhiteSpace(open.PreviousSnapshotJson) && previousSnapshotJson is not null)
            {
                open.PreviousSnapshotJson = previousSnapshotJson;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Completing a changed result after the first completion. Returns true when this was a no-op
    /// (identical answers). Throws <see cref="AssessmentAdjustmentLimitException"/> when limit hit.
    /// </summary>
    public async Task<bool> CommitCompleteAsync(
        Guid userId,
        AssessmentKind kind,
        AssessmentVariant variant,
        string newAnswersJson,
        string? newScoresJson,
        string? previousSnapshotJson,
        string completedAnswersJson,
        CancellationToken cancellationToken = default)
    {
        if (AnswersEqual(newAnswersJson, completedAnswersJson))
        {
            return true;
        }

        var open = await _db.CandidateAssessmentAttempts
            .FirstOrDefaultAsync(
                a => a.UserId == userId
                     && a.Kind == kind
                     && a.Variant == variant
                     && a.Status == CandidateAssessmentAttempt.AttemptStatus.Open,
                cancellationToken);

        if (open is null)
        {
            open = new CandidateAssessmentAttempt
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Kind = kind,
                Variant = variant,
                Status = CandidateAssessmentAttempt.AttemptStatus.Open,
                StartedAtUtc = DateTime.UtcNow,
                Origin = CandidateAssessmentAttempt.AttemptOrigin.Edit,
                PreviousSnapshotJson = previousSnapshotJson
            };
            _db.CandidateAssessmentAttempts.Add(open);
        }

        var type = string.Equals(open.Origin, CandidateAssessmentAttempt.AttemptOrigin.Retake, StringComparison.OrdinalIgnoreCase)
            ? AssessmentAdjustmentType.Retake
            : AssessmentAdjustmentType.Edit;

        var idempotencyKey = $"{open.Id:D}:complete";
        await _adjustments.EnsureRemainingAsync(userId, kind, variant, cancellationToken);
        await _adjustments.RecordAsync(userId, kind, variant, type, open.Id, idempotencyKey, cancellationToken);

        open.AnswersJson = newAnswersJson;
        open.ScoresJson = newScoresJson;
        open.PreviousSnapshotJson ??= previousSnapshotJson;
        open.Status = CandidateAssessmentAttempt.AttemptStatus.Completed;
        open.CompletedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return false;
    }

    public static bool AnswersEqual(string? a, string? b)
    {
        if (string.IsNullOrWhiteSpace(a) && string.IsNullOrWhiteSpace(b)) return true;
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return false;
        try
        {
            using var da = JsonDocument.Parse(a);
            using var dbDoc = JsonDocument.Parse(b);
            return JsonElementDeepEquals(da.RootElement, dbDoc.RootElement);
        }
        catch
        {
            return string.Equals(a.Trim(), b.Trim(), StringComparison.Ordinal);
        }
    }

    private static bool JsonElementDeepEquals(JsonElement a, JsonElement b)
    {
        if (a.ValueKind != b.ValueKind) return false;
        switch (a.ValueKind)
        {
            case JsonValueKind.Object:
                var aProps = a.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal).ToList();
                var bProps = b.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal).ToList();
                if (aProps.Count != bProps.Count) return false;
                for (var i = 0; i < aProps.Count; i++)
                {
                    if (!string.Equals(aProps[i].Name, bProps[i].Name, StringComparison.Ordinal)) return false;
                    if (!JsonElementDeepEquals(aProps[i].Value, bProps[i].Value)) return false;
                }

                return true;
            case JsonValueKind.Array:
                var aArr = a.EnumerateArray().ToList();
                var bArr = b.EnumerateArray().ToList();
                if (aArr.Count != bArr.Count) return false;
                for (var i = 0; i < aArr.Count; i++)
                {
                    if (!JsonElementDeepEquals(aArr[i], bArr[i])) return false;
                }

                return true;
            case JsonValueKind.String:
                return a.GetString() == b.GetString();
            case JsonValueKind.Number:
                return a.GetRawText() == b.GetRawText();
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
                return true;
            default:
                return a.GetRawText() == b.GetRawText();
        }
    }
}
