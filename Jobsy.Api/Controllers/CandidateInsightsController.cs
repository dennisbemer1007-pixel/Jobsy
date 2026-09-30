using Jobsy.Core.Authorization;
using Jobsy.Core.Contracts;
using Jobsy.Core.Exceptions;
using Jobsy.Core.Interfaces;
using Jobsy.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Jobsy.Api.Controllers;

[ApiController]
[Route("api/employer/candidate-insights")]
[Authorize(Policy = JobsyPolicies.RequireEmployer)]
public sealed class CandidateInsightsController : ControllerBase
{
    private static readonly HashSet<string> ForbiddenIdentityParams = new(StringComparer.OrdinalIgnoreCase)
    {
        "minAge", "maxAge", "age", "dateOfBirth", "userId", "candidateId", "candidateUserId"
    };

    private readonly ICandidateInsightsService _insights;

    public CandidateInsightsController(ICandidateInsightsService insights)
    {
        _insights = insights;
    }

    [HttpGet]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<CandidateInsightsDto>> Get(
        [FromQuery] Guid? branchId,
        [FromQuery] int radiusKm = 20,
        [FromQuery] int period = 90,
        CancellationToken cancellationToken = default)
    {
        if (HasForbiddenIdentityQuery())
        {
            return BadRequest(new { message = "Identiteits- of leeftijdsfilters zijn niet toegestaan." });
        }

        if (!CandidateInsightsService.AllowedRadiiKm.Contains(radiusKm)
            || !CandidateInsightsService.AllowedPeriodsDays.Contains(period))
        {
            return BadRequest(new { message = "Ongeldige radius of periode." });
        }

        if (!IsInsightsRole())
        {
            return Forbid();
        }

        try
        {
            var dto = await _insights.GetInsightsAsync(User, branchId, radiusKm, period, cancellationToken);
            return Ok(dto);
        }
        catch (CandidateInsightsException ex) when (ex.Code == "feature_disabled")
        {
            return NotFound(new { code = "feature_disabled", message = ex.Message });
        }
        catch (ForbiddenCompanyAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("branches")]
    [EnableRateLimiting("public-read")]
    public async Task<ActionResult<IReadOnlyList<CandidateInsightsBranchDto>>> Branches(
        CancellationToken cancellationToken = default)
    {
        if (!IsInsightsRole())
        {
            return Forbid();
        }

        try
        {
            return Ok(await _insights.GetBranchesAsync(User, cancellationToken));
        }
        catch (CandidateInsightsException ex) when (ex.Code == "feature_disabled")
        {
            return NotFound(new { code = "feature_disabled", message = ex.Message });
        }
        catch (ForbiddenCompanyAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("unlock")]
    [Authorize(Roles = JobsyRoles.EmployerMutateRoles)]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<CandidateInsightsUnlockResultDto>> Unlock(
        [FromBody] UnlockInsightsRequest body,
        CancellationToken cancellationToken = default)
    {
        if (!IsInsightsRole())
        {
            return Forbid();
        }

        if (!Request.Headers.TryGetValue("Idempotency-Key", out var keyValues)
            || string.IsNullOrWhiteSpace(keyValues.FirstOrDefault()))
        {
            return BadRequest(new { code = "missing_idempotency_key", message = "Idempotency-Key header is verplicht." });
        }

        try
        {
            var result = await _insights.UnlockAsync(
                User,
                body.Scope ?? "company",
                body.BranchId,
                keyValues.ToString()!,
                body.UnlockRequestId,
                cancellationToken);
            return Ok(result);
        }
        catch (CandidateInsightsException ex)
        {
            return StatusCode(ex.StatusCode, new { code = ex.Code, message = ex.Message });
        }
        catch (ForbiddenCompanyAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("unlock-request")]
    [Authorize(Roles = JobsyRoles.BranchManager)]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<CandidateInsightsUnlockRequestDto>> UnlockRequest(
        [FromBody] UnlockInsightsRequestBody body,
        CancellationToken cancellationToken = default)
    {
        if (!IsInsightsRole())
        {
            return Forbid();
        }

        try
        {
            var result = await _insights.CreateUnlockRequestAsync(User, body.BranchId, cancellationToken);
            return Ok(result);
        }
        catch (CandidateInsightsException ex)
        {
            return StatusCode(ex.StatusCode, new { code = ex.Code, message = ex.Message });
        }
        catch (ForbiddenCompanyAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("unlock-request/{id:guid}/reject")]
    [Authorize(Roles = JobsyRoles.EnterpriseManager)]
    [EnableRateLimiting("public-write")]
    public async Task<ActionResult<CandidateInsightsUnlockRequestDto>> RejectUnlockRequest(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (!IsInsightsRole())
        {
            return Forbid();
        }

        try
        {
            return Ok(await _insights.RejectUnlockRequestAsync(User, id, cancellationToken));
        }
        catch (CandidateInsightsException ex)
        {
            return StatusCode(ex.StatusCode, new { code = ex.Code, message = ex.Message });
        }
        catch (ForbiddenCompanyAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("export.csv")]
    [EnableRateLimiting("public-read")]
    public async Task<IActionResult> ExportCsv(
        [FromQuery] Guid? branchId,
        [FromQuery] int radiusKm = 20,
        [FromQuery] int period = 90,
        CancellationToken cancellationToken = default)
    {
        if (!IsInsightsRole())
        {
            return Forbid();
        }

        try
        {
            var (fileName, csv) = await _insights.ExportCsvAsync(User, branchId, radiusKm, period, cancellationToken);
            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
        }
        catch (CandidateInsightsException ex)
        {
            return StatusCode(ex.StatusCode, new { code = ex.Code, message = ex.Message });
        }
        catch (ForbiddenCompanyAccessException)
        {
            return Forbid();
        }
    }

    private bool IsInsightsRole()
        => User.IsInRole(JobsyRoles.BranchManager)
           || User.IsInRole(JobsyRoles.RegionalManager)
           || User.IsInRole(JobsyRoles.EnterpriseManager)
           || RoleClaimMatching.HasRole(User, JobsyRoles.BranchManager)
           || RoleClaimMatching.HasRole(User, JobsyRoles.RegionalManager)
           || RoleClaimMatching.HasRole(User, JobsyRoles.EnterpriseManager);

    private bool HasForbiddenIdentityQuery()
        => Request.Query.Keys.Any(k => ForbiddenIdentityParams.Contains(k));
}

public sealed record UnlockInsightsRequest(string? Scope, Guid? BranchId, Guid? UnlockRequestId = null);

public sealed record UnlockInsightsRequestBody(Guid BranchId);
