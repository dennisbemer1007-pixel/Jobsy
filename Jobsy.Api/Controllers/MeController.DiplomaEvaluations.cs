using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Entities;
using Jobsy.Core.Privacy;
using Jobsy.Core.Rules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Jobsy.Api.Controllers;

public partial class MeController
{
    [HttpGet("diploma-evaluations")]
    [Authorize(Policy = JobsyPolicies.RequireCandidate)]
    public async Task<ActionResult<IReadOnlyList<DiplomaEvaluationFactDto>>> ListDiplomaEvaluations(
        CancellationToken cancellationToken)
    {
        var user = await ResolveActiveCandidateAsync(cancellationToken);
        if (user is null)
        {
            return NotFound(new { code = "not_found", message = "Gebruiker niet gevonden in Jobsy." });
        }

        return Ok(await LoadDiplomaEvaluationFactsAsync(user.Id, cancellationToken));
    }

    [HttpPost("diploma-evaluations")]
    [Authorize(Policy = JobsyPolicies.RequireCandidate)]
    public async Task<ActionResult<DiplomaEvaluationFactDto>> CreateDiplomaEvaluation(
        [FromBody] UpsertDiplomaEvaluationRequest request,
        CancellationToken cancellationToken)
    {
        var user = await ResolveActiveCandidateAsync(cancellationToken);
        if (user is null)
        {
            return NotFound(new { code = "not_found", message = "Gebruiker niet gevonden in Jobsy." });
        }

        if (!CandidateConsentRules.CanUseCandidateFeatures(user))
        {
            return BadRequest(new { code = "parental_consent_required", message = CandidateConsentRules.ParentalConsentRequiredMessage });
        }

        var count = await _db.CandidateDiplomaEvaluations.CountAsync(e => e.UserId == user.Id, cancellationToken);
        if (count >= DiplomaEvaluationRules.MaxPerCandidate)
        {
            return DiplomaError("too_many");
        }

        if (!TryNormalizeRequest(request, out var normalized, out var error))
        {
            return DiplomaError(error);
        }

        var now = DateTime.UtcNow;
        var row = new CandidateDiplomaEvaluation
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        ApplyNormalized(row, normalized!);
        _db.CandidateDiplomaEvaluations.Add(row);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(DiplomaEvaluationRules.ToFactDto(row));
    }

    [HttpPut("diploma-evaluations/{id:guid}")]
    [Authorize(Policy = JobsyPolicies.RequireCandidate)]
    public async Task<ActionResult<DiplomaEvaluationFactDto>> UpdateDiplomaEvaluation(
        Guid id,
        [FromBody] UpsertDiplomaEvaluationRequest request,
        CancellationToken cancellationToken)
    {
        var user = await ResolveActiveCandidateAsync(cancellationToken);
        if (user is null)
        {
            return NotFound(new { code = "not_found", message = "Gebruiker niet gevonden in Jobsy." });
        }

        if (!CandidateConsentRules.CanUseCandidateFeatures(user))
        {
            return BadRequest(new { code = "parental_consent_required", message = CandidateConsentRules.ParentalConsentRequiredMessage });
        }

        var row = await _db.CandidateDiplomaEvaluations
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == user.Id, cancellationToken);
        if (row is null)
        {
            return DiplomaError("not_found");
        }

        if (!TryNormalizeRequest(request, out var normalized, out var error))
        {
            return DiplomaError(error);
        }

        ApplyNormalized(row, normalized!);
        row.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(DiplomaEvaluationRules.ToFactDto(row));
    }

    [HttpDelete("diploma-evaluations/{id:guid}")]
    [Authorize(Policy = JobsyPolicies.RequireCandidate)]
    public async Task<IActionResult> DeleteDiplomaEvaluation(Guid id, CancellationToken cancellationToken)
    {
        var user = await ResolveActiveCandidateAsync(cancellationToken);
        if (user is null)
        {
            return NotFound(new { code = "not_found", message = "Gebruiker niet gevonden in Jobsy." });
        }

        var row = await _db.CandidateDiplomaEvaluations
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == user.Id, cancellationToken);
        if (row is null)
        {
            return DiplomaError("not_found");
        }

        _db.CandidateDiplomaEvaluations.Remove(row);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("diploma-evaluations/{id:guid}/document")]
    [Authorize(Policy = JobsyPolicies.RequireCandidate)]
    [EnableRateLimiting("ai")]
    [RequestSizeLimit(DiplomaEvaluationRules.MaxDocumentBytes + 64_000)]
    public async Task<ActionResult<DiplomaEvaluationFactDto>> UploadDiplomaEvaluationDocument(
        Guid id,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        var user = await ResolveActiveCandidateAsync(cancellationToken);
        if (user is null)
        {
            return NotFound(new { code = "not_found", message = "Gebruiker niet gevonden in Jobsy." });
        }

        if (!CandidateConsentRules.CanUseCandidateFeatures(user))
        {
            return BadRequest(new { code = "parental_consent_required", message = CandidateConsentRules.ParentalConsentRequiredMessage });
        }

        var row = await _db.CandidateDiplomaEvaluations
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == user.Id, cancellationToken);
        if (row is null)
        {
            return DiplomaError("not_found");
        }

        if (file is null || file.Length == 0)
        {
            return DiplomaError("file_empty");
        }

        if (file.Length > DiplomaEvaluationRules.MaxDocumentBytes)
        {
            return DiplomaError("file_too_large");
        }

        await using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();
        if (!DiplomaEvaluationRules.TryNormalizeDocument(
                file.FileName,
                file.ContentType,
                bytes,
                out var safeName,
                out var contentType,
                out var fileError))
        {
            return DiplomaError(fileError);
        }

        row.DocumentFileName = safeName;
        row.DocumentContentType = contentType;
        row.DocumentContent = bytes;
        row.DocumentSizeBytes = bytes.Length;
        row.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(DiplomaEvaluationRules.ToFactDto(row));
    }

    [HttpDelete("diploma-evaluations/{id:guid}/document")]
    [Authorize(Policy = JobsyPolicies.RequireCandidate)]
    public async Task<ActionResult<DiplomaEvaluationFactDto>> DeleteDiplomaEvaluationDocument(
        Guid id,
        CancellationToken cancellationToken)
    {
        var user = await ResolveActiveCandidateAsync(cancellationToken);
        if (user is null)
        {
            return NotFound(new { code = "not_found", message = "Gebruiker niet gevonden in Jobsy." });
        }

        var row = await _db.CandidateDiplomaEvaluations
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == user.Id, cancellationToken);
        if (row is null)
        {
            return DiplomaError("not_found");
        }

        row.DocumentFileName = null;
        row.DocumentContentType = null;
        row.DocumentContent = null;
        row.DocumentSizeBytes = null;
        row.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(DiplomaEvaluationRules.ToFactDto(row));
    }

    [HttpGet("diploma-evaluations/{id:guid}/document")]
    [Authorize(Policy = JobsyPolicies.RequireCandidate)]
    [EnableRateLimiting("public-pdf")]
    public async Task<IActionResult> DownloadDiplomaEvaluationDocument(Guid id, CancellationToken cancellationToken)
    {
        var user = await ResolveActiveCandidateAsync(cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        var row = await _db.CandidateDiplomaEvaluations.AsNoTracking()
            .Where(e => e.Id == id && e.UserId == user.Id && e.DocumentContent != null)
            .Select(e => new { e.DocumentContent, e.DocumentContentType, e.DocumentFileName })
            .FirstOrDefaultAsync(cancellationToken);
        if (row?.DocumentContent is not { Length: > 0 })
        {
            return NotFound();
        }

        return File(row.DocumentContent, row.DocumentContentType ?? "application/octet-stream", row.DocumentFileName ?? "waardering.pdf");
    }

    private async Task<List<DiplomaEvaluationFactDto>> LoadDiplomaEvaluationFactsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var rows = await _db.CandidateDiplomaEvaluations.AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderBy(e => e.CreatedAtUtc)
            .Select(e => new DiplomaEvaluationFactDto(
                e.Id,
                e.DiplomaTitle,
                e.IssuingBody,
                e.IssuingBodyOther,
                e.EquivalentLevelText,
                e.EquivalentLevelCode,
                e.EvaluationDate,
                e.ReferenceNumber,
                e.DocumentSizeBytes != null && e.DocumentSizeBytes > 0,
                e.DocumentSizeBytes != null && e.DocumentSizeBytes > 0 ? e.DocumentFileName : null))
            .ToListAsync(cancellationToken);
        return rows;
    }

    private async Task<IReadOnlyList<DiplomaEvaluationSharedFact>> LoadDiplomaEvaluationSharedFactsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _db.CandidateDiplomaEvaluations.AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderBy(e => e.CreatedAtUtc)
            .Select(e => new DiplomaEvaluationSharedFact(
                e.DiplomaTitle,
                e.IssuingBody,
                e.IssuingBodyOther,
                e.EquivalentLevelText,
                e.EquivalentLevelCode,
                e.EvaluationDate,
                e.ReferenceNumber))
            .ToListAsync(cancellationToken);
    }

    private static bool TryNormalizeRequest(
        UpsertDiplomaEvaluationRequest? request,
        out DiplomaEvaluationRules.Normalized? normalized,
        out string? errorCode)
    {
        if (request is null)
        {
            normalized = null;
            errorCode = "level_required";
            return false;
        }

        return DiplomaEvaluationRules.TryNormalize(
            request.DiplomaTitle,
            request.IssuingBody,
            request.IssuingBodyOther,
            request.EquivalentLevelText,
            request.EquivalentLevelCode,
            request.EvaluationDate,
            request.ReferenceNumber,
            DateOnly.FromDateTime(DateTime.UtcNow),
            out normalized,
            out errorCode);
    }

    private static void ApplyNormalized(CandidateDiplomaEvaluation row, DiplomaEvaluationRules.Normalized normalized)
    {
        row.DiplomaTitle = normalized.DiplomaTitle;
        row.IssuingBody = normalized.IssuingBody;
        row.IssuingBodyOther = normalized.IssuingBodyOther;
        row.EquivalentLevelText = normalized.EquivalentLevelText;
        row.EquivalentLevelCode = normalized.EquivalentLevelCode;
        row.EvaluationDate = normalized.EvaluationDate;
        row.ReferenceNumber = normalized.ReferenceNumber;
    }

    private ActionResult DiplomaError(string? code)
        => code == "not_found"
            ? NotFound(new { code, message = DiplomaEvaluationRules.DutchMessage(code) })
            : BadRequest(new { code, message = DiplomaEvaluationRules.DutchMessage(code) });
}
